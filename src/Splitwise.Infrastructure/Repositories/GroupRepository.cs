using Microsoft.EntityFrameworkCore;
using Splitwise.Core.Entities;
using Splitwise.Core.Interfaces;

namespace Splitwise.Infrastructure.Repositories;

public class GroupRepository(AppDbContext db) : IGroupRepository
{
    public Task<Group?> GetWithMembersByIdAsync(int groupId, CancellationToken ct = default) =>
        db.Groups.Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == groupId, ct);

    public async Task<IReadOnlyList<Group>> GetForUserAsync(int userId, CancellationToken ct = default) =>
        await db.Groups
            .AsNoTracking()
            .Include(g => g.Members)
            .Where(g => g.Members.Any(m => m.UserId == userId))
            .OrderBy(g => g.Name)
            .ToListAsync(ct);

    public async Task AddAsync(Group group, CancellationToken ct = default) =>
        await db.Groups.AddAsync(group, ct);
}
