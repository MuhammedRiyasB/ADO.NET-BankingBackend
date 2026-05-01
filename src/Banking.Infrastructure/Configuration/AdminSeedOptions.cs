using Banking.Domain.Enums;

namespace Banking.Infrastructure.Configuration;

public sealed class AdminSeedOptions
{
    public const string SectionName = "AdminSeed";

    public string Username { get; set; } = "Admin";
    public string Password { get; set; } = "Admin@123";
    public UserRole Role { get; set; } = UserRole.Admin;
}
