using Banking.Domain.Entities;

namespace Banking.Application.Interfaces;

public interface IUserRepository
{
    Task<AuthUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken);
    Task<bool> AnyUsersAsync(CancellationToken cancellationToken);
    Task AddAsync(AuthUser user, CancellationToken cancellationToken);
}
