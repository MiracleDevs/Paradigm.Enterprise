using Microsoft.Data.SqlClient;
using Microsoft.DemoManagementSystem.Data.Core.Extensions;
using Paradigm.Enterprise.Domain.Uow;
using System.Data;
using System.Data.Common;

namespace Paradigm.Enterprise.Data.SqlServer.StoredProcedures;

internal abstract class StoredProcedureBase<TParameters> : StoredProcedureBase
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

internal abstract class StoredProcedureBase
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
    protected SqlParameter[]? GetSqlParameters<TParameters>(TParameters? parameters)
    {
        if (parameters is null) return null;
        using var mapper = SqlParameterMapperFactory.GetMapper<TParameters>();
        return mapper.Map(parameters);
    }

    /// <summary>
    /// Executes the stored procedure.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="parameters">The parameters.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    protected async Task ExecuteAsync(DbConnection connection, SqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
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
    protected async Task<T> ExecuteAsync<T>(DbConnection connection, SqlParameter[]? parameters, Func<DbDataReader, Task<T>> readerExecutedAction, IUnitOfWork? unitOfWork = null)
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
    /// Executes the stored procedure.
    /// </summary>
    /// <typeparam name="TR1">The type of the result 1.</typeparam>
    /// <param name="connection">The connection.</param>
    /// <param name="parameters">The parameters.</param>
    /// <returns></returns>
    protected async Task<TR1?> ExecuteAsync<TR1>(DbConnection connection, SqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
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
    protected async Task<(TR1, TR2)> ExecuteAsync<TR1, TR2>(DbConnection connection, SqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync(connection, parameters, async reader =>
            (await reader.TranslateAndMoveAsync<TR1>(),
             await reader.TranslateAndMoveAsync<TR2>()), unitOfWork);
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
    protected async Task<(TR1, TR2, TR3)> ExecuteAsync<TR1, TR2, TR3>(DbConnection connection, SqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync(connection, parameters, async reader =>
            (await reader.TranslateAndMoveAsync<TR1>(),
             await reader.TranslateAndMoveAsync<TR2>(),
             await reader.TranslateAndMoveAsync<TR3>()), unitOfWork);
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
    protected async Task<(TR1, TR2, TR3, TR4)> ExecuteAsync<TR1, TR2, TR3, TR4>(DbConnection connection, SqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync(connection, parameters, async reader =>
            (await reader.TranslateAndMoveAsync<TR1>(),
                await reader.TranslateAndMoveAsync<TR2>(),
                await reader.TranslateAndMoveAsync<TR3>(),
                await reader.TranslateAndMoveAsync<TR4>()), unitOfWork);
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
    protected async Task<(TR1, TR2, TR3, TR4, TR5)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5>(DbConnection connection, SqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync(connection, parameters, async reader =>
            (await reader.TranslateAndMoveAsync<TR1>(),
                await reader.TranslateAndMoveAsync<TR2>(),
                await reader.TranslateAndMoveAsync<TR3>(),
                await reader.TranslateAndMoveAsync<TR4>(),
                await reader.TranslateAndMoveAsync<TR5>()), unitOfWork);
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
    protected async Task<(TR1, TR2, TR3, TR4, TR5, TR6)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6>(DbConnection connection, SqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
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
    protected async Task<(TR1, TR2, TR3, TR4, TR5, TR6, TR7)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7>(DbConnection connection, SqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
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
    protected async Task<(TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8>(DbConnection connection, SqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
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
    protected async Task<(TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9>(DbConnection connection, SqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
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
    protected async Task<(TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10>(DbConnection connection, SqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
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
    protected async Task<(TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10, TR11)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10, TR11>(DbConnection connection, SqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
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
    protected async Task<(TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10, TR11, TR12)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10, TR11, TR12>(DbConnection connection, SqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
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
    protected async Task<(TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10, TR11, TR12, TR13)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10, TR11, TR12, TR13>(DbConnection connection, SqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
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
    protected async Task<(TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10, TR11, TR12, TR13, TR14)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10, TR11, TR12, TR13, TR14>(DbConnection connection, SqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
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
    protected async Task<(TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10, TR11, TR12, TR13, TR14, TR15)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10, TR11, TR12, TR13, TR14, TR15>(DbConnection connection, SqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
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
    protected async Task<(TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10, TR11, TR12, TR13, TR14, TR15, TR16)> ExecuteAsync<TR1, TR2, TR3, TR4, TR5, TR6, TR7, TR8, TR9, TR10, TR11, TR12, TR13, TR14, TR15, TR16>(DbConnection connection, SqlParameter[]? parameters, IUnitOfWork? unitOfWork = null)
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