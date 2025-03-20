using Npgsql;
using Paradigm.Enterprise.Data.Extensions;
using Paradigm.Enterprise.Domain.Uow;
using System.Data;
using System.Data.Common;

namespace Paradigm.Enterprise.Data.PostgreSql.StoredProcedures;

public abstract class StoredProcedureBase<TParameters> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="parameters">The parameters.</param>
    public async Task ExecuteAsync(DbConnection connection, TParameters? parameters, IUnitOfWork? unitOfWork = null)
    {
        await ExecuteAsync(connection, GetSqlParameters(parameters), unitOfWork);
    }
}

/// <summary>
/// 
/// </summary>
/// <seealso cref="Paradigm.Enterprise.Data.PostgreSql.StoredProcedures.StoredProcedureBase" />
public abstract class StoredProcedureBase
{
    #region Properties

    /// <summary>
    /// Gets the name of the stored procedure.
    /// </summary>
    /// <value>
    /// The name of the stored procedure.
    /// </value>
    protected abstract string StoredProcedureName { get; }

    #endregion

    #region Public Methods

    /// <summary>
    /// Executes the stored procedure.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    public async Task ExecuteAsync(DbConnection connection, IUnitOfWork? unitOfWork = null)
    {
        await ExecuteAsync(connection, null, unitOfWork);
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// Gets the SQL parameters.
    /// </summary>
    /// <typeparam name="TParameters">The type of the parameters.</typeparam>
    /// <param name="parameters">The parameters.</param>
    /// <returns></returns>
    protected NpgsqlParameter[]? GetSqlParameters<TParameters>(TParameters? parameters)
    {
        if (parameters is null) return null;
        using var mapper = NpgsqlParameterMapperFactory.GetMapper<TParameters>();
        return mapper.Map(parameters);
    }

    /// <summary>
    /// Executes the stored procedure.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="parameters">The parameters.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    protected async Task ExecuteAsync(DbConnection connection, NpgsqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
    {
        if (connection.State == ConnectionState.Closed)
            await connection.OpenAsync();

        var hasActiveTransaction = unitOfWork?.HasActiveTransaction ?? false;

        using var command = connection.CreateCommand();
        command.CommandText = StoredProcedureName;
        command.CommandType = CommandType.StoredProcedure;

        if (hasActiveTransaction)
            unitOfWork?.UseTransaction(command);

        if (parameters is not null)
            command.Parameters.AddRange(parameters);

        await command.ExecuteNonQueryAsync();

        command.Parameters.Clear();

        if (!hasActiveTransaction)
            connection.Close();
    }

    /// <summary>
    /// Executes the stored procedure.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="connection">The connection.</param>
    /// <param name="parameters">The parameters.</param>
    /// <param name="readerExecutedAction">The reader executed action.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    /// <returns></returns>
    protected async Task<T> ExecuteAsync<T>(DbConnection connection, NpgsqlParameter[]? parameters, Func<DbDataReader, Task<T>> readerExecutedAction, IUnitOfWork? unitOfWork = null)
    {
        if (connection.State == ConnectionState.Closed)
            await connection.OpenAsync();

        var hasActiveTransaction = unitOfWork?.HasActiveTransaction ?? false;

        T result;

        using var command = connection.CreateCommand();
        command.CommandText = StoredProcedureName;
        command.CommandType = CommandType.StoredProcedure;

        if (hasActiveTransaction)
            unitOfWork?.UseTransaction(command);

        if (parameters is not null)
            command.Parameters.AddRange(parameters);

        using (var reader = await command.ExecuteReaderAsync())
        {
            result = await readerExecutedAction(reader);
            reader.Close();
        }

        command.Parameters.Clear();

        if (!hasActiveTransaction)
            connection.Close();

        return result;
    }

    /// <summary>
    /// Executes the stored procedure and receive multiple result sets.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="connection">The connection.</param>
    /// <param name="parameters">The parameters.</param>
    /// <param name="cursorAction">The cursor action.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    /// <param name="count">The count.</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException">A transaction must be opened</exception>
    protected async Task<T> ExecuteMultipleAsync<T>(DbConnection connection, NpgsqlParameter[]? parameters, Func<List<string>, Task<T>> cursorAction, IUnitOfWork? unitOfWork, int count)
    {
        if (connection.State == ConnectionState.Closed)
            await connection.OpenAsync();

        if (unitOfWork is null)
            throw new ArgumentNullException(nameof(unitOfWork));

        ITransaction? localTransaction = null;

        if (!unitOfWork.HasActiveTransaction)
            localTransaction = unitOfWork.CreateTransaction();

        T result;

        using var command = connection.CreateCommand();
        command.CommandText = StoredProcedureName;
        command.CommandType = CommandType.StoredProcedure;

        unitOfWork.UseTransaction(command);

        if (parameters is not null)
            command.Parameters.AddRange(parameters);

        var set = new HashSet<string>();

        using (var reader = await command.ExecuteReaderAsync())
        {
            for (var i = 0; i < count; i++)
            {
                await reader.ReadAsync();
                set.Add(reader.GetString(0));
            }

            reader.Close();
        }

        result = await cursorAction(set.ToList());

        command.Parameters.Clear();

        localTransaction?.Dispose();

        return result;
    }

    /// <summary>
    /// Executes the stored procedure.
    /// </summary>
    /// <typeparam name="TR1">The type of the result 1.</typeparam>
    /// <param name="connection">The connection.</param>
    /// <param name="parameters">The parameters.</param>
    /// <returns></returns>
    protected async Task<TR1?> ExecuteAsync<TR1>(DbConnection connection, NpgsqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync(connection, parameters, async reader => await reader.TranslateAsync<TR1>(), unitOfWork);
    }

    /// <summary>
    /// Executes the stored procedure.
    /// </summary>
    /// <typeparam name="TR1">The type of the result 1.</typeparam>
    /// <typeparam name="TR2">The type of the result 2.</typeparam>
    /// <param name="connection">The connection.</param>
    /// <param name="parameters">The parameters.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    /// <returns></returns>
    protected async Task<(TR1, TR2)> ExecuteAsync<TR1, TR2>(DbConnection connection, NpgsqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteMultipleAsync(connection, parameters, async set =>
            (await FetchCursorAsync<TR1>(connection, set[0]),
             await FetchCursorAsync<TR2>(connection, set[1])), unitOfWork, 2);
    }

    /// <summary>
    /// Executes the stored procedure.
    /// </summary>
    /// <typeparam name="TR1">The type of the result 1.</typeparam>
    /// <typeparam name="TR2">The type of the result 2.</typeparam>
    /// <typeparam name="TR3">The type of the result 3.</typeparam>
    /// <param name="connection">The connection.</param>
    /// <param name="parameters">The parameters.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    /// <returns></returns>
    protected async Task<(TR1, TR2, TR3)> ExecuteAsync<TR1, TR2, TR3>(DbConnection connection, NpgsqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteMultipleAsync(connection, parameters, async set =>
            (await FetchCursorAsync<TR1>(connection, set[0]),
             await FetchCursorAsync<TR2>(connection, set[1]),
             await FetchCursorAsync<TR3>(connection, set[2])), unitOfWork, 3);
    }

    /// <summary>
    /// Executes the stored procedure.
    /// </summary>
    /// <typeparam name="TR1">The type of the r1.</typeparam>
    /// <typeparam name="TR2">The type of the r2.</typeparam>
    /// <typeparam name="TR3">The type of the r3.</typeparam>
    /// <typeparam name="TR4">The type of the r4.</typeparam>
    /// <param name="connection">The connection.</param>
    /// <param name="parameters">The parameters.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    /// <returns></returns>
    protected async Task<(TR1, TR2, TR3, TR4)> ExecuteAsync<TR1, TR2, TR3, TR4>(DbConnection connection, NpgsqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteMultipleAsync(connection, parameters, async set =>
            (await FetchCursorAsync<TR1>(connection, set[0]),
             await FetchCursorAsync<TR2>(connection, set[1]),
             await FetchCursorAsync<TR3>(connection, set[2]),
             await FetchCursorAsync<TR4>(connection, set[3])), unitOfWork, 4);
    }

    /// <summary>
    /// Executes the stored procedure.
    /// </summary>
    /// <typeparam name="TR1">The type of the r1.</typeparam>
    /// <typeparam name="TR2">The type of the r2.</typeparam>
    /// <typeparam name="TR3">The type of the r3.</typeparam>
    /// <typeparam name="TR4">The type of the r4.</typeparam>
    /// <typeparam name="TR5">The type of the r5.</typeparam>
    /// <param name="connection">The connection.</param>
    /// <param name="parameters">The parameters.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    /// <returns></returns>
    protected async Task<(TR1, TR2, TR3, TR4, TR5)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5>(DbConnection connection, NpgsqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteMultipleAsync(connection, parameters, async set =>
            (await FetchCursorAsync<TR1>(connection, set[0]),
             await FetchCursorAsync<TR2>(connection, set[1]),
             await FetchCursorAsync<TR3>(connection, set[2]),
             await FetchCursorAsync<TR4>(connection, set[3]),
             await FetchCursorAsync<TR5>(connection, set[4])), unitOfWork, 5);
    }

    /// <summary>
    /// Executes the stored procedure.
    /// </summary>
    /// <typeparam name="TR1">The type of the r1.</typeparam>
    /// <typeparam name="TR2">The type of the r2.</typeparam>
    /// <typeparam name="TR3">The type of the r3.</typeparam>
    /// <typeparam name="TR4">The type of the r4.</typeparam>
    /// <typeparam name="TR5">The type of the r5.</typeparam>
    /// <typeparam name="TR6">The type of the r6.</typeparam>
    /// <param name="connection">The connection.</param>
    /// <param name="parameters">The parameters.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    /// <returns></returns>
    protected async Task<(TR1, TR2, TR3, TR4, TR5, TR6)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6>(DbConnection connection, NpgsqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteMultipleAsync(connection, parameters, async set =>
            (await FetchCursorAsync<TR1>(connection, set[0]),
             await FetchCursorAsync<TR2>(connection, set[1]),
             await FetchCursorAsync<TR3>(connection, set[2]),
             await FetchCursorAsync<TR4>(connection, set[3]),
             await FetchCursorAsync<TR5>(connection, set[4]),
             await FetchCursorAsync<TR6>(connection, set[5])), unitOfWork, 6);
    }

    /// <summary>
    /// Executes the stored procedure.
    /// </summary>
    /// <typeparam name="TR1">The type of the r1.</typeparam>
    /// <typeparam name="TR2">The type of the r2.</typeparam>
    /// <typeparam name="TR3">The type of the r3.</typeparam>
    /// <typeparam name="TR4">The type of the r4.</typeparam>
    /// <typeparam name="TR5">The type of the r5.</typeparam>
    /// <typeparam name="TR6">The type of the r6.</typeparam>
    /// <typeparam name="TR7">The type of the r7.</typeparam>
    /// <param name="connection">The connection.</param>
    /// <param name="parameters">The parameters.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    /// <returns></returns>
    protected async Task<(TR1, TR2, TR3, TR4, TR5, TR6, TR7)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7>(DbConnection connection, NpgsqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteMultipleAsync(connection, parameters, async set =>
            (await FetchCursorAsync<TR1>(connection, set[0]),
             await FetchCursorAsync<TR2>(connection, set[1]),
             await FetchCursorAsync<TR3>(connection, set[2]),
             await FetchCursorAsync<TR4>(connection, set[3]),
             await FetchCursorAsync<TR5>(connection, set[4]),
             await FetchCursorAsync<TR6>(connection, set[5]),
             await FetchCursorAsync<TR7>(connection, set[6])), unitOfWork, 7);
    }

    /// <summary>
    /// Executes the stored procedure.
    /// </summary>
    /// <typeparam name="TR1">The type of the r1.</typeparam>
    /// <typeparam name="TR2">The type of the r2.</typeparam>
    /// <typeparam name="TR3">The type of the r3.</typeparam>
    /// <typeparam name="TR4">The type of the r4.</typeparam>
    /// <typeparam name="TR5">The type of the r5.</typeparam>
    /// <typeparam name="TR6">The type of the r6.</typeparam>
    /// <typeparam name="TR7">The type of the r7.</typeparam>
    /// <typeparam name="TR8">The type of the r8.</typeparam>
    /// <param name="connection">The connection.</param>
    /// <param name="parameters">The parameters.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    /// <returns></returns>
    protected async Task<(TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8>(DbConnection connection, NpgsqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteMultipleAsync(connection, parameters, async set =>
            (await FetchCursorAsync<TR1>(connection, set[0]),
             await FetchCursorAsync<TR2>(connection, set[1]),
             await FetchCursorAsync<TR3>(connection, set[2]),
             await FetchCursorAsync<TR4>(connection, set[3]),
             await FetchCursorAsync<TR5>(connection, set[4]),
             await FetchCursorAsync<TR6>(connection, set[5]),
             await FetchCursorAsync<TR7>(connection, set[6]),
             await FetchCursorAsync<TR8>(connection, set[7])), unitOfWork, 8);
    }

    /// <summary>
    /// Executes the stored procedure.
    /// </summary>
    /// <typeparam name="TR1">The type of the r1.</typeparam>
    /// <typeparam name="TR2">The type of the r2.</typeparam>
    /// <typeparam name="TR3">The type of the r3.</typeparam>
    /// <typeparam name="TR4">The type of the r4.</typeparam>
    /// <typeparam name="TR5">The type of the r5.</typeparam>
    /// <typeparam name="TR6">The type of the r6.</typeparam>
    /// <typeparam name="TR7">The type of the r7.</typeparam>
    /// <typeparam name="TR8">The type of the r8.</typeparam>
    /// <typeparam name="TR9">The type of the r9.</typeparam>
    /// <param name="connection">The connection.</param>
    /// <param name="parameters">The parameters.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    /// <returns></returns>
    protected async Task<(TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9>(DbConnection connection, NpgsqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteMultipleAsync(connection, parameters, async set =>
            (await FetchCursorAsync<TR1>(connection, set[0]),
             await FetchCursorAsync<TR2>(connection, set[1]),
             await FetchCursorAsync<TR3>(connection, set[2]),
             await FetchCursorAsync<TR4>(connection, set[3]),
             await FetchCursorAsync<TR5>(connection, set[4]),
             await FetchCursorAsync<TR6>(connection, set[5]),
             await FetchCursorAsync<TR7>(connection, set[6]),
             await FetchCursorAsync<TR8>(connection, set[7]),
             await FetchCursorAsync<TR9>(connection, set[8])), unitOfWork, 9);
    }

    /// <summary>
    /// Executes the stored procedure.
    /// </summary>
    /// <typeparam name="TR1">The type of the r1.</typeparam>
    /// <typeparam name="TR2">The type of the r2.</typeparam>
    /// <typeparam name="TR3">The type of the r3.</typeparam>
    /// <typeparam name="TR4">The type of the r4.</typeparam>
    /// <typeparam name="TR5">The type of the r5.</typeparam>
    /// <typeparam name="TR6">The type of the r6.</typeparam>
    /// <typeparam name="TR7">The type of the r7.</typeparam>
    /// <typeparam name="TR8">The type of the r8.</typeparam>
    /// <typeparam name="TR9">The type of the r9.</typeparam>
    /// <typeparam name="TR10">The type of the R10.</typeparam>
    /// <param name="connection">The connection.</param>
    /// <param name="parameters">The parameters.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    /// <returns></returns>
    protected async Task<(TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10>(DbConnection connection, NpgsqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteMultipleAsync(connection, parameters, async set =>
            (await FetchCursorAsync<TR1>(connection, set[0]),
             await FetchCursorAsync<TR2>(connection, set[1]),
             await FetchCursorAsync<TR3>(connection, set[2]),
             await FetchCursorAsync<TR4>(connection, set[3]),
             await FetchCursorAsync<TR5>(connection, set[4]),
             await FetchCursorAsync<TR6>(connection, set[5]),
             await FetchCursorAsync<TR7>(connection, set[6]),
             await FetchCursorAsync<TR8>(connection, set[7]),
             await FetchCursorAsync<TR9>(connection, set[8]),
             await FetchCursorAsync<TR10>(connection, set[9])), unitOfWork, 10);
    }

    /// <summary>
    /// Executes the stored procedure.
    /// </summary>
    /// <typeparam name="TR1">The type of the r1.</typeparam>
    /// <typeparam name="TR2">The type of the r2.</typeparam>
    /// <typeparam name="TR3">The type of the r3.</typeparam>
    /// <typeparam name="TR4">The type of the r4.</typeparam>
    /// <typeparam name="TR5">The type of the r5.</typeparam>
    /// <typeparam name="TR6">The type of the r6.</typeparam>
    /// <typeparam name="TR7">The type of the r7.</typeparam>
    /// <typeparam name="TR8">The type of the r8.</typeparam>
    /// <typeparam name="TR9">The type of the r9.</typeparam>
    /// <typeparam name="TR10">The type of the R10.</typeparam>
    /// <typeparam name="TR11">The type of the R11.</typeparam>
    /// <param name="connection">The connection.</param>
    /// <param name="parameters">The parameters.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    /// <returns></returns>
    protected async Task<(TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10, TR11)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10, TR11>(DbConnection connection, NpgsqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteMultipleAsync(connection, parameters, async set =>
            (await FetchCursorAsync<TR1>(connection, set[0]),
             await FetchCursorAsync<TR2>(connection, set[1]),
             await FetchCursorAsync<TR3>(connection, set[2]),
             await FetchCursorAsync<TR4>(connection, set[3]),
             await FetchCursorAsync<TR5>(connection, set[4]),
             await FetchCursorAsync<TR6>(connection, set[5]),
             await FetchCursorAsync<TR7>(connection, set[6]),
             await FetchCursorAsync<TR8>(connection, set[7]),
             await FetchCursorAsync<TR9>(connection, set[8]),
             await FetchCursorAsync<TR10>(connection, set[9]),
             await FetchCursorAsync<TR11>(connection, set[10])), unitOfWork, 11);
    }

    /// <summary>
    /// Executes the stored procedure.
    /// </summary>
    /// <typeparam name="TR1">The type of the r1.</typeparam>
    /// <typeparam name="TR2">The type of the r2.</typeparam>
    /// <typeparam name="TR3">The type of the r3.</typeparam>
    /// <typeparam name="TR4">The type of the r4.</typeparam>
    /// <typeparam name="TR5">The type of the r5.</typeparam>
    /// <typeparam name="TR6">The type of the r6.</typeparam>
    /// <typeparam name="TR7">The type of the r7.</typeparam>
    /// <typeparam name="TR8">The type of the r8.</typeparam>
    /// <typeparam name="TR9">The type of the r9.</typeparam>
    /// <typeparam name="TR10">The type of the R10.</typeparam>
    /// <typeparam name="TR11">The type of the R11.</typeparam>
    /// <typeparam name="TR12">The type of the R12.</typeparam>
    /// <param name="connection">The connection.</param>
    /// <param name="parameters">The parameters.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    /// <returns></returns>
    protected async Task<(TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10, TR11, TR12)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10, TR11, TR12>(DbConnection connection, NpgsqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteMultipleAsync(connection, parameters, async set =>
            (await FetchCursorAsync<TR1>(connection, set[0]),
             await FetchCursorAsync<TR2>(connection, set[1]),
             await FetchCursorAsync<TR3>(connection, set[2]),
             await FetchCursorAsync<TR4>(connection, set[3]),
             await FetchCursorAsync<TR5>(connection, set[4]),
             await FetchCursorAsync<TR6>(connection, set[5]),
             await FetchCursorAsync<TR7>(connection, set[6]),
             await FetchCursorAsync<TR8>(connection, set[7]),
             await FetchCursorAsync<TR9>(connection, set[8]),
             await FetchCursorAsync<TR10>(connection, set[9]),
             await FetchCursorAsync<TR11>(connection, set[10]),
             await FetchCursorAsync<TR12>(connection, set[11])), unitOfWork, 12);
    }

    /// <summary>
    /// Executes the stored procedure.
    /// </summary>
    /// <typeparam name="TR1">The type of the r1.</typeparam>
    /// <typeparam name="TR2">The type of the r2.</typeparam>
    /// <typeparam name="TR3">The type of the r3.</typeparam>
    /// <typeparam name="TR4">The type of the r4.</typeparam>
    /// <typeparam name="TR5">The type of the r5.</typeparam>
    /// <typeparam name="TR6">The type of the r6.</typeparam>
    /// <typeparam name="TR7">The type of the r7.</typeparam>
    /// <typeparam name="TR8">The type of the r8.</typeparam>
    /// <typeparam name="TR9">The type of the r9.</typeparam>
    /// <typeparam name="TR10">The type of the R10.</typeparam>
    /// <typeparam name="TR11">The type of the R11.</typeparam>
    /// <typeparam name="TR12">The type of the R12.</typeparam>
    /// <typeparam name="TR13">The type of the R13.</typeparam>
    /// <param name="connection">The connection.</param>
    /// <param name="parameters">The parameters.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    /// <returns></returns>
    protected async Task<(TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10, TR11, TR12, TR13)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10, TR11, TR12, TR13>(DbConnection connection, NpgsqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteMultipleAsync(connection, parameters, async set =>
            (await FetchCursorAsync<TR1>(connection, set[0]),
             await FetchCursorAsync<TR2>(connection, set[1]),
             await FetchCursorAsync<TR3>(connection, set[2]),
             await FetchCursorAsync<TR4>(connection, set[3]),
             await FetchCursorAsync<TR5>(connection, set[4]),
             await FetchCursorAsync<TR6>(connection, set[5]),
             await FetchCursorAsync<TR7>(connection, set[6]),
             await FetchCursorAsync<TR8>(connection, set[7]),
             await FetchCursorAsync<TR9>(connection, set[8]),
             await FetchCursorAsync<TR10>(connection, set[9]),
             await FetchCursorAsync<TR11>(connection, set[10]),
             await FetchCursorAsync<TR12>(connection, set[11]),
             await FetchCursorAsync<TR13>(connection, set[12])), unitOfWork, 13);
    }

    /// <summary>
    /// Executes the stored procedure.
    /// </summary>
    /// <typeparam name="TR1">The type of the r1.</typeparam>
    /// <typeparam name="TR2">The type of the r2.</typeparam>
    /// <typeparam name="TR3">The type of the r3.</typeparam>
    /// <typeparam name="TR4">The type of the r4.</typeparam>
    /// <typeparam name="TR5">The type of the r5.</typeparam>
    /// <typeparam name="TR6">The type of the r6.</typeparam>
    /// <typeparam name="TR7">The type of the r7.</typeparam>
    /// <typeparam name="TR8">The type of the r8.</typeparam>
    /// <typeparam name="TR9">The type of the r9.</typeparam>
    /// <typeparam name="TR10">The type of the R10.</typeparam>
    /// <typeparam name="TR11">The type of the R11.</typeparam>
    /// <typeparam name="TR12">The type of the R12.</typeparam>
    /// <typeparam name="TR13">The type of the R13.</typeparam>
    /// <typeparam name="TR14">The type of the R14.</typeparam>
    /// <param name="connection">The connection.</param>
    /// <param name="parameters">The parameters.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    /// <returns></returns>
    protected async Task<(TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10, TR11, TR12, TR13, TR14)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10, TR11, TR12, TR13, TR14>(DbConnection connection, NpgsqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteMultipleAsync(connection, parameters, async set =>
            (await FetchCursorAsync<TR1>(connection, set[0]),
             await FetchCursorAsync<TR2>(connection, set[1]),
             await FetchCursorAsync<TR3>(connection, set[2]),
             await FetchCursorAsync<TR4>(connection, set[3]),
             await FetchCursorAsync<TR5>(connection, set[4]),
             await FetchCursorAsync<TR6>(connection, set[5]),
             await FetchCursorAsync<TR7>(connection, set[6]),
             await FetchCursorAsync<TR8>(connection, set[7]),
             await FetchCursorAsync<TR9>(connection, set[8]),
             await FetchCursorAsync<TR10>(connection, set[9]),
             await FetchCursorAsync<TR11>(connection, set[10]),
             await FetchCursorAsync<TR12>(connection, set[11]),
             await FetchCursorAsync<TR13>(connection, set[12]),
             await FetchCursorAsync<TR14>(connection, set[13])), unitOfWork, 14);
    }

    /// <summary>
    /// Executes the stored procedure.
    /// </summary>
    /// <typeparam name="TR1">The type of the r1.</typeparam>
    /// <typeparam name="TR2">The type of the r2.</typeparam>
    /// <typeparam name="TR3">The type of the r3.</typeparam>
    /// <typeparam name="TR4">The type of the r4.</typeparam>
    /// <typeparam name="TR5">The type of the r5.</typeparam>
    /// <typeparam name="TR6">The type of the r6.</typeparam>
    /// <typeparam name="TR7">The type of the r7.</typeparam>
    /// <typeparam name="TR8">The type of the r8.</typeparam>
    /// <typeparam name="TR9">The type of the r9.</typeparam>
    /// <typeparam name="TR10">The type of the R10.</typeparam>
    /// <typeparam name="TR11">The type of the R11.</typeparam>
    /// <typeparam name="TR12">The type of the R12.</typeparam>
    /// <typeparam name="TR13">The type of the R13.</typeparam>
    /// <typeparam name="TR14">The type of the R14.</typeparam>
    /// <typeparam name="TR15">The type of the R15.</typeparam>
    /// <param name="connection">The connection.</param>
    /// <param name="parameters">The parameters.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    /// <returns></returns>
    protected async Task<(TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10, TR11, TR12, TR13, TR14, TR15)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10, TR11, TR12, TR13, TR14, TR15>(DbConnection connection, NpgsqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteMultipleAsync(connection, parameters, async set =>
            (await FetchCursorAsync<TR1>(connection, set[0]),
             await FetchCursorAsync<TR2>(connection, set[1]),
             await FetchCursorAsync<TR3>(connection, set[2]),
             await FetchCursorAsync<TR4>(connection, set[3]),
             await FetchCursorAsync<TR5>(connection, set[4]),
             await FetchCursorAsync<TR6>(connection, set[5]),
             await FetchCursorAsync<TR7>(connection, set[6]),
             await FetchCursorAsync<TR8>(connection, set[7]),
             await FetchCursorAsync<TR9>(connection, set[8]),
             await FetchCursorAsync<TR10>(connection, set[9]),
             await FetchCursorAsync<TR11>(connection, set[10]),
             await FetchCursorAsync<TR12>(connection, set[11]),
             await FetchCursorAsync<TR13>(connection, set[12]),
             await FetchCursorAsync<TR14>(connection, set[13]),
             await FetchCursorAsync<TR15>(connection, set[14])), unitOfWork, 15);
    }

    /// <summary>
    /// Executes the stored procedure.
    /// </summary>
    /// <typeparam name="TR1">The type of the r1.</typeparam>
    /// <typeparam name="TR2">The type of the r2.</typeparam>
    /// <typeparam name="TR3">The type of the r3.</typeparam>
    /// <typeparam name="TR4">The type of the r4.</typeparam>
    /// <typeparam name="TR5">The type of the r5.</typeparam>
    /// <typeparam name="TR6">The type of the r6.</typeparam>
    /// <typeparam name="TR7">The type of the r7.</typeparam>
    /// <typeparam name="TR8">The type of the r8.</typeparam>
    /// <typeparam name="TR9">The type of the r9.</typeparam>
    /// <typeparam name="TR10">The type of the R10.</typeparam>
    /// <typeparam name="TR11">The type of the R11.</typeparam>
    /// <typeparam name="TR12">The type of the R12.</typeparam>
    /// <typeparam name="TR13">The type of the R13.</typeparam>
    /// <typeparam name="TR14">The type of the R14.</typeparam>
    /// <typeparam name="TR15">The type of the R15.</typeparam>
    /// <typeparam name="TR16">The type of the R16.</typeparam>
    /// <param name="connection">The connection.</param>
    /// <param name="parameters">The parameters.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    /// <returns></returns>
    protected async Task<(TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10, TR11, TR12, TR13, TR14, TR15, TR16)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10, TR11, TR12, TR13, TR14, TR15, TR16>(DbConnection connection, NpgsqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteMultipleAsync(connection, parameters, async set =>
            (await FetchCursorAsync<TR1>(connection, set[0]),
             await FetchCursorAsync<TR2>(connection, set[1]),
             await FetchCursorAsync<TR3>(connection, set[2]),
             await FetchCursorAsync<TR4>(connection, set[3]),
             await FetchCursorAsync<TR5>(connection, set[4]),
             await FetchCursorAsync<TR6>(connection, set[5]),
             await FetchCursorAsync<TR7>(connection, set[6]),
             await FetchCursorAsync<TR8>(connection, set[7]),
             await FetchCursorAsync<TR9>(connection, set[8]),
             await FetchCursorAsync<TR10>(connection, set[9]),
             await FetchCursorAsync<TR11>(connection, set[10]),
             await FetchCursorAsync<TR12>(connection, set[11]),
             await FetchCursorAsync<TR13>(connection, set[12]),
             await FetchCursorAsync<TR14>(connection, set[13]),
             await FetchCursorAsync<TR15>(connection, set[14]),
             await FetchCursorAsync<TR16>(connection, set[15])), unitOfWork, 16);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Fetches the cursor.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="connection">The connection.</param>
    /// <param name="cursorName">Name of the cursor.</param>
    /// <returns></returns>
    private async Task<T?> FetchCursorAsync<T>(DbConnection connection, string cursorName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $@"FETCH ALL FROM ""{cursorName}""";
        using var reader = command.ExecuteReader();
        return await reader.TranslateAndMoveAsync<T>();
    }

    #endregion
}