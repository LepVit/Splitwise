using Microsoft.EntityFrameworkCore;
using Splitwise.Core.Entities;
using Splitwise.Core.Interfaces;

namespace Splitwise.Infrastructure.Repositories;

public class ExpenseRepository(AppDbContext db) : IExpenseRepository
{
    public Task<Expense?> GetWithSplitsByIdAsync(int expenseId, CancellationToken ct = default) =>
        db.Expenses.Include(e => e.Splits).AsNoTracking().FirstOrDefaultAsync(e => e.Id == expenseId, ct);

    public async Task<IReadOnlyList<Expense>> GetByGroupAsync(int groupId, CancellationToken ct = default) =>
        await db.Expenses
            .AsNoTracking()
            .Include(e => e.Splits)
            .Where(e => e.GroupId == groupId)
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.Id)
            .ToListAsync(ct);

    public async Task<IReadOnlyDictionary<int, decimal>> GetNetBalancesByGroupAsync(int groupId, CancellationToken ct = default)
    {
        // Одна формула рахує і витрати, і повернення боргів: Σ(PaidShare − OwedShare). Агрегація — на стороні БД.
        var balances = await db.ExpenseSplits
            .Where(s => s.Expense!.GroupId == groupId)
            .GroupBy(s => s.UserId)
            .Select(g => new { UserId = g.Key, Net = g.Sum(s => s.PaidShare - s.OwedShare) })
            .ToDictionaryAsync(x => x.UserId, x => x.Net, ct);

        return balances;
    }

    public async Task AddAsync(Expense expense, CancellationToken ct = default) =>
        await db.Expenses.AddAsync(expense, ct);
}
