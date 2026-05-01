using Banking.Application.Common;
using Banking.Application.DTOs.Account;

namespace Banking.Application.Services.Accounts;

/// <summary>
/// Handles account statement retrieval and formatting.
/// </summary>
public interface IStatementService
{
    /// <summary>
    /// Retrieves account statement entries for a given date range.
    /// </summary>
    Task<Result<IReadOnlyCollection<StatementEntryResponse>>> GetStatementAsync(
        Guid accountId,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken);
}
