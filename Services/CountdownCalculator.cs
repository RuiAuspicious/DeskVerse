namespace DeskVerse;

internal static class CountdownCalculator
{
    public static int RemainingDays(DateTime today, DateTime targetDate)
    {
        return Math.Max(0, (targetDate.Date - today.Date).Days);
    }
}
