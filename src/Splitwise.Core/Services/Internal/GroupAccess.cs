using Splitwise.Core.Entities;
using Splitwise.Core.Exceptions;
using Splitwise.Core.Interfaces;

namespace Splitwise.Core.Services.Internal;

internal static class GroupAccess
{
    /// <summary>
    /// Завантажує групу з учасниками і перевіряє, що користувач є її учасником.
    /// Захист від IDOR: чужу групу за чужим id отримати не можна.
    /// </summary>
    public static async Task<Group> LoadForMemberAsync(IUnitOfWork uow, int groupId, int userId, CancellationToken ct)
    {
        var group = await uow.Groups.GetWithMembersByIdAsync(groupId, ct)
                    ?? throw new GroupNotFoundException(groupId);

        if (group.Members.All(m => m.UserId != userId))
            throw new UserNotInGroupException(userId, groupId);

        return group;
    }
}
