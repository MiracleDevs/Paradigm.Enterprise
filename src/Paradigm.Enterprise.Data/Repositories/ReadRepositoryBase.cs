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
    /// Gets all the entities.
    /// </summary>
    /// <returns>All entities produced by the repository query.</returns>
    public virtual async Task<IEnumerable<TEntity>> GetAllAsync() => await AsQueryable().ToListAsync();

    /// <summary>
    /// Gets the entity by identifier.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <returns>The matching entity, or <see langword="null"/> when it does not exist.</returns>
    public virtual async Task<TEntity?> GetByIdAsync(TId id) => await AsQueryable().FirstOrDefaultAsync(x => x.Id.Equals(id));

    /// <summary>
    /// Gets the entities by their identifiers.
    /// </summary>
    /// <param name="ids">The identifiers.</param>
    /// <returns>The matching entities; identifiers with no match are omitted.</returns>
    public virtual async Task<IEnumerable<TEntity>> GetByIdsAsync(IEnumerable<TId> ids)
    {
        // todo: look for the IN(...) limit, and separate the request into chunks.
        return await AsQueryable().Where(x => ids.Contains(x.Id)).ToListAsync();
    }

    /// <summary>
    /// Executes the search using the specified parameters.
    /// </summary>
    /// <typeparam name="TParameters">The type of the parameters.</typeparam>
    /// <param name="parameters">The parameters.</param>
    /// <returns>The requested entities and their pagination metadata.</returns>
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
    /// Gets the entity set as queryable.
    /// </summary>
    /// <returns>The base query used by read operations.</returns>
    protected virtual IQueryable<TEntity> AsQueryable() => EntityContext.Set<TEntity>();

    /// <summary>
    /// Gets the method to be executed for filter entities.
    /// </summary>
    /// <param name="parameters">The parameters used to select a search implementation.</param>
    /// <returns>A function that executes the application-specific paginated query.</returns>
    /// <exception cref="NotImplementedException">A derived repository does not provide a search implementation.</exception>
    protected virtual Func<PaginationParametersBase, Task<(PaginationInfo, List<TEntity>)>> GetSearchPaginatedFunction(PaginationParametersBase parameters) => throw new NotImplementedException();

    #endregion
}
