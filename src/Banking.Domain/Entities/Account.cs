using Banking.Domain.Enums;

namespace Banking.Domain.Entities;

public sealed class Account
{
    public Account(
        Guid id,
        string accountNumber,
        Guid customerId,
        AccountType accountType,
        string currency,
        decimal balance,
        decimal dailyDebitLimit,
        bool isActive,
        DateTime createdAtUtc,
        DateTime lastUpdatedAtUtc)
    {
        Id = id;
        AccountNumber = accountNumber;
        CustomerId = customerId;
        AccountType = accountType;
        Currency = currency;
        Balance = balance;
        DailyDebitLimit = dailyDebitLimit;
        IsActive = isActive;
        CreatedAtUtc = createdAtUtc;
        LastUpdatedAtUtc = lastUpdatedAtUtc;
    }

    public Guid Id { get; }
    public string AccountNumber { get; }
    public Guid CustomerId { get; }
    public AccountType AccountType { get; }
    public string Currency { get; }
    public decimal Balance { get; private set; }
    public decimal DailyDebitLimit { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; }
    public DateTime LastUpdatedAtUtc { get; private set; }

    public static Account OpenNew(
        Guid customerId,
        string accountNumber,
        AccountType accountType,
        string currency,
        decimal dailyDebitLimit,
        DateTime createdAtUtc)
    {
        return new Account(
            Guid.NewGuid(),
            accountNumber,
            customerId,
            accountType,
            currency.ToUpperInvariant(),
            0m,
            dailyDebitLimit,
            true,
            createdAtUtc,
            createdAtUtc);
    }

    public void Credit(decimal amount, DateTime utcNow)
    {
        EnsureActive();
        EnsurePositiveAmount(amount);
        Balance += amount;
        LastUpdatedAtUtc = utcNow;
    }

    public void Debit(decimal amount, DateTime utcNow)
    {
        EnsureActive();
        EnsurePositiveAmount(amount);

        if (Balance < amount)
        {
            throw new InvalidOperationException("Insufficient funds.");
        }

        Balance -= amount;
        LastUpdatedAtUtc = utcNow;
    }

    public void UpdateDailyDebitLimit(decimal newLimit, DateTime utcNow)
    {
        if (newLimit <= 0)
        {
            throw new InvalidOperationException("Daily debit limit must be positive.");
        }

        DailyDebitLimit = newLimit;
        LastUpdatedAtUtc = utcNow;
    }

    public void Freeze(DateTime utcNow)
    {
        IsActive = false;
        LastUpdatedAtUtc = utcNow;
    }

    private void EnsureActive()
    {
        if (!IsActive)
        {
            throw new InvalidOperationException("Account is not active.");
        }
    }

    private static void EnsurePositiveAmount(decimal amount)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("Amount must be greater than zero.");
        }
    }
}
