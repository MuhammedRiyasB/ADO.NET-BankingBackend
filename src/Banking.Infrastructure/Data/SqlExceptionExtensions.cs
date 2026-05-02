using Microsoft.Data.SqlClient;

namespace Banking.Infrastructure.Data;

internal static class SqlExceptionExtensions
{
    public static bool IsUniqueConstraintViolation(this SqlException exception)
    {
        return exception.Number is 2601 or 2627;
    }
}
