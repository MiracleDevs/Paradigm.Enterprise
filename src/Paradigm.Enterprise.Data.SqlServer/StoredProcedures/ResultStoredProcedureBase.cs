using Paradigm.Enterprise.Domain.Uow;
using System.Data.Common;

namespace Paradigm.Enterprise.Data.SqlServer.StoredProcedures;

/// <summary>
/// Defines a stored procedure that maps its first result set to a single result value.
/// </summary>
/// <typeparam name="TParameters">The application type mapped to command parameters.</typeparam>
/// <typeparam name="TResult">The value mapped from result set 1.</typeparam>
/// <example>
/// Derive a procedure for the database contract, then handle an empty result explicitly:
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
///     protected override string StoredProcedureName =&gt; "dbo.FindOrder";
/// }
/// </code>
/// Register both the <c>FindOrderParameters</c> parameter mapper and the
/// <c>OrderView</c> data-reader mapper before calling the procedure.
/// </example>
public abstract class ResultStoredProcedureBase<TParameters, TResult> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure and maps its first result set.
    /// </summary>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The application parameters to map to provider parameters.</param>
    /// <param name="unitOfWork">The optional unit of work whose active transaction is attached to the command.</param>
    /// <returns>The mapped value. An empty scalar or mapped-object result returns <see langword="null"/> for a reference type or the default for a value type; a concrete <c>IList</c> result returns an empty list.</returns>
    /// <remarks>
    /// On success without an active unit-of-work transaction, execution closes the connection even if
    /// it was already open on entry. With an active transaction, the connection remains open. An
    /// execution or mapping failure can leave the caller-owned connection open.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// A non-null parameter object has no registered mapper, or a non-primitive result type or list
    /// element type has no registered data-reader mapper.
    /// </exception>
    public async Task<TResult?> ExecuteAsync(DbConnection connection, TParameters? parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync<TResult>(connection, GetSqlParameters(parameters), unitOfWork);
    }
}

/// <summary>
/// Defines a stored procedure that maps 2 result sets to an ordered result tuple.
/// </summary>
/// <typeparam name="TParameters">The application type mapped to command parameters.</typeparam>
/// <typeparam name="TResult1">The value mapped from result set 1.</typeparam>
/// <typeparam name="TResult2">The value mapped from result set 2.</typeparam>
/// <remarks>
/// Result sets are consumed sequentially. The first tuple element is mapped from the first result set
/// and the second element from the second result set. An element is <see langword="null"/> for an empty
/// reference-type scalar or mapped-object result; an empty value-type scalar or mapped-object result yields its default value. A concrete <c>IList</c> result yields an empty list.
/// </remarks>
/// <example>
/// The tuple annotations make empty result sets explicit to callers:
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
///     // Result sets are consumed in their database order.
/// }
/// </code>
/// Register the <c>LookupParameters</c> parameter mapper and data-reader mappers for
/// <c>Customer</c> and <c>Address</c> before execution.
/// </example>
public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure and maps 2 result sets in order.
    /// </summary>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The application parameters to map to provider parameters.</param>
    /// <param name="unitOfWork">The optional unit of work whose active transaction is attached to the command.</param>
    /// <returns>An ordered tuple containing one mapped value per result set. Empty scalar or mapped-object result sets produce null reference values or default value-type values; concrete <c>IList</c> result types produce empty lists.</returns>
    /// <remarks>
    /// On success without an active unit-of-work transaction, execution closes the connection even if
    /// it was already open on entry. With an active transaction, the connection remains open. An
    /// execution or mapping failure can leave the caller-owned connection open.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// A non-null parameter object has no registered mapper, or a non-primitive result type or list
    /// element type has no registered data-reader mapper.
    /// </exception>
    public async Task<(TResult1?, TResult2?)> ExecuteAsync(DbConnection connection, TParameters? parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync<TResult1, TResult2>(connection, GetSqlParameters(parameters), unitOfWork);
    }
}

/// <summary>
/// Defines a stored procedure that maps 3 result sets to an ordered result tuple.
/// </summary>
/// <typeparam name="TParameters">The application type mapped to command parameters.</typeparam>
/// <typeparam name="TResult1">The value mapped from result set 1.</typeparam>
/// <typeparam name="TResult2">The value mapped from result set 2.</typeparam>
/// <typeparam name="TResult3">The value mapped from result set 3.</typeparam>
/// <remarks>
/// Result tuple elements correspond to result sets in declaration order. An element is
/// <see langword="null"/> for an empty reference-type scalar or mapped-object result; an empty value-type scalar or mapped-object result yields its default value. A concrete <c>IList</c> result yields an empty list.
/// </remarks>
public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure and maps 3 result sets in order.
    /// </summary>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The application parameters to map to provider parameters.</param>
    /// <param name="unitOfWork">The optional unit of work whose active transaction is attached to the command.</param>
    /// <returns>An ordered tuple containing one mapped value per result set. Empty scalar or mapped-object result sets produce null reference values or default value-type values; concrete <c>IList</c> result types produce empty lists.</returns>
    /// <remarks>
    /// On success without an active unit-of-work transaction, execution closes the connection even if
    /// it was already open on entry. With an active transaction, the connection remains open. An
    /// execution or mapping failure can leave the caller-owned connection open.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// A non-null parameter object has no registered mapper, or a non-primitive result type or list
    /// element type has no registered data-reader mapper.
    /// </exception>
    public async Task<(TResult1?, TResult2?, TResult3?)> ExecuteAsync(DbConnection connection, TParameters parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync<TResult1, TResult2, TResult3>(connection, GetSqlParameters(parameters), unitOfWork);
    }
}

/// <summary>
/// Defines a stored procedure that maps 4 result sets to an ordered result tuple.
/// </summary>
/// <typeparam name="TParameters">The application type mapped to command parameters.</typeparam>
/// <typeparam name="TResult1">The value mapped from result set 1.</typeparam>
/// <typeparam name="TResult2">The value mapped from result set 2.</typeparam>
/// <typeparam name="TResult3">The value mapped from result set 3.</typeparam>
/// <typeparam name="TResult4">The value mapped from result set 4.</typeparam>
/// <remarks>
/// Result tuple elements correspond to result sets in declaration order. An element is
/// <see langword="null"/> for an empty reference-type scalar or mapped-object result; an empty value-type scalar or mapped-object result yields its default value. A concrete <c>IList</c> result yields an empty list.
/// </remarks>
public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure and maps 4 result sets in order.
    /// </summary>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The application parameters to map to provider parameters.</param>
    /// <param name="unitOfWork">The optional unit of work whose active transaction is attached to the command.</param>
    /// <returns>An ordered tuple containing one mapped value per result set. Empty scalar or mapped-object result sets produce null reference values or default value-type values; concrete <c>IList</c> result types produce empty lists.</returns>
    /// <remarks>
    /// On success without an active unit-of-work transaction, execution closes the connection even if
    /// it was already open on entry. With an active transaction, the connection remains open. An
    /// execution or mapping failure can leave the caller-owned connection open.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// A non-null parameter object has no registered mapper, or a non-primitive result type or list
    /// element type has no registered data-reader mapper.
    /// </exception>
    public async Task<(TResult1?, TResult2?, TResult3?, TResult4?)> ExecuteAsync(DbConnection connection, TParameters parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync<TResult1, TResult2, TResult3, TResult4>(connection, GetSqlParameters(parameters), unitOfWork);
    }
}

/// <summary>
/// Defines a stored procedure that maps 5 result sets to an ordered result tuple.
/// </summary>
/// <typeparam name="TParameters">The application type mapped to command parameters.</typeparam>
/// <typeparam name="TResult1">The value mapped from result set 1.</typeparam>
/// <typeparam name="TResult2">The value mapped from result set 2.</typeparam>
/// <typeparam name="TResult3">The value mapped from result set 3.</typeparam>
/// <typeparam name="TResult4">The value mapped from result set 4.</typeparam>
/// <typeparam name="TResult5">The value mapped from result set 5.</typeparam>
/// <remarks>
/// Result tuple elements correspond to result sets in declaration order. An element is
/// <see langword="null"/> for an empty reference-type scalar or mapped-object result; an empty value-type scalar or mapped-object result yields its default value. A concrete <c>IList</c> result yields an empty list.
/// </remarks>
public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4, TResult5> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure and maps 5 result sets in order.
    /// </summary>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The application parameters to map to provider parameters.</param>
    /// <param name="unitOfWork">The optional unit of work whose active transaction is attached to the command.</param>
    /// <returns>An ordered tuple containing one mapped value per result set. Empty scalar or mapped-object result sets produce null reference values or default value-type values; concrete <c>IList</c> result types produce empty lists.</returns>
    /// <remarks>
    /// On success without an active unit-of-work transaction, execution closes the connection even if
    /// it was already open on entry. With an active transaction, the connection remains open. An
    /// execution or mapping failure can leave the caller-owned connection open.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// A non-null parameter object has no registered mapper, or a non-primitive result type or list
    /// element type has no registered data-reader mapper.
    /// </exception>
    public async Task<(TResult1?, TResult2?, TResult3?, TResult4?, TResult5?)> ExecuteAsync(DbConnection connection, TParameters parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync<TResult1, TResult2, TResult3, TResult4, TResult5>(connection, GetSqlParameters(parameters), unitOfWork);
    }
}

/// <summary>
/// Defines a stored procedure that maps 6 result sets to an ordered result tuple.
/// </summary>
/// <typeparam name="TParameters">The application type mapped to command parameters.</typeparam>
/// <typeparam name="TResult1">The value mapped from result set 1.</typeparam>
/// <typeparam name="TResult2">The value mapped from result set 2.</typeparam>
/// <typeparam name="TResult3">The value mapped from result set 3.</typeparam>
/// <typeparam name="TResult4">The value mapped from result set 4.</typeparam>
/// <typeparam name="TResult5">The value mapped from result set 5.</typeparam>
/// <typeparam name="TResult6">The value mapped from result set 6.</typeparam>
/// <remarks>
/// Result tuple elements correspond to result sets in declaration order. An element is
/// <see langword="null"/> for an empty reference-type scalar or mapped-object result; an empty value-type scalar or mapped-object result yields its default value. A concrete <c>IList</c> result yields an empty list.
/// </remarks>
public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure and maps 6 result sets in order.
    /// </summary>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The application parameters to map to provider parameters.</param>
    /// <param name="unitOfWork">The optional unit of work whose active transaction is attached to the command.</param>
    /// <returns>An ordered tuple containing one mapped value per result set. Empty scalar or mapped-object result sets produce null reference values or default value-type values; concrete <c>IList</c> result types produce empty lists.</returns>
    /// <remarks>
    /// On success without an active unit-of-work transaction, execution closes the connection even if
    /// it was already open on entry. With an active transaction, the connection remains open. An
    /// execution or mapping failure can leave the caller-owned connection open.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// A non-null parameter object has no registered mapper, or a non-primitive result type or list
    /// element type has no registered data-reader mapper.
    /// </exception>
    public async Task<(TResult1?, TResult2?, TResult3?, TResult4?, TResult5?, TResult6?)> ExecuteAsync(DbConnection connection, TParameters parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6>(connection, GetSqlParameters(parameters), unitOfWork);
    }
}

/// <summary>
/// Defines a stored procedure that maps 7 result sets to an ordered result tuple.
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
/// Result tuple elements correspond to result sets in declaration order. An element is
/// <see langword="null"/> for an empty reference-type scalar or mapped-object result; an empty value-type scalar or mapped-object result yields its default value. A concrete <c>IList</c> result yields an empty list.
/// </remarks>
public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure and maps 7 result sets in order.
    /// </summary>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The application parameters to map to provider parameters.</param>
    /// <param name="unitOfWork">The optional unit of work whose active transaction is attached to the command.</param>
    /// <returns>An ordered tuple containing one mapped value per result set. Empty scalar or mapped-object result sets produce null reference values or default value-type values; concrete <c>IList</c> result types produce empty lists.</returns>
    /// <remarks>
    /// On success without an active unit-of-work transaction, execution closes the connection even if
    /// it was already open on entry. With an active transaction, the connection remains open. An
    /// execution or mapping failure can leave the caller-owned connection open.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// A non-null parameter object has no registered mapper, or a non-primitive result type or list
    /// element type has no registered data-reader mapper.
    /// </exception>
    public async Task<(TResult1?, TResult2?, TResult3?, TResult4?, TResult5?, TResult6?, TResult7?)> ExecuteAsync(DbConnection connection, TParameters parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7>(connection, GetSqlParameters(parameters), unitOfWork);
    }
}

/// <summary>
/// Defines a stored procedure that maps 8 result sets to an ordered result tuple.
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
/// Result tuple elements correspond to result sets in declaration order. An element is
/// <see langword="null"/> for an empty reference-type scalar or mapped-object result; an empty value-type scalar or mapped-object result yields its default value. A concrete <c>IList</c> result yields an empty list.
/// </remarks>
public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure and maps 8 result sets in order.
    /// </summary>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The application parameters to map to provider parameters.</param>
    /// <param name="unitOfWork">The optional unit of work whose active transaction is attached to the command.</param>
    /// <returns>An ordered tuple containing one mapped value per result set. Empty scalar or mapped-object result sets produce null reference values or default value-type values; concrete <c>IList</c> result types produce empty lists.</returns>
    /// <remarks>
    /// On success without an active unit-of-work transaction, execution closes the connection even if
    /// it was already open on entry. With an active transaction, the connection remains open. An
    /// execution or mapping failure can leave the caller-owned connection open.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// A non-null parameter object has no registered mapper, or a non-primitive result type or list
    /// element type has no registered data-reader mapper.
    /// </exception>
    public async Task<(TResult1?, TResult2?, TResult3?, TResult4?, TResult5?, TResult6?, TResult7?, TResult8?)> ExecuteAsync(DbConnection connection, TParameters parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8>(connection, GetSqlParameters(parameters), unitOfWork);
    }
}

/// <summary>
/// Defines a stored procedure that maps 9 result sets to an ordered result tuple.
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
/// Result tuple elements correspond to result sets in declaration order. An element is
/// <see langword="null"/> for an empty reference-type scalar or mapped-object result; an empty value-type scalar or mapped-object result yields its default value. A concrete <c>IList</c> result yields an empty list.
/// </remarks>
public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure and maps 9 result sets in order.
    /// </summary>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The application parameters to map to provider parameters.</param>
    /// <param name="unitOfWork">The optional unit of work whose active transaction is attached to the command.</param>
    /// <returns>An ordered tuple containing one mapped value per result set. Empty scalar or mapped-object result sets produce null reference values or default value-type values; concrete <c>IList</c> result types produce empty lists.</returns>
    /// <remarks>
    /// On success without an active unit-of-work transaction, execution closes the connection even if
    /// it was already open on entry. With an active transaction, the connection remains open. An
    /// execution or mapping failure can leave the caller-owned connection open.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// A non-null parameter object has no registered mapper, or a non-primitive result type or list
    /// element type has no registered data-reader mapper.
    /// </exception>
    public async Task<(TResult1?, TResult2?, TResult3?, TResult4?, TResult5?, TResult6?, TResult7?, TResult8?, TResult9?)> ExecuteAsync(DbConnection connection, TParameters parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9>(connection, GetSqlParameters(parameters), unitOfWork);
    }
}

/// <summary>
/// Defines a stored procedure that maps 10 result sets to an ordered result tuple.
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
/// Result tuple elements correspond to result sets in declaration order. An element is
/// <see langword="null"/> for an empty reference-type scalar or mapped-object result; an empty value-type scalar or mapped-object result yields its default value. A concrete <c>IList</c> result yields an empty list.
/// </remarks>
public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure and maps 10 result sets in order.
    /// </summary>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The application parameters to map to provider parameters.</param>
    /// <param name="unitOfWork">The optional unit of work whose active transaction is attached to the command.</param>
    /// <returns>An ordered tuple containing one mapped value per result set. Empty scalar or mapped-object result sets produce null reference values or default value-type values; concrete <c>IList</c> result types produce empty lists.</returns>
    /// <remarks>
    /// On success without an active unit-of-work transaction, execution closes the connection even if
    /// it was already open on entry. With an active transaction, the connection remains open. An
    /// execution or mapping failure can leave the caller-owned connection open.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// A non-null parameter object has no registered mapper, or a non-primitive result type or list
    /// element type has no registered data-reader mapper.
    /// </exception>
    public async Task<(TResult1?, TResult2?, TResult3?, TResult4?, TResult5?, TResult6?, TResult7?, TResult8?, TResult9?, TResult10?)> ExecuteAsync(DbConnection connection, TParameters parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10>(connection, GetSqlParameters(parameters), unitOfWork);
    }
}

/// <summary>
/// Defines a stored procedure that maps 11 result sets to an ordered result tuple.
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
/// Result tuple elements correspond to result sets in declaration order. An element is
/// <see langword="null"/> for an empty reference-type scalar or mapped-object result; an empty value-type scalar or mapped-object result yields its default value. A concrete <c>IList</c> result yields an empty list.
/// </remarks>
public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure and maps 11 result sets in order.
    /// </summary>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The application parameters to map to provider parameters.</param>
    /// <param name="unitOfWork">The optional unit of work whose active transaction is attached to the command.</param>
    /// <returns>An ordered tuple containing one mapped value per result set. Empty scalar or mapped-object result sets produce null reference values or default value-type values; concrete <c>IList</c> result types produce empty lists.</returns>
    /// <remarks>
    /// On success without an active unit-of-work transaction, execution closes the connection even if
    /// it was already open on entry. With an active transaction, the connection remains open. An
    /// execution or mapping failure can leave the caller-owned connection open.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// A non-null parameter object has no registered mapper, or a non-primitive result type or list
    /// element type has no registered data-reader mapper.
    /// </exception>
    public async Task<(TResult1?, TResult2?, TResult3?, TResult4?, TResult5?, TResult6?, TResult7?, TResult8?, TResult9?, TResult10?, TResult11?)> ExecuteAsync(DbConnection connection, TParameters parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11>(connection, GetSqlParameters(parameters), unitOfWork);
    }
}

/// <summary>
/// Defines a stored procedure that maps 12 result sets to an ordered result tuple.
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
/// Result tuple elements correspond to result sets in declaration order. An element is
/// <see langword="null"/> for an empty reference-type scalar or mapped-object result; an empty value-type scalar or mapped-object result yields its default value. A concrete <c>IList</c> result yields an empty list.
/// </remarks>
public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11, TResult12> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure and maps 12 result sets in order.
    /// </summary>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The application parameters to map to provider parameters.</param>
    /// <param name="unitOfWork">The optional unit of work whose active transaction is attached to the command.</param>
    /// <returns>An ordered tuple containing one mapped value per result set. Empty scalar or mapped-object result sets produce null reference values or default value-type values; concrete <c>IList</c> result types produce empty lists.</returns>
    /// <remarks>
    /// On success without an active unit-of-work transaction, execution closes the connection even if
    /// it was already open on entry. With an active transaction, the connection remains open. An
    /// execution or mapping failure can leave the caller-owned connection open.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// A non-null parameter object has no registered mapper, or a non-primitive result type or list
    /// element type has no registered data-reader mapper.
    /// </exception>
    public async Task<(TResult1?, TResult2?, TResult3?, TResult4?, TResult5?, TResult6?, TResult7?, TResult8?, TResult9?, TResult10?, TResult11?, TResult12?)> ExecuteAsync(DbConnection connection, TParameters parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11, TResult12>(connection, GetSqlParameters(parameters), unitOfWork);
    }
}

/// <summary>
/// Defines a stored procedure that maps 13 result sets to an ordered result tuple.
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
/// Result tuple elements correspond to result sets in declaration order. An element is
/// <see langword="null"/> for an empty reference-type scalar or mapped-object result; an empty value-type scalar or mapped-object result yields its default value. A concrete <c>IList</c> result yields an empty list.
/// </remarks>
public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11, TResult12, TResult13> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure and maps 13 result sets in order.
    /// </summary>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The application parameters to map to provider parameters.</param>
    /// <param name="unitOfWork">The optional unit of work whose active transaction is attached to the command.</param>
    /// <returns>An ordered tuple containing one mapped value per result set. Empty scalar or mapped-object result sets produce null reference values or default value-type values; concrete <c>IList</c> result types produce empty lists.</returns>
    /// <remarks>
    /// On success without an active unit-of-work transaction, execution closes the connection even if
    /// it was already open on entry. With an active transaction, the connection remains open. An
    /// execution or mapping failure can leave the caller-owned connection open.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// A non-null parameter object has no registered mapper, or a non-primitive result type or list
    /// element type has no registered data-reader mapper.
    /// </exception>
    public async Task<(TResult1?, TResult2?, TResult3?, TResult4?, TResult5?, TResult6?, TResult7?, TResult8?, TResult9?, TResult10?, TResult11?, TResult12?, TResult13?)> ExecuteAsync(DbConnection connection, TParameters parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11, TResult12, TResult13>(connection, GetSqlParameters(parameters), unitOfWork);
    }
}

/// <summary>
/// Defines a stored procedure that maps 14 result sets to an ordered result tuple.
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
/// Result tuple elements correspond to result sets in declaration order. An element is
/// <see langword="null"/> for an empty reference-type scalar or mapped-object result; an empty value-type scalar or mapped-object result yields its default value. A concrete <c>IList</c> result yields an empty list.
/// </remarks>
public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11, TResult12, TResult13, TResult14> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure and maps 14 result sets in order.
    /// </summary>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The application parameters to map to provider parameters.</param>
    /// <param name="unitOfWork">The optional unit of work whose active transaction is attached to the command.</param>
    /// <returns>An ordered tuple containing one mapped value per result set. Empty scalar or mapped-object result sets produce null reference values or default value-type values; concrete <c>IList</c> result types produce empty lists.</returns>
    /// <remarks>
    /// On success without an active unit-of-work transaction, execution closes the connection even if
    /// it was already open on entry. With an active transaction, the connection remains open. An
    /// execution or mapping failure can leave the caller-owned connection open.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// A non-null parameter object has no registered mapper, or a non-primitive result type or list
    /// element type has no registered data-reader mapper.
    /// </exception>
    public async Task<(TResult1?, TResult2?, TResult3?, TResult4?, TResult5?, TResult6?, TResult7?, TResult8?, TResult9?, TResult10?, TResult11?, TResult12?, TResult13?, TResult14?)> ExecuteAsync(DbConnection connection, TParameters parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11, TResult12, TResult13, TResult14>(connection, GetSqlParameters(parameters), unitOfWork);
    }
}

/// <summary>
/// Defines a stored procedure that maps 15 result sets to an ordered result tuple.
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
/// Result tuple elements correspond to result sets in declaration order. An element is
/// <see langword="null"/> for an empty reference-type scalar or mapped-object result; an empty value-type scalar or mapped-object result yields its default value. A concrete <c>IList</c> result yields an empty list.
/// </remarks>
public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11, TResult12, TResult13, TResult14, TResult15> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure and maps 15 result sets in order.
    /// </summary>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The application parameters to map to provider parameters.</param>
    /// <param name="unitOfWork">The optional unit of work whose active transaction is attached to the command.</param>
    /// <returns>An ordered tuple containing one mapped value per result set. Empty scalar or mapped-object result sets produce null reference values or default value-type values; concrete <c>IList</c> result types produce empty lists.</returns>
    /// <remarks>
    /// On success without an active unit-of-work transaction, execution closes the connection even if
    /// it was already open on entry. With an active transaction, the connection remains open. An
    /// execution or mapping failure can leave the caller-owned connection open.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// A non-null parameter object has no registered mapper, or a non-primitive result type or list
    /// element type has no registered data-reader mapper.
    /// </exception>
    public async Task<(TResult1?, TResult2?, TResult3?, TResult4?, TResult5?, TResult6?, TResult7?, TResult8?, TResult9?, TResult10?, TResult11?, TResult12?, TResult13?, TResult14?, TResult15?)> ExecuteAsync(DbConnection connection, TParameters parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11, TResult12, TResult13, TResult14, TResult15>(connection, GetSqlParameters(parameters), unitOfWork);
    }
}

/// <summary>
/// Defines a stored procedure that maps 16 result sets to an ordered result tuple.
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
/// Result tuple elements correspond to result sets in declaration order. An element is
/// <see langword="null"/> for an empty reference-type scalar or mapped-object result; an empty value-type scalar or mapped-object result yields its default value. A concrete <c>IList</c> result yields an empty list.
/// </remarks>
public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11, TResult12, TResult13, TResult14, TResult15, TResult16> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure and maps 16 result sets in order.
    /// </summary>
    /// <param name="connection">The caller-supplied database connection. The connection is not disposed.</param>
    /// <param name="parameters">The application parameters to map to provider parameters.</param>
    /// <param name="unitOfWork">The optional unit of work whose active transaction is attached to the command.</param>
    /// <returns>An ordered tuple containing one mapped value per result set. Empty scalar or mapped-object result sets produce null reference values or default value-type values; concrete <c>IList</c> result types produce empty lists.</returns>
    /// <remarks>
    /// On success without an active unit-of-work transaction, execution closes the connection even if
    /// it was already open on entry. With an active transaction, the connection remains open. An
    /// execution or mapping failure can leave the caller-owned connection open.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// A non-null parameter object has no registered mapper, or a non-primitive result type or list
    /// element type has no registered data-reader mapper.
    /// </exception>
    public async Task<(TResult1?, TResult2?, TResult3?, TResult4?, TResult5?, TResult6?, TResult7?, TResult8?, TResult9?, TResult10?, TResult11?, TResult12?, TResult13?, TResult14?, TResult15?, TResult16?)> ExecuteAsync(DbConnection connection, TParameters parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11, TResult12, TResult13, TResult14, TResult15, TResult16>(connection, GetSqlParameters(parameters), unitOfWork);
    }
}
