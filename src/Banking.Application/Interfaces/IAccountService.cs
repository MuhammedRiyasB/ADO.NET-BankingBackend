using Banking.Application.Common;
using Banking.Application.DTOs.Account;

namespace Banking.Application.Interfaces;

public interface IAccountService
{
    Task<Result<AccountResponse>> OpenAccountAsync(OpenAccountRequest request, CancellationToken cancellationToken);
    Task<Result<AccountResponse>> GetByIdAsync(Guid accountId, CancellationToken cancellationToken);
}
