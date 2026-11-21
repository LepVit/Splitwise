using Splitwise.Core.Dtos;
using Splitwise.Core.Exceptions;
using Splitwise.Core.Interfaces;

namespace Splitwise.Core.Services;

public class UserService(IUnitOfWork uow) : IUserService
{
    public async Task<UserProfileDto> GetProfileAsync(int userId, CancellationToken ct = default)
    {
        var user = await uow.Users.GetByIdAsync(userId, ct) ?? throw new UserNotFoundException(userId);
        return new UserProfileDto(user.Id, user.Email, user.UserName, user.FirstName, user.LastName);
    }

    public async Task<UserProfileDto> UpdateProfileAsync(int userId, UpdateProfileDto dto, CancellationToken ct = default)
    {
        var updated = await uow.Users.UpdateProfileAsync(userId, dto.FirstName?.Trim(), dto.LastName?.Trim(), ct);
        if (!updated)
            throw new UserNotFoundException(userId);

        await uow.SaveChangesAsync(ct);
        return await GetProfileAsync(userId, ct);
    }
}
