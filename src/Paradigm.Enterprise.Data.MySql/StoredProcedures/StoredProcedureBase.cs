using MySql.Data.MySqlClient;
using Paradigm.Enterprise.Data.Extensions;
using Paradigm.Enterprise.Domain.Uow;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace Paradigm.Enterprise.Data.MySql.StoredProcedures;

/// <summary>Executes typed MySQL stored-procedure commands using Oracle Connector/NET.</summary>
/// <remarks>
/// Connections remain caller-owned. Nontransactional execution closes them on success and failure,
/// including connections already open on entry. An active unit-of-work transaction retains its connection.
/// Commands and readers are disposed; cleanup failures never replace an execution or mapping exception.
/// Result translation uses the existing shared Data reader-mapper contracts.
/// </remarks>
public abstract class StoredProcedureBase
{
    #region Constants

    private const int QueryCancellationTimeoutSeconds = 5;
    private const uint MaximumTimerDelayMilliseconds = uint.MaxValue - 1;

    #endregion

    #region Properties

    /// <summary>Gets the trusted procedure name used as stored-procedure command text.</summary>
    protected abstract string StoredProcedureName { get; }

    /// <summary>Gets an optional execution deadline in seconds, or null for the driver's default timeout.</summary>
    /// <remarks>
    /// Positive values bound command execution and result consumption, including application mapping,
    /// after opening the connection. Zero permits indefinite execution. An explicit deadline uses
    /// query cancellation instead of Oracle's native async timer, which can hang during recovery.
    /// A deadline expiration throws <see cref="TimeoutException"/> even if the server reports success
    /// for an interrupted operation such as <c>SLEEP</c>. Cancellation uses a separate bounded connection.
    /// </remarks>
    protected virtual int? ExecutionTimeout { get; }

    #endregion

    #region Public Methods

    /// <summary>Executes a procedure without application parameters or mapped results.</summary>
    /// <param name="connection">A caller-owned Oracle MySQL connection.</param>
    /// <param name="unitOfWork">The optional unit of work owning the command's transaction.</param>
    /// <returns>A task representing command completion.</returns>
    public Task ExecuteAsync(DbConnection connection, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync(connection, (MySqlParameter[]?)null, unitOfWork);

    #endregion

    #region Protected Methods

    /// <summary>Maps an application object to Oracle MySQL parameters.</summary>
    /// <typeparam name="TParameters">The registered application parameter type.</typeparam>
    /// <param name="parameters">The application object, or null for no parameters.</param>
    /// <returns>Independent parameters usable after mapper disposal, or null.</returns>
    protected MySqlParameter[]? GetMySqlParameters<TParameters>(TParameters? parameters)
    {
        if (parameters is null)
            return null;

        var mapper = MySqlParameterMapperFactory.GetMapper<TParameters>();
        Exception? mappingException = null;
        try
        {
            return mapper.Map(parameters);
        }
        catch (Exception exception)
        {
            mappingException = exception;
            throw;
        }
        finally
        {
            try
            {
                mapper.Dispose();
            }
            catch when (mappingException is not null)
            {
                // Custom mapper cleanup must not replace the application's mapping failure.
            }
        }
    }

    /// <summary>Executes a procedure with provider parameters and no mapped result.</summary>
    /// <param name="connection">A caller-owned Oracle MySQL connection.</param>
    /// <param name="parameters">Parameters to attach for this command, or null.</param>
    /// <param name="unitOfWork">The optional transaction owner.</param>
    /// <returns>A task representing command completion.</returns>
    protected Task ExecuteAsync(DbConnection connection, MySqlParameter[]? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteCommandAsync(connection, parameters, command => command.ExecuteNonQueryAsync(), unitOfWork);

    /// <summary>Executes a procedure and delegates result consumption to a typed reader operation.</summary>
    /// <typeparam name="T">The mapped result type.</typeparam>
    /// <param name="connection">A caller-owned Oracle MySQL connection.</param>
    /// <param name="parameters">Parameters to attach for this command, or null.</param>
    /// <param name="readerExecutedAction">The operation consuming the open reader.</param>
    /// <param name="unitOfWork">The optional transaction owner.</param>
    /// <returns>The operation's mapped result.</returns>
    protected Task<T> ExecuteAsync<T>(DbConnection connection, MySqlParameter[]? parameters,
        Func<DbDataReader, Task<T>> readerExecutedAction, IUnitOfWork? unitOfWork = null)
    {
        ArgumentNullException.ThrowIfNull(readerExecutedAction);
        return ExecuteCommandAsync(connection, parameters, command => ReadAsync(command, readerExecutedAction), unitOfWork);
    }

    /// <summary>Executes the procedure and maps its first result set.</summary>
    /// <typeparam name="TR1">The value mapped from result set 1.</typeparam>
    /// <param name="connection">A caller-owned Oracle MySQL connection.</param>
    /// <param name="parameters">The provider parameters, or null.</param>
    /// <param name="unitOfWork">The optional transaction owner.</param>
    /// <returns>Mapped results in database order; empty scalar/object results are default and supported lists are empty.</returns>
    protected Task<TR1?> ExecuteAsync<TR1>(DbConnection connection, MySqlParameter[]? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync(connection, parameters, reader => reader.TranslateAsync<TR1>(), unitOfWork);

    /// <summary>Executes the procedure and maps 2 sequential result sets.</summary>
    /// <typeparam name="TR1">The value mapped from result set 1.</typeparam>
    /// <typeparam name="TR2">The value mapped from result set 2.</typeparam>
    /// <param name="connection">A caller-owned Oracle MySQL connection.</param>
    /// <param name="parameters">The provider parameters, or null.</param>
    /// <param name="unitOfWork">The optional transaction owner.</param>
    /// <returns>Mapped results in database order; empty scalar/object results are default and supported lists are empty.</returns>
    protected Task<(TR1?, TR2?)> ExecuteAsync<TR1, TR2>(DbConnection connection, MySqlParameter[]? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync(connection, parameters, async reader =>
            (await reader.TranslateAndMoveAsync<TR1>(),
             await reader.TranslateAndMoveAsync<TR2>()), unitOfWork);

    /// <summary>Executes the procedure and maps 3 sequential result sets.</summary>
    /// <typeparam name="TR1">The value mapped from result set 1.</typeparam>
    /// <typeparam name="TR2">The value mapped from result set 2.</typeparam>
    /// <typeparam name="TR3">The value mapped from result set 3.</typeparam>
    /// <param name="connection">A caller-owned Oracle MySQL connection.</param>
    /// <param name="parameters">The provider parameters, or null.</param>
    /// <param name="unitOfWork">The optional transaction owner.</param>
    /// <returns>Mapped results in database order; empty scalar/object results are default and supported lists are empty.</returns>
    protected Task<(TR1?, TR2?, TR3?)> ExecuteAsync<TR1, TR2, TR3>(DbConnection connection, MySqlParameter[]? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync(connection, parameters, async reader =>
            (await reader.TranslateAndMoveAsync<TR1>(),
             await reader.TranslateAndMoveAsync<TR2>(),
             await reader.TranslateAndMoveAsync<TR3>()), unitOfWork);

    /// <summary>Executes the procedure and maps 4 sequential result sets.</summary>
    /// <typeparam name="TR1">The value mapped from result set 1.</typeparam>
    /// <typeparam name="TR2">The value mapped from result set 2.</typeparam>
    /// <typeparam name="TR3">The value mapped from result set 3.</typeparam>
    /// <typeparam name="TR4">The value mapped from result set 4.</typeparam>
    /// <param name="connection">A caller-owned Oracle MySQL connection.</param>
    /// <param name="parameters">The provider parameters, or null.</param>
    /// <param name="unitOfWork">The optional transaction owner.</param>
    /// <returns>Mapped results in database order; empty scalar/object results are default and supported lists are empty.</returns>
    protected Task<(TR1?, TR2?, TR3?, TR4?)> ExecuteAsync<TR1, TR2, TR3, TR4>(DbConnection connection, MySqlParameter[]? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync(connection, parameters, async reader =>
            (await reader.TranslateAndMoveAsync<TR1>(),
             await reader.TranslateAndMoveAsync<TR2>(),
             await reader.TranslateAndMoveAsync<TR3>(),
             await reader.TranslateAndMoveAsync<TR4>()), unitOfWork);

    /// <summary>Executes the procedure and maps 5 sequential result sets.</summary>
    /// <typeparam name="TR1">The value mapped from result set 1.</typeparam>
    /// <typeparam name="TR2">The value mapped from result set 2.</typeparam>
    /// <typeparam name="TR3">The value mapped from result set 3.</typeparam>
    /// <typeparam name="TR4">The value mapped from result set 4.</typeparam>
    /// <typeparam name="TR5">The value mapped from result set 5.</typeparam>
    /// <param name="connection">A caller-owned Oracle MySQL connection.</param>
    /// <param name="parameters">The provider parameters, or null.</param>
    /// <param name="unitOfWork">The optional transaction owner.</param>
    /// <returns>Mapped results in database order; empty scalar/object results are default and supported lists are empty.</returns>
    protected Task<(TR1?, TR2?, TR3?, TR4?, TR5?)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5>(DbConnection connection, MySqlParameter[]? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync(connection, parameters, async reader =>
            (await reader.TranslateAndMoveAsync<TR1>(),
             await reader.TranslateAndMoveAsync<TR2>(),
             await reader.TranslateAndMoveAsync<TR3>(),
             await reader.TranslateAndMoveAsync<TR4>(),
             await reader.TranslateAndMoveAsync<TR5>()), unitOfWork);

    /// <summary>Executes the procedure and maps 6 sequential result sets.</summary>
    /// <typeparam name="TR1">The value mapped from result set 1.</typeparam>
    /// <typeparam name="TR2">The value mapped from result set 2.</typeparam>
    /// <typeparam name="TR3">The value mapped from result set 3.</typeparam>
    /// <typeparam name="TR4">The value mapped from result set 4.</typeparam>
    /// <typeparam name="TR5">The value mapped from result set 5.</typeparam>
    /// <typeparam name="TR6">The value mapped from result set 6.</typeparam>
    /// <param name="connection">A caller-owned Oracle MySQL connection.</param>
    /// <param name="parameters">The provider parameters, or null.</param>
    /// <param name="unitOfWork">The optional transaction owner.</param>
    /// <returns>Mapped results in database order; empty scalar/object results are default and supported lists are empty.</returns>
    protected Task<(TR1?, TR2?, TR3?, TR4?, TR5?, TR6?)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6>(DbConnection connection, MySqlParameter[]? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync(connection, parameters, async reader =>
            (await reader.TranslateAndMoveAsync<TR1>(),
             await reader.TranslateAndMoveAsync<TR2>(),
             await reader.TranslateAndMoveAsync<TR3>(),
             await reader.TranslateAndMoveAsync<TR4>(),
             await reader.TranslateAndMoveAsync<TR5>(),
             await reader.TranslateAndMoveAsync<TR6>()), unitOfWork);

    /// <summary>Executes the procedure and maps 7 sequential result sets.</summary>
    /// <typeparam name="TR1">The value mapped from result set 1.</typeparam>
    /// <typeparam name="TR2">The value mapped from result set 2.</typeparam>
    /// <typeparam name="TR3">The value mapped from result set 3.</typeparam>
    /// <typeparam name="TR4">The value mapped from result set 4.</typeparam>
    /// <typeparam name="TR5">The value mapped from result set 5.</typeparam>
    /// <typeparam name="TR6">The value mapped from result set 6.</typeparam>
    /// <typeparam name="TR7">The value mapped from result set 7.</typeparam>
    /// <param name="connection">A caller-owned Oracle MySQL connection.</param>
    /// <param name="parameters">The provider parameters, or null.</param>
    /// <param name="unitOfWork">The optional transaction owner.</param>
    /// <returns>Mapped results in database order; empty scalar/object results are default and supported lists are empty.</returns>
    protected Task<(TR1?, TR2?, TR3?, TR4?, TR5?, TR6?, TR7?)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7>(DbConnection connection, MySqlParameter[]? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync(connection, parameters, async reader =>
            (await reader.TranslateAndMoveAsync<TR1>(),
             await reader.TranslateAndMoveAsync<TR2>(),
             await reader.TranslateAndMoveAsync<TR3>(),
             await reader.TranslateAndMoveAsync<TR4>(),
             await reader.TranslateAndMoveAsync<TR5>(),
             await reader.TranslateAndMoveAsync<TR6>(),
             await reader.TranslateAndMoveAsync<TR7>()), unitOfWork);

    /// <summary>Executes the procedure and maps 8 sequential result sets.</summary>
    /// <typeparam name="TR1">The value mapped from result set 1.</typeparam>
    /// <typeparam name="TR2">The value mapped from result set 2.</typeparam>
    /// <typeparam name="TR3">The value mapped from result set 3.</typeparam>
    /// <typeparam name="TR4">The value mapped from result set 4.</typeparam>
    /// <typeparam name="TR5">The value mapped from result set 5.</typeparam>
    /// <typeparam name="TR6">The value mapped from result set 6.</typeparam>
    /// <typeparam name="TR7">The value mapped from result set 7.</typeparam>
    /// <typeparam name="TR8">The value mapped from result set 8.</typeparam>
    /// <param name="connection">A caller-owned Oracle MySQL connection.</param>
    /// <param name="parameters">The provider parameters, or null.</param>
    /// <param name="unitOfWork">The optional transaction owner.</param>
    /// <returns>Mapped results in database order; empty scalar/object results are default and supported lists are empty.</returns>
    protected Task<(TR1?, TR2?, TR3?, TR4?, TR5?, TR6?, TR7?, TR8?)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8>(DbConnection connection, MySqlParameter[]? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync(connection, parameters, async reader =>
            (await reader.TranslateAndMoveAsync<TR1>(),
             await reader.TranslateAndMoveAsync<TR2>(),
             await reader.TranslateAndMoveAsync<TR3>(),
             await reader.TranslateAndMoveAsync<TR4>(),
             await reader.TranslateAndMoveAsync<TR5>(),
             await reader.TranslateAndMoveAsync<TR6>(),
             await reader.TranslateAndMoveAsync<TR7>(),
             await reader.TranslateAndMoveAsync<TR8>()), unitOfWork);

    /// <summary>Executes the procedure and maps 9 sequential result sets.</summary>
    /// <typeparam name="TR1">The value mapped from result set 1.</typeparam>
    /// <typeparam name="TR2">The value mapped from result set 2.</typeparam>
    /// <typeparam name="TR3">The value mapped from result set 3.</typeparam>
    /// <typeparam name="TR4">The value mapped from result set 4.</typeparam>
    /// <typeparam name="TR5">The value mapped from result set 5.</typeparam>
    /// <typeparam name="TR6">The value mapped from result set 6.</typeparam>
    /// <typeparam name="TR7">The value mapped from result set 7.</typeparam>
    /// <typeparam name="TR8">The value mapped from result set 8.</typeparam>
    /// <typeparam name="TR9">The value mapped from result set 9.</typeparam>
    /// <param name="connection">A caller-owned Oracle MySQL connection.</param>
    /// <param name="parameters">The provider parameters, or null.</param>
    /// <param name="unitOfWork">The optional transaction owner.</param>
    /// <returns>Mapped results in database order; empty scalar/object results are default and supported lists are empty.</returns>
    protected Task<(TR1?, TR2?, TR3?, TR4?, TR5?, TR6?, TR7?, TR8?, TR9?)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9>(DbConnection connection, MySqlParameter[]? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync(connection, parameters, async reader =>
            (await reader.TranslateAndMoveAsync<TR1>(),
             await reader.TranslateAndMoveAsync<TR2>(),
             await reader.TranslateAndMoveAsync<TR3>(),
             await reader.TranslateAndMoveAsync<TR4>(),
             await reader.TranslateAndMoveAsync<TR5>(),
             await reader.TranslateAndMoveAsync<TR6>(),
             await reader.TranslateAndMoveAsync<TR7>(),
             await reader.TranslateAndMoveAsync<TR8>(),
             await reader.TranslateAndMoveAsync<TR9>()), unitOfWork);

    /// <summary>Executes the procedure and maps 10 sequential result sets.</summary>
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
    /// <param name="connection">A caller-owned Oracle MySQL connection.</param>
    /// <param name="parameters">The provider parameters, or null.</param>
    /// <param name="unitOfWork">The optional transaction owner.</param>
    /// <returns>Mapped results in database order; empty scalar/object results are default and supported lists are empty.</returns>
    protected Task<(TR1?, TR2?, TR3?, TR4?, TR5?, TR6?, TR7?, TR8?, TR9?, TR10?)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10>(DbConnection connection, MySqlParameter[]? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync(connection, parameters, async reader =>
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

    /// <summary>Executes the procedure and maps 11 sequential result sets.</summary>
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
    /// <param name="connection">A caller-owned Oracle MySQL connection.</param>
    /// <param name="parameters">The provider parameters, or null.</param>
    /// <param name="unitOfWork">The optional transaction owner.</param>
    /// <returns>Mapped results in database order; empty scalar/object results are default and supported lists are empty.</returns>
    protected Task<(TR1?, TR2?, TR3?, TR4?, TR5?, TR6?, TR7?, TR8?, TR9?, TR10?, TR11?)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10, TR11>(DbConnection connection, MySqlParameter[]? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync(connection, parameters, async reader =>
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

    /// <summary>Executes the procedure and maps 12 sequential result sets.</summary>
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
    /// <param name="connection">A caller-owned Oracle MySQL connection.</param>
    /// <param name="parameters">The provider parameters, or null.</param>
    /// <param name="unitOfWork">The optional transaction owner.</param>
    /// <returns>Mapped results in database order; empty scalar/object results are default and supported lists are empty.</returns>
    protected Task<(TR1?, TR2?, TR3?, TR4?, TR5?, TR6?, TR7?, TR8?, TR9?, TR10?, TR11?, TR12?)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10, TR11, TR12>(DbConnection connection, MySqlParameter[]? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync(connection, parameters, async reader =>
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

    /// <summary>Executes the procedure and maps 13 sequential result sets.</summary>
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
    /// <param name="connection">A caller-owned Oracle MySQL connection.</param>
    /// <param name="parameters">The provider parameters, or null.</param>
    /// <param name="unitOfWork">The optional transaction owner.</param>
    /// <returns>Mapped results in database order; empty scalar/object results are default and supported lists are empty.</returns>
    protected Task<(TR1?, TR2?, TR3?, TR4?, TR5?, TR6?, TR7?, TR8?, TR9?, TR10?, TR11?, TR12?, TR13?)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10, TR11, TR12, TR13>(DbConnection connection, MySqlParameter[]? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync(connection, parameters, async reader =>
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

    /// <summary>Executes the procedure and maps 14 sequential result sets.</summary>
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
    /// <param name="connection">A caller-owned Oracle MySQL connection.</param>
    /// <param name="parameters">The provider parameters, or null.</param>
    /// <param name="unitOfWork">The optional transaction owner.</param>
    /// <returns>Mapped results in database order; empty scalar/object results are default and supported lists are empty.</returns>
    protected Task<(TR1?, TR2?, TR3?, TR4?, TR5?, TR6?, TR7?, TR8?, TR9?, TR10?, TR11?, TR12?, TR13?, TR14?)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10, TR11, TR12, TR13, TR14>(DbConnection connection, MySqlParameter[]? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync(connection, parameters, async reader =>
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

    /// <summary>Executes the procedure and maps 15 sequential result sets.</summary>
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
    /// <param name="connection">A caller-owned Oracle MySQL connection.</param>
    /// <param name="parameters">The provider parameters, or null.</param>
    /// <param name="unitOfWork">The optional transaction owner.</param>
    /// <returns>Mapped results in database order; empty scalar/object results are default and supported lists are empty.</returns>
    protected Task<(TR1?, TR2?, TR3?, TR4?, TR5?, TR6?, TR7?, TR8?, TR9?, TR10?, TR11?, TR12?, TR13?, TR14?, TR15?)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10, TR11, TR12, TR13, TR14, TR15>(DbConnection connection, MySqlParameter[]? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync(connection, parameters, async reader =>
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

    /// <summary>Executes the procedure and maps 16 sequential result sets.</summary>
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
    /// <param name="connection">A caller-owned Oracle MySQL connection.</param>
    /// <param name="parameters">The provider parameters, or null.</param>
    /// <param name="unitOfWork">The optional transaction owner.</param>
    /// <returns>Mapped results in database order; empty scalar/object results are default and supported lists are empty.</returns>
    protected Task<(TR1?, TR2?, TR3?, TR4?, TR5?, TR6?, TR7?, TR8?, TR9?, TR10?, TR11?, TR12?, TR13?, TR14?, TR15?, TR16?)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10, TR11, TR12, TR13, TR14, TR15, TR16>(DbConnection connection, MySqlParameter[]? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync(connection, parameters, async reader =>
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

    #endregion

    #region Private Methods

    private async Task<T> ExecuteCommandAsync<T>(DbConnection connection, MySqlParameter[]? parameters,
        Func<MySqlCommand, Task<T>> execute, IUnitOfWork? unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(connection);
        if (connection is not MySqlConnection mysqlConnection)
            throw new ArgumentException("An Oracle MySql.Data MySqlConnection is required.", nameof(connection));

        var hasActiveTransaction = unitOfWork?.HasActiveTransaction ?? false;
        MySqlCommand? command = null;
        Exception? executionException = null;
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(StoredProcedureName);
            command = mysqlConnection.CreateCommand();
            command.CommandText = StoredProcedureName;
            command.CommandType = CommandType.StoredProcedure;
            var executionTimeout = ExecutionTimeout;
            if (executionTimeout.HasValue)
            {
                ArgumentOutOfRangeException.ThrowIfNegative(executionTimeout.Value, nameof(ExecutionTimeout));
                // Oracle's native async timeout can hang during recovery; the explicit deadline below
                // cancels the query using the supported API without enabling the conflicting timer.
                command.CommandTimeout = 0;
            }

            if (hasActiveTransaction)
            {
                unitOfWork!.UseTransaction(command);
                if (command.Transaction is null || !ReferenceEquals(command.Transaction.Connection, mysqlConnection))
                    throw new InvalidOperationException("The active MySQL transaction must use the command's connection.");
            }

            if (parameters is not null)
                command.Parameters.AddRange(parameters);

            if (mysqlConnection.State == ConnectionState.Closed)
                await mysqlConnection.OpenAsync();

            return await ExecuteWithDeadlineAsync(command, execute, executionTimeout);
        }
        catch (Exception exception)
        {
            executionException = exception;
            throw;
        }
        finally
        {
            // Attempt every cleanup step and retain the original execution failure when one exists.
            Exception? cleanupException = null;
            try
            {
                command?.Parameters.Clear();
            }
            catch (Exception exception)
            {
                cleanupException = exception;
            }

            try
            {
                command?.Dispose();
            }
            catch (Exception exception)
            {
                cleanupException ??= exception;
            }

            try
            {
                if (!hasActiveTransaction)
                    await mysqlConnection.CloseAsync();
            }
            catch (Exception exception)
            {
                cleanupException ??= exception;
            }

            if (executionException is null && cleanupException is not null)
                ExceptionDispatchInfo.Capture(cleanupException).Throw();
        }
    }

    private static async Task<T> ExecuteWithDeadlineAsync<T>(MySqlCommand command,
        Func<MySqlCommand, Task<T>> execute, int? timeoutSeconds)
    {
        if (timeoutSeconds is null or 0)
            return await execute(command);

        using var deadlineCancellation = new CancellationTokenSource();
        var duration = TimeSpan.FromSeconds(timeoutSeconds.Value);
        var elapsed = Stopwatch.StartNew();
        var deadline = WaitForDeadlineAsync(elapsed, duration, deadlineCancellation.Token);
        Task<T> execution;
        try
        {
            execution = execute(command);
        }
        catch (Exception exception)
        {
            await deadlineCancellation.CancelAsync();
            if (elapsed.Elapsed >= duration)
                throw new TimeoutException($"MySQL procedure execution exceeded its {timeoutSeconds.Value}-second deadline.", exception);

            throw;
        }

        if (await Task.WhenAny(execution, deadline) == execution && elapsed.Elapsed < duration)
        {
            await deadlineCancellation.CancelAsync();
            return await execution;
        }

        await deadlineCancellation.CancelAsync();
        Exception? cancellationException = null;
        if (!execution.IsCompleted)
        {
            using var queryCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(QueryCancellationTimeoutSeconds));
            try
            {
                // Await cancellation before returning the connection so it cannot affect a later command.
                await command.Connection.CancelQueryAsync(QueryCancellationTimeoutSeconds, queryCancellation.Token);
            }
            catch (Exception exception)
            {
                cancellationException = exception;
            }
        }

        try
        {
            // Observe the original command and finish reader cleanup before any connection reuse.
            await execution;
        }
        catch (Exception exception)
        {
            cancellationException ??= exception;
        }

        throw new TimeoutException($"MySQL procedure execution exceeded its {timeoutSeconds.Value}-second deadline.", cancellationException);
    }

    private static async Task WaitForDeadlineAsync(Stopwatch elapsed, TimeSpan duration, CancellationToken cancellationToken)
    {
        var remaining = duration - elapsed.Elapsed;
        while (remaining > TimeSpan.Zero)
        {
            // Task.Delay has a smaller range than the public timeout's positive integer seconds.
            var interval = remaining.TotalMilliseconds > MaximumTimerDelayMilliseconds
                ? TimeSpan.FromMilliseconds(MaximumTimerDelayMilliseconds)
                : remaining;
            await Task.Delay(interval, cancellationToken);
            remaining = duration - elapsed.Elapsed;
        }
    }

    private static async Task<T> ReadAsync<T>(MySqlCommand command, Func<DbDataReader, Task<T>> read)
    {
        DbDataReader? reader = null;
        Exception? executionException = null;
        try
        {
            reader = await command.ExecuteReaderAsync();
            var result = await read(reader);
            // Drain remaining results, including CALL status, before the connection can be reused.
            while (await reader.NextResultAsync())
            {
            }

            return result;
        }
        catch (Exception exception)
        {
            executionException = exception;
            throw;
        }
        finally
        {
            try
            {
                if (reader is not null)
                    await reader.DisposeAsync();
            }
            catch when (executionException is not null)
            {
                // Reader cleanup must not replace the database or mapper exception.
            }
        }
    }

    #endregion
}