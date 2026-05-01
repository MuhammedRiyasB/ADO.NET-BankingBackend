namespace Banking.Application.Utilities;

/// <summary>
/// Provides UTC date range utilities.
/// </summary>
public sealed class DateRangeHelper : IDateRangeHelper
{
    public (DateTime StartUtc, DateTime EndUtc) GetUtcDayRange(DateTime utcNow)
    {
        var start = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, DateTimeKind.Utc);
        return (start, start.AddDays(1));
    }
}
