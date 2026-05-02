using Banking.Application.Common;
using Banking.Application.Interfaces;
using Banking.Domain.Entities;
using Banking.Domain.Enums;
using Banking.Infrastructure.Data;
using Microsoft.Data.SqlClient;

namespace Banking.Infrastructure.Repositories;

internal sealed class UserRepository : SqlRepositoryBase, IUserRepository
{
    public UserRepository(SqlUnitOfWork unitOfWork)
        : base(unitOfWork)
    {
    }

    public Task<AuthUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        return WithConnectionAsync(async connection =>
        {
            await using var command = CreateCommand(@"
SELECT Id, Username, PasswordHash, PasswordSalt, PasswordIterations, Role, IsActive, CreatedAtUtc
FROM dbo.Users
WHERE Username = @Username;", connection);
            command.Parameters.Add(new SqlParameter("@Username", System.Data.SqlDbType.NVarChar, 100)
            {
                Value = username.Trim()
            });

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return Map(reader);
        }, cancellationToken);
    }

    public Task<bool> AnyUsersAsync(CancellationToken cancellationToken)
    {
        return WithConnectionAsync(async connection =>
        {
            await using var command = CreateCommand("SELECT COUNT(1) FROM dbo.Users;", connection);
            var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
            return count > 0;
        }, cancellationToken);
    }

    public Task AddAsync(AuthUser user, CancellationToken cancellationToken)
    {
        return WithConnectionAsync(async connection =>
        {
            await using var command = CreateCommand(@"
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
);", connection);

            command.Parameters.Add(new SqlParameter("@Id", System.Data.SqlDbType.UniqueIdentifier) { Value = user.Id });
            command.Parameters.Add(new SqlParameter("@Username", System.Data.SqlDbType.NVarChar, 100) { Value = user.Username });
            command.Parameters.Add(new SqlParameter("@PasswordHash", System.Data.SqlDbType.VarBinary, -1) { Value = user.PasswordHash });
            command.Parameters.Add(new SqlParameter("@PasswordSalt", System.Data.SqlDbType.VarBinary, 64) { Value = user.PasswordSalt });
            command.Parameters.Add(new SqlParameter("@PasswordIterations", System.Data.SqlDbType.Int) { Value = user.PasswordIterations });
            command.Parameters.Add(new SqlParameter("@Role", System.Data.SqlDbType.Int) { Value = (int)user.Role });
            command.Parameters.Add(new SqlParameter("@IsActive", System.Data.SqlDbType.Bit) { Value = user.IsActive });
            command.Parameters.Add(new SqlParameter("@CreatedAtUtc", System.Data.SqlDbType.DateTime2) { Value = user.CreatedAtUtc });

            try
            {
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (SqlException ex) when (ex.IsUniqueConstraintViolation())
            {
                throw new DuplicateResourceException("user", "username", user.Username, ex);
            }
        }, cancellationToken);
    }

    private static AuthUser Map(SqlDataReader reader)
    {
        return new AuthUser(
            reader.GetGuid(0),
            reader.GetString(1),
            (byte[])reader["PasswordHash"],
            (byte[])reader["PasswordSalt"],
            reader.GetInt32(4),
            (UserRole)reader.GetInt32(5),
            reader.GetBoolean(6),
            reader.GetDateTime(7));
    }
}
