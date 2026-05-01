namespace Banking.Application.DTOs.Transaction;

public sealed record DepositRequest(
    Guid AccountId,
    decimal Amount,
    string Narrative,
    string Reference);
