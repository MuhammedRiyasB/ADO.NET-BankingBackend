using Banking.Application.Common;
using Banking.Domain.Entities;

namespace Banking.Application.Interfaces;

public interface IJwtTokenService
{
    AccessTokenResult GenerateToken(AuthUser user, DateTime issuedAtUtc);
}
