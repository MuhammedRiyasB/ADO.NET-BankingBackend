namespace Banking.Application.Common;

public sealed record AccessTokenResult(string Token, DateTime ExpiresAtUtc);
