using Banking.Application.Common;
using Banking.Application.DTOs.Transaction;

namespace Banking.Application.Services.Transactions;

/// <summary>
/// Handles withdrawal transactions.
/// </summary>
public interface IWithdrawService
{
    /// <summary>
    /// Processes a withdrawal from an account.
    /// </summary>
    Task<Result<TransactionResponse>> WithdrawAsync(
        WithdrawRequest request,
        CancellationToken cancellationToken);
}
