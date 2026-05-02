namespace Banking.Infrastructure.Data;

internal sealed class MigrationScript
{
    public MigrationScript(string version, string sql)
    {
        Version = version;
        Sql = sql;
    }

    public string Version { get; }
    public string Sql { get; }
}
