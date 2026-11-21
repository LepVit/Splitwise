namespace Splitwise.Core.Common;

public static class SplitCalculator
{
    /// <summary>
    /// Ділить суму на <paramref name="count"/> частин із точністю до копійки так, щоб сума часток
    /// дорівнювала сумі точно: округляємо вниз, а залишок копійок роздаємо по одній перших учасників.
    /// 100.00 / 3 → 33.34, 33.33, 33.33.
    /// </summary>
    public static decimal[] SplitEvenly(decimal total, int count)
    {
        if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count), "Count must be positive.");
        if (total < 0) throw new ArgumentOutOfRangeException(nameof(total), "Total must not be negative.");

        total = decimal.Round(total, 2);

        decimal baseShare = Math.Floor(total / count * 100m) / 100m;
        int centsRemainder = (int)Math.Round((total - baseShare * count) * 100m);

        var shares = new decimal[count];
        for (int i = 0; i < count; i++)
            shares[i] = baseShare + (i < centsRemainder ? 0.01m : 0m);

        return shares;
    }
}
