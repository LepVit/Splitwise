using Splitwise.Core.Dtos;

namespace Splitwise.Core.Common;

public static class BalanceCalculator
{
    /// <summary>
    /// Зводить чисті баланси до мінімально можливого набору переказів (жадібний алгоритм):
    /// найбільший боржник віддає найбільшому кредитору. Кількість переказів не перевищує n − 1.
    /// Складність O(n log n).
    /// </summary>
    public static IReadOnlyList<DebtDto> SimplifyDebts(IReadOnlyDictionary<int, decimal> netBalances)
    {
        var debtors = netBalances.Where(b => b.Value < 0)
            .Select(b => new Entry(b.Key, -b.Value)).OrderByDescending(e => e.Amount).ToList();
        var creditors = netBalances.Where(b => b.Value > 0)
            .Select(b => new Entry(b.Key, b.Value)).OrderByDescending(e => e.Amount).ToList();

        var result = new List<DebtDto>();
        int d = 0, c = 0;

        while (d < debtors.Count && c < creditors.Count)
        {
            decimal amount = Math.Min(debtors[d].Amount, creditors[c].Amount);
            result.Add(new DebtDto(debtors[d].UserId, creditors[c].UserId, amount));

            debtors[d].Amount -= amount;
            creditors[c].Amount -= amount;

            if (debtors[d].Amount == 0m) d++;
            if (creditors[c].Amount == 0m) c++;
        }

        return result;
    }

    private sealed class Entry(int userId, decimal amount)
    {
        public int UserId { get; } = userId;
        public decimal Amount { get; set; } = amount;
    }
}
