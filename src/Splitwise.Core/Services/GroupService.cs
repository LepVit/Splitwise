using Splitwise.Core.Dtos;
using Splitwise.Core.Entities;
using Splitwise.Core.Exceptions;
using Splitwise.Core.Interfaces;
using Splitwise.Core.Models;
using Splitwise.Core.Services.Internal;

namespace Splitwise.Core.Services;

public class GroupService(IUnitOfWork uow) : IGroupService
{
    public async Task<GroupDto> CreateAsync(CreateGroupDto dto, int currentUserId, CancellationToken ct = default)
    {
        var creator = await uow.Users.GetByIdAsync(currentUserId, ct)
                      ?? throw new UserNotFoundException(currentUserId);

        var now = DateTime.UtcNow;
        var group = new Group { Name = dto.Name.Trim(), CreatedAt = now };
        group.Members.Add(new GroupMember { UserId = currentUserId, JoinedAt = now });

        await uow.Groups.AddAsync(group, ct);
        await uow.SaveChangesAsync(ct);

        return ToDto(group, [creator]);
    }

    public async Task<GroupDto> GetAsync(int groupId, int currentUserId, CancellationToken ct = default)
    {
        var group = await GroupAccess.LoadForMemberAsync(uow, groupId, currentUserId, ct);
        var users = await uow.Users.GetByIdsAsync(group.Members.Select(m => m.UserId), ct);
        return ToDto(group, users);
    }

    public async Task<IReadOnlyList<GroupSummaryDto>> GetMyGroupsAsync(int currentUserId, CancellationToken ct = default)
    {
        var groups = await uow.Groups.GetForUserAsync(currentUserId, ct);
        return groups.Select(g => new GroupSummaryDto(g.Id, g.Name, g.CreatedAt, g.Members.Count)).ToList();
    }

    public async Task<GroupDto> AddMemberAsync(int groupId, AddMemberDto dto, int currentUserId, CancellationToken ct = default)
    {
        var group = await GroupAccess.LoadForMemberAsync(uow, groupId, currentUserId, ct);

        var email = dto.Email.Trim();
        var newMember = await uow.Users.GetByEmailAsync(email, ct)
                        ?? throw new UserNotFoundException(email);

        if (group.Members.Any(m => m.UserId == newMember.Id))
            throw new UserAlreadyInGroupException(newMember.Id, groupId);

        group.Members.Add(new GroupMember { GroupId = group.Id, UserId = newMember.Id, JoinedAt = DateTime.UtcNow });
        await uow.SaveChangesAsync(ct);

        var users = await uow.Users.GetByIdsAsync(group.Members.Select(m => m.UserId), ct);
        return ToDto(group, users);
    }

    private static GroupDto ToDto(Group group, IReadOnlyCollection<UserInfo> users)
    {
        var byId = users.ToDictionary(u => u.Id);

        var members = group.Members
            .OrderBy(m => m.JoinedAt)
            .Select(m => byId.TryGetValue(m.UserId, out var u)
                ? new GroupMemberDto(m.UserId, u.UserName, u.Email, u.FirstName, u.LastName, m.JoinedAt)
                : new GroupMemberDto(m.UserId, string.Empty, string.Empty, null, null, m.JoinedAt))
            .ToList();

        return new GroupDto(group.Id, group.Name, group.CreatedAt, members);
    }
}
