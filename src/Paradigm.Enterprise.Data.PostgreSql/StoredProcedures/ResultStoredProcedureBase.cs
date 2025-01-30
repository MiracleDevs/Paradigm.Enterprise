using Paradigm.Enterprise.Domain.Uow;
using System.Data.Common;

namespace Paradigm.Enterprise.Data.PostgreSql.StoredProcedures;

public abstract class ResultStoredProcedureBase<TParameters, TResult> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="parameters">The parameters.</param>
    /// <returns></returns>
    public async Task<TResult?> ExecuteAsync(DbConnection connection, TParameters? parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync<TResult>(connection, GetSqlParameters(parameters), unitOfWork);
    }
}

public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure.
    /// </summary>
    /// <param name="parameters">The parameters.</param>
    /// <returns></returns>
    public async Task<(TResult1, TResult2)> ExecuteAsync(DbConnection connection, TParameters? parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync<TResult1, TResult2>(connection, GetSqlParameters(parameters), unitOfWork);
    }
}

public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure.
    /// </summary>
    /// <param name="parameters">The parameters.</param>
    /// <returns></returns>
    public async Task<(TResult1, TResult2, TResult3)> ExecuteAsync(DbConnection connection, TParameters parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync<TResult1, TResult2, TResult3>(connection, GetSqlParameters(parameters), unitOfWork);
    }
}

public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure.
    /// </summary>
    /// <param name="parameters">The parameters.</param>
    /// <returns></returns>
    public async Task<(TResult1, TResult2, TResult3, TResult4)> ExecuteAsync(DbConnection connection, TParameters parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync<TResult1, TResult2, TResult3, TResult4>(connection, GetSqlParameters(parameters), unitOfWork);
    }
}

public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4, TResult5> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure.
    /// </summary>
    /// <param name="parameters">The parameters.</param>
    /// <returns></returns>
    public async Task<(TResult1, TResult2, TResult3, TResult4, TResult5)> ExecuteAsync(DbConnection connection, TParameters parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync<TResult1, TResult2, TResult3, TResult4, TResult5>(connection, GetSqlParameters(parameters), unitOfWork);
    }
}

public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure.
    /// </summary>
    /// <param name="parameters">The parameters.</param>
    /// <returns></returns>
    public async Task<(TResult1, TResult2, TResult3, TResult4, TResult5, TResult6)> ExecuteAsync(DbConnection connection, TParameters parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6>(connection, GetSqlParameters(parameters), unitOfWork);
    }
}

public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure.
    /// </summary>
    /// <param name="parameters">The parameters.</param>
    /// <returns></returns>
    public async Task<(TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7)> ExecuteAsync(DbConnection connection, TParameters parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7>(connection, GetSqlParameters(parameters), unitOfWork);
    }
}

public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure.
    /// </summary>
    /// <param name="parameters">The parameters.</param>
    /// <returns></returns>
    public async Task<(TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8)> ExecuteAsync(DbConnection connection, TParameters parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8>(connection, GetSqlParameters(parameters), unitOfWork);
    }
}

public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure.
    /// </summary>
    /// <param name="parameters">The parameters.</param>
    /// <returns></returns>
    public async Task<(TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9)> ExecuteAsync(DbConnection connection, TParameters parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9>(connection, GetSqlParameters(parameters), unitOfWork);
    }
}

public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure.
    /// </summary>
    /// <param name="parameters">The parameters.</param>
    /// <returns></returns>
    public async Task<(TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10)> ExecuteAsync(DbConnection connection, TParameters parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10>(connection, GetSqlParameters(parameters), unitOfWork);
    }
}

public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure.
    /// </summary>
    /// <param name="parameters">The parameters.</param>
    /// <returns></returns>
    public async Task<(TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11)> ExecuteAsync(DbConnection connection, TParameters parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11>(connection, GetSqlParameters(parameters), unitOfWork);
    }
}

public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11, TResult12> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure.
    /// </summary>
    /// <param name="parameters">The parameters.</param>
    /// <returns></returns>
    public async Task<(TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11, TResult12)> ExecuteAsync(DbConnection connection, TParameters parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11, TResult12>(connection, GetSqlParameters(parameters), unitOfWork);
    }
}

public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11, TResult12, TResult13> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure.
    /// </summary>
    /// <param name="parameters">The parameters.</param>
    /// <returns></returns>
    public async Task<(TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11, TResult12, TResult13)> ExecuteAsync(DbConnection connection, TParameters parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11, TResult12, TResult13>(connection, GetSqlParameters(parameters), unitOfWork);
    }
}

public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11, TResult12, TResult13, TResult14> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure.
    /// </summary>
    /// <param name="parameters">The parameters.</param>
    /// <returns></returns>
    public async Task<(TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11, TResult12, TResult13, TResult14)> ExecuteAsync(DbConnection connection, TParameters parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11, TResult12, TResult13, TResult14>(connection, GetSqlParameters(parameters), unitOfWork);
    }
}

public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11, TResult12, TResult13, TResult14, TResult15> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure.
    /// </summary>
    /// <param name="parameters">The parameters.</param>
    /// <returns></returns>
    public async Task<(TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11, TResult12, TResult13, TResult14, TResult15)> ExecuteAsync(DbConnection connection, TParameters parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11, TResult12, TResult13, TResult14, TResult15>(connection, GetSqlParameters(parameters), unitOfWork);
    }
}

public abstract class ResultStoredProcedureBase<TParameters, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11, TResult12, TResult13, TResult14, TResult15, TResult16> : StoredProcedureBase
{
    /// <summary>
    /// Executes the stored procedure.
    /// </summary>
    /// <param name="parameters">The parameters.</param>
    /// <returns></returns>
    public async Task<(TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11, TResult12, TResult13, TResult14, TResult15, TResult16)> ExecuteAsync(DbConnection connection, TParameters parameters, IUnitOfWork? unitOfWork = null)
    {
        return await ExecuteAsync<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10, TResult11, TResult12, TResult13, TResult14, TResult15, TResult16>(connection, GetSqlParameters(parameters), unitOfWork);
    }
}