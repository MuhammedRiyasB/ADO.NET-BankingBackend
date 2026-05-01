using Banking.Application.DTOs.Transaction;
using Banking.Domain.Entities;

namespace Banking.Application.Mappers;

/// <summary>
/// Maps transfer domain entities to DTOs.
/// </summary>
public interface ITransferMapper
{
    /// <summary>
    /// Maps a transfer entity to a transfer response DTO.
    /// </summary>
    TransferResponse Map(Transfer transfer);
}
