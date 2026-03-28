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
        var idList = ids.ToList();
        if (idList.Count == 0)
            return Enumerable.Empty<TEntity>();

        // Determine chunk size based on database provider limits:
        var chunkSize = GetChunkSize();
        var results = new List<TEntity>();
        var queryable = AsQueryable();

        foreach (var chunk in idList.Chunk(chunkSize))
        {
            var chunkList = chunk.ToList();
            var chunkResults = await queryable.Where(x => chunkList.Contains(x.Id)).ToListAsync();
            results.AddRange(chunkResults);
        }

        return results;
    }

    /// <summary>
    /// Executes the search using the specified parameters.
    /// </summary>
    /// <typeparam name="TParameters">The type of the parameters.</typeparam>
    /// <param name="parameters">The parameters.</param>
    /// <returns></returns>
    public async Task<PaginatedResultDto<TEntity>> SearchAsync<TParameters>(TParameters parameters)
        where TParameters : PaginationParametersBase
    {
        var (paginationInfo, entities) = await GetSearchPaginatedFunction(parameters).Invoke(parameters);
        return new PaginatedResultDto<TEntity>(paginationInfo, entities);
    }

    /// <summary>
    /// Execute the search function for entities that implements the method.
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns></returns>
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
    /// <returns></returns>
    protected virtual IQueryable<TEntity> AsQueryable() => EntityContext.Set<TEntity>().AsNoTracking();

    /// <summary>
    /// Gets the method to be executed for filter entities.
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns></returns>
    protected virtual Func<PaginationParametersBase, Task<(PaginationInfo, List<TEntity>)>> GetSearchPaginatedFunction(PaginationParametersBase parameters) => throw new NotImplementedException();

    /// <summary>
    /// Gets the chunk size for batching ID queries based on the database provider.
    /// </summary>
    /// <returns>The chunk size to use for batching queries.</returns>
    protected virtual int GetChunkSize()
    {
        var providerName = EntityContext.Database.ProviderName;

        // PostgreSQL has a limit of 65535 parameters, using 10000 for efficiency while staying well under the limit
        if (providerName?.Contains("PostgreSQL", StringComparison.OrdinalIgnoreCase) == true ||
            providerName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
            return 10000;

        // SQL Server has a limit of 2100 parameters per query, using 2000 for safety
        // Default to 2000 for unknown providers (safe for most databases)
        return 2000;
    }

    #endregion
}