using Banking.Application.Common;
using Banking.Application.Interfaces;
using Banking.Domain.Entities;
using Banking.Domain.Enums;
using Banking.Infrastructure.Data;
using Microsoft.Data.SqlClient;

namespace Banking.Infrastructure.Repositories;

internal sealed class CustomerRepository : SqlRepositoryBase, ICustomerRepository
{
    public CustomerRepository(SqlUnitOfWork unitOfWork)
        : base(unitOfWork)
    {
    }

    public Task<Customer?> GetByIdAsync(Guid customerId, CancellationToken cancellationToken)
    {
        return WithConnectionAsync(async connection =>
        {
            await using var command = CreateCommand(@"
SELECT Id, FullName, Email, PhoneNumber, KycStatus, IsActive, CreatedAtUtc
FROM dbo.Customers
WHERE Id = @Id;", connection);
            command.Parameters.Add(new SqlParameter("@Id", System.Data.SqlDbType.UniqueIdentifier) { Value = customerId });

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return Map(reader);
        }, cancellationToken);
    }

    public Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        return WithConnectionAsync(async connection =>
        {
            await using var command = CreateCommand(@"
SELECT Id, FullName, Email, PhoneNumber, KycStatus, IsActive, CreatedAtUtc
FROM dbo.Customers
WHERE Email = @Email;", connection);
            command.Parameters.Add(new SqlParameter("@Email", System.Data.SqlDbType.NVarChar, 200) { Value = email.Trim().ToLowerInvariant() });

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return Map(reader);
        }, cancellationToken);
    }

    public Task AddAsync(Customer customer, CancellationToken cancellationToken)
    {
        return WithConnectionAsync(async connection =>
        {
            await using var command = CreateCommand(@"
INSERT INTO dbo.Customers
(
    Id,
    FullName,
    Email,
    PhoneNumber,
    KycStatus,
    IsActive,
    CreatedAtUtc
)
VALUES
(
    @Id,
    @FullName,
    @Email,
    @PhoneNumber,
    @KycStatus,
    @IsActive,
    @CreatedAtUtc
);", connection);

            command.Parameters.Add(new SqlParameter("@Id", System.Data.SqlDbType.UniqueIdentifier) { Value = customer.Id });
            command.Parameters.Add(new SqlParameter("@FullName", System.Data.SqlDbType.NVarChar, 200) { Value = customer.FullName });
            command.Parameters.Add(new SqlParameter("@Email", System.Data.SqlDbType.NVarChar, 200) { Value = customer.Email });
            command.Parameters.Add(new SqlParameter("@PhoneNumber", System.Data.SqlDbType.NVarChar, 30) { Value = customer.PhoneNumber });
            command.Parameters.Add(new SqlParameter("@KycStatus", System.Data.SqlDbType.Int) { Value = (int)customer.KycStatus });
            command.Parameters.Add(new SqlParameter("@IsActive", System.Data.SqlDbType.Bit) { Value = customer.IsActive });
            command.Parameters.Add(new SqlParameter("@CreatedAtUtc", System.Data.SqlDbType.DateTime2) { Value = customer.CreatedAtUtc });

            try
            {
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (SqlException ex) when (ex.IsUniqueConstraintViolation())
            {
                throw new DuplicateResourceException("customer", "email", customer.Email, ex);
            }
        }, cancellationToken);
    }

    private static Customer Map(SqlDataReader reader)
    {
        return new Customer(
            reader.GetGuid(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            (KycStatus)reader.GetInt32(4),
            reader.GetBoolean(5),
            reader.GetDateTime(6));
    }
}
