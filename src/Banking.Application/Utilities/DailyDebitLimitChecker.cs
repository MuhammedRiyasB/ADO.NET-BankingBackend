using Banking.Application.Interfaces;

namespace Banking.Application.Utilities;

/// <summary>
/// Checks if a daily debit limit has been exceeded.
/// </summary>
public sealed class DailyDebitLimitChecker : IDailyDebitLimitChecker
{
    private readonly ILedgerRepository _ledgerRepository;
    private readonly IDateRangeHelper _dateRangeHelper;

    public DailyDebitLimitChecker(
        ILedgerRepository ledgerRepository,
        IDateRangeHelper dateRangeHelper)
    {
        _ledgerRepository = ledgerRepository;
        _dateRangeHelper = dateRangeHelper;
    }

    public async Task<bool> WouldExceedDailyLimitAsync(
        Guid accountId,
        decimal dailyDebitLimit,
        decimal amountToDebit,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var (dayStartUtc, dayEndUtc) = _dateRangeHelper.GetUtcDayRange(utcNow);
        var totalDebitsToday = await _ledgerRepository.GetTotalDebitsForPeriodAsync(
            accountId,
            dayStartUtc,
            dayEndUtc,
            cancellationToken);

        return totalDebitsToday + amountToDebit > dailyDebitLimit;
    }
}
