using Banking.Application.Common;
using Banking.Application.Interfaces;
using Banking.Domain.Entities;
using Banking.Domain.Enums;
using Banking.Infrastructure.Data;
using Microsoft.Data.SqlClient;

namespace Banking.Infrastructure.Repositories;

internal sealed class AccountRepository : SqlRepositoryBase, IAccountRepository
{
    public AccountRepository(SqlUnitOfWork unitOfWork)
        : base(unitOfWork)
    {
    }

    public Task<Account?> GetByIdAsync(Guid accountId, CancellationToken cancellationToken)
    {
        return WithConnectionAsync(async connection =>
        {
            await using var command = CreateCommand(@"
SELECT Id, AccountNumber, CustomerId, AccountType, Currency, Balance, DailyDebitLimit, IsActive, CreatedAtUtc, LastUpdatedAtUtc
FROM dbo.Accounts
WHERE Id = @Id;", connection);
            command.Parameters.Add(new SqlParameter("@Id", System.Data.SqlDbType.UniqueIdentifier) { Value = accountId });

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return Map(reader);
        }, cancellationToken);
    }

    public async Task<Account?> GetByIdForUpdateAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        return await WithConnectionAsync(async (connection, sqlTransaction) =>
        {

        await using var command = CreateCommand(@"
SELECT Id, AccountNumber, CustomerId, AccountType, Currency, Balance, DailyDebitLimit, IsActive, CreatedAtUtc, LastUpdatedAtUtc
FROM dbo.Accounts WITH (UPDLOCK, HOLDLOCK, ROWLOCK)
WHERE Id = @Id;", connection, sqlTransaction);
        command.Parameters.Add(new SqlParameter("@Id", System.Data.SqlDbType.UniqueIdentifier) { Value = accountId });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

            return Map(reader);
        }, cancellationToken);
    }

    public Task<Account?> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken)
    {
        return WithConnectionAsync(async connection =>
        {
            await using var command = CreateCommand(@"
SELECT Id, AccountNumber, CustomerId, AccountType, Currency, Balance, DailyDebitLimit, IsActive, CreatedAtUtc, LastUpdatedAtUtc
FROM dbo.Accounts
WHERE AccountNumber = @AccountNumber;", connection);
            command.Parameters.Add(new SqlParameter("@AccountNumber", System.Data.SqlDbType.NVarChar, 30) { Value = accountNumber.Trim() });

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return Map(reader);
        }, cancellationToken);
    }

    public Task AddAsync(Account account, CancellationToken cancellationToken)
    {
        return WithConnectionAsync(async connection =>
        {
            await using var command = CreateCommand(@"
INSERT INTO dbo.Accounts
(
    Id,
    AccountNumber,
    CustomerId,
    AccountType,
    Currency,
    Balance,
    DailyDebitLimit,
    IsActive,
    CreatedAtUtc,
    LastUpdatedAtUtc
)
VALUES
(
    @Id,
    @AccountNumber,
    @CustomerId,
    @AccountType,
    @Currency,
    @Balance,
    @DailyDebitLimit,
    @IsActive,
    @CreatedAtUtc,
    @LastUpdatedAtUtc
);", connection);

            command.Parameters.Add(new SqlParameter("@Id", System.Data.SqlDbType.UniqueIdentifier) { Value = account.Id });
            command.Parameters.Add(new SqlParameter("@AccountNumber", System.Data.SqlDbType.NVarChar, 30) { Value = account.AccountNumber });
            command.Parameters.Add(new SqlParameter("@CustomerId", System.Data.SqlDbType.UniqueIdentifier) { Value = account.CustomerId });
            command.Parameters.Add(new SqlParameter("@AccountType", System.Data.SqlDbType.Int) { Value = (int)account.AccountType });
            command.Parameters.Add(new SqlParameter("@Currency", System.Data.SqlDbType.Char, 3) { Value = account.Currency });
            command.Parameters.Add(new SqlParameter("@Balance", System.Data.SqlDbType.Decimal) { Value = account.Balance, Precision = 18, Scale = 2 });
            command.Parameters.Add(new SqlParameter("@DailyDebitLimit", System.Data.SqlDbType.Decimal) { Value = account.DailyDebitLimit, Precision = 18, Scale = 2 });
            command.Parameters.Add(new SqlParameter("@IsActive", System.Data.SqlDbType.Bit) { Value = account.IsActive });
            command.Parameters.Add(new SqlParameter("@CreatedAtUtc", System.Data.SqlDbType.DateTime2) { Value = account.CreatedAtUtc });
            command.Parameters.Add(new SqlParameter("@LastUpdatedAtUtc", System.Data.SqlDbType.DateTime2) { Value = account.LastUpdatedAtUtc });

            try
            {
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (SqlException ex) when (ex.IsUniqueConstraintViolation())
            {
                throw new DuplicateResourceException("account", "account number", account.AccountNumber, ex);
            }
        }, cancellationToken);
    }

    public async Task UpdateAsync(Account account, CancellationToken cancellationToken)
    {
        await WithConnectionAsync(async (connection, sqlTransaction) =>
        {

        await using var command = CreateCommand(@"
UPDATE dbo.Accounts
SET
    Balance = @Balance,
    DailyDebitLimit = @DailyDebitLimit,
    IsActive = @IsActive,
    LastUpdatedAtUtc = @LastUpdatedAtUtc
WHERE Id = @Id;", connection, sqlTransaction);

        command.Parameters.Add(new SqlParameter("@Id", System.Data.SqlDbType.UniqueIdentifier) { Value = account.Id });
        command.Parameters.Add(new SqlParameter("@Balance", System.Data.SqlDbType.Decimal) { Value = account.Balance, Precision = 18, Scale = 2 });
        command.Parameters.Add(new SqlParameter("@DailyDebitLimit", System.Data.SqlDbType.Decimal) { Value = account.DailyDebitLimit, Precision = 18, Scale = 2 });
        command.Parameters.Add(new SqlParameter("@IsActive", System.Data.SqlDbType.Bit) { Value = account.IsActive });
        command.Parameters.Add(new SqlParameter("@LastUpdatedAtUtc", System.Data.SqlDbType.DateTime2) { Value = account.LastUpdatedAtUtc });

        await command.ExecuteNonQueryAsync(cancellationToken);
        }, cancellationToken);
    }

    private static Account Map(SqlDataReader reader)
    {
        return new Account(
            reader.GetGuid(0),
            reader.GetString(1),
            reader.GetGuid(2),
            (AccountType)reader.GetInt32(3),
            reader.GetString(4),
            reader.GetDecimal(5),
            reader.GetDecimal(6),
            reader.GetBoolean(7),
            reader.GetDateTime(8),
            reader.GetDateTime(9));
    }
}
