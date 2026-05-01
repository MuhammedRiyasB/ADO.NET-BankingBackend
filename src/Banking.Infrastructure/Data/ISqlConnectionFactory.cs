using Microsoft.Data.SqlClient;

namespace Banking.Infrastructure.Data;

internal interface ISqlConnectionFactory
{
    SqlConnection Create();
}
