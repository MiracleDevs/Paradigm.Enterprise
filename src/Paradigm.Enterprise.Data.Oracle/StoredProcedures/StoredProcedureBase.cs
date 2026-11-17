using Oracle.ManagedDataAccess.Client;
using Paradigm.Enterprise.Data.Extensions;
using Paradigm.Enterprise.Domain.Uow;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.ExceptionServices;

namespace Paradigm.Enterprise.Data.Oracle.StoredProcedures;

/// <summary>Defines an Oracle stored procedure using a registered application parameter mapper.</summary>
/// <typeparam name="TParameters">The application parameter type.</typeparam>
public abstract class StoredProcedureBase<TParameters> : StoredProcedureBase
{
    #region Public Methods

    /// <summary>Maps the parameters and executes the procedure without a mapped result.</summary>
    /// <param name="connection">A caller-owned Oracle connection.</param>
    /// <param name="parameters">The application parameter object, or null.</param>
    /// <param name="unitOfWork">The optional unit of work owning the transaction.</param>
    /// <returns>A task representing command completion.</returns>
    /// <remarks>Nontransactional connections close on execution success or failure. Mapping occurs before opening.</remarks>
    public Task ExecuteAsync(DbConnection connection, TParameters? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync(connection, GetOracleParameters(parameters), unitOfWork);

    #endregion
}

/// <summary>Executes typed Oracle stored-procedure commands using Oracle ODP.NET Core.</summary>
/// <remarks>
/// Connections remain caller-owned. Nontransactional execution closes them on success and failure,
/// including connections already open on entry. An active unit-of-work transaction retains its connection.
/// Commands and readers are disposed; cleanup failures never replace an execution or mapping exception.
/// Object and list translation uses the shared Data reader-mapper contracts. Oracle numeric primitives
/// are converted to their requested CLR types, rejecting fractional integral values and overflow;
/// numeric Boolean values must be zero or one. Typed result arities bind output REF CURSOR parameters.
/// </remarks>
public abstract class StoredProcedureBase
{
    #region Nested Types

    private sealed class TransactionBindingCommand(OracleCommand command) : IDbCommand
    {
        #region Properties

        [AllowNull]
        public string CommandText { get => command.CommandText; set => command.CommandText = value!; }

        public int CommandTimeout { get => command.CommandTimeout; set => command.CommandTimeout = value; }

        public CommandType CommandType { get => command.CommandType; set => command.CommandType = value; }

        public IDbConnection? Connection { get => command.Connection; set => ((IDbCommand)command).Connection = value; }

        public IDataParameterCollection Parameters => command.Parameters;

        public IDbTransaction? Transaction { get; set; }

        public UpdateRowSource UpdatedRowSource { get => command.UpdatedRowSource; set => command.UpdatedRowSource = value; }

        #endregion

        #region Public Methods

        public void Cancel() => command.Cancel();

        public IDbDataParameter CreateParameter() => command.CreateParameter();

        public void Dispose() { /* The adapter owns the underlying command. */ }

        public int ExecuteNonQuery() => command.ExecuteNonQuery();

        public IDataReader ExecuteReader() => command.ExecuteReader();

        public IDataReader ExecuteReader(CommandBehavior behavior) => command.ExecuteReader(behavior);

        public object? ExecuteScalar() => command.ExecuteScalar();

        public void Prepare() => command.Prepare();

        #endregion
    }

    #endregion

    #region Properties

    /// <summary>Gets the trusted procedure name used as stored-procedure command text.</summary>
    protected abstract string StoredProcedureName { get; }

    /// <summary>Gets an optional Oracle command timeout in seconds, or null for the driver's default.</summary>
    /// <remarks>
    /// Zero permits indefinite execution. A positive value starts the provider's cancellation process
    /// after at least that many seconds; it does not impose a wall-clock deadline on connection opening,
    /// application mapping, or cleanup. Provider cancellation errors propagate unchanged.
    /// Use scoped connections sequentially, because Oracle cancellation applies to the connection.
    /// </remarks>
    protected virtual int? ExecutionTimeout { get; }

    #endregion

    #region Public Methods

    /// <summary>Executes a procedure without application parameters or mapped results.</summary>
    /// <param name="connection">A caller-owned Oracle connection.</param>
    /// <param name="unitOfWork">The optional unit of work owning the command's transaction.</param>
    /// <returns>A task representing command completion.</returns>
    public Task ExecuteAsync(DbConnection connection, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync(connection, (OracleParameter[]?)null, unitOfWork);

    #endregion

    #region Protected Methods

    /// <summary>Gets the name of the output REF CURSOR for a declared typed result position.</summary>
    /// <param name="resultIndex">The one-based result position, from one through sixteen.</param>
    /// <returns>The unprefixed PL/SQL parameter name, defaulting to <c>p_result1</c> through <c>p_result16</c>.</returns>
    /// <remarks>
    /// Override this method to use an existing routine's cursor names. The adapter binds each cursor
    /// as an output parameter in declared tuple order; application parameter mappers supply inputs only.
    /// Every cursor must be opened, even when its query has no rows. A null cursor is not an empty result.
    /// </remarks>
    protected virtual string GetResultParameterName(int resultIndex) => $"p_result{resultIndex}";

    /// <summary>Maps an application object to Oracle parameters.</summary>
    /// <typeparam name="TParameters">The registered application parameter type.</typeparam>
    /// <param name="parameters">The application object, or null for no parameters.</param>
    /// <returns>Independent parameters usable after mapper disposal, or null.</returns>
    protected OracleParameter[]? GetOracleParameters<TParameters>(TParameters? parameters)
    {
        if (parameters is null)
            return null;

        var mapper = OracleParameterMapperFactory.GetMapper<TParameters>();
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
    /// <param name="connection">A caller-owned Oracle connection.</param>
    /// <param name="parameters">Parameters to attach for this command, or null.</param>
    /// <param name="unitOfWork">The optional transaction owner.</param>
    /// <returns>A task representing command completion.</returns>
    protected Task ExecuteAsync(DbConnection connection, OracleParameter[]? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteCommandAsync(connection, parameters, command => command.ExecuteNonQueryAsync(), unitOfWork);

    /// <summary>Executes a procedure and delegates result consumption to a typed reader operation.</summary>
    /// <typeparam name="T">The mapped result type.</typeparam>
    /// <param name="connection">A caller-owned Oracle connection.</param>
    /// <param name="parameters">Parameters to attach for this command, or null.</param>
    /// <param name="readerExecutedAction">The operation consuming the open reader.</param>
    /// <param name="unitOfWork">The optional transaction owner.</param>
    /// <returns>The operation's mapped result.</returns>
    protected Task<T> ExecuteAsync<T>(DbConnection connection, OracleParameter[]? parameters,
        Func<DbDataReader, Task<T>> readerExecutedAction, IUnitOfWork? unitOfWork = null)
    {
        ArgumentNullException.ThrowIfNull(readerExecutedAction);
        return ExecuteCommandAsync(connection, parameters, command => ReadAsync(command, readerExecutedAction), unitOfWork);
    }

    /// <summary>Executes the procedure and maps its first result set.</summary>
    /// <typeparam name="TR1">The value mapped from result set 1.</typeparam>
    /// <param name="connection">A caller-owned Oracle connection.</param>
    /// <param name="parameters">The provider parameters, or null.</param>
    /// <param name="unitOfWork">The optional transaction owner.</param>
    /// <returns>Mapped results in database order; empty scalar/object results are default and supported lists are empty.</returns>
    protected Task<TR1?> ExecuteAsync<TR1>(DbConnection connection, OracleParameter[]? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync(connection, AddResultParameters(parameters, 1), reader => TranslateResultAsync<TR1>(reader), unitOfWork);

    /// <summary>Executes the procedure and maps 2 sequential result sets.</summary>
    /// <typeparam name="TR1">The value mapped from result set 1.</typeparam>
    /// <typeparam name="TR2">The value mapped from result set 2.</typeparam>
    /// <param name="connection">A caller-owned Oracle connection.</param>
    /// <param name="parameters">The provider parameters, or null.</param>
    /// <param name="unitOfWork">The optional transaction owner.</param>
    /// <returns>Mapped results in database order; empty scalar/object results are default and supported lists are empty.</returns>
    protected Task<(TR1?, TR2?)> ExecuteAsync<TR1, TR2>(DbConnection connection, OracleParameter[]? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync(connection, AddResultParameters(parameters, 2), async reader =>
            (await TranslateResultAndMoveAsync<TR1>(reader),
             await TranslateResultAsync<TR2>(reader)), unitOfWork);

    /// <summary>Executes the procedure and maps 3 sequential result sets.</summary>
    /// <typeparam name="TR1">The value mapped from result set 1.</typeparam>
    /// <typeparam name="TR2">The value mapped from result set 2.</typeparam>
    /// <typeparam name="TR3">The value mapped from result set 3.</typeparam>
    /// <param name="connection">A caller-owned Oracle connection.</param>
    /// <param name="parameters">The provider parameters, or null.</param>
    /// <param name="unitOfWork">The optional transaction owner.</param>
    /// <returns>Mapped results in database order; empty scalar/object results are default and supported lists are empty.</returns>
    protected Task<(TR1?, TR2?, TR3?)> ExecuteAsync<TR1, TR2, TR3>(DbConnection connection, OracleParameter[]? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync(connection, AddResultParameters(parameters, 3), async reader =>
            (await TranslateResultAndMoveAsync<TR1>(reader),
             await TranslateResultAndMoveAsync<TR2>(reader),
             await TranslateResultAsync<TR3>(reader)), unitOfWork);

    /// <summary>Executes the procedure and maps 4 sequential result sets.</summary>
    /// <typeparam name="TR1">The value mapped from result set 1.</typeparam>
    /// <typeparam name="TR2">The value mapped from result set 2.</typeparam>
    /// <typeparam name="TR3">The value mapped from result set 3.</typeparam>
    /// <typeparam name="TR4">The value mapped from result set 4.</typeparam>
    /// <param name="connection">A caller-owned Oracle connection.</param>
    /// <param name="parameters">The provider parameters, or null.</param>
    /// <param name="unitOfWork">The optional transaction owner.</param>
    /// <returns>Mapped results in database order; empty scalar/object results are default and supported lists are empty.</returns>
    protected Task<(TR1?, TR2?, TR3?, TR4?)> ExecuteAsync<TR1, TR2, TR3, TR4>(DbConnection connection, OracleParameter[]? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync(connection, AddResultParameters(parameters, 4), async reader =>
            (await TranslateResultAndMoveAsync<TR1>(reader),
             await TranslateResultAndMoveAsync<TR2>(reader),
             await TranslateResultAndMoveAsync<TR3>(reader),
             await TranslateResultAsync<TR4>(reader)), unitOfWork);

    /// <summary>Executes the procedure and maps 5 sequential result sets.</summary>
    /// <typeparam name="TR1">The value mapped from result set 1.</typeparam>
    /// <typeparam name="TR2">The value mapped from result set 2.</typeparam>
    /// <typeparam name="TR3">The value mapped from result set 3.</typeparam>
    /// <typeparam name="TR4">The value mapped from result set 4.</typeparam>
    /// <typeparam name="TR5">The value mapped from result set 5.</typeparam>
    /// <param name="connection">A caller-owned Oracle connection.</param>
    /// <param name="parameters">The provider parameters, or null.</param>
    /// <param name="unitOfWork">The optional transaction owner.</param>
    /// <returns>Mapped results in database order; empty scalar/object results are default and supported lists are empty.</returns>
    protected Task<(TR1?, TR2?, TR3?, TR4?, TR5?)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5>(DbConnection connection, OracleParameter[]? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync(connection, AddResultParameters(parameters, 5), async reader =>
            (await TranslateResultAndMoveAsync<TR1>(reader),
             await TranslateResultAndMoveAsync<TR2>(reader),
             await TranslateResultAndMoveAsync<TR3>(reader),
             await TranslateResultAndMoveAsync<TR4>(reader),
             await TranslateResultAsync<TR5>(reader)), unitOfWork);

    /// <summary>Executes the procedure and maps 6 sequential result sets.</summary>
    /// <typeparam name="TR1">The value mapped from result set 1.</typeparam>
    /// <typeparam name="TR2">The value mapped from result set 2.</typeparam>
    /// <typeparam name="TR3">The value mapped from result set 3.</typeparam>
    /// <typeparam name="TR4">The value mapped from result set 4.</typeparam>
    /// <typeparam name="TR5">The value mapped from result set 5.</typeparam>
    /// <typeparam name="TR6">The value mapped from result set 6.</typeparam>
    /// <param name="connection">A caller-owned Oracle connection.</param>
    /// <param name="parameters">The provider parameters, or null.</param>
    /// <param name="unitOfWork">The optional transaction owner.</param>
    /// <returns>Mapped results in database order; empty scalar/object results are default and supported lists are empty.</returns>
    protected Task<(TR1?, TR2?, TR3?, TR4?, TR5?, TR6?)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6>(DbConnection connection, OracleParameter[]? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync(connection, AddResultParameters(parameters, 6), async reader =>
            (await TranslateResultAndMoveAsync<TR1>(reader),
             await TranslateResultAndMoveAsync<TR2>(reader),
             await TranslateResultAndMoveAsync<TR3>(reader),
             await TranslateResultAndMoveAsync<TR4>(reader),
             await TranslateResultAndMoveAsync<TR5>(reader),
             await TranslateResultAsync<TR6>(reader)), unitOfWork);

    /// <summary>Executes the procedure and maps 7 sequential result sets.</summary>
    /// <typeparam name="TR1">The value mapped from result set 1.</typeparam>
    /// <typeparam name="TR2">The value mapped from result set 2.</typeparam>
    /// <typeparam name="TR3">The value mapped from result set 3.</typeparam>
    /// <typeparam name="TR4">The value mapped from result set 4.</typeparam>
    /// <typeparam name="TR5">The value mapped from result set 5.</typeparam>
    /// <typeparam name="TR6">The value mapped from result set 6.</typeparam>
    /// <typeparam name="TR7">The value mapped from result set 7.</typeparam>
    /// <param name="connection">A caller-owned Oracle connection.</param>
    /// <param name="parameters">The provider parameters, or null.</param>
    /// <param name="unitOfWork">The optional transaction owner.</param>
    /// <returns>Mapped results in database order; empty scalar/object results are default and supported lists are empty.</returns>
    protected Task<(TR1?, TR2?, TR3?, TR4?, TR5?, TR6?, TR7?)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7>(DbConnection connection, OracleParameter[]? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync(connection, AddResultParameters(parameters, 7), async reader =>
            (await TranslateResultAndMoveAsync<TR1>(reader),
             await TranslateResultAndMoveAsync<TR2>(reader),
             await TranslateResultAndMoveAsync<TR3>(reader),
             await TranslateResultAndMoveAsync<TR4>(reader),
             await TranslateResultAndMoveAsync<TR5>(reader),
             await TranslateResultAndMoveAsync<TR6>(reader),
             await TranslateResultAsync<TR7>(reader)), unitOfWork);

    /// <summary>Executes the procedure and maps 8 sequential result sets.</summary>
    /// <typeparam name="TR1">The value mapped from result set 1.</typeparam>
    /// <typeparam name="TR2">The value mapped from result set 2.</typeparam>
    /// <typeparam name="TR3">The value mapped from result set 3.</typeparam>
    /// <typeparam name="TR4">The value mapped from result set 4.</typeparam>
    /// <typeparam name="TR5">The value mapped from result set 5.</typeparam>
    /// <typeparam name="TR6">The value mapped from result set 6.</typeparam>
    /// <typeparam name="TR7">The value mapped from result set 7.</typeparam>
    /// <typeparam name="TR8">The value mapped from result set 8.</typeparam>
    /// <param name="connection">A caller-owned Oracle connection.</param>
    /// <param name="parameters">The provider parameters, or null.</param>
    /// <param name="unitOfWork">The optional transaction owner.</param>
    /// <returns>Mapped results in database order; empty scalar/object results are default and supported lists are empty.</returns>
    protected Task<(TR1?, TR2?, TR3?, TR4?, TR5?, TR6?, TR7?, TR8?)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8>(DbConnection connection, OracleParameter[]? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync(connection, AddResultParameters(parameters, 8), async reader =>
            (await TranslateResultAndMoveAsync<TR1>(reader),
             await TranslateResultAndMoveAsync<TR2>(reader),
             await TranslateResultAndMoveAsync<TR3>(reader),
             await TranslateResultAndMoveAsync<TR4>(reader),
             await TranslateResultAndMoveAsync<TR5>(reader),
             await TranslateResultAndMoveAsync<TR6>(reader),
             await TranslateResultAndMoveAsync<TR7>(reader),
             await TranslateResultAsync<TR8>(reader)), unitOfWork);

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
    /// <param name="connection">A caller-owned Oracle connection.</param>
    /// <param name="parameters">The provider parameters, or null.</param>
    /// <param name="unitOfWork">The optional transaction owner.</param>
    /// <returns>Mapped results in database order; empty scalar/object results are default and supported lists are empty.</returns>
    protected Task<(TR1?, TR2?, TR3?, TR4?, TR5?, TR6?, TR7?, TR8?, TR9?)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9>(DbConnection connection, OracleParameter[]? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync(connection, AddResultParameters(parameters, 9), async reader =>
            (await TranslateResultAndMoveAsync<TR1>(reader),
             await TranslateResultAndMoveAsync<TR2>(reader),
             await TranslateResultAndMoveAsync<TR3>(reader),
             await TranslateResultAndMoveAsync<TR4>(reader),
             await TranslateResultAndMoveAsync<TR5>(reader),
             await TranslateResultAndMoveAsync<TR6>(reader),
             await TranslateResultAndMoveAsync<TR7>(reader),
             await TranslateResultAndMoveAsync<TR8>(reader),
             await TranslateResultAsync<TR9>(reader)), unitOfWork);

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
    /// <param name="connection">A caller-owned Oracle connection.</param>
    /// <param name="parameters">The provider parameters, or null.</param>
    /// <param name="unitOfWork">The optional transaction owner.</param>
    /// <returns>Mapped results in database order; empty scalar/object results are default and supported lists are empty.</returns>
    protected Task<(TR1?, TR2?, TR3?, TR4?, TR5?, TR6?, TR7?, TR8?, TR9?, TR10?)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10>(DbConnection connection, OracleParameter[]? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync(connection, AddResultParameters(parameters, 10), async reader =>
            (await TranslateResultAndMoveAsync<TR1>(reader),
             await TranslateResultAndMoveAsync<TR2>(reader),
             await TranslateResultAndMoveAsync<TR3>(reader),
             await TranslateResultAndMoveAsync<TR4>(reader),
             await TranslateResultAndMoveAsync<TR5>(reader),
             await TranslateResultAndMoveAsync<TR6>(reader),
             await TranslateResultAndMoveAsync<TR7>(reader),
             await TranslateResultAndMoveAsync<TR8>(reader),
             await TranslateResultAndMoveAsync<TR9>(reader),
             await TranslateResultAsync<TR10>(reader)), unitOfWork);

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
    /// <param name="connection">A caller-owned Oracle connection.</param>
    /// <param name="parameters">The provider parameters, or null.</param>
    /// <param name="unitOfWork">The optional transaction owner.</param>
    /// <returns>Mapped results in database order; empty scalar/object results are default and supported lists are empty.</returns>
    protected Task<(TR1?, TR2?, TR3?, TR4?, TR5?, TR6?, TR7?, TR8?, TR9?, TR10?, TR11?)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10, TR11>(DbConnection connection, OracleParameter[]? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync(connection, AddResultParameters(parameters, 11), async reader =>
            (await TranslateResultAndMoveAsync<TR1>(reader),
             await TranslateResultAndMoveAsync<TR2>(reader),
             await TranslateResultAndMoveAsync<TR3>(reader),
             await TranslateResultAndMoveAsync<TR4>(reader),
             await TranslateResultAndMoveAsync<TR5>(reader),
             await TranslateResultAndMoveAsync<TR6>(reader),
             await TranslateResultAndMoveAsync<TR7>(reader),
             await TranslateResultAndMoveAsync<TR8>(reader),
             await TranslateResultAndMoveAsync<TR9>(reader),
             await TranslateResultAndMoveAsync<TR10>(reader),
             await TranslateResultAsync<TR11>(reader)), unitOfWork);

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
    /// <param name="connection">A caller-owned Oracle connection.</param>
    /// <param name="parameters">The provider parameters, or null.</param>
    /// <param name="unitOfWork">The optional transaction owner.</param>
    /// <returns>Mapped results in database order; empty scalar/object results are default and supported lists are empty.</returns>
    protected Task<(TR1?, TR2?, TR3?, TR4?, TR5?, TR6?, TR7?, TR8?, TR9?, TR10?, TR11?, TR12?)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10, TR11, TR12>(DbConnection connection, OracleParameter[]? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync(connection, AddResultParameters(parameters, 12), async reader =>
            (await TranslateResultAndMoveAsync<TR1>(reader),
             await TranslateResultAndMoveAsync<TR2>(reader),
             await TranslateResultAndMoveAsync<TR3>(reader),
             await TranslateResultAndMoveAsync<TR4>(reader),
             await TranslateResultAndMoveAsync<TR5>(reader),
             await TranslateResultAndMoveAsync<TR6>(reader),
             await TranslateResultAndMoveAsync<TR7>(reader),
             await TranslateResultAndMoveAsync<TR8>(reader),
             await TranslateResultAndMoveAsync<TR9>(reader),
             await TranslateResultAndMoveAsync<TR10>(reader),
             await TranslateResultAndMoveAsync<TR11>(reader),
             await TranslateResultAsync<TR12>(reader)), unitOfWork);

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
    /// <param name="connection">A caller-owned Oracle connection.</param>
    /// <param name="parameters">The provider parameters, or null.</param>
    /// <param name="unitOfWork">The optional transaction owner.</param>
    /// <returns>Mapped results in database order; empty scalar/object results are default and supported lists are empty.</returns>
    protected Task<(TR1?, TR2?, TR3?, TR4?, TR5?, TR6?, TR7?, TR8?, TR9?, TR10?, TR11?, TR12?, TR13?)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10, TR11, TR12, TR13>(DbConnection connection, OracleParameter[]? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync(connection, AddResultParameters(parameters, 13), async reader =>
            (await TranslateResultAndMoveAsync<TR1>(reader),
             await TranslateResultAndMoveAsync<TR2>(reader),
             await TranslateResultAndMoveAsync<TR3>(reader),
             await TranslateResultAndMoveAsync<TR4>(reader),
             await TranslateResultAndMoveAsync<TR5>(reader),
             await TranslateResultAndMoveAsync<TR6>(reader),
             await TranslateResultAndMoveAsync<TR7>(reader),
             await TranslateResultAndMoveAsync<TR8>(reader),
             await TranslateResultAndMoveAsync<TR9>(reader),
             await TranslateResultAndMoveAsync<TR10>(reader),
             await TranslateResultAndMoveAsync<TR11>(reader),
             await TranslateResultAndMoveAsync<TR12>(reader),
             await TranslateResultAsync<TR13>(reader)), unitOfWork);

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
    /// <param name="connection">A caller-owned Oracle connection.</param>
    /// <param name="parameters">The provider parameters, or null.</param>
    /// <param name="unitOfWork">The optional transaction owner.</param>
    /// <returns>Mapped results in database order; empty scalar/object results are default and supported lists are empty.</returns>
    protected Task<(TR1?, TR2?, TR3?, TR4?, TR5?, TR6?, TR7?, TR8?, TR9?, TR10?, TR11?, TR12?, TR13?, TR14?)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10, TR11, TR12, TR13, TR14>(DbConnection connection, OracleParameter[]? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync(connection, AddResultParameters(parameters, 14), async reader =>
            (await TranslateResultAndMoveAsync<TR1>(reader),
             await TranslateResultAndMoveAsync<TR2>(reader),
             await TranslateResultAndMoveAsync<TR3>(reader),
             await TranslateResultAndMoveAsync<TR4>(reader),
             await TranslateResultAndMoveAsync<TR5>(reader),
             await TranslateResultAndMoveAsync<TR6>(reader),
             await TranslateResultAndMoveAsync<TR7>(reader),
             await TranslateResultAndMoveAsync<TR8>(reader),
             await TranslateResultAndMoveAsync<TR9>(reader),
             await TranslateResultAndMoveAsync<TR10>(reader),
             await TranslateResultAndMoveAsync<TR11>(reader),
             await TranslateResultAndMoveAsync<TR12>(reader),
             await TranslateResultAndMoveAsync<TR13>(reader),
             await TranslateResultAsync<TR14>(reader)), unitOfWork);

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
    /// <param name="connection">A caller-owned Oracle connection.</param>
    /// <param name="parameters">The provider parameters, or null.</param>
    /// <param name="unitOfWork">The optional transaction owner.</param>
    /// <returns>Mapped results in database order; empty scalar/object results are default and supported lists are empty.</returns>
    protected Task<(TR1?, TR2?, TR3?, TR4?, TR5?, TR6?, TR7?, TR8?, TR9?, TR10?, TR11?, TR12?, TR13?, TR14?, TR15?)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10, TR11, TR12, TR13, TR14, TR15>(DbConnection connection, OracleParameter[]? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync(connection, AddResultParameters(parameters, 15), async reader =>
            (await TranslateResultAndMoveAsync<TR1>(reader),
             await TranslateResultAndMoveAsync<TR2>(reader),
             await TranslateResultAndMoveAsync<TR3>(reader),
             await TranslateResultAndMoveAsync<TR4>(reader),
             await TranslateResultAndMoveAsync<TR5>(reader),
             await TranslateResultAndMoveAsync<TR6>(reader),
             await TranslateResultAndMoveAsync<TR7>(reader),
             await TranslateResultAndMoveAsync<TR8>(reader),
             await TranslateResultAndMoveAsync<TR9>(reader),
             await TranslateResultAndMoveAsync<TR10>(reader),
             await TranslateResultAndMoveAsync<TR11>(reader),
             await TranslateResultAndMoveAsync<TR12>(reader),
             await TranslateResultAndMoveAsync<TR13>(reader),
             await TranslateResultAndMoveAsync<TR14>(reader),
             await TranslateResultAsync<TR15>(reader)), unitOfWork);

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
    /// <param name="connection">A caller-owned Oracle connection.</param>
    /// <param name="parameters">The provider parameters, or null.</param>
    /// <param name="unitOfWork">The optional transaction owner.</param>
    /// <returns>Mapped results in database order; empty scalar/object results are default and supported lists are empty.</returns>
    protected Task<(TR1?, TR2?, TR3?, TR4?, TR5?, TR6?, TR7?, TR8?, TR9?, TR10?, TR11?, TR12?, TR13?, TR14?, TR15?, TR16?)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10, TR11, TR12, TR13, TR14, TR15, TR16>(DbConnection connection, OracleParameter[]? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync(connection, AddResultParameters(parameters, 16), async reader =>
            (await TranslateResultAndMoveAsync<TR1>(reader),
             await TranslateResultAndMoveAsync<TR2>(reader),
             await TranslateResultAndMoveAsync<TR3>(reader),
             await TranslateResultAndMoveAsync<TR4>(reader),
             await TranslateResultAndMoveAsync<TR5>(reader),
             await TranslateResultAndMoveAsync<TR6>(reader),
             await TranslateResultAndMoveAsync<TR7>(reader),
             await TranslateResultAndMoveAsync<TR8>(reader),
             await TranslateResultAndMoveAsync<TR9>(reader),
             await TranslateResultAndMoveAsync<TR10>(reader),
             await TranslateResultAndMoveAsync<TR11>(reader),
             await TranslateResultAndMoveAsync<TR12>(reader),
             await TranslateResultAndMoveAsync<TR13>(reader),
             await TranslateResultAndMoveAsync<TR14>(reader),
             await TranslateResultAndMoveAsync<TR15>(reader),
             await TranslateResultAsync<TR16>(reader)), unitOfWork);

    #endregion

    #region Private Methods

    private OracleParameter[] AddResultParameters(OracleParameter[]? parameters, int count)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<OracleParameter>(parameters ?? []);
        foreach (var parameter in result)
        {
            ArgumentNullException.ThrowIfNull(parameter);
            if (!names.Add(parameter.ParameterName.TrimStart(':')))
                throw new InvalidOperationException("Oracle parameter names must be unique.");
            if (parameter.OracleDbType == OracleDbType.RefCursor)
                throw new InvalidOperationException("Typed result cursors are added by the adapter; application mappers must not bind REF CURSOR parameters.");
        }

        for (var index = 1; index <= count; index++)
        {
            var name = GetResultParameterName(index);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            if (name.StartsWith(':') || !names.Add(name))
                throw new InvalidOperationException("Oracle result parameter names must be unprefixed, unique, and distinct from application parameters.");
            result.Add(new OracleParameter(name, OracleDbType.RefCursor) { Direction = ParameterDirection.Output });
        }

        return [.. result];
    }

    private static async Task<T?> TranslateResultAsync<T>(DbDataReader reader)
    {
        if (!typeof(T).IsPrimitive)
            return await reader.TranslateAsync<T>();

        if (!await reader.ReadAsync())
            return default;

        var value = reader.GetValue(0);
        var typeCode = Type.GetTypeCode(typeof(T));
        if (typeCode == TypeCode.Boolean && value is not bool)
        {
            if (value is double or float)
            {
                var number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                if (number is not (0 or 1))
                    throw new InvalidCastException("An Oracle Boolean result must be zero or one.");
            }
            else
            {
                var number = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                if (number is not (0 or 1))
                    throw new InvalidCastException("An Oracle Boolean result must be zero or one.");
            }
        }
        else if (typeCode is TypeCode.Byte or TypeCode.SByte or TypeCode.Int16 or TypeCode.UInt16 or
                 TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64)
        {
            // Convert.ChangeType rounds fractions; typed integral results must reject data loss.
            if (value is double or float)
            {
                var number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                if (!double.IsFinite(number) || Math.Truncate(number) != number)
                    throw new InvalidCastException("A fractional Oracle value cannot be mapped to an integral result.");
            }
            else
            {
                var number = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                if (decimal.Truncate(number) != number)
                    throw new InvalidCastException("A fractional Oracle value cannot be mapped to an integral result.");
            }
        }

        // Oracle NUMBER GetValue can return Decimal for a requested CLR primitive.
        return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
    }

    private static async Task<T?> TranslateResultAndMoveAsync<T>(DbDataReader reader)
    {
        var result = await TranslateResultAsync<T>(reader);
        await reader.NextResultAsync();
        return result;
    }

    private async Task<T> ExecuteCommandAsync<T>(DbConnection connection, OracleParameter[]? parameters,
        Func<OracleCommand, Task<T>> execute, IUnitOfWork? unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(connection);
        if (connection is not OracleConnection oracleConnection)
            throw new ArgumentException("An Oracle.ManagedDataAccess.Client.OracleConnection is required.", nameof(connection));

        var hasActiveTransaction = unitOfWork?.HasActiveTransaction ?? false;
        OracleCommand? command = null;
        Exception? executionException = null;
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(StoredProcedureName);
            command = oracleConnection.CreateCommand();
            command.CommandText = StoredProcedureName;
            command.CommandType = CommandType.StoredProcedure;
            command.BindByName = true;
            var executionTimeout = ExecutionTimeout;
            if (executionTimeout.HasValue)
            {
                ArgumentOutOfRangeException.ThrowIfNegative(executionTimeout.Value, nameof(ExecutionTimeout));
                command.CommandTimeout = executionTimeout.Value;
            }

            if (hasActiveTransaction)
            {
                // Oracle ignores Transaction assignments through IDbCommand. Capture the unit of
                // work's transaction before relying on Oracle's connection-local transaction context.
                var binding = new TransactionBindingCommand(command);
                unitOfWork!.UseTransaction(binding);
                if (binding.Transaction is not OracleTransaction transaction ||
                    !ReferenceEquals(transaction.Connection, oracleConnection) ||
                    !ReferenceEquals(command.Transaction, transaction))
                    throw new InvalidOperationException("The active Oracle transaction must use the command's connection.");
            }

            if (parameters is not null)
                command.Parameters.AddRange(parameters);

            if (oracleConnection.State == ConnectionState.Closed)
                await oracleConnection.OpenAsync();

            return await execute(command);
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
                    await oracleConnection.CloseAsync();
            }
            catch (Exception exception)
            {
                cleanupException ??= exception;
            }

            if (executionException is null && cleanupException is not null)
                ExceptionDispatchInfo.Capture(cleanupException).Throw();
        }
    }

    private static async Task<T> ReadAsync<T>(OracleCommand command, Func<DbDataReader, Task<T>> read)
    {
        DbDataReader? reader = null;
        Exception? executionException = null;
        try
        {
            reader = await command.ExecuteReaderAsync();
            var result = await read(reader);
            // Release every remaining cursor before the connection can be reused.
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
