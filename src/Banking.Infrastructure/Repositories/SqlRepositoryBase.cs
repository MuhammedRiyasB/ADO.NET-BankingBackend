using Banking.Infrastructure.Data;
using Microsoft.Data.SqlClient;

namespace Banking.Infrastructure.Repositories;

internal abstract class SqlRepositoryBase
{
    private readonly SqlUnitOfWork _unitOfWork;

    protected SqlRepositoryBase(SqlUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    protected async Task<TResult> WithConnectionAsync<TResult>(
        Func<SqlConnection, SqlTransaction?, Task<TResult>> action,
        CancellationToken cancellationToken)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        return await action(connection, _unitOfWork.Transaction);
    }

    protected async Task WithConnectionAsync(
        Func<SqlConnection, SqlTransaction?, Task> action,
        CancellationToken cancellationToken)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        await action(connection, _unitOfWork.Transaction);
    }

    protected async Task<TResult> WithConnectionAsync<TResult>(
        Func<SqlConnection, Task<TResult>> action,
        CancellationToken cancellationToken)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        return await action(connection);
    }

    protected async Task WithConnectionAsync(
        Func<SqlConnection, Task> action,
        CancellationToken cancellationToken)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        await action(connection);
    }

    protected SqlCommand CreateCommand(string sql, SqlConnection connection, SqlTransaction? transaction = null)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        var tx = transaction ?? _unitOfWork.Transaction;
        if (tx is not null)
        {
            command.Transaction = tx;
        }

        return command;
    }
}
