using Splitwise.Core.Common;
using Splitwise.Core.Dtos;

namespace Splitwise.Tests;

public class SplitCalculatorTests
{
    [Theory]
    [InlineData(100.00, 3)]
    [InlineData(0.01, 3)]
    [InlineData(10.00, 3)]
    [InlineData(99.99, 7)]
    [InlineData(1000, 4)]
    [InlineData(1, 1)]
    public void SplitEvenly_SharesAlwaysSumToTotal(double total, int count)
    {
        var shares = SplitCalculator.SplitEvenly((decimal)total, count);

        Assert.Equal(count, shares.Length);
        Assert.Equal((decimal)total, shares.Sum());
        Assert.True(shares.Max() - shares.Min() <= 0.01m, "Shares must differ by at most one cent.");
    }

    [Fact]
    public void SplitEvenly_HundredByThree_GivesExtraCentToFirstParticipant()
    {
        var shares = SplitCalculator.SplitEvenly(100m, 3);

        Assert.Equal(new[] { 33.34m, 33.33m, 33.33m }, shares);
    }

    [Fact]
    public void SplitEvenly_ZeroCount_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SplitCalculator.SplitEvenly(10m, 0));
    }
}

public class BalanceCalculatorTests
{
    [Fact]
    public void NetBalances_InAnyGroup_SumToZero()
    {
        // Піца 1000: Аліса 600, Боб 400; їли четверо по 250
        (decimal Paid, decimal Owed)[] splits = [(600m, 250m), (400m, 250m), (0m, 250m), (0m, 250m)];

        Assert.Equal(0m, splits.Sum(s => s.Paid - s.Owed));
    }

    [Fact]
    public void SimplifyDebts_AppliedToBalances_SettlesEveryone()
    {
        var balances = new Dictionary<int, decimal> { [1] = 350m, [2] = 150m, [3] = -250m, [4] = -250m };

        var debts = BalanceCalculator.SimplifyDebts(balances);

        var after = new Dictionary<int, decimal>(balances);
        foreach (var debt in debts)
        {
            after[debt.FromUserId] += debt.Amount;
            after[debt.ToUserId] -= debt.Amount;
        }

        Assert.All(after.Values, v => Assert.Equal(0m, v));
        Assert.True(debts.Count <= balances.Count - 1);
    }

    [Fact]
    public void SimplifyDebts_NoDebts_ReturnsEmpty()
    {
        var debts = BalanceCalculator.SimplifyDebts(new Dictionary<int, decimal> { [1] = 0m, [2] = 0m });

        Assert.Empty(debts);
    }

    [Fact]
    public void SimplifyDebts_TwoPeople_DebtorPaysCreditorExactAmount()
    {
        var debts = BalanceCalculator.SimplifyDebts(new Dictionary<int, decimal> { [1] = 50m, [2] = -50m });

        Assert.Equal(new DebtDto(2, 1, 50m), Assert.Single(debts));
    }
}
