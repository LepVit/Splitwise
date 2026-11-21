using Microsoft.EntityFrameworkCore;
using Splitwise.Core.Interfaces;
using Splitwise.Core.Models;

namespace Splitwise.Infrastructure.Repositories;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<UserInfo?> GetByIdAsync(int userId, CancellationToken ct = default) =>
        db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new UserInfo(u.Id, u.UserName!, u.Email!, u.FirstName, u.LastName))
            .FirstOrDefaultAsync(ct);

    public Task<UserInfo?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalized = email.ToUpperInvariant();   // Identity зберігає NormalizedEmail у верхньому регістрі

        return db.Users.AsNoTracking()
            .Where(u => u.NormalizedEmail == normalized)
            .Select(u => new UserInfo(u.Id, u.UserName!, u.Email!, u.FirstName, u.LastName))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<UserInfo>> GetByIdsAsync(IEnumerable<int> userIds, CancellationToken ct = default)
    {
        var ids = userIds.Distinct().ToList();

        return await db.Users.AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new UserInfo(u.Id, u.UserName!, u.Email!, u.FirstName, u.LastName))
            .ToListAsync(ct);
    }

    public async Task<bool> UpdateProfileAsync(int userId, string? firstName, string? lastName, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return false;

        user.FirstName = firstName;
        user.LastName = lastName;
        return true;
    }
}
