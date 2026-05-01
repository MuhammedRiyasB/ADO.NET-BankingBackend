using Banking.Domain.Enums;

namespace Banking.Application.DTOs.Account;

public sealed record AccountResponse(
    Guid Id,
    string AccountNumber,
    Guid CustomerId,
    AccountType AccountType,
    string Currency,
    decimal Balance,
    decimal DailyDebitLimit,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime LastUpdatedAtUtc);
