using Microsoft.EntityFrameworkCore;
using Paradigm.Enterprise.Data.Context;
using Paradigm.Enterprise.Domain.Dtos;
using Paradigm.Enterprise.Domain.Entities;
using Paradigm.Enterprise.Domain.Repositories;

namespace Paradigm.Enterprise.Data.Repositories;

public abstract class ReadRepositoryBase<TEntity, TContext> : RepositoryBase<TContext>, IReadRepository<TEntity>
    where TEntity : EntityBase
    where TContext : DbContextBase
{
    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadRepositoryBase{TEntity, TContext}" /> class.
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
    /// <returns></returns>
    public virtual async Task<IEnumerable<TEntity>> GetAllAsync() => await AsQueryable().ToListAsync();

    /// <summary>
    /// Gets the entity by identifier.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <returns></returns>
    public virtual async Task<TEntity?> GetByIdAsync(int id) => await AsQueryable().FirstOrDefaultAsync(x => x.Id == id);

    /// <summary>
    /// Gets the entities by their identifiers.
    /// </summary>
    /// <param name="ids">The identifiers.</param>
    /// <returns></returns>
    public virtual async Task<IEnumerable<TEntity>> GetByIdsAsync(IEnumerable<int> ids)
    {
        // todo: look for the IN(...) limit, and separate the request into chunks.
        return await AsQueryable().Where(x => ids.Contains(x.Id)).ToListAsync();
    }

    /// <summary>
    /// Execute the search function for entities that implements the method.
    /// </summary>
    /// <param name="parametersBase"></param>
    /// <returns></returns>
    public async Task<PaginatedResultDto<TEntity>> SearchPaginatedAsync(FilterTextPaginatedParameters parametersBase)
    {
        var (paginationInfo, entities) = await GetSearchPaginatedFunction(parametersBase).Invoke(parametersBase);
        return new PaginatedResultDto<TEntity>(paginationInfo, entities);
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// Gets the entity set as queryable.
    /// </summary>
    /// <returns></returns>
    protected virtual IQueryable<TEntity> AsQueryable() => EntityContext.Set<TEntity>();

    /// <summary>
    /// Gets the method to be executed for filter entities.
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns></returns>
    protected virtual Func<FilterTextPaginatedParameters, Task<(PaginationInfo, List<TEntity>)>> GetSearchPaginatedFunction(FilterTextPaginatedParameters parameters) => throw new NotImplementedException();

    #endregion
}