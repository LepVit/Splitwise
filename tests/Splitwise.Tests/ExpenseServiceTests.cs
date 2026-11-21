using Moq;
using Splitwise.Core.Dtos;
using Splitwise.Core.Entities;
using Splitwise.Core.Exceptions;
using Splitwise.Core.Interfaces;
using Splitwise.Core.Models;
using Splitwise.Core.Services;

namespace Splitwise.Tests;

public class ExpenseServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IGroupRepository> _groups = new();
    private readonly Mock<IExpenseRepository> _expenses = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly ExpenseService _sut;

    private Expense? _added;

    public ExpenseServiceTests()
    {
        _uow.SetupGet(u => u.Groups).Returns(_groups.Object);
        _uow.SetupGet(u => u.Expenses).Returns(_expenses.Object);
        _uow.SetupGet(u => u.Users).Returns(_users.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _expenses.Setup(r => r.AddAsync(It.IsAny<Expense>(), It.IsAny<CancellationToken>()))
            .Callback<Expense, CancellationToken>((e, _) => _added = e)
            .Returns(Task.CompletedTask);

        _sut = new ExpenseService(_uow.Object);
    }

    private void SetupGroup(int id, params int[] memberIds)
    {
        var group = new Group
        {
            Id = id,
            Name = "Trip",
            Members = memberIds.Select(m => new GroupMember { GroupId = id, UserId = m }).ToList()
        };
        _groups.Setup(r => r.GetWithMembersByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(group);
    }

    private void SetupUsers(params int[] ids)
    {
        var infos = ids.Select(i => new UserInfo(i, $"user{i}", $"user{i}@test.com", null, null)).ToList();
        _users.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>())).ReturnsAsync(infos);
        foreach (var info in infos)
            _users.Setup(r => r.GetByIdAsync(info.Id, It.IsAny<CancellationToken>())).ReturnsAsync(info);
    }

    private static CreateExpenseDto Pizza(int? groupId) => new(
        "Pizza", 1000m, null, groupId,
        [new SplitDto(1, 600m, 250m), new SplitDto(2, 400m, 250m), new SplitDto(3, 0m, 250m), new SplitDto(4, 0m, 250m)]);

    // ---------- CreateAsync ----------

    [Fact]
    public async Task CreateAsync_GroupMember_SavesExpenseCreatedByCurrentUser()
    {
        SetupGroup(10, 1, 2, 3, 4);

        var result = await _sut.CreateAsync(Pizza(10), currentUserId: 1);

        Assert.NotNull(_added);
        Assert.Equal(1, _added!.CreatedByUserId);
        Assert.False(_added.IsPayment);
        Assert.Equal(10, _added.GroupId);
        Assert.Equal(4, _added.Splits.Count);
        Assert.Equal(0m, _added.Splits.Sum(s => s.PaidShare - s.OwedShare));
        Assert.Equal(4, result.Splits.Count);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_PaidShareSumDiffersFromTotal_ThrowsInvalidSplit()
    {
        var dto = new CreateExpenseDto("x", 100m, null, 10, [new SplitDto(1, 90m, 100m)]);

        await Assert.ThrowsAsync<InvalidExpenseSplitException>(() => _sut.CreateAsync(dto, 1));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_OwedShareSumDiffersFromTotal_ThrowsInvalidSplit()
    {
        var dto = new CreateExpenseDto("x", 100m, null, 10, [new SplitDto(1, 100m, 60m), new SplitDto(2, 0m, 30m)]);

        await Assert.ThrowsAsync<InvalidExpenseSplitException>(() => _sut.CreateAsync(dto, 1));
    }

    [Fact]
    public async Task CreateAsync_SameUserTwice_ThrowsInvalidSplit()
    {
        var dto = new CreateExpenseDto("x", 100m, null, 10, [new SplitDto(1, 100m, 50m), new SplitDto(1, 0m, 50m)]);

        await Assert.ThrowsAsync<InvalidExpenseSplitException>(() => _sut.CreateAsync(dto, 1));
    }

    [Fact]
    public async Task CreateAsync_SplitUserNotInGroup_ThrowsUserNotInGroup()
    {
        SetupGroup(10, 1, 2, 3);   // користувача 4 у групі немає

        var ex = await Assert.ThrowsAsync<UserNotInGroupException>(() => _sut.CreateAsync(Pizza(10), 1));

        Assert.Contains("4", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_CurrentUserNotInGroup_ThrowsUserNotInGroup()
    {
        SetupGroup(10, 1, 2, 3, 4);

        await Assert.ThrowsAsync<UserNotInGroupException>(() => _sut.CreateAsync(Pizza(10), currentUserId: 99));
    }

    [Fact]
    public async Task CreateAsync_GroupDoesNotExist_ThrowsGroupNotFound()
    {
        _groups.Setup(r => r.GetWithMembersByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync((Group?)null);

        await Assert.ThrowsAsync<GroupNotFoundException>(() => _sut.CreateAsync(Pizza(10), 1));
    }

    [Fact]
    public async Task CreateAsync_NoGroup_SavesPersonalExpense()
    {
        SetupUsers(1, 2);
        var dto = new CreateExpenseDto("Lunch", 10m, new DateTime(2026, 1, 1), null,
            [new SplitDto(1, 10m, 5m), new SplitDto(2, 0m, 5m)]);

        var result = await _sut.CreateAsync(dto, 1);

        Assert.Null(_added!.GroupId);
        Assert.Equal(DateTimeKind.Utc, result.Date.Kind);
    }

    [Fact]
    public async Task CreateAsync_NoGroupAndCurrentUserNotParticipant_ThrowsInvalidSplit()
    {
        var dto = new CreateExpenseDto("Lunch", 10m, null, null, [new SplitDto(2, 10m, 5m), new SplitDto(3, 0m, 5m)]);

        await Assert.ThrowsAsync<InvalidExpenseSplitException>(() => _sut.CreateAsync(dto, 1));
    }

    [Fact]
    public async Task CreateAsync_NoGroupAndUnknownUser_ThrowsUserNotFound()
    {
        SetupUsers(1);   // користувача 2 у базі немає
        var dto = new CreateExpenseDto("Lunch", 10m, null, null, [new SplitDto(1, 10m, 5m), new SplitDto(2, 0m, 5m)]);

        await Assert.ThrowsAsync<UserNotFoundException>(() => _sut.CreateAsync(dto, 1));
    }

    // ---------- CreateEqualAsync ----------

    [Fact]
    public async Task CreateEqualAsync_HundredAmongThree_SharesSumExactlyToTotal()
    {
        SetupGroup(10, 1, 2, 3);
        var dto = new CreateEqualExpenseDto("Taxi", 100m, null, 10, PaidByUserId: 2, [1, 2, 3]);

        var result = await _sut.CreateEqualAsync(dto, 2);

        Assert.Equal(100m, result.Splits.Sum(s => s.OwedShare));
        Assert.Equal(100m, result.Splits.Sum(s => s.PaidShare));
        Assert.Equal(100m, result.Splits.Single(s => s.UserId == 2).PaidShare);
    }

    [Fact]
    public async Task CreateEqualAsync_PayerDidNotEat_GetsOnlyPaidShare()
    {
        SetupGroup(10, 1, 2, 3);
        var dto = new CreateEqualExpenseDto("Gift", 90m, null, 10, PaidByUserId: 1, [2, 3]);

        var result = await _sut.CreateEqualAsync(dto, 1);

        var payer = result.Splits.Single(s => s.UserId == 1);
        Assert.Equal(90m, payer.PaidShare);
        Assert.Equal(0m, payer.OwedShare);
        Assert.Equal(90m, result.Splits.Sum(s => s.OwedShare));
    }

    [Fact]
    public async Task CreateEqualAsync_NoParticipants_ThrowsInvalidSplit()
    {
        var dto = new CreateEqualExpenseDto("x", 10m, null, 10, 1, []);

        await Assert.ThrowsAsync<InvalidExpenseSplitException>(() => _sut.CreateEqualAsync(dto, 1));
    }

    // ---------- SettleUpAsync ----------

    [Fact]
    public async Task SettleUpAsync_InGroup_CreatesPaymentWithOppositeShares()
    {
        SetupGroup(10, 1, 2);

        var result = await _sut.SettleUpAsync(new CreateSettlementDto(ToUserId: 1, Amount: 250m, GroupId: 10), currentUserId: 2);

        Assert.True(_added!.IsPayment);
        Assert.Equal(250m, _added.TotalCost);
        var payer = result.Splits.Single(s => s.UserId == 2);
        var receiver = result.Splits.Single(s => s.UserId == 1);
        Assert.Equal((250m, 0m), (payer.PaidShare, payer.OwedShare));
        Assert.Equal((0m, 250m), (receiver.PaidShare, receiver.OwedShare));
    }

    [Fact]
    public async Task SettleUpAsync_ToSelf_ThrowsInvalidSplit()
    {
        await Assert.ThrowsAsync<InvalidExpenseSplitException>(
            () => _sut.SettleUpAsync(new CreateSettlementDto(1, 10m, 10), currentUserId: 1));
    }

    [Fact]
    public async Task SettleUpAsync_ReceiverNotInGroup_ThrowsUserNotInGroup()
    {
        SetupGroup(10, 1, 2);

        await Assert.ThrowsAsync<UserNotInGroupException>(
            () => _sut.SettleUpAsync(new CreateSettlementDto(ToUserId: 3, Amount: 10m, GroupId: 10), currentUserId: 1));
    }

    [Fact]
    public async Task SettleUpAsync_NoGroupAndUnknownReceiver_ThrowsUserNotFound()
    {
        _users.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync((UserInfo?)null);

        await Assert.ThrowsAsync<UserNotFoundException>(
            () => _sut.SettleUpAsync(new CreateSettlementDto(ToUserId: 7, Amount: 10m, GroupId: null), currentUserId: 1));
    }

    // ---------- GetByIdAsync / GetGroupExpensesAsync ----------

    [Fact]
    public async Task GetByIdAsync_MissingExpense_ThrowsExpenseNotFound()
    {
        _expenses.Setup(r => r.GetWithSplitsByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((Expense?)null);

        await Assert.ThrowsAsync<ExpenseNotFoundException>(() => _sut.GetByIdAsync(5, 1));
    }

    [Fact]
    public async Task GetByIdAsync_GroupExpenseAndCallerNotMember_ThrowsUserNotInGroup()
    {
        SetupGroup(10, 1, 2);
        _expenses.Setup(r => r.GetWithSplitsByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Expense { Id = 5, GroupId = 10, Splits = [] });

        await Assert.ThrowsAsync<UserNotInGroupException>(() => _sut.GetByIdAsync(5, 99));
    }

    [Fact]
    public async Task GetByIdAsync_PersonalExpenseAndCallerNotParticipant_ThrowsExpenseNotFound()
    {
        _expenses.Setup(r => r.GetWithSplitsByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Expense { Id = 5, GroupId = null, Splits = [new ExpenseSplit { UserId = 1 }, new ExpenseSplit { UserId = 2 }] });

        await Assert.ThrowsAsync<ExpenseNotFoundException>(() => _sut.GetByIdAsync(5, 99));
    }

    [Fact]
    public async Task GetGroupExpensesAsync_Member_ReturnsMappedExpenses()
    {
        SetupGroup(10, 1, 2);
        _expenses.Setup(r => r.GetByGroupAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Expense>
            {
                new() { Id = 1, Description = "Pizza", TotalCost = 10m, GroupId = 10, Splits = [new ExpenseSplit { UserId = 1, PaidShare = 10m }] }
            });

        var result = await _sut.GetGroupExpensesAsync(10, 1);

        var dto = Assert.Single(result);
        Assert.Equal("Pizza", dto.Description);
        Assert.Single(dto.Splits);
    }

    [Fact]
    public async Task GetGroupExpensesAsync_NotMember_ThrowsUserNotInGroup()
    {
        SetupGroup(10, 1, 2);

        await Assert.ThrowsAsync<UserNotInGroupException>(() => _sut.GetGroupExpensesAsync(10, 99));
    }
}
