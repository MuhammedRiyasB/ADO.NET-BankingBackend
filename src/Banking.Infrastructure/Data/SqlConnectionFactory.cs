using Banking.Infrastructure.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Banking.Infrastructure.Data;

internal sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IOptions<SqlServerOptions> sqlServerOptions)
    {
        _connectionString = sqlServerOptions.Value.ConnectionString;
    }

    public SqlConnection Create()
    {
        return new SqlConnection(_connectionString);
    }
}
