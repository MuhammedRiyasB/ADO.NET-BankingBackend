using Banking.Domain.Enums;

namespace Banking.Domain.Entities;

public sealed class Customer
{
    public Customer(
        Guid id,
        string fullName,
        string email,
        string phoneNumber,
        KycStatus kycStatus,
        bool isActive,
        DateTime createdAtUtc)
    {
        Id = id;
        FullName = fullName;
        Email = email;
        PhoneNumber = phoneNumber;
        KycStatus = kycStatus;
        IsActive = isActive;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }
    public string FullName { get; private set; }
    public string Email { get; private set; }
    public string PhoneNumber { get; private set; }
    public KycStatus KycStatus { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; }

    public static Customer CreateNew(string fullName, string email, string phoneNumber, DateTime createdAtUtc)
    {
        return new Customer(
            Guid.NewGuid(),
            fullName.Trim(),
            email.Trim().ToLowerInvariant(),
            phoneNumber.Trim(),
            KycStatus.Pending,
            true,
            createdAtUtc);
    }

    public void VerifyKyc()
    {
        KycStatus = KycStatus.Verified;
    }

    public void RejectKyc()
    {
        KycStatus = KycStatus.Rejected;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
