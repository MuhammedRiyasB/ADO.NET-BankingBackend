namespace Banking.Application.Utilities;

/// <summary>
/// Provides UTC date range utilities.
/// </summary>
public interface IDateRangeHelper
{
    /// <summary>
    /// Gets the start and end of a UTC day (00:00:00 to 24:00:00).
    /// </summary>
    (DateTime StartUtc, DateTime EndUtc) GetUtcDayRange(DateTime utcNow);
}
