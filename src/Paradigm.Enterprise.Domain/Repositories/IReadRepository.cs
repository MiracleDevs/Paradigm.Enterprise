using Paradigm.Enterprise.Domain.Dtos;
using Paradigm.Enterprise.Domain.Entities;

namespace Paradigm.Enterprise.Domain.Repositories;

public interface IReadRepository<TEntity, TId> : IRepository
    where TEntity : EntityBase<TId>
    where TId : struct, IEquatable<TId>
{
    /// <summary>
    /// Gets the entity by identifier.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <returns></returns>
    Task<TEntity?> GetByIdAsync(TId id);

    /// <summary>
    /// Gets the entities by their identifiers.
    /// </summary>
    /// <param name="ids">The ids.</param>
    /// <returns></returns>
    Task<IEnumerable<TEntity>> GetByIdsAsync(IEnumerable<TId> ids);

    /// <summary>
    /// Gets all the entities.
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<TEntity>> GetAllAsync();

    /// <summary>
    /// Executes the search using the specified parameters.
    /// </summary>
    /// <typeparam name="TParameters">The type of the parameters.</typeparam>
    /// <param name="parameters">The parameters.</param>
    /// <returns></returns>
    Task<PaginatedResultDto<TEntity>> SearchAsync<TParameters>(TParameters parameters) where TParameters : PaginationParametersBase;

    /// <summary>
    /// Searches the results paginated.
    /// </summary>
    /// <param name="parameters">The parameters.</param>
    /// <returns></returns>
    [Obsolete("Use SearchAsync<TParameters> instead")]
    Task<PaginatedResultDto<TEntity>> SearchPaginatedAsync(FilterTextPaginatedParameters parameters);
}