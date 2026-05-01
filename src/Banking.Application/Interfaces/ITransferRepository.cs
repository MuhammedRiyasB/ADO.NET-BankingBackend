using Banking.Domain.Entities;

namespace Banking.Application.Interfaces;

public interface ITransferRepository
{
    Task<Transfer?> GetByExternalReferenceAsync(string externalReference, CancellationToken cancellationToken);
    Task AddAsync(Transfer transfer, CancellationToken cancellationToken);
    Task UpdateAsync(Transfer transfer, CancellationToken cancellationToken);
}
