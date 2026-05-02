namespace Banking.Infrastructure.Configuration;

public sealed class DatabaseInitializationOptions
{
    public const string SectionName = "DatabaseInitialization";

    public bool RunOnStartup { get; set; }
}
