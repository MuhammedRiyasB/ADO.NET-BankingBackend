using Banking.Application.Common;
using Banking.Application.Interfaces;
using Banking.Domain.Entities;
using Banking.Domain.Enums;
using Banking.Infrastructure.Data;
using Microsoft.Data.SqlClient;

namespace Banking.Infrastructure.Repositories;

internal sealed class LedgerRepository : SqlRepositoryBase, ILedgerRepository
{
    public LedgerRepository(SqlUnitOfWork unitOfWork)
        : base(unitOfWork)
    {
    }

    public Task<bool> ReferenceExistsAsync(string reference, CancellationToken cancellationToken)
    {
        return WithConnectionAsync(async connection =>
        {
            await using var command = CreateCommand(
                "SELECT COUNT(1) FROM dbo.LedgerEntries WHERE Reference = @Reference;",
                connection);
            command.Parameters.Add(new SqlParameter("@Reference", System.Data.SqlDbType.NVarChar, 80) { Value = reference.Trim().ToUpperInvariant() });

            var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
            return count > 0;
        }, cancellationToken);
    }

    public async Task AddAsync(LedgerEntry entry, CancellationToken cancellationToken)
    {
        await WithConnectionAsync(async (connection, sqlTransaction) =>
        {

        await using var command = CreateCommand(@"
INSERT INTO dbo.LedgerEntries
(
    Id,
    AccountId,
    Reference,
    EntryType,
    Amount,
    BalanceAfter,
    Narrative,
    CreatedAtUtc
)
VALUES
(
    @Id,
    @AccountId,
    @Reference,
    @EntryType,
    @Amount,
    @BalanceAfter,
    @Narrative,
    @CreatedAtUtc
);", connection, sqlTransaction);

        command.Parameters.Add(new SqlParameter("@Id", System.Data.SqlDbType.UniqueIdentifier) { Value = entry.Id });
        command.Parameters.Add(new SqlParameter("@AccountId", System.Data.SqlDbType.UniqueIdentifier) { Value = entry.AccountId });
        command.Parameters.Add(new SqlParameter("@Reference", System.Data.SqlDbType.NVarChar, 80) { Value = entry.Reference });
        command.Parameters.Add(new SqlParameter("@EntryType", System.Data.SqlDbType.Int) { Value = (int)entry.EntryType });
        command.Parameters.Add(new SqlParameter("@Amount", System.Data.SqlDbType.Decimal) { Value = entry.Amount, Precision = 18, Scale = 2 });
        command.Parameters.Add(new SqlParameter("@BalanceAfter", System.Data.SqlDbType.Decimal) { Value = entry.BalanceAfter, Precision = 18, Scale = 2 });
        command.Parameters.Add(new SqlParameter("@Narrative", System.Data.SqlDbType.NVarChar, 300) { Value = entry.Narrative });
        command.Parameters.Add(new SqlParameter("@CreatedAtUtc", System.Data.SqlDbType.DateTime2) { Value = entry.CreatedAtUtc });

            try
            {
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (SqlException ex) when (ex.IsUniqueConstraintViolation())
            {
                throw new DuplicateResourceException("ledger entry", "reference", entry.Reference, ex);
            }
        }, cancellationToken);
    }

    public Task<IReadOnlyCollection<LedgerEntry>> GetStatementAsync(
        Guid accountId,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken)
    {
        return WithConnectionAsync(async connection =>
        {
            await using var command = CreateCommand(@"
SELECT Id, AccountId, Reference, EntryType, Amount, BalanceAfter, Narrative, CreatedAtUtc
FROM dbo.LedgerEntries
WHERE AccountId = @AccountId
AND (@FromUtc IS NULL OR CreatedAtUtc >= @FromUtc)
AND (@ToUtc IS NULL OR CreatedAtUtc <= @ToUtc)
ORDER BY CreatedAtUtc DESC;", connection);

            command.Parameters.Add(new SqlParameter("@AccountId", System.Data.SqlDbType.UniqueIdentifier) { Value = accountId });
            command.Parameters.Add(new SqlParameter("@FromUtc", System.Data.SqlDbType.DateTime2) { Value = fromUtc ?? (object)DBNull.Value });
            command.Parameters.Add(new SqlParameter("@ToUtc", System.Data.SqlDbType.DateTime2) { Value = toUtc ?? (object)DBNull.Value });

            var entries = new List<LedgerEntry>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                entries.Add(Map(reader));
            }

            return (IReadOnlyCollection<LedgerEntry>)entries;
        }, cancellationToken);
    }

    public async Task<decimal> GetTotalDebitsForPeriodAsync(
        Guid accountId,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken)
    {
        return await WithConnectionAsync(async (connection, sqlTransaction) =>
        {

        await using var command = CreateCommand(@"
SELECT ISNULL(SUM(Amount), 0)
FROM dbo.LedgerEntries
WHERE AccountId = @AccountId
AND EntryType = @EntryType
AND CreatedAtUtc >= @StartUtc
AND CreatedAtUtc < @EndUtc;", connection, sqlTransaction);

        command.Parameters.Add(new SqlParameter("@AccountId", System.Data.SqlDbType.UniqueIdentifier) { Value = accountId });
        command.Parameters.Add(new SqlParameter("@EntryType", System.Data.SqlDbType.Int) { Value = (int)LedgerEntryType.Debit });
        command.Parameters.Add(new SqlParameter("@StartUtc", System.Data.SqlDbType.DateTime2) { Value = startUtc });
        command.Parameters.Add(new SqlParameter("@EndUtc", System.Data.SqlDbType.DateTime2) { Value = endUtc });

        var amount = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToDecimal(amount);
        }, cancellationToken);
    }

    private static LedgerEntry Map(SqlDataReader reader)
    {
        return new LedgerEntry(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetString(2),
            (LedgerEntryType)reader.GetInt32(3),
            reader.GetDecimal(4),
            reader.GetDecimal(5),
            reader.GetString(6),
            reader.GetDateTime(7));
    }
}
