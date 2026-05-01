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

        await using (var createSchemaCommand = connection.CreateCommand())
        {
            createSchemaCommand.CommandText = GetSchemaScript();
            await createSchemaCommand.ExecuteNonQueryAsync(cancellationToken);
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

    private static string GetSchemaScript()
    {
        return @"
IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        Username NVARCHAR(100) NOT NULL UNIQUE,
        PasswordHash VARBINARY(MAX) NOT NULL,
        PasswordSalt VARBINARY(64) NOT NULL,
        PasswordIterations INT NOT NULL,
        Role INT NOT NULL,
        IsActive BIT NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL
    );
END;

IF OBJECT_ID(N'dbo.Customers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Customers
    (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        FullName NVARCHAR(200) NOT NULL,
        Email NVARCHAR(200) NOT NULL UNIQUE,
        PhoneNumber NVARCHAR(30) NOT NULL,
        KycStatus INT NOT NULL,
        IsActive BIT NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL
    );
END;

IF OBJECT_ID(N'dbo.Accounts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Accounts
    (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        AccountNumber NVARCHAR(30) NOT NULL UNIQUE,
        CustomerId UNIQUEIDENTIFIER NOT NULL,
        AccountType INT NOT NULL,
        Currency CHAR(3) NOT NULL,
        Balance DECIMAL(18, 2) NOT NULL,
        DailyDebitLimit DECIMAL(18, 2) NOT NULL,
        IsActive BIT NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL,
        LastUpdatedAtUtc DATETIME2 NOT NULL,
        CONSTRAINT FK_Accounts_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(Id)
    );
    CREATE INDEX IX_Accounts_CustomerId ON dbo.Accounts(CustomerId);
END;

IF OBJECT_ID(N'dbo.Transfers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Transfers
    (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        ExternalReference NVARCHAR(64) NOT NULL UNIQUE,
        FromAccountId UNIQUEIDENTIFIER NOT NULL,
        ToAccountId UNIQUEIDENTIFIER NOT NULL,
        Amount DECIMAL(18, 2) NOT NULL,
        Currency CHAR(3) NOT NULL,
        Narrative NVARCHAR(300) NOT NULL,
        Status INT NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL,
        CompletedAtUtc DATETIME2 NULL,
        CONSTRAINT FK_Transfers_FromAccount FOREIGN KEY (FromAccountId) REFERENCES dbo.Accounts(Id),
        CONSTRAINT FK_Transfers_ToAccount FOREIGN KEY (ToAccountId) REFERENCES dbo.Accounts(Id)
    );
    CREATE INDEX IX_Transfers_FromAccountId ON dbo.Transfers(FromAccountId);
    CREATE INDEX IX_Transfers_ToAccountId ON dbo.Transfers(ToAccountId);
END;

IF OBJECT_ID(N'dbo.LedgerEntries', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LedgerEntries
    (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        AccountId UNIQUEIDENTIFIER NOT NULL,
        Reference NVARCHAR(80) NOT NULL UNIQUE,
        EntryType INT NOT NULL,
        Amount DECIMAL(18, 2) NOT NULL,
        BalanceAfter DECIMAL(18, 2) NOT NULL,
        Narrative NVARCHAR(300) NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL,
        CONSTRAINT FK_LedgerEntries_Accounts FOREIGN KEY (AccountId) REFERENCES dbo.Accounts(Id)
    );
    CREATE INDEX IX_LedgerEntries_AccountId_CreatedAt ON dbo.LedgerEntries(AccountId, CreatedAtUtc DESC);
END;
";
    }
}
