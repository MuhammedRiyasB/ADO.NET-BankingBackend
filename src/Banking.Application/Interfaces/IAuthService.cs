using Banking.Application.Common;
using Banking.Application.DTOs.Auth;

namespace Banking.Application.Interfaces;

public interface IAuthService
{
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}
