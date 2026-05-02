using System.Reflection;
using Banking.Application.Interfaces;
using Banking.Infrastructure.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Banking.Infrastructure.Data;

internal sealed class DatabaseInitializer : IDatabaseInitializer
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly IOptions<SqlServerOptions> _sqlServerOptions;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IOptions<AdminSeedOptions> _adminSeedOptions;

    public DatabaseInitializer(
        ISqlConnectionFactory connectionFactory,
        IOptions<SqlServerOptions> sqlServerOptions,
        IPasswordHasher passwordHasher,
        IOptions<AdminSeedOptions> adminSeedOptions)
    {
        _connectionFactory = connectionFactory;
        _sqlServerOptions = sqlServerOptions;
        _passwordHasher = passwordHasher;
        _adminSeedOptions = adminSeedOptions;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await EnsureDatabaseExistsAsync(cancellationToken);

        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        await EnsureMigrationTableAsync(connection, cancellationToken);
        foreach (var migration in GetMigrationScripts())
        {
            if (await MigrationAlreadyAppliedAsync(connection, migration.Version, cancellationToken))
            {
                continue;
            }

            await ApplyMigrationAsync(connection, migration, cancellationToken);
        }

        await EnsureAdminUserAsync(connection, cancellationToken);
    }

    private async Task EnsureDatabaseExistsAsync(CancellationToken cancellationToken)
    {
        var baseBuilder = new SqlConnectionStringBuilder(_sqlServerOptions.Value.ConnectionString);
        if (string.IsNullOrWhiteSpace(baseBuilder.InitialCatalog))
        {
            return;
        }

        var databaseName = baseBuilder.InitialCatalog;
        var masterBuilder = new SqlConnectionStringBuilder(baseBuilder.ConnectionString)
        {
            InitialCatalog = "master"
        };

        await using var masterConnection = new SqlConnection(masterBuilder.ConnectionString);
        await masterConnection.OpenAsync(cancellationToken);

        await using var createDatabaseCommand = masterConnection.CreateCommand();
        createDatabaseCommand.CommandText = @"
IF DB_ID(@DatabaseName) IS NULL
BEGIN
    DECLARE @sql NVARCHAR(MAX) = N'CREATE DATABASE ' + QUOTENAME(@DatabaseName);
    EXEC (@sql);
END;";
        createDatabaseCommand.Parameters.Add(new SqlParameter("@DatabaseName", System.Data.SqlDbType.NVarChar, 128) { Value = databaseName });

        await createDatabaseCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task EnsureAdminUserAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        await using var countCommand = connection.CreateCommand();
        countCommand.CommandText = "SELECT COUNT(1) FROM dbo.Users;";
        var count = Convert.ToInt32(await countCommand.ExecuteScalarAsync(cancellationToken));
        if (count > 0)
        {
            return;
        }

        var seed = _adminSeedOptions.Value;
        var hashedPassword = _passwordHasher.HashPassword(seed.Password);

        await using var insertCommand = connection.CreateCommand();
        insertCommand.CommandText = @"
INSERT INTO dbo.Users
(
    Id,
    Username,
    PasswordHash,
    PasswordSalt,
    PasswordIterations,
    Role,
    IsActive,
    CreatedAtUtc
)
VALUES
(
    @Id,
    @Username,
    @PasswordHash,
    @PasswordSalt,
    @PasswordIterations,
    @Role,
    @IsActive,
    @CreatedAtUtc
);";

        insertCommand.Parameters.Add(new SqlParameter("@Id", System.Data.SqlDbType.UniqueIdentifier) { Value = Guid.NewGuid() });
        insertCommand.Parameters.Add(new SqlParameter("@Username", System.Data.SqlDbType.NVarChar, 100) { Value = seed.Username.Trim() });
        insertCommand.Parameters.Add(new SqlParameter("@PasswordHash", System.Data.SqlDbType.VarBinary, -1) { Value = hashedPassword.Hash });
        insertCommand.Parameters.Add(new SqlParameter("@PasswordSalt", System.Data.SqlDbType.VarBinary, 64) { Value = hashedPassword.Salt });
        insertCommand.Parameters.Add(new SqlParameter("@PasswordIterations", System.Data.SqlDbType.Int) { Value = hashedPassword.Iterations });
        insertCommand.Parameters.Add(new SqlParameter("@Role", System.Data.SqlDbType.Int) { Value = (int)seed.Role });
        insertCommand.Parameters.Add(new SqlParameter("@IsActive", System.Data.SqlDbType.Bit) { Value = true });
        insertCommand.Parameters.Add(new SqlParameter("@CreatedAtUtc", System.Data.SqlDbType.DateTime2) { Value = DateTime.UtcNow });

        await insertCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task EnsureMigrationTableAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = @"
IF OBJECT_ID(N'dbo.__SchemaMigrations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.__SchemaMigrations
    (
        Version NVARCHAR(200) NOT NULL PRIMARY KEY,
        AppliedAtUtc DATETIME2 NOT NULL
    );
END;";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<bool> MigrationAlreadyAppliedAsync(
        SqlConnection connection,
        string version,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM dbo.__SchemaMigrations WHERE Version = @Version;";
        command.Parameters.Add(new SqlParameter("@Version", System.Data.SqlDbType.NVarChar, 200) { Value = version });

        var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        return count > 0;
    }

    private static async Task ApplyMigrationAsync(
        SqlConnection connection,
        MigrationScript migration,
        CancellationToken cancellationToken)
    {
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var migrationCommand = connection.CreateCommand();
            migrationCommand.Transaction = transaction;
            migrationCommand.CommandText = migration.Sql;
            await migrationCommand.ExecuteNonQueryAsync(cancellationToken);

            await using var recordCommand = connection.CreateCommand();
            recordCommand.Transaction = transaction;
            recordCommand.CommandText = @"
INSERT INTO dbo.__SchemaMigrations (Version, AppliedAtUtc)
VALUES (@Version, @AppliedAtUtc);";
            recordCommand.Parameters.Add(new SqlParameter("@Version", System.Data.SqlDbType.NVarChar, 200) { Value = migration.Version });
            recordCommand.Parameters.Add(new SqlParameter("@AppliedAtUtc", System.Data.SqlDbType.DateTime2) { Value = DateTime.UtcNow });
            await recordCommand.ExecuteNonQueryAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            if (transaction.Connection is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            throw;
        }
    }

    private static IReadOnlyCollection<MigrationScript> GetMigrationScripts()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(name => name.Contains(".Data.Migrations.", StringComparison.Ordinal) && name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        var migrations = new List<MigrationScript>(resourceNames.Length);
        foreach (var resourceName in resourceNames)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Unable to load migration resource '{resourceName}'.");
            using var reader = new StreamReader(stream);
            var sql = reader.ReadToEnd();
            var markerIndex = resourceName.LastIndexOf(".Data.Migrations.", StringComparison.Ordinal);
            var fileName = resourceName[(markerIndex + ".Data.Migrations.".Length)..];
            var version = Path.GetFileNameWithoutExtension(fileName);
            migrations.Add(new MigrationScript(version, sql));
        }

        return migrations;
    }
}
