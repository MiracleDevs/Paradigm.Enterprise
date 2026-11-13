using Paradigm.Enterprise.Domain.Uow;
using System.Data.Common;

namespace Paradigm.Enterprise.Data.MySql.StoredProcedures;

/// <summary>Defines a MySQL stored procedure using a registered application parameter mapper.</summary>
/// <typeparam name="TParameters">The application parameter type.</typeparam>
public abstract class StoredProcedureBase<TParameters> : StoredProcedureBase
{
    #region Public Methods

    /// <summary>Maps the parameters and executes the procedure without a mapped result.</summary>
    /// <param name="connection">A caller-owned Oracle MySQL connection.</param>
    /// <param name="parameters">The application parameter object, or null.</param>
    /// <param name="unitOfWork">The optional unit of work owning the transaction.</param>
    /// <returns>A task representing command completion.</returns>
    /// <remarks>Nontransactional connections close on execution success or failure. Mapping occurs before opening.</remarks>
    public Task ExecuteAsync(DbConnection connection, TParameters? parameters, IUnitOfWork? unitOfWork = null) =>
        ExecuteAsync(connection, GetMySqlParameters(parameters), unitOfWork);

    #endregion
}