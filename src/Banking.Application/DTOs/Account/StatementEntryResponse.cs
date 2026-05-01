using Banking.Domain.Enums;

namespace Banking.Application.DTOs.Account;

public sealed record StatementEntryResponse(
    Guid Id,
    Guid AccountId,
    string Reference,
    LedgerEntryType EntryType,
    decimal Amount,
    decimal BalanceAfter,
    string Narrative,
    DateTime CreatedAtUtc);
