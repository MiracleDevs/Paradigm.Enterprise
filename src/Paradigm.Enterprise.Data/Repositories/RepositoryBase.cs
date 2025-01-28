using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Paradigm.Enterprise.Data.Context;
using Paradigm.Enterprise.Domain.Repositories;
using Paradigm.Enterprise.Domain.Uow;
using System.Data.Common;

namespace Paradigm.Enterprise.Data.Repositories;

public abstract class RepositoryBase<TContext> : IRepository
    where TContext : DbContextBase
{
    #region Properties

    /// <summary>
    /// Gets the service provider.
    /// </summary>
    /// <value>
    /// The service provider.
    /// </value>
    protected IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Gets the unit of work.
    /// </summary>
    /// <value>
    /// The unit of work.
    /// </value>
    protected IUnitOfWork UnitOfWork { get; }

    /// <summary>
    /// Gets the entity context.
    /// </summary>
    /// <value>
    /// The entity context.
    /// </value>
    protected TContext EntityContext { get; }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadRepositoryBase{TEntity, TContext}" /> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    protected RepositoryBase(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        UnitOfWork = serviceProvider.GetRequiredService<IUnitOfWork>();
        EntityContext = serviceProvider.GetRequiredService<TContext>();
        RegisterContextAsCommiteable();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Releases unmanaged and - optionally - managed resources.
    /// </summary>
    public void Dispose()
    {
        EntityContext.Dispose();
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// Registers the context as commiteable.
    /// </summary>
    protected virtual void RegisterContextAsCommiteable()
    {
        UnitOfWork.RegisterCommiteable(EntityContext);
    }

    /// <summary>
    /// Gets the database connection.
    /// </summary>
    /// <returns></returns>
    protected virtual DbConnection GetDbConnection() => EntityContext.Database.GetDbConnection();

    /// <summary>
    /// Gets the repository.
    /// </summary>
    /// <typeparam name="TRepository">The type of the repository.</typeparam>
    /// <returns></returns>
    protected TRepository GetRepository<TRepository>() where TRepository : IRepository
    {
        return ServiceProvider.GetRequiredService<TRepository>();
    }

    #endregion
}