using Banking.Application.Common;

namespace Banking.Application.Interfaces;

public interface IPasswordHasher
{
    PasswordHashResult HashPassword(string password);
    bool VerifyPassword(string password, byte[] expectedHash, byte[] salt, int iterations);
}
