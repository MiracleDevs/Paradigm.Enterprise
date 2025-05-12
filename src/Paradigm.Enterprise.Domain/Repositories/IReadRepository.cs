using Paradigm.Enterprise.Domain.Dtos;
using Paradigm.Enterprise.Domain.Entities;

namespace Paradigm.Enterprise.Domain.Repositories;

public interface IReadRepository<TEntity, TParameters> : IRepository
    where TEntity : EntityBase
    where TParameters : FilterTextPaginatedParameters
{
    /// <summary>
    /// Gets the entity by identifier.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <returns></returns>
    Task<TEntity?> GetByIdAsync(int id);

    /// <summary>
    /// Gets the entities by their identifiers.
    /// </summary>
    /// <param name="ids">The ids.</param>
    /// <returns></returns>
    Task<IEnumerable<TEntity>> GetByIdsAsync(IEnumerable<int> ids);

    /// <summary>
    /// Gets all the entities.
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<TEntity>> GetAllAsync();

    /// <summary>
    /// Searches the paginated asynchronous.
    /// </summary>
    /// <param name="parametersBase">The parameters base.</param>
    /// <returns></returns>
    Task<PaginatedResultDto<TEntity>> SearchPaginatedAsync(TParameters parametersBase);
}