using Banking.Application.DTOs.Account;
using Banking.Domain.Entities;

namespace Banking.Application.Mappers;

/// <summary>
/// Maps account domain entities to DTOs.
/// </summary>
public sealed class AccountMapper : IAccountMapper
{
    public AccountResponse Map(Account account)
    {
        return new AccountResponse(
            account.Id,
            account.AccountNumber,
            account.CustomerId,
            account.AccountType,
            account.Currency,
            account.Balance,
            account.DailyDebitLimit,
            account.IsActive,
            account.CreatedAtUtc,
            account.LastUpdatedAtUtc);
    }

    public StatementEntryResponse MapStatement(LedgerEntry entry)
    {
        return new StatementEntryResponse(
            entry.Id,
            entry.AccountId,
            entry.Reference,
            entry.EntryType,
            entry.Amount,
            entry.BalanceAfter,
            entry.Narrative,
            entry.CreatedAtUtc);
    }
}
