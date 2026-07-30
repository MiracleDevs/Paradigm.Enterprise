using Microsoft.Data.SqlClient;
using Paradigm.Enterprise.Data.Extensions;
using Paradigm.Enterprise.Domain.Uow;
using System.Data;
using System.Data.Common;

namespace Paradigm.Enterprise.Data.SqlServer.StoredProcedures;

/// <summary>
/// Defines a SQL Server stored procedure whose application parameter object is mapped to provider parameters.
/// </summary>
/// <typeparam name="TParameters">The application type that supplies stored-procedure parameters.</typeparam>
/// <remarks>
/// A mapper for <typeparamref name="TParameters"/> must be registered with
/// <see cref="SqlParameterMapperFactory"/> before executing with a non-null parameter object.
/// </remarks>
/// <example>
/// Define the parameter contract, its mapper, and the procedure together:
/// <code>
/// static async Task RunAsync(DbConnection connection, IUnitOfWork unitOfWork)
/// {
///     SqlParameterMapperFactory.RegisterMapper&lt;ArchiveOrderParameters&gt;(
///         () =&gt; new ArchiveOrderParameterMapper());
///
///     var procedure = new ArchiveOrderProcedure();
///     await procedure.ExecuteAsync(
///         connection,
///         new ArchiveOrderParameters(42, "Customer request"),
///         unitOfWork);
/// }
///
/// sealed record ArchiveOrderParameters(int OrderId, string? Reason);
///
/// sealed class ArchiveOrderParameterMapper : SqlParameterMapperBase
/// {
///     protected override void AddSqlParameters(object parameters)
///     {
///         var value = (ArchiveOrderParameters)parameters;
///         AddSqlParameter("@OrderId", value.OrderId);
///         AddSqlParameter("@Reason", value.Reason);
///     }
/// }
///
/// sealed class ArchiveOrderProcedure : StoredProcedureBase&lt;ArchiveOrderParameters&gt;
/// {
///     protected override string StoredProcedureName =&gt; "dbo.ArchiveOrder";
///     protected override int? ExecutionTimeout =&gt; 30;
/// }
/// </code>
/// The supplied connection remains caller-owned. On success it stays open with an active transaction;
/// without one, execution closes it even when it was already open on entry. A failure before the
/// success-path close can leave it open.
/// </example>
public abstract class StoredProcedureBase<TParameters> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure with parameters mapped from <typeparamref name="TParameters"/>.
    /// </summary>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The application parameter object to map, or <see langword="null"/> for no parameters.</param>
    /// <param name="unitOfWork">The optional unit of work whose active transaction is attached to the command.</param>
    /// <returns>A task that completes when the command finishes.</returns>
    /// <exception cref="InvalidOperationException">
    /// A non-null parameter object is supplied and no SQL Server parameter mapper is registered for
    /// <typeparamref name="TParameters"/>.
    /// </exception>
    /// <remarks>
    /// On success without an active unit-of-work transaction, the connection is closed even if it was
    /// already open on entry. With an active transaction, the command enlists and the connection remains
    /// open. A mapping or execution failure can leave the connection open; the caller owns recovery
    /// and disposal.
    /// </remarks>
    public async Task ExecuteAsync(DbConnection connection, TParameters? parameters, IUnitOfWork? unitOfWork = null)
    {
        await ExecuteAsync(connection, GetSqlParameters(parameters), unitOfWork);
    }
}

/// <summary>
/// Provides command execution and result-set mapping for SQL Server stored procedures.
/// </summary>
/// <remarks>
/// The caller owns the supplied connection. On successful ordinary execution without an active
/// unit-of-work transaction, the method closes the connection even if it was already open on entry.
/// With an active transaction, the command enlists and the connection remains open. An exception
/// before the success-path close can leave the connection open; the caller owns recovery and disposal.
/// </remarks>
/// <example>
/// Define a procedure with no application parameters and choose connection lifetime through transaction use:
/// <code>
/// static async Task RunAsync(DbConnection connection, IUnitOfWork unitOfWork)
/// {
///     var procedure = new RefreshReportingProcedure();
///
///     // On success without an active transaction, ExecuteAsync closes the connection.
///     await procedure.ExecuteAsync(connection);
///
///     // An active unit-of-work transaction owns connection lifetime and receives the command.
///     using ITransaction transaction = unitOfWork.CreateTransaction();
///     try
///     {
///         await procedure.ExecuteAsync(connection, unitOfWork);
///         transaction.Commit();
///     }
///     catch
///     {
///         transaction.Rollback();
///         throw;
///     }
/// }
///
/// sealed class RefreshReportingProcedure : StoredProcedureBase
/// {
///     protected override string StoredProcedureName =&gt; "dbo.RefreshReporting";
/// }
/// </code>
/// The transaction and command must use compatible connections; this API is not a distributed transaction coordinator.
/// </example>
public abstract class StoredProcedureBase
{
    #region Properties

    /// <summary>
    /// Gets the provider command text identifying the stored procedure to execute.
    /// </summary>
    /// <value>
    /// The schema-qualified or provider-resolvable stored-procedure name assigned to
    /// <see cref="DbCommand.CommandText"/>.
    /// </value>
    protected abstract string StoredProcedureName { get; }

    /// <summary>
    /// Gets the optional command timeout in seconds.
    /// </summary>
    /// <value>
    /// The timeout assigned to <see cref="DbCommand.CommandTimeout"/>, or <see langword="null"/> to
    /// retain the provider command's default timeout.
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
    /// On success without an active unit-of-work transaction, the connection is closed even if it was
    /// open on entry. With an active transaction, connection lifetime remains with the transaction
    /// owner. An execution failure can leave the connection open.
    /// </remarks>
    public async Task ExecuteAsync(DbConnection connection, IUnitOfWork? unitOfWork = null)
    {
        await ExecuteAsync(connection, null, unitOfWork);
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// Maps an application parameter object to SQL Server parameters.
    /// </summary>
    /// <typeparam name="TParameters">The application parameter type with a registered mapper.</typeparam>
    /// <param name="parameters">The parameter object to map, or <see langword="null"/>.</param>
    /// <returns>The mapped provider parameters, or <see langword="null"/> when <paramref name="parameters"/> is null.</returns>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="parameters"/> is non-null and no mapper is registered for
    /// <typeparamref name="TParameters"/>.
    /// </exception>
    /// <remarks>
    /// The mapper is disposed immediately after mapping. Its returned parameters must remain usable
    /// independently and are later detached from the command on successful execution.
    /// </remarks>
    protected SqlParameter[]? GetSqlParameters<TParameters>(TParameters? parameters)
    {
        if (parameters is null) return null;
        using var mapper = SqlParameterMapperFactory.GetMapper<TParameters>();
        return mapper.Map(parameters);
    }

    /// <summary>
    /// Executes the stored procedure without consuming a result set.
    /// </summary>
    /// <param name="connection">The caller-owned connection used by the command.</param>
    /// <param name="parameters">The provider parameters to attach, or <see langword="null"/> for none.</param>
    /// <param name="unitOfWork">The optional unit of work whose active transaction receives the command.</param>
    /// <remarks>
    /// Parameters are cleared from the command only on success. On success without an active
    /// transaction, the connection is closed regardless of its entry state; with one, it remains open.
    /// An exception can leave the connection open.
    /// </remarks>
    protected async Task ExecuteAsync(DbConnection connection, SqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
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
    /// Executes the stored procedure and delegates result consumption.
    /// </summary>
    /// <typeparam name="T">The value produced while consuming the command results.</typeparam>
    /// <param name="connection">The caller-owned connection used by the command.</param>
    /// <param name="parameters">The provider parameters to attach, or <see langword="null"/> for none.</param>
    /// <param name="readerExecutedAction">The delegate that consumes the open data reader.</param>
    /// <param name="unitOfWork">The optional unit of work whose active transaction receives the command.</param>
    /// <returns>The value produced by the supplied result-processing delegate.</returns>
    /// <remarks>
    /// The reader and command are disposed by this method. Parameters are cleared and a nontransactional
    /// connection is closed only on success. A command, reader, or delegate failure can leave the
    /// caller-owned connection open.
    /// </remarks>
    protected async Task<T> ExecuteAsync<T>(DbConnection connection, SqlParameter[]? parameters, Func<DbDataReader, Task<T>> readerExecutedAction, IUnitOfWork? unitOfWork = null)
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
    /// Executes the command and maps its first result set.
    /// </summary>
    /// <typeparam name="TR1">The value mapped from result set 1.</typeparam>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The provider parameters to add to the command, or <see langword="null"/> for none.</param>
    /// <param name="unitOfWork">The optional unit of work whose active transaction is attached to the command.</param>
    /// <returns>The mapped value. An empty scalar or mapped-object result returns <see langword="null"/> for a reference type or the default for a value type; a concrete <c>IList</c> result returns an empty list.</returns>
    protected async Task<TR1?> ExecuteAsync<TR1>(DbConnection connection, SqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync(connection, parameters, async reader => await reader.TranslateAsync<TR1>(), unitOfWork);
    }

    /// <summary>
    /// Executes the command and maps 2 result sets in declaration order.
    /// </summary>
    /// <typeparam name="TR1">The value mapped from result set 1.</typeparam>
    /// <typeparam name="TR2">The value mapped from result set 2.</typeparam>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The provider parameters to add to the command, or <see langword="null"/> for none.</param>
    /// <param name="unitOfWork">The optional unit of work whose active transaction is attached to the command.</param>
    /// <returns>An ordered tuple containing one mapped value per result set; empty scalar or mapped-object sets produce null reference values or default value-type values, while concrete <c>IList</c> result types produce empty lists.</returns>
    /// <remarks>Each tuple element corresponds to the result set at the same one-based position.</remarks>
    protected async Task<(TR1?, TR2?)> ExecuteAsync<TR1, TR2>(DbConnection connection, SqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync(connection, parameters, async reader =>
            (await reader.TranslateAndMoveAsync<TR1>(),
             await reader.TranslateAndMoveAsync<TR2>()), unitOfWork);
    }

    /// <summary>
    /// Executes the command and maps 3 result sets in declaration order.
    /// </summary>
    /// <typeparam name="TR1">The value mapped from result set 1.</typeparam>
    /// <typeparam name="TR2">The value mapped from result set 2.</typeparam>
    /// <typeparam name="TR3">The value mapped from result set 3.</typeparam>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The provider parameters to add to the command, or <see langword="null"/> for none.</param>
    /// <param name="unitOfWork">The optional unit of work whose active transaction is attached to the command.</param>
    /// <returns>An ordered tuple containing one mapped value per result set; empty scalar or mapped-object sets produce null reference values or default value-type values, while concrete <c>IList</c> result types produce empty lists.</returns>
    /// <remarks>Each tuple element corresponds to the result set at the same one-based position.</remarks>
    protected async Task<(TR1?, TR2?, TR3?)> ExecuteAsync<TR1, TR2, TR3>(DbConnection connection, SqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync(connection, parameters, async reader =>
            (await reader.TranslateAndMoveAsync<TR1>(),
             await reader.TranslateAndMoveAsync<TR2>(),
             await reader.TranslateAndMoveAsync<TR3>()), unitOfWork);
    }

    /// <summary>
    /// Executes the command and maps 4 result sets in declaration order.
    /// </summary>
    /// <typeparam name="TR1">The value mapped from result set 1.</typeparam>
    /// <typeparam name="TR2">The value mapped from result set 2.</typeparam>
    /// <typeparam name="TR3">The value mapped from result set 3.</typeparam>
    /// <typeparam name="TR4">The value mapped from result set 4.</typeparam>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The provider parameters to add to the command, or <see langword="null"/> for none.</param>
    /// <param name="unitOfWork">The optional unit of work whose active transaction is attached to the command.</param>
    /// <returns>An ordered tuple containing one mapped value per result set; empty scalar or mapped-object sets produce null reference values or default value-type values, while concrete <c>IList</c> result types produce empty lists.</returns>
    /// <remarks>Each tuple element corresponds to the result set at the same one-based position.</remarks>
    protected async Task<(TR1?, TR2?, TR3?, TR4?)> ExecuteAsync<TR1, TR2, TR3, TR4>(DbConnection connection, SqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync(connection, parameters, async reader =>
            (await reader.TranslateAndMoveAsync<TR1>(),
                await reader.TranslateAndMoveAsync<TR2>(),
                await reader.TranslateAndMoveAsync<TR3>(),
                await reader.TranslateAndMoveAsync<TR4>()), unitOfWork);
    }

    /// <summary>
    /// Executes the command and maps 5 result sets in declaration order.
    /// </summary>
    /// <typeparam name="TR1">The value mapped from result set 1.</typeparam>
    /// <typeparam name="TR2">The value mapped from result set 2.</typeparam>
    /// <typeparam name="TR3">The value mapped from result set 3.</typeparam>
    /// <typeparam name="TR4">The value mapped from result set 4.</typeparam>
    /// <typeparam name="TR5">The value mapped from result set 5.</typeparam>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The provider parameters to add to the command, or <see langword="null"/> for none.</param>
    /// <param name="unitOfWork">The optional unit of work whose active transaction is attached to the command.</param>
    /// <returns>An ordered tuple containing one mapped value per result set; empty scalar or mapped-object sets produce null reference values or default value-type values, while concrete <c>IList</c> result types produce empty lists.</returns>
    /// <remarks>Each tuple element corresponds to the result set at the same one-based position.</remarks>
    protected async Task<(TR1?, TR2?, TR3?, TR4?, TR5?)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5>(DbConnection connection, SqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync(connection, parameters, async reader =>
            (await reader.TranslateAndMoveAsync<TR1>(),
                await reader.TranslateAndMoveAsync<TR2>(),
                await reader.TranslateAndMoveAsync<TR3>(),
                await reader.TranslateAndMoveAsync<TR4>(),
                await reader.TranslateAndMoveAsync<TR5>()), unitOfWork);
    }

    /// <summary>
    /// Executes the command and maps 6 result sets in declaration order.
    /// </summary>
    /// <typeparam name="TR1">The value mapped from result set 1.</typeparam>
    /// <typeparam name="TR2">The value mapped from result set 2.</typeparam>
    /// <typeparam name="TR3">The value mapped from result set 3.</typeparam>
    /// <typeparam name="TR4">The value mapped from result set 4.</typeparam>
    /// <typeparam name="TR5">The value mapped from result set 5.</typeparam>
    /// <typeparam name="TR6">The value mapped from result set 6.</typeparam>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The provider parameters to add to the command, or <see langword="null"/> for none.</param>
    /// <param name="unitOfWork">The optional unit of work whose active transaction is attached to the command.</param>
    /// <returns>An ordered tuple containing one mapped value per result set; empty scalar or mapped-object sets produce null reference values or default value-type values, while concrete <c>IList</c> result types produce empty lists.</returns>
    /// <remarks>Each tuple element corresponds to the result set at the same one-based position.</remarks>
    protected async Task<(TR1?, TR2?, TR3?, TR4?, TR5?, TR6?)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6>(DbConnection connection, SqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync(connection, parameters, async reader =>
            (await reader.TranslateAndMoveAsync<TR1>(),
                await reader.TranslateAndMoveAsync<TR2>(),
                await reader.TranslateAndMoveAsync<TR3>(),
                await reader.TranslateAndMoveAsync<TR4>(),
                await reader.TranslateAndMoveAsync<TR5>(),
                await reader.TranslateAndMoveAsync<TR6>()), unitOfWork);
    }

    /// <summary>
    /// Executes the command and maps 7 result sets in declaration order.
    /// </summary>
    /// <typeparam name="TR1">The value mapped from result set 1.</typeparam>
    /// <typeparam name="TR2">The value mapped from result set 2.</typeparam>
    /// <typeparam name="TR3">The value mapped from result set 3.</typeparam>
    /// <typeparam name="TR4">The value mapped from result set 4.</typeparam>
    /// <typeparam name="TR5">The value mapped from result set 5.</typeparam>
    /// <typeparam name="TR6">The value mapped from result set 6.</typeparam>
    /// <typeparam name="TR7">The value mapped from result set 7.</typeparam>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The provider parameters to add to the command, or <see langword="null"/> for none.</param>
    /// <param name="unitOfWork">The optional unit of work whose active transaction is attached to the command.</param>
    /// <returns>An ordered tuple containing one mapped value per result set; empty scalar or mapped-object sets produce null reference values or default value-type values, while concrete <c>IList</c> result types produce empty lists.</returns>
    /// <remarks>Each tuple element corresponds to the result set at the same one-based position.</remarks>
    protected async Task<(TR1?, TR2?, TR3?, TR4?, TR5?, TR6?, TR7?)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7>(DbConnection connection, SqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync(connection, parameters, async reader =>
            (await reader.TranslateAndMoveAsync<TR1>(),
                await reader.TranslateAndMoveAsync<TR2>(),
                await reader.TranslateAndMoveAsync<TR3>(),
                await reader.TranslateAndMoveAsync<TR4>(),
                await reader.TranslateAndMoveAsync<TR5>(),
                await reader.TranslateAndMoveAsync<TR6>(),
                await reader.TranslateAndMoveAsync<TR7>()), unitOfWork);
    }

    /// <summary>
    /// Executes the command and maps 8 result sets in declaration order.
    /// </summary>
    /// <typeparam name="TR1">The value mapped from result set 1.</typeparam>
    /// <typeparam name="TR2">The value mapped from result set 2.</typeparam>
    /// <typeparam name="TR3">The value mapped from result set 3.</typeparam>
    /// <typeparam name="TR4">The value mapped from result set 4.</typeparam>
    /// <typeparam name="TR5">The value mapped from result set 5.</typeparam>
    /// <typeparam name="TR6">The value mapped from result set 6.</typeparam>
    /// <typeparam name="TR7">The value mapped from result set 7.</typeparam>
    /// <typeparam name="TR8">The value mapped from result set 8.</typeparam>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The provider parameters to add to the command, or <see langword="null"/> for none.</param>
    /// <param name="unitOfWork">The optional unit of work whose active transaction is attached to the command.</param>
    /// <returns>An ordered tuple containing one mapped value per result set; empty scalar or mapped-object sets produce null reference values or default value-type values, while concrete <c>IList</c> result types produce empty lists.</returns>
    /// <remarks>Each tuple element corresponds to the result set at the same one-based position.</remarks>
    protected async Task<(TR1?, TR2?, TR3?, TR4?, TR5?, TR6?, TR7?, TR8?)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8>(DbConnection connection, SqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync(connection, parameters, async reader =>
            (await reader.TranslateAndMoveAsync<TR1>(),
                await reader.TranslateAndMoveAsync<TR2>(),
                await reader.TranslateAndMoveAsync<TR3>(),
                await reader.TranslateAndMoveAsync<TR4>(),
                await reader.TranslateAndMoveAsync<TR5>(),
                await reader.TranslateAndMoveAsync<TR6>(),
                await reader.TranslateAndMoveAsync<TR7>(),
                await reader.TranslateAndMoveAsync<TR8>()), unitOfWork);
    }

    /// <summary>
    /// Executes the command and maps 9 result sets in declaration order.
    /// </summary>
    /// <typeparam name="TR1">The value mapped from result set 1.</typeparam>
    /// <typeparam name="TR2">The value mapped from result set 2.</typeparam>
    /// <typeparam name="TR3">The value mapped from result set 3.</typeparam>
    /// <typeparam name="TR4">The value mapped from result set 4.</typeparam>
    /// <typeparam name="TR5">The value mapped from result set 5.</typeparam>
    /// <typeparam name="TR6">The value mapped from result set 6.</typeparam>
    /// <typeparam name="TR7">The value mapped from result set 7.</typeparam>
    /// <typeparam name="TR8">The value mapped from result set 8.</typeparam>
    /// <typeparam name="TR9">The value mapped from result set 9.</typeparam>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The provider parameters to add to the command, or <see langword="null"/> for none.</param>
    /// <param name="unitOfWork">The optional unit of work whose active transaction is attached to the command.</param>
    /// <returns>An ordered tuple containing one mapped value per result set; empty scalar or mapped-object sets produce null reference values or default value-type values, while concrete <c>IList</c> result types produce empty lists.</returns>
    /// <remarks>Each tuple element corresponds to the result set at the same one-based position.</remarks>
    protected async Task<(TR1?, TR2?, TR3?, TR4?, TR5?, TR6?, TR7?, TR8?, TR9?)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9>(DbConnection connection, SqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync(connection, parameters, async reader =>
            (await reader.TranslateAndMoveAsync<TR1>(),
                await reader.TranslateAndMoveAsync<TR2>(),
                await reader.TranslateAndMoveAsync<TR3>(),
                await reader.TranslateAndMoveAsync<TR4>(),
                await reader.TranslateAndMoveAsync<TR5>(),
                await reader.TranslateAndMoveAsync<TR6>(),
                await reader.TranslateAndMoveAsync<TR7>(),
                await reader.TranslateAndMoveAsync<TR8>(),
                await reader.TranslateAndMoveAsync<TR9>()), unitOfWork);
    }

    /// <summary>
    /// Executes the command and maps 10 result sets in declaration order.
    /// </summary>
    /// <typeparam name="TR1">The value mapped from result set 1.</typeparam>
    /// <typeparam name="TR2">The value mapped from result set 2.</typeparam>
    /// <typeparam name="TR3">The value mapped from result set 3.</typeparam>
    /// <typeparam name="TR4">The value mapped from result set 4.</typeparam>
    /// <typeparam name="TR5">The value mapped from result set 5.</typeparam>
    /// <typeparam name="TR6">The value mapped from result set 6.</typeparam>
    /// <typeparam name="TR7">The value mapped from result set 7.</typeparam>
    /// <typeparam name="TR8">The value mapped from result set 8.</typeparam>
    /// <typeparam name="TR9">The value mapped from result set 9.</typeparam>
    /// <typeparam name="TR10">The value mapped from result set 10.</typeparam>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The provider parameters to add to the command, or <see langword="null"/> for none.</param>
    /// <param name="unitOfWork">The optional unit of work whose active transaction is attached to the command.</param>
    /// <returns>An ordered tuple containing one mapped value per result set; empty scalar or mapped-object sets produce null reference values or default value-type values, while concrete <c>IList</c> result types produce empty lists.</returns>
    /// <remarks>Each tuple element corresponds to the result set at the same one-based position.</remarks>
    protected async Task<(TR1?, TR2?, TR3?, TR4?, TR5?, TR6?, TR7?, TR8?, TR9?, TR10?)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10>(DbConnection connection, SqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync(connection, parameters, async reader =>
            (await reader.TranslateAndMoveAsync<TR1>(),
                await reader.TranslateAndMoveAsync<TR2>(),
                await reader.TranslateAndMoveAsync<TR3>(),
                await reader.TranslateAndMoveAsync<TR4>(),
                await reader.TranslateAndMoveAsync<TR5>(),
                await reader.TranslateAndMoveAsync<TR6>(),
                await reader.TranslateAndMoveAsync<TR7>(),
                await reader.TranslateAndMoveAsync<TR8>(),
                await reader.TranslateAndMoveAsync<TR9>(),
                await reader.TranslateAndMoveAsync<TR10>()), unitOfWork);
    }

    /// <summary>
    /// Executes the command and maps 11 result sets in declaration order.
    /// </summary>
    /// <typeparam name="TR1">The value mapped from result set 1.</typeparam>
    /// <typeparam name="TR2">The value mapped from result set 2.</typeparam>
    /// <typeparam name="TR3">The value mapped from result set 3.</typeparam>
    /// <typeparam name="TR4">The value mapped from result set 4.</typeparam>
    /// <typeparam name="TR5">The value mapped from result set 5.</typeparam>
    /// <typeparam name="TR6">The value mapped from result set 6.</typeparam>
    /// <typeparam name="TR7">The value mapped from result set 7.</typeparam>
    /// <typeparam name="TR8">The value mapped from result set 8.</typeparam>
    /// <typeparam name="TR9">The value mapped from result set 9.</typeparam>
    /// <typeparam name="TR10">The value mapped from result set 10.</typeparam>
    /// <typeparam name="TR11">The value mapped from result set 11.</typeparam>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The provider parameters to add to the command, or <see langword="null"/> for none.</param>
    /// <param name="unitOfWork">The optional unit of work whose active transaction is attached to the command.</param>
    /// <returns>An ordered tuple containing one mapped value per result set; empty scalar or mapped-object sets produce null reference values or default value-type values, while concrete <c>IList</c> result types produce empty lists.</returns>
    /// <remarks>Each tuple element corresponds to the result set at the same one-based position.</remarks>
    protected async Task<(TR1?, TR2?, TR3?, TR4?, TR5?, TR6?, TR7?, TR8?, TR9?, TR10?, TR11?)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10, TR11>(DbConnection connection, SqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync(connection, parameters, async reader =>
            (await reader.TranslateAndMoveAsync<TR1>(),
                await reader.TranslateAndMoveAsync<TR2>(),
                await reader.TranslateAndMoveAsync<TR3>(),
                await reader.TranslateAndMoveAsync<TR4>(),
                await reader.TranslateAndMoveAsync<TR5>(),
                await reader.TranslateAndMoveAsync<TR6>(),
                await reader.TranslateAndMoveAsync<TR7>(),
                await reader.TranslateAndMoveAsync<TR8>(),
                await reader.TranslateAndMoveAsync<TR9>(),
                await reader.TranslateAndMoveAsync<TR10>(),
                await reader.TranslateAndMoveAsync<TR11>()), unitOfWork);
    }

    /// <summary>
    /// Executes the command and maps 12 result sets in declaration order.
    /// </summary>
    /// <typeparam name="TR1">The value mapped from result set 1.</typeparam>
    /// <typeparam name="TR2">The value mapped from result set 2.</typeparam>
    /// <typeparam name="TR3">The value mapped from result set 3.</typeparam>
    /// <typeparam name="TR4">The value mapped from result set 4.</typeparam>
    /// <typeparam name="TR5">The value mapped from result set 5.</typeparam>
    /// <typeparam name="TR6">The value mapped from result set 6.</typeparam>
    /// <typeparam name="TR7">The value mapped from result set 7.</typeparam>
    /// <typeparam name="TR8">The value mapped from result set 8.</typeparam>
    /// <typeparam name="TR9">The value mapped from result set 9.</typeparam>
    /// <typeparam name="TR10">The value mapped from result set 10.</typeparam>
    /// <typeparam name="TR11">The value mapped from result set 11.</typeparam>
    /// <typeparam name="TR12">The value mapped from result set 12.</typeparam>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The provider parameters to add to the command, or <see langword="null"/> for none.</param>
    /// <param name="unitOfWork">The optional unit of work whose active transaction is attached to the command.</param>
    /// <returns>An ordered tuple containing one mapped value per result set; empty scalar or mapped-object sets produce null reference values or default value-type values, while concrete <c>IList</c> result types produce empty lists.</returns>
    /// <remarks>Each tuple element corresponds to the result set at the same one-based position.</remarks>
    protected async Task<(TR1?, TR2?, TR3?, TR4?, TR5?, TR6?, TR7?, TR8?, TR9?, TR10?, TR11?, TR12?)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10, TR11, TR12>(DbConnection connection, SqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync(connection, parameters, async reader =>
            (await reader.TranslateAndMoveAsync<TR1>(),
                await reader.TranslateAndMoveAsync<TR2>(),
                await reader.TranslateAndMoveAsync<TR3>(),
                await reader.TranslateAndMoveAsync<TR4>(),
                await reader.TranslateAndMoveAsync<TR5>(),
                await reader.TranslateAndMoveAsync<TR6>(),
                await reader.TranslateAndMoveAsync<TR7>(),
                await reader.TranslateAndMoveAsync<TR8>(),
                await reader.TranslateAndMoveAsync<TR9>(),
                await reader.TranslateAndMoveAsync<TR10>(),
                await reader.TranslateAndMoveAsync<TR11>(),
                await reader.TranslateAndMoveAsync<TR12>()), unitOfWork);
    }

    /// <summary>
    /// Executes the command and maps 13 result sets in declaration order.
    /// </summary>
    /// <typeparam name="TR1">The value mapped from result set 1.</typeparam>
    /// <typeparam name="TR2">The value mapped from result set 2.</typeparam>
    /// <typeparam name="TR3">The value mapped from result set 3.</typeparam>
    /// <typeparam name="TR4">The value mapped from result set 4.</typeparam>
    /// <typeparam name="TR5">The value mapped from result set 5.</typeparam>
    /// <typeparam name="TR6">The value mapped from result set 6.</typeparam>
    /// <typeparam name="TR7">The value mapped from result set 7.</typeparam>
    /// <typeparam name="TR8">The value mapped from result set 8.</typeparam>
    /// <typeparam name="TR9">The value mapped from result set 9.</typeparam>
    /// <typeparam name="TR10">The value mapped from result set 10.</typeparam>
    /// <typeparam name="TR11">The value mapped from result set 11.</typeparam>
    /// <typeparam name="TR12">The value mapped from result set 12.</typeparam>
    /// <typeparam name="TR13">The value mapped from result set 13.</typeparam>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The provider parameters to add to the command, or <see langword="null"/> for none.</param>
    /// <param name="unitOfWork">The optional unit of work whose active transaction is attached to the command.</param>
    /// <returns>An ordered tuple containing one mapped value per result set; empty scalar or mapped-object sets produce null reference values or default value-type values, while concrete <c>IList</c> result types produce empty lists.</returns>
    /// <remarks>Each tuple element corresponds to the result set at the same one-based position.</remarks>
    protected async Task<(TR1?, TR2?, TR3?, TR4?, TR5?, TR6?, TR7?, TR8?, TR9?, TR10?, TR11?, TR12?, TR13?)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10, TR11, TR12, TR13>(DbConnection connection, SqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync(connection, parameters, async reader =>
            (await reader.TranslateAndMoveAsync<TR1>(),
                await reader.TranslateAndMoveAsync<TR2>(),
                await reader.TranslateAndMoveAsync<TR3>(),
                await reader.TranslateAndMoveAsync<TR4>(),
                await reader.TranslateAndMoveAsync<TR5>(),
                await reader.TranslateAndMoveAsync<TR6>(),
                await reader.TranslateAndMoveAsync<TR7>(),
                await reader.TranslateAndMoveAsync<TR8>(),
                await reader.TranslateAndMoveAsync<TR9>(),
                await reader.TranslateAndMoveAsync<TR10>(),
                await reader.TranslateAndMoveAsync<TR11>(),
                await reader.TranslateAndMoveAsync<TR12>(),
                await reader.TranslateAndMoveAsync<TR13>()), unitOfWork);
    }

    /// <summary>
    /// Executes the command and maps 14 result sets in declaration order.
    /// </summary>
    /// <typeparam name="TR1">The value mapped from result set 1.</typeparam>
    /// <typeparam name="TR2">The value mapped from result set 2.</typeparam>
    /// <typeparam name="TR3">The value mapped from result set 3.</typeparam>
    /// <typeparam name="TR4">The value mapped from result set 4.</typeparam>
    /// <typeparam name="TR5">The value mapped from result set 5.</typeparam>
    /// <typeparam name="TR6">The value mapped from result set 6.</typeparam>
    /// <typeparam name="TR7">The value mapped from result set 7.</typeparam>
    /// <typeparam name="TR8">The value mapped from result set 8.</typeparam>
    /// <typeparam name="TR9">The value mapped from result set 9.</typeparam>
    /// <typeparam name="TR10">The value mapped from result set 10.</typeparam>
    /// <typeparam name="TR11">The value mapped from result set 11.</typeparam>
    /// <typeparam name="TR12">The value mapped from result set 12.</typeparam>
    /// <typeparam name="TR13">The value mapped from result set 13.</typeparam>
    /// <typeparam name="TR14">The value mapped from result set 14.</typeparam>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The provider parameters to add to the command, or <see langword="null"/> for none.</param>
    /// <param name="unitOfWork">The optional unit of work whose active transaction is attached to the command.</param>
    /// <returns>An ordered tuple containing one mapped value per result set; empty scalar or mapped-object sets produce null reference values or default value-type values, while concrete <c>IList</c> result types produce empty lists.</returns>
    /// <remarks>Each tuple element corresponds to the result set at the same one-based position.</remarks>
    protected async Task<(TR1?, TR2?, TR3?, TR4?, TR5?, TR6?, TR7?, TR8?, TR9?, TR10?, TR11?, TR12?, TR13?, TR14?)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10, TR11, TR12, TR13, TR14>(DbConnection connection, SqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync(connection, parameters, async reader =>
            (await reader.TranslateAndMoveAsync<TR1>(),
                await reader.TranslateAndMoveAsync<TR2>(),
                await reader.TranslateAndMoveAsync<TR3>(),
                await reader.TranslateAndMoveAsync<TR4>(),
                await reader.TranslateAndMoveAsync<TR5>(),
                await reader.TranslateAndMoveAsync<TR6>(),
                await reader.TranslateAndMoveAsync<TR7>(),
                await reader.TranslateAndMoveAsync<TR8>(),
                await reader.TranslateAndMoveAsync<TR9>(),
                await reader.TranslateAndMoveAsync<TR10>(),
                await reader.TranslateAndMoveAsync<TR11>(),
                await reader.TranslateAndMoveAsync<TR12>(),
                await reader.TranslateAndMoveAsync<TR13>(),
                await reader.TranslateAndMoveAsync<TR14>()), unitOfWork);
    }

    /// <summary>
    /// Executes the command and maps 15 result sets in declaration order.
    /// </summary>
    /// <typeparam name="TR1">The value mapped from result set 1.</typeparam>
    /// <typeparam name="TR2">The value mapped from result set 2.</typeparam>
    /// <typeparam name="TR3">The value mapped from result set 3.</typeparam>
    /// <typeparam name="TR4">The value mapped from result set 4.</typeparam>
    /// <typeparam name="TR5">The value mapped from result set 5.</typeparam>
    /// <typeparam name="TR6">The value mapped from result set 6.</typeparam>
    /// <typeparam name="TR7">The value mapped from result set 7.</typeparam>
    /// <typeparam name="TR8">The value mapped from result set 8.</typeparam>
    /// <typeparam name="TR9">The value mapped from result set 9.</typeparam>
    /// <typeparam name="TR10">The value mapped from result set 10.</typeparam>
    /// <typeparam name="TR11">The value mapped from result set 11.</typeparam>
    /// <typeparam name="TR12">The value mapped from result set 12.</typeparam>
    /// <typeparam name="TR13">The value mapped from result set 13.</typeparam>
    /// <typeparam name="TR14">The value mapped from result set 14.</typeparam>
    /// <typeparam name="TR15">The value mapped from result set 15.</typeparam>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The provider parameters to add to the command, or <see langword="null"/> for none.</param>
    /// <param name="unitOfWork">The optional unit of work whose active transaction is attached to the command.</param>
    /// <returns>An ordered tuple containing one mapped value per result set; empty scalar or mapped-object sets produce null reference values or default value-type values, while concrete <c>IList</c> result types produce empty lists.</returns>
    /// <remarks>Each tuple element corresponds to the result set at the same one-based position.</remarks>
    protected async Task<(TR1?, TR2?, TR3?, TR4?, TR5?, TR6?, TR7?, TR8?, TR9?, TR10?, TR11?, TR12?, TR13?, TR14?, TR15?)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10, TR11, TR12, TR13, TR14, TR15>(DbConnection connection, SqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync(connection, parameters, async reader =>
            (await reader.TranslateAndMoveAsync<TR1>(),
                await reader.TranslateAndMoveAsync<TR2>(),
                await reader.TranslateAndMoveAsync<TR3>(),
                await reader.TranslateAndMoveAsync<TR4>(),
                await reader.TranslateAndMoveAsync<TR5>(),
                await reader.TranslateAndMoveAsync<TR6>(),
                await reader.TranslateAndMoveAsync<TR7>(),
                await reader.TranslateAndMoveAsync<TR8>(),
                await reader.TranslateAndMoveAsync<TR9>(),
                await reader.TranslateAndMoveAsync<TR10>(),
                await reader.TranslateAndMoveAsync<TR11>(),
                await reader.TranslateAndMoveAsync<TR12>(),
                await reader.TranslateAndMoveAsync<TR13>(),
                await reader.TranslateAndMoveAsync<TR14>(),
                await reader.TranslateAndMoveAsync<TR15>()), unitOfWork);
    }

    /// <summary>
    /// Executes the command and maps 16 result sets in declaration order.
    /// </summary>
    /// <typeparam name="TR1">The value mapped from result set 1.</typeparam>
    /// <typeparam name="TR2">The value mapped from result set 2.</typeparam>
    /// <typeparam name="TR3">The value mapped from result set 3.</typeparam>
    /// <typeparam name="TR4">The value mapped from result set 4.</typeparam>
    /// <typeparam name="TR5">The value mapped from result set 5.</typeparam>
    /// <typeparam name="TR6">The value mapped from result set 6.</typeparam>
    /// <typeparam name="TR7">The value mapped from result set 7.</typeparam>
    /// <typeparam name="TR8">The value mapped from result set 8.</typeparam>
    /// <typeparam name="TR9">The value mapped from result set 9.</typeparam>
    /// <typeparam name="TR10">The value mapped from result set 10.</typeparam>
    /// <typeparam name="TR11">The value mapped from result set 11.</typeparam>
    /// <typeparam name="TR12">The value mapped from result set 12.</typeparam>
    /// <typeparam name="TR13">The value mapped from result set 13.</typeparam>
    /// <typeparam name="TR14">The value mapped from result set 14.</typeparam>
    /// <typeparam name="TR15">The value mapped from result set 15.</typeparam>
    /// <typeparam name="TR16">The value mapped from result set 16.</typeparam>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The provider parameters to add to the command, or <see langword="null"/> for none.</param>
    /// <param name="unitOfWork">The optional unit of work whose active transaction is attached to the command.</param>
    /// <returns>An ordered tuple containing one mapped value per result set; empty scalar or mapped-object sets produce null reference values or default value-type values, while concrete <c>IList</c> result types produce empty lists.</returns>
    /// <remarks>Each tuple element corresponds to the result set at the same one-based position.</remarks>
    protected async Task<(TR1?, TR2?, TR3?, TR4?, TR5?, TR6?, TR7?, TR8?, TR9?, TR10?, TR11?, TR12?, TR13?, TR14?, TR15?, TR16?)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10, TR11, TR12, TR13, TR14, TR15, TR16>(DbConnection connection, SqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync(connection, parameters, async reader =>
            (await reader.TranslateAndMoveAsync<TR1>(),
                await reader.TranslateAndMoveAsync<TR2>(),
                await reader.TranslateAndMoveAsync<TR3>(),
                await reader.TranslateAndMoveAsync<TR4>(),
                await reader.TranslateAndMoveAsync<TR5>(),
                await reader.TranslateAndMoveAsync<TR6>(),
                await reader.TranslateAndMoveAsync<TR7>(),
                await reader.TranslateAndMoveAsync<TR8>(),
                await reader.TranslateAndMoveAsync<TR9>(),
                await reader.TranslateAndMoveAsync<TR10>(),
                await reader.TranslateAndMoveAsync<TR11>(),
                await reader.TranslateAndMoveAsync<TR12>(),
                await reader.TranslateAndMoveAsync<TR13>(),
                await reader.TranslateAndMoveAsync<TR14>(),
                await reader.TranslateAndMoveAsync<TR15>(),
                await reader.TranslateAndMoveAsync<TR16>()), unitOfWork);
    }

    #endregion
}
