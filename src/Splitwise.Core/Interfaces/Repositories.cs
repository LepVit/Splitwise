using Splitwise.Core.Entities;
using Splitwise.Core.Models;

namespace Splitwise.Core.Interfaces;

public interface IGroupRepository
{
    Task<Group?> GetWithMembersByIdAsync(int groupId, CancellationToken ct = default);
    Task<IReadOnlyList<Group>> GetForUserAsync(int userId, CancellationToken ct = default);
    Task AddAsync(Group group, CancellationToken ct = default);
}

public interface IExpenseRepository
{
    Task<Expense?> GetWithSplitsByIdAsync(int expenseId, CancellationToken ct = default);
    Task<IReadOnlyList<Expense>> GetByGroupAsync(int groupId, CancellationToken ct = default);

    /// <summary>Чистий баланс кожного користувача групи: Σ(PaidShare − OwedShare). Рахується в базі.</summary>
    Task<IReadOnlyDictionary<int, decimal>> GetNetBalancesByGroupAsync(int groupId, CancellationToken ct = default);

    Task AddAsync(Expense expense, CancellationToken ct = default);
}

public interface IUserRepository
{
    Task<UserInfo?> GetByIdAsync(int userId, CancellationToken ct = default);
    Task<UserInfo?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<IReadOnlyList<UserInfo>> GetByIdsAsync(IEnumerable<int> userIds, CancellationToken ct = default);

    /// <summary>Змінює ім'я/прізвище (зберігається через IUnitOfWork.SaveChangesAsync). false, якщо користувача немає.</summary>
    Task<bool> UpdateProfileAsync(int userId, string? firstName, string? lastName, CancellationToken ct = default);
}

public interface IUnitOfWork
{
    IGroupRepository Groups { get; }
    IExpenseRepository Expenses { get; }
    IUserRepository Users { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
