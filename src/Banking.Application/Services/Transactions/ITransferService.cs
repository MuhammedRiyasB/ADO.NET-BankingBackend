using Banking.Application.Common;
using Banking.Application.DTOs.Transaction;

namespace Banking.Application.Services.Transactions;

/// <summary>
/// Handles account-to-account transfers.
/// </summary>
public interface ITransferService
{
    /// <summary>
    /// Processes an atomic transfer between two accounts.
    /// </summary>
    Task<Result<TransferResponse>> TransferAsync(
        TransferRequest request,
        CancellationToken cancellationToken);
}
