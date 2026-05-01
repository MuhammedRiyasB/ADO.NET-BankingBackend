namespace Banking.Application.Validators;

/// <summary>
/// Validates currency codes.
/// </summary>
public interface ICurrencyValidator
{
    /// <summary>
    /// Checks if a currency code is valid (3-letter ISO code).
    /// </summary>
    bool IsValidCurrency(string currency);
}
