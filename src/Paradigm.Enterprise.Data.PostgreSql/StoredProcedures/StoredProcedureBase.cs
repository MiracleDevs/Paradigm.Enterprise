using Npgsql;
using Paradigm.Enterprise.Data.Extensions;
using Paradigm.Enterprise.Domain.Uow;
using System.Data;
using System.Data.Common;

namespace Paradigm.Enterprise.Data.PostgreSql.StoredProcedures;

/// <summary>
/// Defines a PostgreSQL stored procedure whose application parameter object is mapped to provider parameters.
/// </summary>
/// <typeparam name="TParameters">The application type that supplies stored-procedure parameters.</typeparam>
/// <remarks>
/// A mapper for <typeparamref name="TParameters"/> must be registered with
/// <see cref="NpgsqlParameterMapperFactory"/> before execution.
/// </remarks>
public abstract class StoredProcedureBase<TParameters> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure with parameters mapped from <typeparamref name="TParameters"/>.
    /// </summary>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The application parameter object to map, or <see langword="null"/> for no parameters.</param>
    /// <param name="unitOfWork">The optional unit of work whose active transaction is attached to the command.</param>
    /// <returns>A task that completes when the command finishes.</returns>
    /// <remarks>
    /// Without an active unit-of-work transaction, the connection is opened if necessary and closed
    /// after execution. With an active transaction, the command enlists and the connection remains open.
    /// </remarks>
    public async Task ExecuteAsync(DbConnection connection, TParameters? parameters, IUnitOfWork? unitOfWork = null)
    {
        await ExecuteAsync(connection, GetSqlParameters(parameters), unitOfWork);
    }
}

/// <summary>
/// Provides command execution and result-set and cursor mapping for PostgreSQL stored procedures.
/// </summary>
/// <remarks>
/// The caller owns the supplied connection. Without an active unit-of-work transaction, execution
/// opens the connection when necessary and closes it afterward. With an active transaction, the
/// command enlists in it and connection lifetime remains with the transaction owner.
/// </remarks>
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

    /// <summary>
    /// Gets the execution timeout in seconds.
    /// </summary>
    /// <value>
    /// The execution timeout.
    /// </value>
    protected virtual int? ExecutionTimeout { get; }

    #endregion

    #region Public Methods

    /// <summary>
    /// Executes the stored procedure without application parameters.
    /// </summary>
    /// <param name="connection">The caller-supplied connection. It is opened if necessary but is never disposed.</param>
    /// <param name="unitOfWork">The optional unit of work whose active transaction is attached to the command.</param>
    /// <returns>A task that completes when the command finishes.</returns>
    /// <remarks>
    /// Without an active unit-of-work transaction, the connection is closed after execution. With an
    /// active transaction, connection lifetime remains with the transaction owner.
    /// </remarks>
    public async Task ExecuteAsync(DbConnection connection, IUnitOfWork? unitOfWork = null)
    {
        await ExecuteAsync(connection, null, unitOfWork);
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// Maps an application parameter object to PostgreSQL parameters.
    /// </summary>
    /// <typeparam name="TParameters">The application parameter type with a registered mapper.</typeparam>
    /// <param name="parameters">The parameter object to map, or <see langword="null"/>.</param>
    /// <returns>The mapped provider parameters, or <see langword="null"/> when <paramref name="parameters"/> is null.</returns>
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

        if (ExecutionTimeout.HasValue)
            command.CommandTimeout = ExecutionTimeout.Value;

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
    /// <typeparam name="T">The value produced while consuming the command results.</typeparam>
    /// <param name="connection">The connection.</param>
    /// <param name="parameters">The parameters.</param>
    /// <param name="readerExecutedAction">The reader executed action.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    /// <returns>The value produced by the supplied result-processing delegate.</returns>
    protected async Task<T> ExecuteAsync<T>(DbConnection connection, NpgsqlParameter[]? parameters, Func<DbDataReader, Task<T>> readerExecutedAction, IUnitOfWork? unitOfWork = null)
    {
        if (connection.State == ConnectionState.Closed)
            await connection.OpenAsync();

        var hasActiveTransaction = unitOfWork?.HasActiveTransaction ?? false;

        T result;

        using var command = connection.CreateCommand();
        command.CommandText = StoredProcedureName;
        command.CommandType = CommandType.StoredProcedure;

        if (ExecutionTimeout.HasValue)
            command.CommandTimeout = ExecutionTimeout.Value;

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
    /// Executes a cursor-returning stored procedure and delegates consumption of the returned cursor names.
    /// </summary>
    /// <typeparam name="T">The value produced while consuming the command results.</typeparam>
    /// <param name="connection">The caller-supplied connection used for both the procedure and cursor fetches.</param>
    /// <param name="parameters">The PostgreSQL parameters to attach, or <see langword="null"/> for none.</param>
    /// <param name="cursorAction">The asynchronous delegate that consumes the returned cursor names.</param>
    /// <param name="unitOfWork">The unit of work used to create or reuse the transaction required by PostgreSQL cursors.</param>
    /// <param name="count">The number of cursor names expected from the command.</param>
    /// <returns>The value produced by the supplied result-processing delegate.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="unitOfWork"/> is <see langword="null"/>.</exception>
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

        if (ExecutionTimeout.HasValue)
            command.CommandTimeout = ExecutionTimeout.Value;

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
    /// Executes the command and maps its first result set.
    /// </summary>
    /// <typeparam name="TR1">The value mapped from result set 1.</typeparam>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The provider parameters to add to the command, or <see langword="null"/> for none.</param>
    /// <param name="unitOfWork">The optional unit of work whose active transaction is attached to the command.</param>
    /// <returns>The mapped value, or <see langword="null"/> when the result set contains no row.</returns>
    protected async Task<TR1?> ExecuteAsync<TR1>(DbConnection connection, NpgsqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync(connection, parameters, async reader => await reader.TranslateAsync<TR1>(), unitOfWork);
    }

    /// <summary>
    /// Executes the command and maps 2 distinct returned PostgreSQL cursor names to tuple elements.
    /// </summary>
    /// <typeparam name="TR1">The value mapped from returned cursor 1.</typeparam>
    /// <typeparam name="TR2">The value mapped from returned cursor 2.</typeparam>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The provider parameters to add to the command, or <see langword="null"/> for none.</param>
    /// <param name="unitOfWork">The required unit of work that owns the cursor transaction.</param>
    /// <returns>A tuple whose positions follow the implementation-defined enumeration of distinct cursor names; empty cursors produce null reference values or default value-type values.</returns>
    /// <remarks>The command must return 2 distinct cursor names. Tuple positions do not promise database return order.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="unitOfWork"/> is <see langword="null"/>.</exception>
    protected async Task<(TR1?, TR2?)> ExecuteAsync<TR1, TR2>(DbConnection connection, NpgsqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteMultipleAsync(connection, parameters, async set =>
            (await FetchCursorAsync<TR1>(connection, set[0]),
             await FetchCursorAsync<TR2>(connection, set[1])), unitOfWork, 2);
    }

    /// <summary>
    /// Executes the command and maps 3 distinct returned PostgreSQL cursor names to tuple elements.
    /// </summary>
    /// <typeparam name="TR1">The value mapped from returned cursor 1.</typeparam>
    /// <typeparam name="TR2">The value mapped from returned cursor 2.</typeparam>
    /// <typeparam name="TR3">The value mapped from returned cursor 3.</typeparam>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The provider parameters to add to the command, or <see langword="null"/> for none.</param>
    /// <param name="unitOfWork">The required unit of work that owns the cursor transaction.</param>
    /// <returns>A tuple whose positions follow the implementation-defined enumeration of distinct cursor names; empty cursors produce null reference values or default value-type values.</returns>
    /// <remarks>The command must return 3 distinct cursor names. Tuple positions do not promise database return order.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="unitOfWork"/> is <see langword="null"/>.</exception>
    protected async Task<(TR1?, TR2?, TR3?)> ExecuteAsync<TR1, TR2, TR3>(DbConnection connection, NpgsqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteMultipleAsync(connection, parameters, async set =>
            (await FetchCursorAsync<TR1>(connection, set[0]),
             await FetchCursorAsync<TR2>(connection, set[1]),
             await FetchCursorAsync<TR3>(connection, set[2])), unitOfWork, 3);
    }

    /// <summary>
    /// Executes the command and maps 4 distinct returned PostgreSQL cursor names to tuple elements.
    /// </summary>
    /// <typeparam name="TR1">The value mapped from returned cursor 1.</typeparam>
    /// <typeparam name="TR2">The value mapped from returned cursor 2.</typeparam>
    /// <typeparam name="TR3">The value mapped from returned cursor 3.</typeparam>
    /// <typeparam name="TR4">The value mapped from returned cursor 4.</typeparam>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The provider parameters to add to the command, or <see langword="null"/> for none.</param>
    /// <param name="unitOfWork">The required unit of work that owns the cursor transaction.</param>
    /// <returns>A tuple whose positions follow the implementation-defined enumeration of distinct cursor names; empty cursors produce null reference values or default value-type values.</returns>
    /// <remarks>The command must return 4 distinct cursor names. Tuple positions do not promise database return order.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="unitOfWork"/> is <see langword="null"/>.</exception>
    protected async Task<(TR1?, TR2?, TR3?, TR4?)> ExecuteAsync<TR1, TR2, TR3, TR4>(DbConnection connection, NpgsqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteMultipleAsync(connection, parameters, async set =>
            (await FetchCursorAsync<TR1>(connection, set[0]),
             await FetchCursorAsync<TR2>(connection, set[1]),
             await FetchCursorAsync<TR3>(connection, set[2]),
             await FetchCursorAsync<TR4>(connection, set[3])), unitOfWork, 4);
    }

    /// <summary>
    /// Executes the command and maps 5 distinct returned PostgreSQL cursor names to tuple elements.
    /// </summary>
    /// <typeparam name="TR1">The value mapped from returned cursor 1.</typeparam>
    /// <typeparam name="TR2">The value mapped from returned cursor 2.</typeparam>
    /// <typeparam name="TR3">The value mapped from returned cursor 3.</typeparam>
    /// <typeparam name="TR4">The value mapped from returned cursor 4.</typeparam>
    /// <typeparam name="TR5">The value mapped from returned cursor 5.</typeparam>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The provider parameters to add to the command, or <see langword="null"/> for none.</param>
    /// <param name="unitOfWork">The required unit of work that owns the cursor transaction.</param>
    /// <returns>A tuple whose positions follow the implementation-defined enumeration of distinct cursor names; empty cursors produce null reference values or default value-type values.</returns>
    /// <remarks>The command must return 5 distinct cursor names. Tuple positions do not promise database return order.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="unitOfWork"/> is <see langword="null"/>.</exception>
    protected async Task<(TR1?, TR2?, TR3?, TR4?, TR5?)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5>(DbConnection connection, NpgsqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteMultipleAsync(connection, parameters, async set =>
            (await FetchCursorAsync<TR1>(connection, set[0]),
             await FetchCursorAsync<TR2>(connection, set[1]),
             await FetchCursorAsync<TR3>(connection, set[2]),
             await FetchCursorAsync<TR4>(connection, set[3]),
             await FetchCursorAsync<TR5>(connection, set[4])), unitOfWork, 5);
    }

    /// <summary>
    /// Executes the command and maps 6 distinct returned PostgreSQL cursor names to tuple elements.
    /// </summary>
    /// <typeparam name="TR1">The value mapped from returned cursor 1.</typeparam>
    /// <typeparam name="TR2">The value mapped from returned cursor 2.</typeparam>
    /// <typeparam name="TR3">The value mapped from returned cursor 3.</typeparam>
    /// <typeparam name="TR4">The value mapped from returned cursor 4.</typeparam>
    /// <typeparam name="TR5">The value mapped from returned cursor 5.</typeparam>
    /// <typeparam name="TR6">The value mapped from returned cursor 6.</typeparam>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The provider parameters to add to the command, or <see langword="null"/> for none.</param>
    /// <param name="unitOfWork">The required unit of work that owns the cursor transaction.</param>
    /// <returns>A tuple whose positions follow the implementation-defined enumeration of distinct cursor names; empty cursors produce null reference values or default value-type values.</returns>
    /// <remarks>The command must return 6 distinct cursor names. Tuple positions do not promise database return order.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="unitOfWork"/> is <see langword="null"/>.</exception>
    protected async Task<(TR1?, TR2?, TR3?, TR4?, TR5?, TR6?)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6>(DbConnection connection, NpgsqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
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
    /// Executes the command and maps 7 distinct returned PostgreSQL cursor names to tuple elements.
    /// </summary>
    /// <typeparam name="TR1">The value mapped from returned cursor 1.</typeparam>
    /// <typeparam name="TR2">The value mapped from returned cursor 2.</typeparam>
    /// <typeparam name="TR3">The value mapped from returned cursor 3.</typeparam>
    /// <typeparam name="TR4">The value mapped from returned cursor 4.</typeparam>
    /// <typeparam name="TR5">The value mapped from returned cursor 5.</typeparam>
    /// <typeparam name="TR6">The value mapped from returned cursor 6.</typeparam>
    /// <typeparam name="TR7">The value mapped from returned cursor 7.</typeparam>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The provider parameters to add to the command, or <see langword="null"/> for none.</param>
    /// <param name="unitOfWork">The required unit of work that owns the cursor transaction.</param>
    /// <returns>A tuple whose positions follow the implementation-defined enumeration of distinct cursor names; empty cursors produce null reference values or default value-type values.</returns>
    /// <remarks>The command must return 7 distinct cursor names. Tuple positions do not promise database return order.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="unitOfWork"/> is <see langword="null"/>.</exception>
    protected async Task<(TR1?, TR2?, TR3?, TR4?, TR5?, TR6?, TR7?)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7>(DbConnection connection, NpgsqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
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
    /// Executes the command and maps 8 distinct returned PostgreSQL cursor names to tuple elements.
    /// </summary>
    /// <typeparam name="TR1">The value mapped from returned cursor 1.</typeparam>
    /// <typeparam name="TR2">The value mapped from returned cursor 2.</typeparam>
    /// <typeparam name="TR3">The value mapped from returned cursor 3.</typeparam>
    /// <typeparam name="TR4">The value mapped from returned cursor 4.</typeparam>
    /// <typeparam name="TR5">The value mapped from returned cursor 5.</typeparam>
    /// <typeparam name="TR6">The value mapped from returned cursor 6.</typeparam>
    /// <typeparam name="TR7">The value mapped from returned cursor 7.</typeparam>
    /// <typeparam name="TR8">The value mapped from returned cursor 8.</typeparam>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The provider parameters to add to the command, or <see langword="null"/> for none.</param>
    /// <param name="unitOfWork">The required unit of work that owns the cursor transaction.</param>
    /// <returns>A tuple whose positions follow the implementation-defined enumeration of distinct cursor names; empty cursors produce null reference values or default value-type values.</returns>
    /// <remarks>The command must return 8 distinct cursor names. Tuple positions do not promise database return order.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="unitOfWork"/> is <see langword="null"/>.</exception>
    protected async Task<(TR1?, TR2?, TR3?, TR4?, TR5?, TR6?, TR7?, TR8?)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8>(DbConnection connection, NpgsqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
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
    /// Executes the command and maps 9 distinct returned PostgreSQL cursor names to tuple elements.
    /// </summary>
    /// <typeparam name="TR1">The value mapped from returned cursor 1.</typeparam>
    /// <typeparam name="TR2">The value mapped from returned cursor 2.</typeparam>
    /// <typeparam name="TR3">The value mapped from returned cursor 3.</typeparam>
    /// <typeparam name="TR4">The value mapped from returned cursor 4.</typeparam>
    /// <typeparam name="TR5">The value mapped from returned cursor 5.</typeparam>
    /// <typeparam name="TR6">The value mapped from returned cursor 6.</typeparam>
    /// <typeparam name="TR7">The value mapped from returned cursor 7.</typeparam>
    /// <typeparam name="TR8">The value mapped from returned cursor 8.</typeparam>
    /// <typeparam name="TR9">The value mapped from returned cursor 9.</typeparam>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The provider parameters to add to the command, or <see langword="null"/> for none.</param>
    /// <param name="unitOfWork">The required unit of work that owns the cursor transaction.</param>
    /// <returns>A tuple whose positions follow the implementation-defined enumeration of distinct cursor names; empty cursors produce null reference values or default value-type values.</returns>
    /// <remarks>The command must return 9 distinct cursor names. Tuple positions do not promise database return order.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="unitOfWork"/> is <see langword="null"/>.</exception>
    protected async Task<(TR1?, TR2?, TR3?, TR4?, TR5?, TR6?, TR7?, TR8?, TR9?)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9>(DbConnection connection, NpgsqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
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
    /// Executes the command and maps 10 distinct returned PostgreSQL cursor names to tuple elements.
    /// </summary>
    /// <typeparam name="TR1">The value mapped from returned cursor 1.</typeparam>
    /// <typeparam name="TR2">The value mapped from returned cursor 2.</typeparam>
    /// <typeparam name="TR3">The value mapped from returned cursor 3.</typeparam>
    /// <typeparam name="TR4">The value mapped from returned cursor 4.</typeparam>
    /// <typeparam name="TR5">The value mapped from returned cursor 5.</typeparam>
    /// <typeparam name="TR6">The value mapped from returned cursor 6.</typeparam>
    /// <typeparam name="TR7">The value mapped from returned cursor 7.</typeparam>
    /// <typeparam name="TR8">The value mapped from returned cursor 8.</typeparam>
    /// <typeparam name="TR9">The value mapped from returned cursor 9.</typeparam>
    /// <typeparam name="TR10">The value mapped from returned cursor 10.</typeparam>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The provider parameters to add to the command, or <see langword="null"/> for none.</param>
    /// <param name="unitOfWork">The required unit of work that owns the cursor transaction.</param>
    /// <returns>A tuple whose positions follow the implementation-defined enumeration of distinct cursor names; empty cursors produce null reference values or default value-type values.</returns>
    /// <remarks>The command must return 10 distinct cursor names. Tuple positions do not promise database return order.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="unitOfWork"/> is <see langword="null"/>.</exception>
    protected async Task<(TR1?, TR2?, TR3?, TR4?, TR5?, TR6?, TR7?, TR8?, TR9?, TR10?)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10>(DbConnection connection, NpgsqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
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
    /// Executes the command and maps 11 distinct returned PostgreSQL cursor names to tuple elements.
    /// </summary>
    /// <typeparam name="TR1">The value mapped from returned cursor 1.</typeparam>
    /// <typeparam name="TR2">The value mapped from returned cursor 2.</typeparam>
    /// <typeparam name="TR3">The value mapped from returned cursor 3.</typeparam>
    /// <typeparam name="TR4">The value mapped from returned cursor 4.</typeparam>
    /// <typeparam name="TR5">The value mapped from returned cursor 5.</typeparam>
    /// <typeparam name="TR6">The value mapped from returned cursor 6.</typeparam>
    /// <typeparam name="TR7">The value mapped from returned cursor 7.</typeparam>
    /// <typeparam name="TR8">The value mapped from returned cursor 8.</typeparam>
    /// <typeparam name="TR9">The value mapped from returned cursor 9.</typeparam>
    /// <typeparam name="TR10">The value mapped from returned cursor 10.</typeparam>
    /// <typeparam name="TR11">The value mapped from returned cursor 11.</typeparam>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The provider parameters to add to the command, or <see langword="null"/> for none.</param>
    /// <param name="unitOfWork">The required unit of work that owns the cursor transaction.</param>
    /// <returns>A tuple whose positions follow the implementation-defined enumeration of distinct cursor names; empty cursors produce null reference values or default value-type values.</returns>
    /// <remarks>The command must return 11 distinct cursor names. Tuple positions do not promise database return order.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="unitOfWork"/> is <see langword="null"/>.</exception>
    protected async Task<(TR1?, TR2?, TR3?, TR4?, TR5?, TR6?, TR7?, TR8?, TR9?, TR10?, TR11?)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10, TR11>(DbConnection connection, NpgsqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
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
    /// Executes the command and maps 12 distinct returned PostgreSQL cursor names to tuple elements.
    /// </summary>
    /// <typeparam name="TR1">The value mapped from returned cursor 1.</typeparam>
    /// <typeparam name="TR2">The value mapped from returned cursor 2.</typeparam>
    /// <typeparam name="TR3">The value mapped from returned cursor 3.</typeparam>
    /// <typeparam name="TR4">The value mapped from returned cursor 4.</typeparam>
    /// <typeparam name="TR5">The value mapped from returned cursor 5.</typeparam>
    /// <typeparam name="TR6">The value mapped from returned cursor 6.</typeparam>
    /// <typeparam name="TR7">The value mapped from returned cursor 7.</typeparam>
    /// <typeparam name="TR8">The value mapped from returned cursor 8.</typeparam>
    /// <typeparam name="TR9">The value mapped from returned cursor 9.</typeparam>
    /// <typeparam name="TR10">The value mapped from returned cursor 10.</typeparam>
    /// <typeparam name="TR11">The value mapped from returned cursor 11.</typeparam>
    /// <typeparam name="TR12">The value mapped from returned cursor 12.</typeparam>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The provider parameters to add to the command, or <see langword="null"/> for none.</param>
    /// <param name="unitOfWork">The required unit of work that owns the cursor transaction.</param>
    /// <returns>A tuple whose positions follow the implementation-defined enumeration of distinct cursor names; empty cursors produce null reference values or default value-type values.</returns>
    /// <remarks>The command must return 12 distinct cursor names. Tuple positions do not promise database return order.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="unitOfWork"/> is <see langword="null"/>.</exception>
    protected async Task<(TR1?, TR2?, TR3?, TR4?, TR5?, TR6?, TR7?, TR8?, TR9?, TR10?, TR11?, TR12?)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10, TR11, TR12>(DbConnection connection, NpgsqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
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
    /// Executes the command and maps 13 distinct returned PostgreSQL cursor names to tuple elements.
    /// </summary>
    /// <typeparam name="TR1">The value mapped from returned cursor 1.</typeparam>
    /// <typeparam name="TR2">The value mapped from returned cursor 2.</typeparam>
    /// <typeparam name="TR3">The value mapped from returned cursor 3.</typeparam>
    /// <typeparam name="TR4">The value mapped from returned cursor 4.</typeparam>
    /// <typeparam name="TR5">The value mapped from returned cursor 5.</typeparam>
    /// <typeparam name="TR6">The value mapped from returned cursor 6.</typeparam>
    /// <typeparam name="TR7">The value mapped from returned cursor 7.</typeparam>
    /// <typeparam name="TR8">The value mapped from returned cursor 8.</typeparam>
    /// <typeparam name="TR9">The value mapped from returned cursor 9.</typeparam>
    /// <typeparam name="TR10">The value mapped from returned cursor 10.</typeparam>
    /// <typeparam name="TR11">The value mapped from returned cursor 11.</typeparam>
    /// <typeparam name="TR12">The value mapped from returned cursor 12.</typeparam>
    /// <typeparam name="TR13">The value mapped from returned cursor 13.</typeparam>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The provider parameters to add to the command, or <see langword="null"/> for none.</param>
    /// <param name="unitOfWork">The required unit of work that owns the cursor transaction.</param>
    /// <returns>A tuple whose positions follow the implementation-defined enumeration of distinct cursor names; empty cursors produce null reference values or default value-type values.</returns>
    /// <remarks>The command must return 13 distinct cursor names. Tuple positions do not promise database return order.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="unitOfWork"/> is <see langword="null"/>.</exception>
    protected async Task<(TR1?, TR2?, TR3?, TR4?, TR5?, TR6?, TR7?, TR8?, TR9?, TR10?, TR11?, TR12?, TR13?)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10, TR11, TR12, TR13>(DbConnection connection, NpgsqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
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
    /// Executes the command and maps 14 distinct returned PostgreSQL cursor names to tuple elements.
    /// </summary>
    /// <typeparam name="TR1">The value mapped from returned cursor 1.</typeparam>
    /// <typeparam name="TR2">The value mapped from returned cursor 2.</typeparam>
    /// <typeparam name="TR3">The value mapped from returned cursor 3.</typeparam>
    /// <typeparam name="TR4">The value mapped from returned cursor 4.</typeparam>
    /// <typeparam name="TR5">The value mapped from returned cursor 5.</typeparam>
    /// <typeparam name="TR6">The value mapped from returned cursor 6.</typeparam>
    /// <typeparam name="TR7">The value mapped from returned cursor 7.</typeparam>
    /// <typeparam name="TR8">The value mapped from returned cursor 8.</typeparam>
    /// <typeparam name="TR9">The value mapped from returned cursor 9.</typeparam>
    /// <typeparam name="TR10">The value mapped from returned cursor 10.</typeparam>
    /// <typeparam name="TR11">The value mapped from returned cursor 11.</typeparam>
    /// <typeparam name="TR12">The value mapped from returned cursor 12.</typeparam>
    /// <typeparam name="TR13">The value mapped from returned cursor 13.</typeparam>
    /// <typeparam name="TR14">The value mapped from returned cursor 14.</typeparam>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The provider parameters to add to the command, or <see langword="null"/> for none.</param>
    /// <param name="unitOfWork">The required unit of work that owns the cursor transaction.</param>
    /// <returns>A tuple whose positions follow the implementation-defined enumeration of distinct cursor names; empty cursors produce null reference values or default value-type values.</returns>
    /// <remarks>The command must return 14 distinct cursor names. Tuple positions do not promise database return order.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="unitOfWork"/> is <see langword="null"/>.</exception>
    protected async Task<(TR1?, TR2?, TR3?, TR4?, TR5?, TR6?, TR7?, TR8?, TR9?, TR10?, TR11?, TR12?, TR13?, TR14?)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10, TR11, TR12, TR13, TR14>(DbConnection connection, NpgsqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
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
    /// Executes the command and maps 15 distinct returned PostgreSQL cursor names to tuple elements.
    /// </summary>
    /// <typeparam name="TR1">The value mapped from returned cursor 1.</typeparam>
    /// <typeparam name="TR2">The value mapped from returned cursor 2.</typeparam>
    /// <typeparam name="TR3">The value mapped from returned cursor 3.</typeparam>
    /// <typeparam name="TR4">The value mapped from returned cursor 4.</typeparam>
    /// <typeparam name="TR5">The value mapped from returned cursor 5.</typeparam>
    /// <typeparam name="TR6">The value mapped from returned cursor 6.</typeparam>
    /// <typeparam name="TR7">The value mapped from returned cursor 7.</typeparam>
    /// <typeparam name="TR8">The value mapped from returned cursor 8.</typeparam>
    /// <typeparam name="TR9">The value mapped from returned cursor 9.</typeparam>
    /// <typeparam name="TR10">The value mapped from returned cursor 10.</typeparam>
    /// <typeparam name="TR11">The value mapped from returned cursor 11.</typeparam>
    /// <typeparam name="TR12">The value mapped from returned cursor 12.</typeparam>
    /// <typeparam name="TR13">The value mapped from returned cursor 13.</typeparam>
    /// <typeparam name="TR14">The value mapped from returned cursor 14.</typeparam>
    /// <typeparam name="TR15">The value mapped from returned cursor 15.</typeparam>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The provider parameters to add to the command, or <see langword="null"/> for none.</param>
    /// <param name="unitOfWork">The required unit of work that owns the cursor transaction.</param>
    /// <returns>A tuple whose positions follow the implementation-defined enumeration of distinct cursor names; empty cursors produce null reference values or default value-type values.</returns>
    /// <remarks>The command must return 15 distinct cursor names. Tuple positions do not promise database return order.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="unitOfWork"/> is <see langword="null"/>.</exception>
    protected async Task<(TR1?, TR2?, TR3?, TR4?, TR5?, TR6?, TR7?, TR8?, TR9?, TR10?, TR11?, TR12?, TR13?, TR14?, TR15?)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10, TR11, TR12, TR13, TR14, TR15>(DbConnection connection, NpgsqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
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
    /// Executes the command and maps 16 distinct returned PostgreSQL cursor names to tuple elements.
    /// </summary>
    /// <typeparam name="TR1">The value mapped from returned cursor 1.</typeparam>
    /// <typeparam name="TR2">The value mapped from returned cursor 2.</typeparam>
    /// <typeparam name="TR3">The value mapped from returned cursor 3.</typeparam>
    /// <typeparam name="TR4">The value mapped from returned cursor 4.</typeparam>
    /// <typeparam name="TR5">The value mapped from returned cursor 5.</typeparam>
    /// <typeparam name="TR6">The value mapped from returned cursor 6.</typeparam>
    /// <typeparam name="TR7">The value mapped from returned cursor 7.</typeparam>
    /// <typeparam name="TR8">The value mapped from returned cursor 8.</typeparam>
    /// <typeparam name="TR9">The value mapped from returned cursor 9.</typeparam>
    /// <typeparam name="TR10">The value mapped from returned cursor 10.</typeparam>
    /// <typeparam name="TR11">The value mapped from returned cursor 11.</typeparam>
    /// <typeparam name="TR12">The value mapped from returned cursor 12.</typeparam>
    /// <typeparam name="TR13">The value mapped from returned cursor 13.</typeparam>
    /// <typeparam name="TR14">The value mapped from returned cursor 14.</typeparam>
    /// <typeparam name="TR15">The value mapped from returned cursor 15.</typeparam>
    /// <typeparam name="TR16">The value mapped from returned cursor 16.</typeparam>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The provider parameters to add to the command, or <see langword="null"/> for none.</param>
    /// <param name="unitOfWork">The required unit of work that owns the cursor transaction.</param>
    /// <returns>A tuple whose positions follow the implementation-defined enumeration of distinct cursor names; empty cursors produce null reference values or default value-type values.</returns>
    /// <remarks>The command must return 16 distinct cursor names. Tuple positions do not promise database return order.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="unitOfWork"/> is <see langword="null"/>.</exception>
    protected async Task<(TR1?, TR2?, TR3?, TR4?, TR5?, TR6?, TR7?, TR8?, TR9?, TR10?, TR11?, TR12?, TR13?, TR14?, TR15?, TR16?)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10, TR11, TR12, TR13, TR14, TR15, TR16>(DbConnection connection, NpgsqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
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
    /// Fetches and maps all rows from a PostgreSQL cursor.
    /// </summary>
    /// <typeparam name="T">The value produced while consuming the command results.</typeparam>
    /// <param name="connection">The connection associated with the transaction that owns the cursor.</param>
    /// <param name="cursorName">The cursor name returned by the stored procedure.</param>
    /// <returns>The value mapped from the named PostgreSQL cursor.</returns>
    private async Task<T?> FetchCursorAsync<T>(DbConnection connection, string cursorName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $@"FETCH ALL FROM ""{cursorName}""";
        using var reader = command.ExecuteReader();
        return await reader.TranslateAndMoveAsync<T>();
    }

    #endregion
}
