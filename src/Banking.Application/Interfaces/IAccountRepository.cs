using Banking.Domain.Entities;

namespace Banking.Application.Interfaces;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid accountId, CancellationToken cancellationToken);
    Task<Account?> GetByIdForUpdateAsync(Guid accountId, CancellationToken cancellationToken);
    Task<Account?> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken);
    Task AddAsync(Account account, CancellationToken cancellationToken);
    Task UpdateAsync(Account account, CancellationToken cancellationToken);
}
