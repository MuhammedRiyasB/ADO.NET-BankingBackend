namespace Banking.Application.Validators;

/// <summary>
/// Validates monetary amounts.
/// </summary>
public sealed class AmountValidator : IAmountValidator
{
    public bool IsValidAmount(decimal amount)
    {
        return amount > 0 && decimal.Round(amount, 2) == amount;
    }

    public bool IsValidLimit(decimal limit)
    {
        return limit > 0;
    }
}
