namespace Splitwise.Core.Entities;

public class ExpenseSplit
{
    public int Id { get; set; }
    public int ExpenseId { get; set; }
    public int UserId { get; set; }

    /// <summary>Скільки користувач поклав у касу.</summary>
    public decimal PaidShare { get; set; }

    /// <summary>Скільки користувач мав би заплатити (його «тарілка»).</summary>
    public decimal OwedShare { get; set; }

    public Expense? Expense { get; set; }
}
