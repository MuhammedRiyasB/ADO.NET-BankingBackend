using Banking.Domain.Enums;

namespace Banking.Domain.Entities;

public sealed class LedgerEntry
{
    public LedgerEntry(
        Guid id,
        Guid accountId,
        string reference,
        LedgerEntryType entryType,
        decimal amount,
        decimal balanceAfter,
        string narrative,
        DateTime createdAtUtc)
    {
        Id = id;
        AccountId = accountId;
        Reference = reference;
        EntryType = entryType;
        Amount = amount;
        BalanceAfter = balanceAfter;
        Narrative = narrative;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }
    public Guid AccountId { get; }
    public string Reference { get; }
    public LedgerEntryType EntryType { get; }
    public decimal Amount { get; }
    public decimal BalanceAfter { get; }
    public string Narrative { get; }
    public DateTime CreatedAtUtc { get; }

    public static LedgerEntry Create(
        Guid accountId,
        string reference,
        LedgerEntryType entryType,
        decimal amount,
        decimal balanceAfter,
        string narrative,
        DateTime createdAtUtc)
    {
        return new LedgerEntry(
            Guid.NewGuid(),
            accountId,
            reference.Trim(),
            entryType,
            amount,
            balanceAfter,
            narrative.Trim(),
            createdAtUtc);
    }
}
