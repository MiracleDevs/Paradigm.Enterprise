using Microsoft.EntityFrameworkCore;
using Paradigm.Enterprise.Data.Context;
using Paradigm.Enterprise.Domain.Dtos;
using Paradigm.Enterprise.Domain.Entities;
using Paradigm.Enterprise.Domain.Repositories;

namespace Paradigm.Enterprise.Data.Repositories;

/// <summary>
/// Implements common Entity Framework read operations and delegates application-specific paging to a hook.
/// </summary>
/// <typeparam name="TEntity">The entity returned by the repository.</typeparam>
/// <typeparam name="TContext">The Entity Framework context used for queries.</typeparam>
/// <typeparam name="TId">The value type used for entity identifiers.</typeparam>
/// <remarks>
/// <see cref="AsQueryable"/> is the source for identifier and full-list queries and may be overridden
/// to add eager loading, tenant filters, projections, or no-tracking behavior. Paginated search is
/// intentionally application-specific: override <see cref="GetSearchPaginatedFunction"/> before calling
/// <see cref="SearchAsync{TParameters}(TParameters)"/>.
/// </remarks>
/// <example>
/// Keep simple bounded identifier/list queries in EF. Route application pagination through a focused
/// stored-procedure caller owned by Data:
/// <code>
/// public sealed class OrderRepository
///     : ReadRepositoryBase&lt;Order, SalesDbContext, int&gt;
/// {
///     private readonly SearchOrdersProcedure search;
///
///     public OrderRepository(IServiceProvider services, SearchOrdersProcedure search)
///         : base(services) =&gt; this.search = search;
///
///     protected override IQueryable&lt;Order&gt; AsQueryable() =&gt;
///         EntityContext.Orders.AsNoTracking();
///
///     protected override Func&lt;PaginationParametersBase,
///         Task&lt;(PaginationInfo, List&lt;Order&gt;)&gt;&gt;
///         GetSearchPaginatedFunction(PaginationParametersBase parameters) =&gt;
///         async input =&gt; await search.ExecuteAsync(
///             GetDbConnection(),
///             (OrderSearchParameters)input,
///             UnitOfWork);
/// }
///
/// var page = await repository.SearchAsync(
///     new OrderSearchParameters { PageNumber = 2, PageSize = 25 });
/// </code>
/// Use the provider-specific <c>ResultStoredProcedureBase</c>, its registered generated mapper,
/// deterministic result-set ordering, a bounded timeout, and the context-owned connection. Do not
/// dispose that connection. Resolve all repositories before opening an explicit Unit of Work transaction.
/// </example>
public abstract class ReadRepositoryBase<TEntity, TContext, TId> : RepositoryBase<TContext, TId>, IReadRepository<TEntity, TId>
    where TEntity : EntityBase<TId>
    where TContext : DbContextBase<TId>
    where TId : struct, IEquatable<TId>
{
    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadRepositoryBase{TEntity, TContext, TId}" /> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    protected ReadRepositoryBase(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Materializes all entities from <see cref="AsQueryable"/>.
    /// </summary>
    /// <returns>All entities produced by the repository query.</returns>
    /// <remarks>
    /// This method does not apply pagination. Override it or <see cref="AsQueryable"/> when the application
    /// requires filters, includes, ordering, or no-tracking behavior.
    /// </remarks>
    public virtual async Task<IEnumerable<TEntity>> GetAllAsync() => await AsQueryable().ToListAsync();

    /// <summary>
    /// Returns the first entity whose identifier equals the supplied value.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <returns>The matching entity, or <see langword="null"/> when it does not exist.</returns>
    public virtual async Task<TEntity?> GetByIdAsync(TId id) => await AsQueryable().FirstOrDefaultAsync(x => x.Id.Equals(id));

    /// <summary>
    /// Returns the entities whose identifiers occur in the supplied sequence.
    /// </summary>
    /// <param name="ids">The identifiers.</param>
    /// <returns>The matching entities; identifiers with no match are omitted.</returns>
    /// <remarks>
    /// The result order is determined by the database query and is not guaranteed to match
    /// <paramref name="ids"/>. Large identifier sets are passed to the provider as one query.
    /// </remarks>
    public virtual async Task<IEnumerable<TEntity>> GetByIdsAsync(IEnumerable<TId> ids)
    {
        // todo: look for the IN(...) limit, and separate the request into chunks.
        return await AsQueryable().Where(x => ids.Contains(x.Id)).ToListAsync();
    }

    /// <summary>
    /// Executes the application-specific paginated search selected for the supplied parameters.
    /// </summary>
    /// <typeparam name="TParameters">The type of the parameters.</typeparam>
    /// <param name="parameters">The parameters.</param>
    /// <returns>The requested entities and their pagination metadata.</returns>
    /// <exception cref="NotImplementedException">
    /// The repository does not override <see cref="GetSearchPaginatedFunction"/>.
    /// </exception>
    public async Task<PaginatedResultDto<TEntity>> SearchAsync<TParameters>(TParameters parameters)
        where TParameters : PaginationParametersBase
    {
        var (paginationInfo, entities) = await GetSearchPaginatedFunction(parameters).Invoke(parameters);
        return new PaginatedResultDto<TEntity>(paginationInfo, entities);
    }

    /// <summary>
    /// Execute the search function for entities that implements the method.
    /// </summary>
    /// <param name="parameters">The legacy filter and paging parameters.</param>
    /// <returns>The requested entities and their pagination metadata.</returns>
    [Obsolete("Use SearchAsync<TParameters> instead")]
    public async Task<PaginatedResultDto<TEntity>> SearchPaginatedAsync(FilterTextPaginatedParameters parameters)
    {
        var (paginationInfo, entities) = await GetSearchPaginatedFunction(parameters).Invoke(parameters);
        return new PaginatedResultDto<TEntity>(paginationInfo, entities);
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// Creates the base query used by the built-in read operations.
    /// </summary>
    /// <returns>The base query used by read operations.</returns>
    /// <remarks>
    /// The default query tracks returned entities. Override this member to use <c>AsNoTracking()</c>,
    /// add includes, or enforce a query-wide application filter.
    /// </remarks>
    protected virtual IQueryable<TEntity> AsQueryable() => EntityContext.Set<TEntity>();

    /// <summary>
    /// Selects the function that executes an application-specific paginated query.
    /// </summary>
    /// <param name="parameters">The parameters used to select a search implementation.</param>
    /// <returns>A function that executes the application-specific paginated query.</returns>
    /// <exception cref="NotImplementedException">A derived repository does not provide a search implementation.</exception>
    /// <remarks>
    /// The default implementation always throws. Derived repositories may inspect
    /// <paramref name="parameters"/> to select among several search functions; the returned function
    /// receives the same parameter object when invoked by <see cref="SearchAsync{TParameters}(TParameters)"/>.
    /// </remarks>
    protected virtual Func<PaginationParametersBase, Task<(PaginationInfo, List<TEntity>)>> GetSearchPaginatedFunction(PaginationParametersBase parameters) => throw new NotImplementedException();

    #endregion
}
