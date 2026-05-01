namespace Banking.Application.DTOs.Transaction;

public sealed record TransferResponse(
    Guid TransferId,
    string ExternalReference,
    Guid FromAccountId,
    Guid ToAccountId,
    decimal Amount,
    string Currency,
    string Status,
    string Narrative,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc);
