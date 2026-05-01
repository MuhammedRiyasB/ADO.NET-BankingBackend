using Banking.Application.DTOs.Account;
using Banking.Domain.Entities;

namespace Banking.Application.Mappers;

/// <summary>
/// Maps account domain entities to DTOs.
/// </summary>
public interface IAccountMapper
{
    /// <summary>
    /// Maps an account entity to an account response DTO.
    /// </summary>
    AccountResponse Map(Account account);

    /// <summary>
    /// Maps a ledger entry to a statement entry response DTO.
    /// </summary>
    StatementEntryResponse MapStatement(LedgerEntry entry);
}
