using Moq;
using Splitwise.Core.Dtos;
using Splitwise.Core.Entities;
using Splitwise.Core.Exceptions;
using Splitwise.Core.Interfaces;
using Splitwise.Core.Models;
using Splitwise.Core.Services;

namespace Splitwise.Tests;

public class BalanceServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IGroupRepository> _groups = new();
    private readonly Mock<IExpenseRepository> _expenses = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly BalanceService _sut;

    public BalanceServiceTests()
    {
        _uow.SetupGet(u => u.Groups).Returns(_groups.Object);
        _uow.SetupGet(u => u.Expenses).Returns(_expenses.Object);
        _uow.SetupGet(u => u.Users).Returns(_users.Object);
        _sut = new BalanceService(_uow.Object);
    }

    private void SetupGroup(int id, params int[] memberIds)
    {
        var group = new Group { Id = id, Name = "Trip", Members = memberIds.Select(m => new GroupMember { GroupId = id, UserId = m }).ToList() };
        _groups.Setup(r => r.GetWithMembersByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(group);
        _users.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(memberIds.Select(i => new UserInfo(i, $"user{i}", $"user{i}@test.com", null, null)).ToList());
    }

    [Fact]
    public async Task GetGroupBalancesAsync_BalancesSumToZeroAndMembersWithoutSplitsGetZero()
    {
        SetupGroup(10, 1, 2, 3);
        _expenses.Setup(r => r.GetNetBalancesByGroupAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, decimal> { [1] = 50m, [2] = -50m });

        var result = await _sut.GetGroupBalancesAsync(10, 1);

        Assert.Equal(0m, result.Balances.Sum(b => b.NetBalance));
        Assert.Equal(0m, result.Balances.Single(b => b.UserId == 3).NetBalance);
        Assert.Equal(50m, result.Balances.Single(b => b.UserId == 1).NetBalance);
        Assert.Equal("user1", result.Balances.Single(b => b.UserId == 1).UserName);
    }

    [Fact]
    public async Task GetGroupBalancesAsync_ReturnsSuggestedSettlements()
    {
        SetupGroup(10, 1, 2);
        _expenses.Setup(r => r.GetNetBalancesByGroupAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, decimal> { [1] = 50m, [2] = -50m });

        var result = await _sut.GetGroupBalancesAsync(10, 2);

        Assert.Equal(new DebtDto(2, 1, 50m), Assert.Single(result.SuggestedSettlements));
    }

    [Fact]
    public async Task GetGroupBalancesAsync_UserNotInGroup_ThrowsUserNotInGroup()
    {
        SetupGroup(10, 1, 2);

        await Assert.ThrowsAsync<UserNotInGroupException>(() => _sut.GetGroupBalancesAsync(10, 99));
    }

    [Fact]
    public async Task GetGroupBalancesAsync_MissingGroup_ThrowsGroupNotFound()
    {
        _groups.Setup(r => r.GetWithMembersByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync((Group?)null);

        await Assert.ThrowsAsync<GroupNotFoundException>(() => _sut.GetGroupBalancesAsync(10, 1));
    }
}

public class UserServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _uow.SetupGet(u => u.Users).Returns(_users.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _sut = new UserService(_uow.Object);
    }

    [Fact]
    public async Task GetProfileAsync_ExistingUser_ReturnsProfile()
    {
        _users.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserInfo(1, "alice", "alice@test.com", "Alice", "Smith"));

        var profile = await _sut.GetProfileAsync(1);

        Assert.Equal("alice@test.com", profile.Email);
        Assert.Equal("Smith", profile.LastName);
    }

    [Fact]
    public async Task GetProfileAsync_MissingUser_ThrowsUserNotFound()
    {
        _users.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((UserInfo?)null);

        await Assert.ThrowsAsync<UserNotFoundException>(() => _sut.GetProfileAsync(1));
    }

    [Fact]
    public async Task UpdateProfileAsync_TrimsNamesAndSaves()
    {
        _users.Setup(r => r.UpdateProfileAsync(1, "Alice", "Smith", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _users.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserInfo(1, "alice", "alice@test.com", "Alice", "Smith"));

        var profile = await _sut.UpdateProfileAsync(1, new UpdateProfileDto("  Alice ", " Smith  "));

        Assert.Equal("Alice", profile.FirstName);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProfileAsync_MissingUser_ThrowsUserNotFoundAndDoesNotSave()
    {
        _users.Setup(r => r.UpdateProfileAsync(1, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await Assert.ThrowsAsync<UserNotFoundException>(() => _sut.UpdateProfileAsync(1, new UpdateProfileDto("A", "B")));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
