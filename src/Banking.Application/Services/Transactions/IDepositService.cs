using Banking.Application.Common;
using Banking.Application.DTOs.Transaction;

namespace Banking.Application.Services.Transactions;

/// <summary>
/// Handles deposit transactions.
/// </summary>
public interface IDepositService
{
    /// <summary>
    /// Processes a deposit to an account.
    /// </summary>
    Task<Result<TransactionResponse>> DepositAsync(
        DepositRequest request,
        CancellationToken cancellationToken);
}
