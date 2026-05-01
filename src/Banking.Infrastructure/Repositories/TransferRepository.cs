using Banking.Application.Interfaces;
using Banking.Domain.Entities;
using Banking.Domain.Enums;
using Banking.Infrastructure.Data;
using Microsoft.Data.SqlClient;

namespace Banking.Infrastructure.Repositories;

internal sealed class TransferRepository : SqlRepositoryBase, ITransferRepository
{
    public TransferRepository(SqlUnitOfWork unitOfWork)
        : base(unitOfWork)
    {
    }

    public Task<Transfer?> GetByExternalReferenceAsync(string externalReference, CancellationToken cancellationToken)
    {
        return WithConnectionAsync(async connection =>
        {
            await using var command = CreateCommand(@"
SELECT Id, ExternalReference, FromAccountId, ToAccountId, Amount, Currency, Narrative, Status, CreatedAtUtc, CompletedAtUtc
FROM dbo.Transfers
WHERE ExternalReference = @ExternalReference;", connection);
            command.Parameters.Add(new SqlParameter("@ExternalReference", System.Data.SqlDbType.NVarChar, 64)
            {
                Value = externalReference.Trim().ToUpperInvariant()
            });

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return Map(reader);
        }, cancellationToken);
    }

    public async Task AddAsync(Transfer transfer, CancellationToken cancellationToken)
    {
        await WithConnectionAsync(async (connection, sqlTransaction) =>
        {

        await using var command = CreateCommand(@"
INSERT INTO dbo.Transfers
(
    Id,
    ExternalReference,
    FromAccountId,
    ToAccountId,
    Amount,
    Currency,
    Narrative,
    Status,
    CreatedAtUtc,
    CompletedAtUtc
)
VALUES
(
    @Id,
    @ExternalReference,
    @FromAccountId,
    @ToAccountId,
    @Amount,
    @Currency,
    @Narrative,
    @Status,
    @CreatedAtUtc,
    @CompletedAtUtc
);", connection, sqlTransaction);

        command.Parameters.Add(new SqlParameter("@Id", System.Data.SqlDbType.UniqueIdentifier) { Value = transfer.Id });
        command.Parameters.Add(new SqlParameter("@ExternalReference", System.Data.SqlDbType.NVarChar, 64) { Value = transfer.ExternalReference });
        command.Parameters.Add(new SqlParameter("@FromAccountId", System.Data.SqlDbType.UniqueIdentifier) { Value = transfer.FromAccountId });
        command.Parameters.Add(new SqlParameter("@ToAccountId", System.Data.SqlDbType.UniqueIdentifier) { Value = transfer.ToAccountId });
        command.Parameters.Add(new SqlParameter("@Amount", System.Data.SqlDbType.Decimal) { Value = transfer.Amount, Precision = 18, Scale = 2 });
        command.Parameters.Add(new SqlParameter("@Currency", System.Data.SqlDbType.Char, 3) { Value = transfer.Currency });
        command.Parameters.Add(new SqlParameter("@Narrative", System.Data.SqlDbType.NVarChar, 300) { Value = transfer.Narrative });
        command.Parameters.Add(new SqlParameter("@Status", System.Data.SqlDbType.Int) { Value = (int)transfer.Status });
        command.Parameters.Add(new SqlParameter("@CreatedAtUtc", System.Data.SqlDbType.DateTime2) { Value = transfer.CreatedAtUtc });
        command.Parameters.Add(new SqlParameter("@CompletedAtUtc", System.Data.SqlDbType.DateTime2)
        {
            Value = transfer.CompletedAtUtc ?? (object)DBNull.Value
        });

        await command.ExecuteNonQueryAsync(cancellationToken);
        }, cancellationToken);
    }

    public async Task UpdateAsync(Transfer transfer, CancellationToken cancellationToken)
    {
        await WithConnectionAsync(async (connection, sqlTransaction) =>
        {
        await using var command = CreateCommand(@"
UPDATE dbo.Transfers
SET
    Status = @Status,
    CompletedAtUtc = @CompletedAtUtc
WHERE Id = @Id;", connection, sqlTransaction);

        command.Parameters.Add(new SqlParameter("@Id", System.Data.SqlDbType.UniqueIdentifier) { Value = transfer.Id });
        command.Parameters.Add(new SqlParameter("@Status", System.Data.SqlDbType.Int) { Value = (int)transfer.Status });
        command.Parameters.Add(new SqlParameter("@CompletedAtUtc", System.Data.SqlDbType.DateTime2)
        {
            Value = transfer.CompletedAtUtc ?? (object)DBNull.Value
        });

        await command.ExecuteNonQueryAsync(cancellationToken);
        }, cancellationToken);
    }

    private static Transfer Map(SqlDataReader reader)
    {
        return new Transfer(
            reader.GetGuid(0),
            reader.GetString(1),
            reader.GetGuid(2),
            reader.GetGuid(3),
            reader.GetDecimal(4),
            reader.GetString(5),
            reader.GetString(6),
            (TransferStatus)reader.GetInt32(7),
            reader.GetDateTime(8),
            reader.IsDBNull(9) ? null : reader.GetDateTime(9));
    }
}
