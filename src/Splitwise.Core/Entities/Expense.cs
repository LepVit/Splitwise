namespace Splitwise.Core.Entities;

/// <summary>
/// Витрата. Платник у самій витраті не зберігається: хто скільки заплатив і хто скільки «з'їв»
/// записано в частках (<see cref="ExpenseSplit"/>). Закриття боргу (Settle Up) — це теж витрата з IsPayment = true.
/// </summary>
public class Expense
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal TotalCost { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;

    /// <summary>Група необов'язкова: витрата може бути й без групи.</summary>
    public int? GroupId { get; set; }
    public int CreatedByUserId { get; set; }
    public bool IsPayment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Group? Group { get; set; }
    public ICollection<ExpenseSplit> Splits { get; set; } = new List<ExpenseSplit>();
}
