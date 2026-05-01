namespace Banking.Application.Validators;

/// <summary>
/// Validates currency codes (3-letter ISO format).
/// </summary>
public sealed class CurrencyValidator : ICurrencyValidator
{
    public bool IsValidCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            return false;
        }

        return currency.Trim().Length == 3 && currency.Trim().All(char.IsLetter);
    }
}
