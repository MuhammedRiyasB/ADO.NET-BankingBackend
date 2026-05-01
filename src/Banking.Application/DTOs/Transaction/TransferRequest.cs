namespace Banking.Application.DTOs.Transaction;

public sealed record TransferRequest(
    Guid FromAccountId,
    Guid ToAccountId,
    decimal Amount,
    string Narrative,
    string ExternalReference);
