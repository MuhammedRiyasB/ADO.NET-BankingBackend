namespace Banking.Application.DTOs.Customer;

public sealed record CustomerResponse(
    Guid Id,
    string FullName,
    string Email,
    string PhoneNumber,
    string KycStatus,
    bool IsActive,
    DateTime CreatedAtUtc);
