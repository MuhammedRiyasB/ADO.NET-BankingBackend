namespace Banking.Application.Common;

public sealed record PasswordHashResult(byte[] Hash, byte[] Salt, int Iterations);
