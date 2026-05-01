using System.Security.Cryptography;
using Banking.Application.Common;
using Banking.Application.Interfaces;

namespace Banking.Infrastructure.Security;

internal sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 32;
    private const int KeySize = 64;
    private const int IterationCount = 120_000;

    public PasswordHashResult HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Hash(password, salt, IterationCount);
        return new PasswordHashResult(hash, salt, IterationCount);
    }

    public bool VerifyPassword(string password, byte[] expectedHash, byte[] salt, int iterations)
    {
        var computedHash = Hash(password, salt, iterations);
        return CryptographicOperations.FixedTimeEquals(computedHash, expectedHash);
    }

    private static byte[] Hash(string password, byte[] salt, int iterations)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA512,
            KeySize);
    }
}
