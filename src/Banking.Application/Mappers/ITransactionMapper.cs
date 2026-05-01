using Banking.Application.DTOs.Transaction;
using Banking.Domain.Entities;

namespace Banking.Application.Mappers;

/// <summary>
/// Maps transaction domain entities to DTOs.
/// </summary>
public interface ITransactionMapper
{
    /// <summary>
    /// Maps a ledger entry to a transaction response DTO.
    /// </summary>
    TransactionResponse Map(LedgerEntry entry, string currency);
}
