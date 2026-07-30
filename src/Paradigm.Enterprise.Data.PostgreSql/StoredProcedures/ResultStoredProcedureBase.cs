using Paradigm.Enterprise.Domain.Uow;
using System.Data.Common;

namespace Paradigm.Enterprise.Data.PostgreSql.StoredProcedures;

/// <summary>
/// Defines a stored procedure that maps its first result set to a single result value.
/// </summary>
/// <typeparam name="TParameters">The application type mapped to command parameters.</typeparam>
/// <typeparam name="TResult">The value mapped from result set 1.</typeparam>
/// <example>
/// A single-result PostgreSQL procedure maps its ordinary first result set and does not use the
/// multi-cursor path:
/// <code>
/// static async Task&lt;OrderView?&gt; FindAsync(
///     FindOrderProcedure procedure,
///     DbConnection connection,
///     FindOrderParameters parameters,
///     IUnitOfWork unitOfWork)
/// {
///     return await procedure.ExecuteAsync(connection, parameters, unitOfWork);
/// }
///
/// sealed record FindOrderParameters(int OrderId);
/// sealed record OrderView(int OrderId, string Status);
///
/// sealed class FindOrderProcedure :
///     ResultStoredProcedureBase&lt;FindOrderParameters, OrderView&gt;
/// {
///     protected override string StoredProcedureName =&gt; "find_order";
/// }
/// </code>
/// Register the <c>FindOrderParameters</c> mapper before calling the procedure.
/// </example>
public abstract class ResultStoredProcedureBase<TParameters, TResult> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure and maps its first result set.
    /// </summary>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The application parameters to map to provider parameters.</param>
    /// <param name="unitOfWork">The optional unit of work whose active transaction is attached to the command.</param>
    /// <returns>The mapped value, or <see langword="null"/> when the result set contains no row.</returns>
    /// <remarks>
    /// When no active unit-of-work transaction is supplied, execution opens the connection if needed and closes it afterward.
    /// With an active transaction, the connection remains open for the transaction owner.
    /// </remarks>
    public async Task<TResult?> ExecuteAsync(DbConnection connection, TParameters? parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync<TResult>(connection, GetSqlParameters(parameters), unitOfWork);
    }
}

/// <summary>
/// Defines a stored procedure that maps two distinct returned PostgreSQL cursor names to a result tuple.
/// </summary>
/// <typeparam name="TParameters">The application type mapped to command parameters.</typeparam>
/// <typeparam name="TResult1">The value mapped from result set 1.</typeparam>
/// <typeparam name="TResult2">The value mapped from result set 2.</typeparam>
/// <remarks>
/// The procedure must return two distinct cursor names. Tuple positions follow the implementation-defined
/// enumeration of those distinct names and do not promise database return order. Empty cursors
/// produce null reference values or default value-type values.
/// </remarks>
/// <example>
/// The tuple annotations make empty cursors explicit to callers:
/// <code>
/// static async Task ReadAsync(
///     ResultStoredProcedureBase&lt;LookupParameters, Customer, Address&gt; procedure,
///     DbConnection connection,
///     LookupParameters parameters,
///     IUnitOfWork unitOfWork)
/// {
///     (Customer? customer, Address? address) =
///         await procedure.ExecuteAsync(connection, parameters, unitOfWork);
///
///     if (customer is null || address is null)
///         return;
///
///     // A transaction is required for refcursor fetches. Cursor-to-tuple positions use
///     // implementation-defined distinct-name enumeration, not database return order.
/// }
/// </code>
/// </example>
public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure and maps 2 distinct returned PostgreSQL cursor names to tuple elements.
    /// </summary>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The application parameters to map to PostgreSQL parameters.</param>
    /// <param name="unitOfWork">The required unit of work used to create or reuse the transaction that owns the returned cursors.</param>
    /// <returns>A tuple whose positions follow the implementation-defined enumeration of distinct cursor names. Empty cursors produce null reference values or default value-type values.</returns>
    /// <remarks>
    /// PostgreSQL cursors require a transaction. If the unit of work has no active transaction,
    /// execution creates a transaction for the cursor fetches and disposes it when mapping completes.
    /// The procedure must return 2 distinct cursor names; tuple positions do not promise database return order.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="unitOfWork"/> is <see langword="null"/>.</exception>
    public async Task<(TResult1?, TResult2?)> ExecuteAsync(DbConnection connection, TParameters? parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync<TResult1, TResult2>(connection, GetSqlParameters(parameters), unitOfWork);
    }
}

/// <summary>
/// Defines a stored procedure that maps 3 distinct returned PostgreSQL cursor names to a result tuple.
/// </summary>
/// <typeparam name="TParameters">The application type mapped to command parameters.</typeparam>
/// <typeparam name="TResult1">The value mapped from result set 1.</typeparam>
/// <typeparam name="TResult2">The value mapped from result set 2.</typeparam>
/// <typeparam name="TResult3">The value mapped from result set 3.</typeparam>
/// <remarks>
/// Tuple positions follow the implementation-defined enumeration of distinct cursor names and do not
/// promise database return order. The procedure must return the declared number of distinct cursor names.
/// </remarks>
public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure and maps 3 distinct returned PostgreSQL cursor names to tuple elements.
    /// </summary>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The application parameters to map to PostgreSQL parameters.</param>
    /// <param name="unitOfWork">The required unit of work used to create or reuse the transaction that owns the returned cursors.</param>
    /// <returns>A tuple whose positions follow the implementation-defined enumeration of distinct cursor names. Empty cursors produce null reference values or default value-type values.</returns>
    /// <remarks>
    /// PostgreSQL cursors require a transaction. If the unit of work has no active transaction,
    /// execution creates a transaction for the cursor fetches and disposes it when mapping completes.
    /// The procedure must return 3 distinct cursor names; tuple positions do not promise database return order.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="unitOfWork"/> is <see langword="null"/>.</exception>
    public async Task<(TResult1?, TResult2?, TResult3?)> ExecuteAsync(DbConnection connection, TParameters parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync<TResult1, TResult2, TResult3>(connection, GetSqlParameters(parameters), unitOfWork);
    }
}

/// <summary>
/// Defines a stored procedure that maps 4 distinct returned PostgreSQL cursor names to a result tuple.
/// </summary>
/// <typeparam name="TParameters">The application type mapped to command parameters.</typeparam>
/// <typeparam name="TResult1">The value mapped from result set 1.</typeparam>
/// <typeparam name="TResult2">The value mapped from result set 2.</typeparam>
/// <typeparam name="TResult3">The value mapped from result set 3.</typeparam>
/// <typeparam name="TResult4">The value mapped from result set 4.</typeparam>
/// <remarks>
/// Tuple positions follow the implementation-defined enumeration of distinct cursor names and do not
/// promise database return order. The procedure must return the declared number of distinct cursor names.
/// </remarks>
public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure and maps 4 distinct returned PostgreSQL cursor names to tuple elements.
    /// </summary>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The application parameters to map to PostgreSQL parameters.</param>
    /// <param name="unitOfWork">The required unit of work used to create or reuse the transaction that owns the returned cursors.</param>
    /// <returns>A tuple whose positions follow the implementation-defined enumeration of distinct cursor names. Empty cursors produce null reference values or default value-type values.</returns>
    /// <remarks>
    /// PostgreSQL cursors require a transaction. If the unit of work has no active transaction,
    /// execution creates a transaction for the cursor fetches and disposes it when mapping completes.
    /// The procedure must return 4 distinct cursor names; tuple positions do not promise database return order.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="unitOfWork"/> is <see langword="null"/>.</exception>
    public async Task<(TResult1?, TResult2?, TResult3?, TResult4?)> ExecuteAsync(DbConnection connection, TParameters parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync<TResult1, TResult2, TResult3, TResult4>(connection, GetSqlParameters(parameters), unitOfWork);
    }
}

/// <summary>
/// Defines a stored procedure that maps 5 distinct returned PostgreSQL cursor names to a result tuple.
/// </summary>
/// <typeparam name="TParameters">The application type mapped to command parameters.</typeparam>
/// <typeparam name="TResult1">The value mapped from result set 1.</typeparam>
/// <typeparam name="TResult2">The value mapped from result set 2.</typeparam>
/// <typeparam name="TResult3">The value mapped from result set 3.</typeparam>
/// <typeparam name="TResult4">The value mapped from result set 4.</typeparam>
/// <typeparam name="TResult5">The value mapped from result set 5.</typeparam>
/// <remarks>
/// Tuple positions follow the implementation-defined enumeration of distinct cursor names and do not
/// promise database return order. The procedure must return the declared number of distinct cursor names.
/// </remarks>
public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4, TResult5> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure and maps 5 distinct returned PostgreSQL cursor names to tuple elements.
    /// </summary>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The application parameters to map to PostgreSQL parameters.</param>
    /// <param name="unitOfWork">The required unit of work used to create or reuse the transaction that owns the returned cursors.</param>
    /// <returns>A tuple whose positions follow the implementation-defined enumeration of distinct cursor names. Empty cursors produce null reference values or default value-type values.</returns>
    /// <remarks>
    /// PostgreSQL cursors require a transaction. If the unit of work has no active transaction,
    /// execution creates a transaction for the cursor fetches and disposes it when mapping completes.
    /// The procedure must return 5 distinct cursor names; tuple positions do not promise database return order.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="unitOfWork"/> is <see langword="null"/>.</exception>
    public async Task<(TResult1?, TResult2?, TResult3?, TResult4?, TResult5?)> ExecuteAsync(DbConnection connection, TParameters parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync<TResult1, TResult2, TResult3, TResult4, TResult5>(connection, GetSqlParameters(parameters), unitOfWork);
    }
}

/// <summary>
/// Defines a stored procedure that maps 6 distinct returned PostgreSQL cursor names to a result tuple.
/// </summary>
/// <typeparam name="TParameters">The application type mapped to command parameters.</typeparam>
/// <typeparam name="TResult1">The value mapped from result set 1.</typeparam>
/// <typeparam name="TResult2">The value mapped from result set 2.</typeparam>
/// <typeparam name="TResult3">The value mapped from result set 3.</typeparam>
/// <typeparam name="TResult4">The value mapped from result set 4.</typeparam>
/// <typeparam name="TResult5">The value mapped from result set 5.</typeparam>
/// <typeparam name="TResult6">The value mapped from result set 6.</typeparam>
/// <remarks>
/// Tuple positions follow the implementation-defined enumeration of distinct cursor names and do not
/// promise database return order. The procedure must return the declared number of distinct cursor names.
/// </remarks>
public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure and maps 6 distinct returned PostgreSQL cursor names to tuple elements.
    /// </summary>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The application parameters to map to PostgreSQL parameters.</param>
    /// <param name="unitOfWork">The required unit of work used to create or reuse the transaction that owns the returned cursors.</param>
    /// <returns>A tuple whose positions follow the implementation-defined enumeration of distinct cursor names. Empty cursors produce null reference values or default value-type values.</returns>
    /// <remarks>
    /// PostgreSQL cursors require a transaction. If the unit of work has no active transaction,
    /// execution creates a transaction for the cursor fetches and disposes it when mapping completes.
    /// The procedure must return 6 distinct cursor names; tuple positions do not promise database return order.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="unitOfWork"/> is <see langword="null"/>.</exception>
    public async Task<(TResult1?, TResult2?, TResult3?, TResult4?, TResult5?, TResult6?)> ExecuteAsync(DbConnection connection, TParameters parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6>(connection, GetSqlParameters(parameters), unitOfWork);
    }
}

/// <summary>
/// Defines a stored procedure that maps 7 distinct returned PostgreSQL cursor names to a result tuple.
/// </summary>
/// <typeparam name="TParameters">The application type mapped to command parameters.</typeparam>
/// <typeparam name="TResult1">The value mapped from result set 1.</typeparam>
/// <typeparam name="TResult2">The value mapped from result set 2.</typeparam>
/// <typeparam name="TResult3">The value mapped from result set 3.</typeparam>
/// <typeparam name="TResult4">The value mapped from result set 4.</typeparam>
/// <typeparam name="TResult5">The value mapped from result set 5.</typeparam>
/// <typeparam name="TResult6">The value mapped from result set 6.</typeparam>
/// <typeparam name="TResult7">The value mapped from result set 7.</typeparam>
/// <remarks>
/// Tuple positions follow the implementation-defined enumeration of distinct cursor names and do not
/// promise database return order. The procedure must return the declared number of distinct cursor names.
/// </remarks>
public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure and maps 7 distinct returned PostgreSQL cursor names to tuple elements.
    /// </summary>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The application parameters to map to PostgreSQL parameters.</param>
    /// <param name="unitOfWork">The required unit of work used to create or reuse the transaction that owns the returned cursors.</param>
    /// <returns>A tuple whose positions follow the implementation-defined enumeration of distinct cursor names. Empty cursors produce null reference values or default value-type values.</returns>
    /// <remarks>
    /// PostgreSQL cursors require a transaction. If the unit of work has no active transaction,
    /// execution creates a transaction for the cursor fetches and disposes it when mapping completes.
    /// The procedure must return 7 distinct cursor names; tuple positions do not promise database return order.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="unitOfWork"/> is <see langword="null"/>.</exception>
    public async Task<(TResult1?, TResult2?, TResult3?, TResult4?, TResult5?, TResult6?, TResult7?)> ExecuteAsync(DbConnection connection, TParameters parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7>(connection, GetSqlParameters(parameters), unitOfWork);
    }
}

/// <summary>
/// Defines a stored procedure that maps 8 distinct returned PostgreSQL cursor names to a result tuple.
/// </summary>
/// <typeparam name="TParameters">The application type mapped to command parameters.</typeparam>
/// <typeparam name="TResult1">The value mapped from result set 1.</typeparam>
/// <typeparam name="TResult2">The value mapped from result set 2.</typeparam>
/// <typeparam name="TResult3">The value mapped from result set 3.</typeparam>
/// <typeparam name="TResult4">The value mapped from result set 4.</typeparam>
/// <typeparam name="TResult5">The value mapped from result set 5.</typeparam>
/// <typeparam name="TResult6">The value mapped from result set 6.</typeparam>
/// <typeparam name="TResult7">The value mapped from result set 7.</typeparam>
/// <typeparam name="TResult8">The value mapped from result set 8.</typeparam>
/// <remarks>
/// Tuple positions follow the implementation-defined enumeration of distinct cursor names and do not
/// promise database return order. The procedure must return the declared number of distinct cursor names.
/// </remarks>
public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure and maps 8 distinct returned PostgreSQL cursor names to tuple elements.
    /// </summary>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The application parameters to map to PostgreSQL parameters.</param>
    /// <param name="unitOfWork">The required unit of work used to create or reuse the transaction that owns the returned cursors.</param>
    /// <returns>A tuple whose positions follow the implementation-defined enumeration of distinct cursor names. Empty cursors produce null reference values or default value-type values.</returns>
    /// <remarks>
    /// PostgreSQL cursors require a transaction. If the unit of work has no active transaction,
    /// execution creates a transaction for the cursor fetches and disposes it when mapping completes.
    /// The procedure must return 8 distinct cursor names; tuple positions do not promise database return order.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="unitOfWork"/> is <see langword="null"/>.</exception>
    public async Task<(TResult1?, TResult2?, TResult3?, TResult4?, TResult5?, TResult6?, TResult7?, TResult8?)> ExecuteAsync(DbConnection connection, TParameters parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8>(connection, GetSqlParameters(parameters), unitOfWork);
    }
}

/// <summary>
/// Defines a stored procedure that maps 9 distinct returned PostgreSQL cursor names to a result tuple.
/// </summary>
/// <typeparam name="TParameters">The application type mapped to command parameters.</typeparam>
/// <typeparam name="TResult1">The value mapped from result set 1.</typeparam>
/// <typeparam name="TResult2">The value mapped from result set 2.</typeparam>
/// <typeparam name="TResult3">The value mapped from result set 3.</typeparam>
/// <typeparam name="TResult4">The value mapped from result set 4.</typeparam>
/// <typeparam name="TResult5">The value mapped from result set 5.</typeparam>
/// <typeparam name="TResult6">The value mapped from result set 6.</typeparam>
/// <typeparam name="TResult7">The value mapped from result set 7.</typeparam>
/// <typeparam name="TResult8">The value mapped from result set 8.</typeparam>
/// <typeparam name="TResult9">The value mapped from result set 9.</typeparam>
/// <remarks>
/// Tuple positions follow the implementation-defined enumeration of distinct cursor names and do not
/// promise database return order. The procedure must return the declared number of distinct cursor names.
/// </remarks>
public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure and maps 9 distinct returned PostgreSQL cursor names to tuple elements.
    /// </summary>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The application parameters to map to PostgreSQL parameters.</param>
    /// <param name="unitOfWork">The required unit of work used to create or reuse the transaction that owns the returned cursors.</param>
    /// <returns>A tuple whose positions follow the implementation-defined enumeration of distinct cursor names. Empty cursors produce null reference values or default value-type values.</returns>
    /// <remarks>
    /// PostgreSQL cursors require a transaction. If the unit of work has no active transaction,
    /// execution creates a transaction for the cursor fetches and disposes it when mapping completes.
    /// The procedure must return 9 distinct cursor names; tuple positions do not promise database return order.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="unitOfWork"/> is <see langword="null"/>.</exception>
    public async Task<(TResult1?, TResult2?, TResult3?, TResult4?, TResult5?, TResult6?, TResult7?, TResult8?, TResult9?)> ExecuteAsync(DbConnection connection, TParameters parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9>(connection, GetSqlParameters(parameters), unitOfWork);
    }
}

/// <summary>
/// Defines a stored procedure that maps 10 distinct returned PostgreSQL cursor names to a result tuple.
/// </summary>
/// <typeparam name="TParameters">The application type mapped to command parameters.</typeparam>
/// <typeparam name="TResult1">The value mapped from result set 1.</typeparam>
/// <typeparam name="TResult2">The value mapped from result set 2.</typeparam>
/// <typeparam name="TResult3">The value mapped from result set 3.</typeparam>
/// <typeparam name="TResult4">The value mapped from result set 4.</typeparam>
/// <typeparam name="TResult5">The value mapped from result set 5.</typeparam>
/// <typeparam name="TResult6">The value mapped from result set 6.</typeparam>
/// <typeparam name="TResult7">The value mapped from result set 7.</typeparam>
/// <typeparam name="TResult8">The value mapped from result set 8.</typeparam>
/// <typeparam name="TResult9">The value mapped from result set 9.</typeparam>
/// <typeparam name="TResult10">The value mapped from result set 10.</typeparam>
/// <remarks>
/// Tuple positions follow the implementation-defined enumeration of distinct cursor names and do not
/// promise database return order. The procedure must return the declared number of distinct cursor names.
/// </remarks>
public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure and maps 10 distinct returned PostgreSQL cursor names to tuple elements.
    /// </summary>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The application parameters to map to PostgreSQL parameters.</param>
    /// <param name="unitOfWork">The required unit of work used to create or reuse the transaction that owns the returned cursors.</param>
    /// <returns>A tuple whose positions follow the implementation-defined enumeration of distinct cursor names. Empty cursors produce null reference values or default value-type values.</returns>
    /// <remarks>
    /// PostgreSQL cursors require a transaction. If the unit of work has no active transaction,
    /// execution creates a transaction for the cursor fetches and disposes it when mapping completes.
    /// The procedure must return 10 distinct cursor names; tuple positions do not promise database return order.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="unitOfWork"/> is <see langword="null"/>.</exception>
    public async Task<(TResult1?, TResult2?, TResult3?, TResult4?, TResult5?, TResult6?, TResult7?, TResult8?, TResult9?, TResult10?)> ExecuteAsync(DbConnection connection, TParameters parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10>(connection, GetSqlParameters(parameters), unitOfWork);
    }
}

/// <summary>
/// Defines a stored procedure that maps 11 distinct returned PostgreSQL cursor names to a result tuple.
/// </summary>
/// <typeparam name="TParameters">The application type mapped to command parameters.</typeparam>
/// <typeparam name="TResult1">The value mapped from result set 1.</typeparam>
/// <typeparam name="TResult2">The value mapped from result set 2.</typeparam>
/// <typeparam name="TResult3">The value mapped from result set 3.</typeparam>
/// <typeparam name="TResult4">The value mapped from result set 4.</typeparam>
/// <typeparam name="TResult5">The value mapped from result set 5.</typeparam>
/// <typeparam name="TResult6">The value mapped from result set 6.</typeparam>
/// <typeparam name="TResult7">The value mapped from result set 7.</typeparam>
/// <typeparam name="TResult8">The value mapped from result set 8.</typeparam>
/// <typeparam name="TResult9">The value mapped from result set 9.</typeparam>
/// <typeparam name="TResult10">The value mapped from result set 10.</typeparam>
/// <typeparam name="TResult11">The value mapped from result set 11.</typeparam>
/// <remarks>
/// Tuple positions follow the implementation-defined enumeration of distinct cursor names and do not
/// promise database return order. The procedure must return the declared number of distinct cursor names.
/// </remarks>
public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure and maps 11 distinct returned PostgreSQL cursor names to tuple elements.
    /// </summary>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The application parameters to map to PostgreSQL parameters.</param>
    /// <param name="unitOfWork">The required unit of work used to create or reuse the transaction that owns the returned cursors.</param>
    /// <returns>A tuple whose positions follow the implementation-defined enumeration of distinct cursor names. Empty cursors produce null reference values or default value-type values.</returns>
    /// <remarks>
    /// PostgreSQL cursors require a transaction. If the unit of work has no active transaction,
    /// execution creates a transaction for the cursor fetches and disposes it when mapping completes.
    /// The procedure must return 11 distinct cursor names; tuple positions do not promise database return order.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="unitOfWork"/> is <see langword="null"/>.</exception>
    public async Task<(TResult1?, TResult2?, TResult3?, TResult4?, TResult5?, TResult6?, TResult7?, TResult8?, TResult9?, TResult10?, TResult11?)> ExecuteAsync(DbConnection connection, TParameters parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11>(connection, GetSqlParameters(parameters), unitOfWork);
    }
}

/// <summary>
/// Defines a stored procedure that maps 12 distinct returned PostgreSQL cursor names to a result tuple.
/// </summary>
/// <typeparam name="TParameters">The application type mapped to command parameters.</typeparam>
/// <typeparam name="TResult1">The value mapped from result set 1.</typeparam>
/// <typeparam name="TResult2">The value mapped from result set 2.</typeparam>
/// <typeparam name="TResult3">The value mapped from result set 3.</typeparam>
/// <typeparam name="TResult4">The value mapped from result set 4.</typeparam>
/// <typeparam name="TResult5">The value mapped from result set 5.</typeparam>
/// <typeparam name="TResult6">The value mapped from result set 6.</typeparam>
/// <typeparam name="TResult7">The value mapped from result set 7.</typeparam>
/// <typeparam name="TResult8">The value mapped from result set 8.</typeparam>
/// <typeparam name="TResult9">The value mapped from result set 9.</typeparam>
/// <typeparam name="TResult10">The value mapped from result set 10.</typeparam>
/// <typeparam name="TResult11">The value mapped from result set 11.</typeparam>
/// <typeparam name="TResult12">The value mapped from result set 12.</typeparam>
/// <remarks>
/// Tuple positions follow the implementation-defined enumeration of distinct cursor names and do not
/// promise database return order. The procedure must return the declared number of distinct cursor names.
/// </remarks>
public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11, TResult12> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure and maps 12 distinct returned PostgreSQL cursor names to tuple elements.
    /// </summary>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The application parameters to map to PostgreSQL parameters.</param>
    /// <param name="unitOfWork">The required unit of work used to create or reuse the transaction that owns the returned cursors.</param>
    /// <returns>A tuple whose positions follow the implementation-defined enumeration of distinct cursor names. Empty cursors produce null reference values or default value-type values.</returns>
    /// <remarks>
    /// PostgreSQL cursors require a transaction. If the unit of work has no active transaction,
    /// execution creates a transaction for the cursor fetches and disposes it when mapping completes.
    /// The procedure must return 12 distinct cursor names; tuple positions do not promise database return order.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="unitOfWork"/> is <see langword="null"/>.</exception>
    public async Task<(TResult1?, TResult2?, TResult3?, TResult4?, TResult5?, TResult6?, TResult7?, TResult8?, TResult9?, TResult10?, TResult11?, TResult12?)> ExecuteAsync(DbConnection connection, TParameters parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11, TResult12>(connection, GetSqlParameters(parameters), unitOfWork);
    }
}

/// <summary>
/// Defines a stored procedure that maps 13 distinct returned PostgreSQL cursor names to a result tuple.
/// </summary>
/// <typeparam name="TParameters">The application type mapped to command parameters.</typeparam>
/// <typeparam name="TResult1">The value mapped from result set 1.</typeparam>
/// <typeparam name="TResult2">The value mapped from result set 2.</typeparam>
/// <typeparam name="TResult3">The value mapped from result set 3.</typeparam>
/// <typeparam name="TResult4">The value mapped from result set 4.</typeparam>
/// <typeparam name="TResult5">The value mapped from result set 5.</typeparam>
/// <typeparam name="TResult6">The value mapped from result set 6.</typeparam>
/// <typeparam name="TResult7">The value mapped from result set 7.</typeparam>
/// <typeparam name="TResult8">The value mapped from result set 8.</typeparam>
/// <typeparam name="TResult9">The value mapped from result set 9.</typeparam>
/// <typeparam name="TResult10">The value mapped from result set 10.</typeparam>
/// <typeparam name="TResult11">The value mapped from result set 11.</typeparam>
/// <typeparam name="TResult12">The value mapped from result set 12.</typeparam>
/// <typeparam name="TResult13">The value mapped from result set 13.</typeparam>
/// <remarks>
/// Tuple positions follow the implementation-defined enumeration of distinct cursor names and do not
/// promise database return order. The procedure must return the declared number of distinct cursor names.
/// </remarks>
public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11, TResult12, TResult13> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure and maps 13 distinct returned PostgreSQL cursor names to tuple elements.
    /// </summary>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The application parameters to map to PostgreSQL parameters.</param>
    /// <param name="unitOfWork">The required unit of work used to create or reuse the transaction that owns the returned cursors.</param>
    /// <returns>A tuple whose positions follow the implementation-defined enumeration of distinct cursor names. Empty cursors produce null reference values or default value-type values.</returns>
    /// <remarks>
    /// PostgreSQL cursors require a transaction. If the unit of work has no active transaction,
    /// execution creates a transaction for the cursor fetches and disposes it when mapping completes.
    /// The procedure must return 13 distinct cursor names; tuple positions do not promise database return order.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="unitOfWork"/> is <see langword="null"/>.</exception>
    public async Task<(TResult1?, TResult2?, TResult3?, TResult4?, TResult5?, TResult6?, TResult7?, TResult8?, TResult9?, TResult10?, TResult11?, TResult12?, TResult13?)> ExecuteAsync(DbConnection connection, TParameters parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11, TResult12, TResult13>(connection, GetSqlParameters(parameters), unitOfWork);
    }
}

/// <summary>
/// Defines a stored procedure that maps 14 distinct returned PostgreSQL cursor names to a result tuple.
/// </summary>
/// <typeparam name="TParameters">The application type mapped to command parameters.</typeparam>
/// <typeparam name="TResult1">The value mapped from result set 1.</typeparam>
/// <typeparam name="TResult2">The value mapped from result set 2.</typeparam>
/// <typeparam name="TResult3">The value mapped from result set 3.</typeparam>
/// <typeparam name="TResult4">The value mapped from result set 4.</typeparam>
/// <typeparam name="TResult5">The value mapped from result set 5.</typeparam>
/// <typeparam name="TResult6">The value mapped from result set 6.</typeparam>
/// <typeparam name="TResult7">The value mapped from result set 7.</typeparam>
/// <typeparam name="TResult8">The value mapped from result set 8.</typeparam>
/// <typeparam name="TResult9">The value mapped from result set 9.</typeparam>
/// <typeparam name="TResult10">The value mapped from result set 10.</typeparam>
/// <typeparam name="TResult11">The value mapped from result set 11.</typeparam>
/// <typeparam name="TResult12">The value mapped from result set 12.</typeparam>
/// <typeparam name="TResult13">The value mapped from result set 13.</typeparam>
/// <typeparam name="TResult14">The value mapped from result set 14.</typeparam>
/// <remarks>
/// Tuple positions follow the implementation-defined enumeration of distinct cursor names and do not
/// promise database return order. The procedure must return the declared number of distinct cursor names.
/// </remarks>
public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11, TResult12, TResult13, TResult14> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure and maps 14 distinct returned PostgreSQL cursor names to tuple elements.
    /// </summary>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The application parameters to map to PostgreSQL parameters.</param>
    /// <param name="unitOfWork">The required unit of work used to create or reuse the transaction that owns the returned cursors.</param>
    /// <returns>A tuple whose positions follow the implementation-defined enumeration of distinct cursor names. Empty cursors produce null reference values or default value-type values.</returns>
    /// <remarks>
    /// PostgreSQL cursors require a transaction. If the unit of work has no active transaction,
    /// execution creates a transaction for the cursor fetches and disposes it when mapping completes.
    /// The procedure must return 14 distinct cursor names; tuple positions do not promise database return order.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="unitOfWork"/> is <see langword="null"/>.</exception>
    public async Task<(TResult1?, TResult2?, TResult3?, TResult4?, TResult5?, TResult6?, TResult7?, TResult8?, TResult9?, TResult10?, TResult11?, TResult12?, TResult13?, TResult14?)> ExecuteAsync(DbConnection connection, TParameters parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11, TResult12, TResult13, TResult14>(connection, GetSqlParameters(parameters), unitOfWork);
    }
}

/// <summary>
/// Defines a stored procedure that maps 15 distinct returned PostgreSQL cursor names to a result tuple.
/// </summary>
/// <typeparam name="TParameters">The application type mapped to command parameters.</typeparam>
/// <typeparam name="TResult1">The value mapped from result set 1.</typeparam>
/// <typeparam name="TResult2">The value mapped from result set 2.</typeparam>
/// <typeparam name="TResult3">The value mapped from result set 3.</typeparam>
/// <typeparam name="TResult4">The value mapped from result set 4.</typeparam>
/// <typeparam name="TResult5">The value mapped from result set 5.</typeparam>
/// <typeparam name="TResult6">The value mapped from result set 6.</typeparam>
/// <typeparam name="TResult7">The value mapped from result set 7.</typeparam>
/// <typeparam name="TResult8">The value mapped from result set 8.</typeparam>
/// <typeparam name="TResult9">The value mapped from result set 9.</typeparam>
/// <typeparam name="TResult10">The value mapped from result set 10.</typeparam>
/// <typeparam name="TResult11">The value mapped from result set 11.</typeparam>
/// <typeparam name="TResult12">The value mapped from result set 12.</typeparam>
/// <typeparam name="TResult13">The value mapped from result set 13.</typeparam>
/// <typeparam name="TResult14">The value mapped from result set 14.</typeparam>
/// <typeparam name="TResult15">The value mapped from result set 15.</typeparam>
/// <remarks>
/// Tuple positions follow the implementation-defined enumeration of distinct cursor names and do not
/// promise database return order. The procedure must return the declared number of distinct cursor names.
/// </remarks>
public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11, TResult12, TResult13, TResult14, TResult15> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure and maps 15 distinct returned PostgreSQL cursor names to tuple elements.
    /// </summary>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The application parameters to map to PostgreSQL parameters.</param>
    /// <param name="unitOfWork">The required unit of work used to create or reuse the transaction that owns the returned cursors.</param>
    /// <returns>A tuple whose positions follow the implementation-defined enumeration of distinct cursor names. Empty cursors produce null reference values or default value-type values.</returns>
    /// <remarks>
    /// PostgreSQL cursors require a transaction. If the unit of work has no active transaction,
    /// execution creates a transaction for the cursor fetches and disposes it when mapping completes.
    /// The procedure must return 15 distinct cursor names; tuple positions do not promise database return order.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="unitOfWork"/> is <see langword="null"/>.</exception>
    public async Task<(TResult1?, TResult2?, TResult3?, TResult4?, TResult5?, TResult6?, TResult7?, TResult8?, TResult9?, TResult10?, TResult11?, TResult12?, TResult13?, TResult14?, TResult15?)> ExecuteAsync(DbConnection connection, TParameters parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11, TResult12, TResult13, TResult14, TResult15>(connection, GetSqlParameters(parameters), unitOfWork);
    }
}

/// <summary>
/// Defines a stored procedure that maps 16 distinct returned PostgreSQL cursor names to a result tuple.
/// </summary>
/// <typeparam name="TParameters">The application type mapped to command parameters.</typeparam>
/// <typeparam name="TResult1">The value mapped from result set 1.</typeparam>
/// <typeparam name="TResult2">The value mapped from result set 2.</typeparam>
/// <typeparam name="TResult3">The value mapped from result set 3.</typeparam>
/// <typeparam name="TResult4">The value mapped from result set 4.</typeparam>
/// <typeparam name="TResult5">The value mapped from result set 5.</typeparam>
/// <typeparam name="TResult6">The value mapped from result set 6.</typeparam>
/// <typeparam name="TResult7">The value mapped from result set 7.</typeparam>
/// <typeparam name="TResult8">The value mapped from result set 8.</typeparam>
/// <typeparam name="TResult9">The value mapped from result set 9.</typeparam>
/// <typeparam name="TResult10">The value mapped from result set 10.</typeparam>
/// <typeparam name="TResult11">The value mapped from result set 11.</typeparam>
/// <typeparam name="TResult12">The value mapped from result set 12.</typeparam>
/// <typeparam name="TResult13">The value mapped from result set 13.</typeparam>
/// <typeparam name="TResult14">The value mapped from result set 14.</typeparam>
/// <typeparam name="TResult15">The value mapped from result set 15.</typeparam>
/// <typeparam name="TResult16">The value mapped from result set 16.</typeparam>
/// <remarks>
/// Tuple positions follow the implementation-defined enumeration of distinct cursor names and do not
/// promise database return order. The procedure must return the declared number of distinct cursor names.
/// </remarks>
public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11, TResult12, TResult13, TResult14, TResult15, TResult16> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure and maps 16 distinct returned PostgreSQL cursor names to tuple elements.
    /// </summary>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The application parameters to map to PostgreSQL parameters.</param>
    /// <param name="unitOfWork">The required unit of work used to create or reuse the transaction that owns the returned cursors.</param>
    /// <returns>A tuple whose positions follow the implementation-defined enumeration of distinct cursor names. Empty cursors produce null reference values or default value-type values.</returns>
    /// <remarks>
    /// PostgreSQL cursors require a transaction. If the unit of work has no active transaction,
    /// execution creates a transaction for the cursor fetches and disposes it when mapping completes.
    /// The procedure must return 16 distinct cursor names; tuple positions do not promise database return order.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="unitOfWork"/> is <see langword="null"/>.</exception>
    public async Task<(TResult1?, TResult2?, TResult3?, TResult4?, TResult5?, TResult6?, TResult7?, TResult8?, TResult9?, TResult10?, TResult11?, TResult12?, TResult13?, TResult14?, TResult15?, TResult16?)> ExecuteAsync(DbConnection connection, TParameters parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11, TResult12, TResult13, TResult14, TResult15, TResult16>(connection, GetSqlParameters(parameters), unitOfWork);
    }
}
