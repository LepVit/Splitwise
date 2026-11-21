using Splitwise.Core.Common;
using Splitwise.Core.Dtos;
using Splitwise.Core.Entities;
using Splitwise.Core.Exceptions;
using Splitwise.Core.Interfaces;
using Splitwise.Core.Services.Internal;

namespace Splitwise.Core.Services;

public class ExpenseService(IUnitOfWork uow) : IExpenseService
{
    public async Task<ExpenseDto> CreateAsync(CreateExpenseDto dto, int currentUserId, CancellationToken ct = default)
    {
        var splits = dto.Splits;

        // Інваріанти витрати
        if (splits.Select(s => s.UserId).Distinct().Count() != splits.Count)
            throw new InvalidExpenseSplitException("A user can appear in the splits only once.");

        if (splits.Sum(s => s.PaidShare) != dto.TotalCost)
            throw new InvalidExpenseSplitException("The sum of all PaidShare values must be equal to TotalCost.");

        if (splits.Sum(s => s.OwedShare) != dto.TotalCost)
            throw new InvalidExpenseSplitException("The sum of all OwedShare values must be equal to TotalCost.");

        await EnsureParticipantsAsync(dto.GroupId, splits.Select(s => s.UserId).ToList(), currentUserId, ct);

        var now = DateTime.UtcNow;
        var expense = new Expense
        {
            Description = dto.Description.Trim(),
            TotalCost = dto.TotalCost,
            Date = NormalizeDate(dto.Date),
            GroupId = dto.GroupId,
            CreatedByUserId = currentUserId,   // автор завжди береться з токена
            IsPayment = false,
            CreatedAt = now,
            Splits = splits
                .Select(s => new ExpenseSplit { UserId = s.UserId, PaidShare = s.PaidShare, OwedShare = s.OwedShare })
                .ToList()
        };

        await uow.Expenses.AddAsync(expense, ct);
        await uow.SaveChangesAsync(ct);

        return ToDto(expense);
    }

    public async Task<ExpenseDto> CreateEqualAsync(CreateEqualExpenseDto dto, int currentUserId, CancellationToken ct = default)
    {
        var participants = dto.ParticipantUserIds.Distinct().ToList();
        if (participants.Count == 0)
            throw new InvalidExpenseSplitException("At least one participant is required.");

        var owed = SplitCalculator.SplitEvenly(dto.TotalCost, participants.Count);

        var splits = participants
            .Select((userId, i) => new SplitDto(userId, userId == dto.PaidByUserId ? dto.TotalCost : 0m, owed[i]))
            .ToList();

        // Платник міг сам не їсти: тоді він має лише PaidShare
        if (!participants.Contains(dto.PaidByUserId))
            splits.Add(new SplitDto(dto.PaidByUserId, dto.TotalCost, 0m));

        return await CreateAsync(
            new CreateExpenseDto(dto.Description, dto.TotalCost, dto.Date, dto.GroupId, splits),
            currentUserId, ct);
    }

    public async Task<ExpenseDto> SettleUpAsync(CreateSettlementDto dto, int currentUserId, CancellationToken ct = default)
    {
        if (dto.ToUserId == currentUserId)
            throw new InvalidExpenseSplitException("You cannot settle up with yourself.");

        if (dto.GroupId is int groupId)
        {
            var group = await GroupAccess.LoadForMemberAsync(uow, groupId, currentUserId, ct);
            if (group.Members.All(m => m.UserId != dto.ToUserId))
                throw new UserNotInGroupException(dto.ToUserId, groupId);
        }
        else if (await uow.Users.GetByIdAsync(dto.ToUserId, ct) is null)
        {
            throw new UserNotFoundException(dto.ToUserId);
        }

        // Закриття боргу — це той самий леджер: платник «поклав» суму, отримувач «мав» її.
        var now = DateTime.UtcNow;
        var expense = new Expense
        {
            Description = "Settle up",
            TotalCost = dto.Amount,
            Date = now,
            GroupId = dto.GroupId,
            CreatedByUserId = currentUserId,
            IsPayment = true,
            CreatedAt = now,
            Splits =
            [
                new ExpenseSplit { UserId = currentUserId, PaidShare = dto.Amount, OwedShare = 0m },
                new ExpenseSplit { UserId = dto.ToUserId, PaidShare = 0m, OwedShare = dto.Amount }
            ]
        };

        await uow.Expenses.AddAsync(expense, ct);
        await uow.SaveChangesAsync(ct);

        return ToDto(expense);
    }

    public async Task<ExpenseDto> GetByIdAsync(int expenseId, int currentUserId, CancellationToken ct = default)
    {
        var expense = await uow.Expenses.GetWithSplitsByIdAsync(expenseId, ct)
                      ?? throw new ExpenseNotFoundException(expenseId);

        if (expense.GroupId is int groupId)
        {
            await GroupAccess.LoadForMemberAsync(uow, groupId, currentUserId, ct);
        }
        else if (expense.Splits.All(s => s.UserId != currentUserId))
        {
            // Для витрат без групи не розкриваємо навіть сам факт існування
            throw new ExpenseNotFoundException(expenseId);
        }

        return ToDto(expense);
    }

    public async Task<IReadOnlyList<ExpenseDto>> GetGroupExpensesAsync(int groupId, int currentUserId, CancellationToken ct = default)
    {
        await GroupAccess.LoadForMemberAsync(uow, groupId, currentUserId, ct);

        var expenses = await uow.Expenses.GetByGroupAsync(groupId, ct);
        return expenses.Select(ToDto).ToList();
    }

    private async Task EnsureParticipantsAsync(int? groupId, IReadOnlyCollection<int> userIds, int currentUserId, CancellationToken ct)
    {
        if (groupId is int gid)
        {
            // Автор — учасник групи, і всі користувачі в частках теж учасники
            var group = await GroupAccess.LoadForMemberAsync(uow, gid, currentUserId, ct);
            var memberIds = group.Members.Select(m => m.UserId).ToHashSet();

            foreach (var userId in userIds)
                if (!memberIds.Contains(userId))
                    throw new UserNotInGroupException(userId, gid);

            return;
        }

        if (!userIds.Contains(currentUserId))
            throw new InvalidExpenseSplitException("An expense without a group must include you as a participant.");

        var existing = (await uow.Users.GetByIdsAsync(userIds, ct)).Select(u => u.Id).ToHashSet();
        foreach (var userId in userIds)
            if (!existing.Contains(userId))
                throw new UserNotFoundException(userId);
    }

    // Npgsql для timestamptz вимагає UTC
    private static DateTime NormalizeDate(DateTime? date) => date switch
    {
        null => DateTime.UtcNow,
        { Kind: DateTimeKind.Unspecified } d => DateTime.SpecifyKind(d, DateTimeKind.Utc),
        { } d => d.ToUniversalTime()
    };

    private static ExpenseDto ToDto(Expense e) => new(
        e.Id, e.Description, e.TotalCost, e.Date, e.GroupId, e.CreatedByUserId, e.IsPayment, e.CreatedAt,
        e.Splits.Select(s => new SplitDto(s.UserId, s.PaidShare, s.OwedShare)).ToList());
}
