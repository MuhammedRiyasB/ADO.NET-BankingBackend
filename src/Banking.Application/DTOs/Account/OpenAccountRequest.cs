using Banking.Domain.Enums;

namespace Banking.Application.DTOs.Account;

public sealed record OpenAccountRequest(
    Guid CustomerId,
    AccountType AccountType,
    string Currency,
    decimal DailyDebitLimit);
