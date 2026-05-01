using Banking.Application.Common;

namespace Banking.Application.Validators;

/// <summary>
/// Validates transaction requests.
/// </summary>
public sealed class TransactionValidator : ITransactionValidator
{
    private readonly IAmountValidator _amountValidator;

    public TransactionValidator(IAmountValidator amountValidator)
    {
        _amountValidator = amountValidator;
    }

    public Result<Unit>? ValidateSingleAccountTransaction(
        Guid accountId,
        decimal amount,
        string reference,
        string narrative)
    {
        if (accountId == Guid.Empty)
        {
            return Result<Unit>.Failure(ErrorCodes.Validation, "AccountId is required.");
        }

        if (!_amountValidator.IsValidAmount(amount))
        {
            return Result<Unit>.Failure(ErrorCodes.Validation, "Amount must be greater than zero with at most 2 decimals.");
        }

        if (string.IsNullOrWhiteSpace(reference))
        {
            return Result<Unit>.Failure(ErrorCodes.Validation, "Reference is required.");
        }

        if (string.IsNullOrWhiteSpace(narrative))
        {
            return Result<Unit>.Failure(ErrorCodes.Validation, "Narrative is required.");
        }

        return null;
    }

    public Result<Unit>? ValidateTransfer(
        Guid fromAccountId,
        Guid toAccountId,
        decimal amount,
        string narrative,
        string externalReference)
    {
        if (fromAccountId == Guid.Empty || toAccountId == Guid.Empty)
        {
            return Result<Unit>.Failure(ErrorCodes.Validation, "FromAccountId and ToAccountId are required.");
        }

        if (fromAccountId == toAccountId)
        {
            return Result<Unit>.Failure(ErrorCodes.Validation, "Cannot transfer to the same account.");
        }

        if (!_amountValidator.IsValidAmount(amount))
        {
            return Result<Unit>.Failure(ErrorCodes.Validation, "Amount must be greater than zero with at most 2 decimals.");
        }

        if (string.IsNullOrWhiteSpace(narrative))
        {
            return Result<Unit>.Failure(ErrorCodes.Validation, "Narrative is required.");
        }

        if (string.IsNullOrWhiteSpace(externalReference))
        {
            return Result<Unit>.Failure(ErrorCodes.Validation, "ExternalReference is required.");
        }

        return null;
    }
}
