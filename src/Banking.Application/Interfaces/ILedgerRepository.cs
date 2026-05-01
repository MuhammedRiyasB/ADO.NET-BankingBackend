using Banking.Domain.Entities;

namespace Banking.Application.Interfaces;

public interface ILedgerRepository
{
    Task<bool> ReferenceExistsAsync(string reference, CancellationToken cancellationToken);
    Task AddAsync(LedgerEntry entry, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<LedgerEntry>> GetStatementAsync(
        Guid accountId,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken);
    Task<decimal> GetTotalDebitsForPeriodAsync(
        Guid accountId,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken);
}
