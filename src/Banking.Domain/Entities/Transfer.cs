using Banking.Domain.Enums;

namespace Banking.Domain.Entities;

public sealed class Transfer
{
    public Transfer(
        Guid id,
        string externalReference,
        Guid fromAccountId,
        Guid toAccountId,
        decimal amount,
        string currency,
        string narrative,
        TransferStatus status,
        DateTime createdAtUtc,
        DateTime? completedAtUtc)
    {
        Id = id;
        ExternalReference = externalReference;
        FromAccountId = fromAccountId;
        ToAccountId = toAccountId;
        Amount = amount;
        Currency = currency;
        Narrative = narrative;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        CompletedAtUtc = completedAtUtc;
    }

    public Guid Id { get; }
    public string ExternalReference { get; }
    public Guid FromAccountId { get; }
    public Guid ToAccountId { get; }
    public decimal Amount { get; }
    public string Currency { get; }
    public string Narrative { get; }
    public TransferStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; }
    public DateTime? CompletedAtUtc { get; private set; }

    public static Transfer CreatePending(
        string externalReference,
        Account fromAccount,
        Account toAccount,
        decimal amount,
        string narrative,
        DateTime createdAtUtc)
    {
        if (!string.Equals(fromAccount.Currency, toAccount.Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Cross-currency transfers are not supported.");
        }

        return new Transfer(
            Guid.NewGuid(),
            externalReference.Trim(),
            fromAccount.Id,
            toAccount.Id,
            amount,
            fromAccount.Currency.ToUpperInvariant(),
            narrative.Trim(),
            TransferStatus.Pending,
            createdAtUtc,
            null);
    }

    public void MarkCompleted(DateTime completedAtUtc)
    {
        Status = TransferStatus.Completed;
        CompletedAtUtc = completedAtUtc;
    }

    public void MarkFailed()
    {
        Status = TransferStatus.Failed;
    }
}
