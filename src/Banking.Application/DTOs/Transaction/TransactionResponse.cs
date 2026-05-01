using Banking.Domain.Enums;

namespace Banking.Application.DTOs.Transaction;

public sealed record TransactionResponse(
    Guid AccountId,
    string Reference,
    LedgerEntryType EntryType,
    decimal Amount,
    decimal BalanceAfter,
    string Currency,
    string Narrative,
    DateTime CreatedAtUtc);
