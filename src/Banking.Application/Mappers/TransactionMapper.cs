using Banking.Application.DTOs.Transaction;
using Banking.Domain.Entities;

namespace Banking.Application.Mappers;

/// <summary>
/// Maps transaction domain entities to DTOs.
/// </summary>
public sealed class TransactionMapper : ITransactionMapper
{
    public TransactionResponse Map(LedgerEntry entry, string currency)
    {
        return new TransactionResponse(
            entry.AccountId,
            entry.Reference,
            entry.EntryType,
            entry.Amount,
            entry.BalanceAfter,
            currency,
            entry.Narrative,
            entry.CreatedAtUtc);
    }
}
