using Banking.Application.Interfaces;

namespace Banking.Application.Utilities;

/// <summary>
/// Checks if a daily debit limit has been exceeded.
/// </summary>
public interface IDailyDebitLimitChecker
{
    /// <summary>
    /// Checks if adding an amount would exceed the daily debit limit.
    /// Returns true if the limit would be exceeded.
    /// </summary>
    Task<bool> WouldExceedDailyLimitAsync(
        Guid accountId,
        decimal dailyDebitLimit,
        decimal amountToDebit,
        DateTime utcNow,
        CancellationToken cancellationToken);
}
