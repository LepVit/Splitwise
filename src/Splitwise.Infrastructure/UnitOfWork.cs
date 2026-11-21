using Splitwise.Core.Interfaces;

namespace Splitwise.Infrastructure;

public class UnitOfWork(
    AppDbContext db,
    IGroupRepository groups,
    IExpenseRepository expenses,
    IUserRepository users) : IUnitOfWork
{
    public IGroupRepository Groups => groups;
    public IExpenseRepository Expenses => expenses;
    public IUserRepository Users => users;

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
