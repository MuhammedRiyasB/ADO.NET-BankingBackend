using Banking.Application.DTOs.Transaction;
using Banking.Domain.Entities;

namespace Banking.Application.Mappers;

/// <summary>
/// Maps transfer domain entities to DTOs.
/// </summary>
public sealed class TransferMapper : ITransferMapper
{
    public TransferResponse Map(Transfer transfer)
    {
        return new TransferResponse(
            transfer.Id,
            transfer.ExternalReference,
            transfer.FromAccountId,
            transfer.ToAccountId,
            transfer.Amount,
            transfer.Currency,
            transfer.Status.ToString(),
            transfer.Narrative,
            transfer.CreatedAtUtc,
            transfer.CompletedAtUtc);
    }
}
