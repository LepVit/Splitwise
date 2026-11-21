using Moq;
using Splitwise.Core.Dtos;
using Splitwise.Core.Entities;
using Splitwise.Core.Exceptions;
using Splitwise.Core.Interfaces;
using Splitwise.Core.Models;
using Splitwise.Core.Services;

namespace Splitwise.Tests;

public class GroupServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IGroupRepository> _groups = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly GroupService _sut;

    private static readonly UserInfo Alice = new(1, "alice", "alice@test.com", "Alice", null);
    private static readonly UserInfo Bob = new(2, "bob", "bob@test.com", null, null);

    public GroupServiceTests()
    {
        _uow.SetupGet(u => u.Groups).Returns(_groups.Object);
        _uow.SetupGet(u => u.Users).Returns(_users.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _sut = new GroupService(_uow.Object);
    }

    private Group SetupGroup(int id, params int[] memberIds)
    {
        var group = new Group
        {
            Id = id,
            Name = "Trip",
            Members = memberIds.Select(m => new GroupMember { GroupId = id, UserId = m }).ToList()
        };
        _groups.Setup(r => r.GetWithMembersByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(group);
        return group;
    }

    [Fact]
    public async Task CreateAsync_ValidDto_SavesGroupWithCreatorAsOnlyMember()
    {
        _users.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Alice);
        Group? added = null;
        _groups.Setup(r => r.AddAsync(It.IsAny<Group>(), It.IsAny<CancellationToken>()))
            .Callback<Group, CancellationToken>((g, _) => added = g)
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(new CreateGroupDto("  Trip  "), 1);

        Assert.NotNull(added);
        Assert.Equal("Trip", added!.Name);
        Assert.Equal(1, Assert.Single(added.Members).UserId);
        Assert.Equal("Trip", result.Name);
        Assert.Equal("alice", Assert.Single(result.Members).UserName);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_UnknownCurrentUser_ThrowsUserNotFound()
    {
        _users.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((UserInfo?)null);

        await Assert.ThrowsAsync<UserNotFoundException>(() => _sut.CreateAsync(new CreateGroupDto("Trip"), 99));
    }

    [Fact]
    public async Task GetAsync_MissingGroup_ThrowsGroupNotFound()
    {
        _groups.Setup(r => r.GetWithMembersByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Group?)null);

        await Assert.ThrowsAsync<GroupNotFoundException>(() => _sut.GetAsync(99, 1));
    }

    [Fact]
    public async Task GetAsync_UserNotInGroup_ThrowsUserNotInGroup()
    {
        SetupGroup(42, 1);

        await Assert.ThrowsAsync<UserNotInGroupException>(() => _sut.GetAsync(42, 2));
    }

    [Fact]
    public async Task GetAsync_Member_ReturnsGroupWithMembers()
    {
        SetupGroup(42, 1, 2);
        _users.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserInfo> { Alice, Bob });

        var result = await _sut.GetAsync(42, 1);

        Assert.Equal(2, result.Members.Count);
        Assert.Contains(result.Members, m => m.Email == "bob@test.com");
    }

    [Fact]
    public async Task GetMyGroupsAsync_ReturnsSummariesWithMemberCount()
    {
        var group = new Group { Id = 5, Name = "Flat", Members = [new GroupMember { UserId = 1 }, new GroupMember { UserId = 2 }] };
        _groups.Setup(r => r.GetForUserAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Group> { group });

        var result = await _sut.GetMyGroupsAsync(1);

        var summary = Assert.Single(result);
        Assert.Equal("Flat", summary.Name);
        Assert.Equal(2, summary.MembersCount);
    }

    [Fact]
    public async Task AddMemberAsync_NewUser_AddsMemberAndSaves()
    {
        var group = SetupGroup(42, 1);
        _users.Setup(r => r.GetByEmailAsync("bob@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(Bob);
        _users.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserInfo> { Alice, Bob });

        var result = await _sut.AddMemberAsync(42, new AddMemberDto("bob@test.com"), 1);

        Assert.Contains(group.Members, m => m.UserId == 2);
        Assert.Equal(2, result.Members.Count);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddMemberAsync_UnknownEmail_ThrowsUserNotFound()
    {
        SetupGroup(42, 1);
        _users.Setup(r => r.GetByEmailAsync("ghost@test.com", It.IsAny<CancellationToken>())).ReturnsAsync((UserInfo?)null);

        await Assert.ThrowsAsync<UserNotFoundException>(
            () => _sut.AddMemberAsync(42, new AddMemberDto("ghost@test.com"), 1));
    }

    [Fact]
    public async Task AddMemberAsync_AlreadyMember_ThrowsUserAlreadyInGroup()
    {
        SetupGroup(42, 1, 2);
        _users.Setup(r => r.GetByEmailAsync("bob@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(Bob);

        await Assert.ThrowsAsync<UserAlreadyInGroupException>(
            () => _sut.AddMemberAsync(42, new AddMemberDto("bob@test.com"), 1));
    }

    [Fact]
    public async Task AddMemberAsync_CallerNotInGroup_ThrowsUserNotInGroup()
    {
        SetupGroup(42, 1);

        await Assert.ThrowsAsync<UserNotInGroupException>(
            () => _sut.AddMemberAsync(42, new AddMemberDto("bob@test.com"), 2));
    }
}
