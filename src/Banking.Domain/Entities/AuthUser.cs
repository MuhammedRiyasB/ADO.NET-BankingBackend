using Banking.Domain.Enums;

namespace Banking.Domain.Entities;

public sealed class AuthUser
{
    public AuthUser(
        Guid id,
        string username,
        byte[] passwordHash,
        byte[] passwordSalt,
        int passwordIterations,
        UserRole role,
        bool isActive,
        DateTime createdAtUtc)
    {
        Id = id;
        Username = username;
        PasswordHash = passwordHash;
        PasswordSalt = passwordSalt;
        PasswordIterations = passwordIterations;
        Role = role;
        IsActive = isActive;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }
    public string Username { get; }
    public byte[] PasswordHash { get; }
    public byte[] PasswordSalt { get; }
    public int PasswordIterations { get; }
    public UserRole Role { get; }
    public bool IsActive { get; }
    public DateTime CreatedAtUtc { get; }
}
