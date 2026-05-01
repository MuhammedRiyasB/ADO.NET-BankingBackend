using System.Security.Cryptography;
using Banking.Application.Interfaces;

namespace Banking.Infrastructure.Security;

internal sealed class AccountNumberGenerator : IAccountNumberGenerator
{
    public string Generate()
    {
        Span<byte> random = stackalloc byte[10];
        RandomNumberGenerator.Fill(random);

        var digits = random.ToArray().Select(x => (x % 10).ToString());
        return string.Concat(digits);
    }
}
