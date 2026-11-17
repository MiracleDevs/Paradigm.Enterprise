using Paradigm.Enterprise.Domain.Uow;
using System.Data.Common;

namespace Paradigm.Enterprise.Data.MySql.StoredProcedures;

/// <summary>Defines a MySQL stored procedure returning one mapped result set.</summary>
/// <typeparam name="TParameters">The registered application parameter type.</typeparam>
/// <typeparam name="TResult1">The value mapped from result set 1.</typeparam>
/// <remarks>Register object and list-element reader mappers. Empty scalar/object results yield default; supported lists yield empty lists.</remarks>
public abstract class ResultStoredProcedureBase<TParameters, TResult1> : StoredProcedureBase
{
    #region Public Methods

    /// <summary>Executes the procedure and maps its first result set.</summary>
    /// <param name="connection">A caller-owned Oracle MySQL connection, never disposed by this operation.</param>
    /// <param name="parameters">The application parameter object, or null.</param>
    /// <param name="unitOfWork">The optional unit of work owning the active transaction.</param>
    /// <returns>The mapped value or ordered tuple of mapped values.</returns>
    /// <remarks>Nontransactional connections close after execution success or failure; active transaction connections remain open.</remarks>
    public Task<TResult1?> ExecuteAsync(DbConnection connection, TParameters? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync<TResult1>(connection, GetMySqlParameters(parameters), unitOfWork);

    #endregion
}

/// <summary>Defines a MySQL stored procedure returning 2 sequential result sets.</summary>
/// <typeparam name="TParameters">The registered application parameter type.</typeparam>
/// <typeparam name="TResult1">The value mapped from result set 1.</typeparam>
/// <typeparam name="TResult2">The value mapped from result set 2.</typeparam>
/// <remarks>Register object and list-element reader mappers. Empty scalar/object results yield default; supported lists yield empty lists.</remarks>
public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2> : StoredProcedureBase
{
    #region Public Methods

    /// <summary>Executes the procedure and maps 2 results in their database order.</summary>
    /// <param name="connection">A caller-owned Oracle MySQL connection, never disposed by this operation.</param>
    /// <param name="parameters">The application parameter object, or null.</param>
    /// <param name="unitOfWork">The optional unit of work owning the active transaction.</param>
    /// <returns>The mapped value or ordered tuple of mapped values.</returns>
    /// <remarks>Nontransactional connections close after execution success or failure; active transaction connections remain open.</remarks>
    public Task<(TResult1?, TResult2?)> ExecuteAsync(DbConnection connection, TParameters? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync<TResult1, TResult2>(connection, GetMySqlParameters(parameters), unitOfWork);

    #endregion
}

/// <summary>Defines a MySQL stored procedure returning 3 sequential result sets.</summary>
/// <typeparam name="TParameters">The registered application parameter type.</typeparam>
/// <typeparam name="TResult1">The value mapped from result set 1.</typeparam>
/// <typeparam name="TResult2">The value mapped from result set 2.</typeparam>
/// <typeparam name="TResult3">The value mapped from result set 3.</typeparam>
/// <remarks>Register object and list-element reader mappers. Empty scalar/object results yield default; supported lists yield empty lists.</remarks>
public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3> : StoredProcedureBase
{
    #region Public Methods

    /// <summary>Executes the procedure and maps 3 results in their database order.</summary>
    /// <param name="connection">A caller-owned Oracle MySQL connection, never disposed by this operation.</param>
    /// <param name="parameters">The application parameter object, or null.</param>
    /// <param name="unitOfWork">The optional unit of work owning the active transaction.</param>
    /// <returns>The mapped value or ordered tuple of mapped values.</returns>
    /// <remarks>Nontransactional connections close after execution success or failure; active transaction connections remain open.</remarks>
    public Task<(TResult1?, TResult2?, TResult3?)> ExecuteAsync(DbConnection connection, TParameters? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync<TResult1, TResult2, TResult3>(connection, GetMySqlParameters(parameters), unitOfWork);

    #endregion
}

/// <summary>Defines a MySQL stored procedure returning 4 sequential result sets.</summary>
/// <typeparam name="TParameters">The registered application parameter type.</typeparam>
/// <typeparam name="TResult1">The value mapped from result set 1.</typeparam>
/// <typeparam name="TResult2">The value mapped from result set 2.</typeparam>
/// <typeparam name="TResult3">The value mapped from result set 3.</typeparam>
/// <typeparam name="TResult4">The value mapped from result set 4.</typeparam>
/// <remarks>Register object and list-element reader mappers. Empty scalar/object results yield default; supported lists yield empty lists.</remarks>
public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4> : StoredProcedureBase
{
    #region Public Methods

    /// <summary>Executes the procedure and maps 4 results in their database order.</summary>
    /// <param name="connection">A caller-owned Oracle MySQL connection, never disposed by this operation.</param>
    /// <param name="parameters">The application parameter object, or null.</param>
    /// <param name="unitOfWork">The optional unit of work owning the active transaction.</param>
    /// <returns>The mapped value or ordered tuple of mapped values.</returns>
    /// <remarks>Nontransactional connections close after execution success or failure; active transaction connections remain open.</remarks>
    public Task<(TResult1?, TResult2?, TResult3?, TResult4?)> ExecuteAsync(DbConnection connection, TParameters? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync<TResult1, TResult2, TResult3, TResult4>(connection, GetMySqlParameters(parameters), unitOfWork);

    #endregion
}

/// <summary>Defines a MySQL stored procedure returning 5 sequential result sets.</summary>
/// <typeparam name="TParameters">The registered application parameter type.</typeparam>
/// <typeparam name="TResult1">The value mapped from result set 1.</typeparam>
/// <typeparam name="TResult2">The value mapped from result set 2.</typeparam>
/// <typeparam name="TResult3">The value mapped from result set 3.</typeparam>
/// <typeparam name="TResult4">The value mapped from result set 4.</typeparam>
/// <typeparam name="TResult5">The value mapped from result set 5.</typeparam>
/// <remarks>Register object and list-element reader mappers. Empty scalar/object results yield default; supported lists yield empty lists.</remarks>
public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4, TResult5> : StoredProcedureBase
{
    #region Public Methods

    /// <summary>Executes the procedure and maps 5 results in their database order.</summary>
    /// <param name="connection">A caller-owned Oracle MySQL connection, never disposed by this operation.</param>
    /// <param name="parameters">The application parameter object, or null.</param>
    /// <param name="unitOfWork">The optional unit of work owning the active transaction.</param>
    /// <returns>The mapped value or ordered tuple of mapped values.</returns>
    /// <remarks>Nontransactional connections close after execution success or failure; active transaction connections remain open.</remarks>
    public Task<(TResult1?, TResult2?, TResult3?, TResult4?, TResult5?)> ExecuteAsync(DbConnection connection, TParameters? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync<TResult1, TResult2, TResult3, TResult4, TResult5>(connection, GetMySqlParameters(parameters), unitOfWork);

    #endregion
}

/// <summary>Defines a MySQL stored procedure returning 6 sequential result sets.</summary>
/// <typeparam name="TParameters">The registered application parameter type.</typeparam>
/// <typeparam name="TResult1">The value mapped from result set 1.</typeparam>
/// <typeparam name="TResult2">The value mapped from result set 2.</typeparam>
/// <typeparam name="TResult3">The value mapped from result set 3.</typeparam>
/// <typeparam name="TResult4">The value mapped from result set 4.</typeparam>
/// <typeparam name="TResult5">The value mapped from result set 5.</typeparam>
/// <typeparam name="TResult6">The value mapped from result set 6.</typeparam>
/// <remarks>Register object and list-element reader mappers. Empty scalar/object results yield default; supported lists yield empty lists.</remarks>
public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6> : StoredProcedureBase
{
    #region Public Methods

    /// <summary>Executes the procedure and maps 6 results in their database order.</summary>
    /// <param name="connection">A caller-owned Oracle MySQL connection, never disposed by this operation.</param>
    /// <param name="parameters">The application parameter object, or null.</param>
    /// <param name="unitOfWork">The optional unit of work owning the active transaction.</param>
    /// <returns>The mapped value or ordered tuple of mapped values.</returns>
    /// <remarks>Nontransactional connections close after execution success or failure; active transaction connections remain open.</remarks>
    public Task<(TResult1?, TResult2?, TResult3?, TResult4?, TResult5?, TResult6?)> ExecuteAsync(DbConnection connection, TParameters? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6>(connection, GetMySqlParameters(parameters), unitOfWork);

    #endregion
}

/// <summary>Defines a MySQL stored procedure returning 7 sequential result sets.</summary>
/// <typeparam name="TParameters">The registered application parameter type.</typeparam>
/// <typeparam name="TResult1">The value mapped from result set 1.</typeparam>
/// <typeparam name="TResult2">The value mapped from result set 2.</typeparam>
/// <typeparam name="TResult3">The value mapped from result set 3.</typeparam>
/// <typeparam name="TResult4">The value mapped from result set 4.</typeparam>
/// <typeparam name="TResult5">The value mapped from result set 5.</typeparam>
/// <typeparam name="TResult6">The value mapped from result set 6.</typeparam>
/// <typeparam name="TResult7">The value mapped from result set 7.</typeparam>
/// <remarks>Register object and list-element reader mappers. Empty scalar/object results yield default; supported lists yield empty lists.</remarks>
public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7> : StoredProcedureBase
{
    #region Public Methods

    /// <summary>Executes the procedure and maps 7 results in their database order.</summary>
    /// <param name="connection">A caller-owned Oracle MySQL connection, never disposed by this operation.</param>
    /// <param name="parameters">The application parameter object, or null.</param>
    /// <param name="unitOfWork">The optional unit of work owning the active transaction.</param>
    /// <returns>The mapped value or ordered tuple of mapped values.</returns>
    /// <remarks>Nontransactional connections close after execution success or failure; active transaction connections remain open.</remarks>
    public Task<(TResult1?, TResult2?, TResult3?, TResult4?, TResult5?, TResult6?, TResult7?)> ExecuteAsync(DbConnection connection, TParameters? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7>(connection, GetMySqlParameters(parameters), unitOfWork);

    #endregion
}

/// <summary>Defines a MySQL stored procedure returning 8 sequential result sets.</summary>
/// <typeparam name="TParameters">The registered application parameter type.</typeparam>
/// <typeparam name="TResult1">The value mapped from result set 1.</typeparam>
/// <typeparam name="TResult2">The value mapped from result set 2.</typeparam>
/// <typeparam name="TResult3">The value mapped from result set 3.</typeparam>
/// <typeparam name="TResult4">The value mapped from result set 4.</typeparam>
/// <typeparam name="TResult5">The value mapped from result set 5.</typeparam>
/// <typeparam name="TResult6">The value mapped from result set 6.</typeparam>
/// <typeparam name="TResult7">The value mapped from result set 7.</typeparam>
/// <typeparam name="TResult8">The value mapped from result set 8.</typeparam>
/// <remarks>Register object and list-element reader mappers. Empty scalar/object results yield default; supported lists yield empty lists.</remarks>
public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8> : StoredProcedureBase
{
    #region Public Methods

    /// <summary>Executes the procedure and maps 8 results in their database order.</summary>
    /// <param name="connection">A caller-owned Oracle MySQL connection, never disposed by this operation.</param>
    /// <param name="parameters">The application parameter object, or null.</param>
    /// <param name="unitOfWork">The optional unit of work owning the active transaction.</param>
    /// <returns>The mapped value or ordered tuple of mapped values.</returns>
    /// <remarks>Nontransactional connections close after execution success or failure; active transaction connections remain open.</remarks>
    public Task<(TResult1?, TResult2?, TResult3?, TResult4?, TResult5?, TResult6?, TResult7?, TResult8?)> ExecuteAsync(DbConnection connection, TParameters? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8>(connection, GetMySqlParameters(parameters), unitOfWork);

    #endregion
}

/// <summary>Defines a MySQL stored procedure returning 9 sequential result sets.</summary>
/// <typeparam name="TParameters">The registered application parameter type.</typeparam>
/// <typeparam name="TResult1">The value mapped from result set 1.</typeparam>
/// <typeparam name="TResult2">The value mapped from result set 2.</typeparam>
/// <typeparam name="TResult3">The value mapped from result set 3.</typeparam>
/// <typeparam name="TResult4">The value mapped from result set 4.</typeparam>
/// <typeparam name="TResult5">The value mapped from result set 5.</typeparam>
/// <typeparam name="TResult6">The value mapped from result set 6.</typeparam>
/// <typeparam name="TResult7">The value mapped from result set 7.</typeparam>
/// <typeparam name="TResult8">The value mapped from result set 8.</typeparam>
/// <typeparam name="TResult9">The value mapped from result set 9.</typeparam>
/// <remarks>Register object and list-element reader mappers. Empty scalar/object results yield default; supported lists yield empty lists.</remarks>
public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9> : StoredProcedureBase
{
    #region Public Methods

    /// <summary>Executes the procedure and maps 9 results in their database order.</summary>
    /// <param name="connection">A caller-owned Oracle MySQL connection, never disposed by this operation.</param>
    /// <param name="parameters">The application parameter object, or null.</param>
    /// <param name="unitOfWork">The optional unit of work owning the active transaction.</param>
    /// <returns>The mapped value or ordered tuple of mapped values.</returns>
    /// <remarks>Nontransactional connections close after execution success or failure; active transaction connections remain open.</remarks>
    public Task<(TResult1?, TResult2?, TResult3?, TResult4?, TResult5?, TResult6?, TResult7?, TResult8?, TResult9?)> ExecuteAsync(DbConnection connection, TParameters? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9>(connection, GetMySqlParameters(parameters), unitOfWork);

    #endregion
}

/// <summary>Defines a MySQL stored procedure returning 10 sequential result sets.</summary>
/// <typeparam name="TParameters">The registered application parameter type.</typeparam>
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
/// <remarks>Register object and list-element reader mappers. Empty scalar/object results yield default; supported lists yield empty lists.</remarks>
public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10> : StoredProcedureBase
{
    #region Public Methods

    /// <summary>Executes the procedure and maps 10 results in their database order.</summary>
    /// <param name="connection">A caller-owned Oracle MySQL connection, never disposed by this operation.</param>
    /// <param name="parameters">The application parameter object, or null.</param>
    /// <param name="unitOfWork">The optional unit of work owning the active transaction.</param>
    /// <returns>The mapped value or ordered tuple of mapped values.</returns>
    /// <remarks>Nontransactional connections close after execution success or failure; active transaction connections remain open.</remarks>
    public Task<(TResult1?, TResult2?, TResult3?, TResult4?, TResult5?, TResult6?, TResult7?, TResult8?, TResult9?, TResult10?)> ExecuteAsync(DbConnection connection, TParameters? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10>(connection, GetMySqlParameters(parameters), unitOfWork);

    #endregion
}

/// <summary>Defines a MySQL stored procedure returning 11 sequential result sets.</summary>
/// <typeparam name="TParameters">The registered application parameter type.</typeparam>
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
/// <remarks>Register object and list-element reader mappers. Empty scalar/object results yield default; supported lists yield empty lists.</remarks>
public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11> : StoredProcedureBase
{
    #region Public Methods

    /// <summary>Executes the procedure and maps 11 results in their database order.</summary>
    /// <param name="connection">A caller-owned Oracle MySQL connection, never disposed by this operation.</param>
    /// <param name="parameters">The application parameter object, or null.</param>
    /// <param name="unitOfWork">The optional unit of work owning the active transaction.</param>
    /// <returns>The mapped value or ordered tuple of mapped values.</returns>
    /// <remarks>Nontransactional connections close after execution success or failure; active transaction connections remain open.</remarks>
    public Task<(TResult1?, TResult2?, TResult3?, TResult4?, TResult5?, TResult6?, TResult7?, TResult8?, TResult9?, TResult10?, TResult11?)> ExecuteAsync(DbConnection connection, TParameters? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11>(connection, GetMySqlParameters(parameters), unitOfWork);

    #endregion
}

/// <summary>Defines a MySQL stored procedure returning 12 sequential result sets.</summary>
/// <typeparam name="TParameters">The registered application parameter type.</typeparam>
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
/// <remarks>Register object and list-element reader mappers. Empty scalar/object results yield default; supported lists yield empty lists.</remarks>
public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11, TResult12> : StoredProcedureBase
{
    #region Public Methods

    /// <summary>Executes the procedure and maps 12 results in their database order.</summary>
    /// <param name="connection">A caller-owned Oracle MySQL connection, never disposed by this operation.</param>
    /// <param name="parameters">The application parameter object, or null.</param>
    /// <param name="unitOfWork">The optional unit of work owning the active transaction.</param>
    /// <returns>The mapped value or ordered tuple of mapped values.</returns>
    /// <remarks>Nontransactional connections close after execution success or failure; active transaction connections remain open.</remarks>
    public Task<(TResult1?, TResult2?, TResult3?, TResult4?, TResult5?, TResult6?, TResult7?, TResult8?, TResult9?, TResult10?, TResult11?, TResult12?)> ExecuteAsync(DbConnection connection, TParameters? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11, TResult12>(connection, GetMySqlParameters(parameters), unitOfWork);

    #endregion
}

/// <summary>Defines a MySQL stored procedure returning 13 sequential result sets.</summary>
/// <typeparam name="TParameters">The registered application parameter type.</typeparam>
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
/// <remarks>Register object and list-element reader mappers. Empty scalar/object results yield default; supported lists yield empty lists.</remarks>
public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11, TResult12, TResult13> : StoredProcedureBase
{
    #region Public Methods

    /// <summary>Executes the procedure and maps 13 results in their database order.</summary>
    /// <param name="connection">A caller-owned Oracle MySQL connection, never disposed by this operation.</param>
    /// <param name="parameters">The application parameter object, or null.</param>
    /// <param name="unitOfWork">The optional unit of work owning the active transaction.</param>
    /// <returns>The mapped value or ordered tuple of mapped values.</returns>
    /// <remarks>Nontransactional connections close after execution success or failure; active transaction connections remain open.</remarks>
    public Task<(TResult1?, TResult2?, TResult3?, TResult4?, TResult5?, TResult6?, TResult7?, TResult8?, TResult9?, TResult10?, TResult11?, TResult12?, TResult13?)> ExecuteAsync(DbConnection connection, TParameters? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11, TResult12, TResult13>(connection, GetMySqlParameters(parameters), unitOfWork);

    #endregion
}

/// <summary>Defines a MySQL stored procedure returning 14 sequential result sets.</summary>
/// <typeparam name="TParameters">The registered application parameter type.</typeparam>
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
/// <remarks>Register object and list-element reader mappers. Empty scalar/object results yield default; supported lists yield empty lists.</remarks>
public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11, TResult12, TResult13, TResult14> : StoredProcedureBase
{
    #region Public Methods

    /// <summary>Executes the procedure and maps 14 results in their database order.</summary>
    /// <param name="connection">A caller-owned Oracle MySQL connection, never disposed by this operation.</param>
    /// <param name="parameters">The application parameter object, or null.</param>
    /// <param name="unitOfWork">The optional unit of work owning the active transaction.</param>
    /// <returns>The mapped value or ordered tuple of mapped values.</returns>
    /// <remarks>Nontransactional connections close after execution success or failure; active transaction connections remain open.</remarks>
    public Task<(TResult1?, TResult2?, TResult3?, TResult4?, TResult5?, TResult6?, TResult7?, TResult8?, TResult9?, TResult10?, TResult11?, TResult12?, TResult13?, TResult14?)> ExecuteAsync(DbConnection connection, TParameters? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11, TResult12, TResult13, TResult14>(connection, GetMySqlParameters(parameters), unitOfWork);

    #endregion
}

/// <summary>Defines a MySQL stored procedure returning 15 sequential result sets.</summary>
/// <typeparam name="TParameters">The registered application parameter type.</typeparam>
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
/// <remarks>Register object and list-element reader mappers. Empty scalar/object results yield default; supported lists yield empty lists.</remarks>
public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11, TResult12, TResult13, TResult14, TResult15> : StoredProcedureBase
{
    #region Public Methods

    /// <summary>Executes the procedure and maps 15 results in their database order.</summary>
    /// <param name="connection">A caller-owned Oracle MySQL connection, never disposed by this operation.</param>
    /// <param name="parameters">The application parameter object, or null.</param>
    /// <param name="unitOfWork">The optional unit of work owning the active transaction.</param>
    /// <returns>The mapped value or ordered tuple of mapped values.</returns>
    /// <remarks>Nontransactional connections close after execution success or failure; active transaction connections remain open.</remarks>
    public Task<(TResult1?, TResult2?, TResult3?, TResult4?, TResult5?, TResult6?, TResult7?, TResult8?, TResult9?, TResult10?, TResult11?, TResult12?, TResult13?, TResult14?, TResult15?)> ExecuteAsync(DbConnection connection, TParameters? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11, TResult12, TResult13, TResult14, TResult15>(connection, GetMySqlParameters(parameters), unitOfWork);

    #endregion
}

/// <summary>Defines a MySQL stored procedure returning 16 sequential result sets.</summary>
/// <typeparam name="TParameters">The registered application parameter type.</typeparam>
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
/// <remarks>Register object and list-element reader mappers. Empty scalar/object results yield default; supported lists yield empty lists.</remarks>
public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11, TResult12, TResult13, TResult14, TResult15, TResult16> : StoredProcedureBase
{
    #region Public Methods

    /// <summary>Executes the procedure and maps 16 results in their database order.</summary>
    /// <param name="connection">A caller-owned Oracle MySQL connection, never disposed by this operation.</param>
    /// <param name="parameters">The application parameter object, or null.</param>
    /// <param name="unitOfWork">The optional unit of work owning the active transaction.</param>
    /// <returns>The mapped value or ordered tuple of mapped values.</returns>
    /// <remarks>Nontransactional connections close after execution success or failure; active transaction connections remain open.</remarks>
    public Task<(TResult1?, TResult2?, TResult3?, TResult4?, TResult5?, TResult6?, TResult7?, TResult8?, TResult9?, TResult10?, TResult11?, TResult12?, TResult13?, TResult14?, TResult15?, TResult16?)> ExecuteAsync(DbConnection connection, TParameters? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11, TResult12, TResult13, TResult14, TResult15, TResult16>(connection, GetMySqlParameters(parameters), unitOfWork);

    #endregion
}