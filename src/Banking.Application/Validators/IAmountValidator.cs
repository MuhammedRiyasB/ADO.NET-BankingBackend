namespace Banking.Application.Validators;

/// <summary>
/// Validates monetary amounts.
/// </summary>
public interface IAmountValidator
{
    /// <summary>
    /// Checks if an amount is valid (positive and at most 2 decimals).
    /// </summary>
    bool IsValidAmount(decimal amount);

    /// <summary>
    /// Checks if a debit/credit limit is valid (positive).
    /// </summary>
    bool IsValidLimit(decimal limit);
}
