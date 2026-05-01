using Banking.Application.Common;
using Banking.Application.DTOs.Transaction;

namespace Banking.Application.Validators;

/// <summary>
/// Validates transaction requests (deposits, withdrawals, transfers).
/// </summary>
public interface ITransactionValidator
{
    /// <summary>
    /// Validates a single-account transaction (deposit/withdrawal).
    /// </summary>
    Result<Unit>? ValidateSingleAccountTransaction(
        Guid accountId,
        decimal amount,
        string reference,
        string narrative);

    /// <summary>
    /// Validates a transfer request.
    /// </summary>
    Result<Unit>? ValidateTransfer(
        Guid fromAccountId,
        Guid toAccountId,
        decimal amount,
        string narrative,
        string externalReference);
}
