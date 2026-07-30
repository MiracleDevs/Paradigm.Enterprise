using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Paradigm.Enterprise.Data.Context;
using Paradigm.Enterprise.Domain.Repositories;
using Paradigm.Enterprise.Domain.Uow;
using System.Data.Common;

namespace Paradigm.Enterprise.Data.Repositories;

/// <summary>
/// Resolves a scoped Entity Framework context and registers it with the current unit of work.
/// </summary>
/// <typeparam name="TContext">The Entity Framework context used by the repository.</typeparam>
/// <typeparam name="TId">The value type used for entity identifiers.</typeparam>
/// <remarks>
/// Construction resolves <typeparamref name="TContext"/> and <see cref="IUnitOfWork"/> from the supplied
/// service scope, then registers the context as a commit participant. Repositories created in the same
/// scope therefore coordinate through the scoped unit of work when they share that instance.
/// </remarks>
/// <example>
/// A concrete repository normally derives from <see cref="ReadRepositoryBase{TEntity, TContext, TId}"/>
/// or <see cref="EditRepositoryBase{TEntity, TContext, TId}"/> and forwards the scoped provider:
/// <code>
/// public sealed class OrderRepository
///     : EditRepositoryBase&lt;Order, SalesDbContext, int&gt;, IOrderRepository
/// {
///     public OrderRepository(IServiceProvider services)
///         : base(services)
///     {
///     }
///
///     protected override IQueryable&lt;Order&gt; AsQueryable() =&gt;
///         EntityContext.Orders.AsNoTracking();
/// }
///
/// services.AddScoped&lt;IUnitOfWork, UnitOfWork&gt;();
/// services.AddDbContext&lt;SalesDbContext&gt;(...);
/// services.AddScoped&lt;IOrderRepository, OrderRepository&gt;();
/// </code>
/// Resolving the repository registers <c>SalesDbContext</c>; calls such as <c>AddAsync</c> only stage
/// work until <c>IUnitOfWork.CommitChangesAsync()</c> is invoked.
/// </example>
public abstract class RepositoryBase<TContext, TId> : IRepository
    where TContext : DbContextBase<TId>
    where TId : struct, IEquatable<TId>
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
    /// Initializes a new instance of the <see cref="RepositoryBase{TContext, TId}" /> class.
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
    /// Disposes the resolved Entity Framework context.
    /// </summary>
    /// <remarks>
    /// Repository disposal does not save pending changes. When the context is managed by dependency
    /// injection, prefer disposing the containing service scope rather than disposing one repository
    /// while other scoped repositories may still share the context.
    /// </remarks>
    public void Dispose()
    {
        EntityContext.Dispose();
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// Registers the resolved context as a unit-of-work commit participant.
    /// </summary>
    /// <remarks>
    /// Override only when a repository needs custom participation. Skipping the base behavior means
    /// <see cref="ICommiteable.CommitChangesAsync"/> will not persist this context automatically.
    /// </remarks>
    protected virtual void RegisterContextAsCommiteable()
    {
        UnitOfWork.RegisterCommiteable(EntityContext);
    }

    /// <summary>
    /// Gets the relational database connection used by the repository context.
    /// </summary>
    /// <returns>The database connection owned by the repository context.</returns>
    /// <remarks>
    /// The caller must not dispose the returned connection. Its open/closed state is managed by
    /// Entity Framework unless application code explicitly opens it.
    /// </remarks>
    protected virtual DbConnection GetDbConnection() => EntityContext.Database.GetDbConnection();

    /// <summary>
    /// Resolves another repository from the same service scope.
    /// </summary>
    /// <typeparam name="TRepository">The type of the repository.</typeparam>
    /// <returns>The repository resolved from the current service scope.</returns>
    /// <remarks>
    /// Use this helper when aggregate logic must collaborate with another registered repository.
    /// The resolved repository may register an additional context with the same unit of work.
    /// </remarks>
    protected TRepository GetRepository<TRepository>() where TRepository : IRepository
    {
        return ServiceProvider.GetRequiredService<TRepository>();
    }

    #endregion
}
