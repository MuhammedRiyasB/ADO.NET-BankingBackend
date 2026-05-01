namespace Banking.Application.DTOs.Transaction;

public sealed record WithdrawRequest(
    Guid AccountId,
    decimal Amount,
    string Narrative,
    string Reference);
