using Paradigm.Enterprise.Domain.Dtos;

namespace Paradigm.Enterprise.Providers;

public interface IReadProvider<TView> : IProvider
{
    /// <summary>
    /// Gets the entity identifier.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <returns></returns>
    Task<TView> GetByIdAsync(int id);

    /// <summary>
    /// Gets the entities by ids.
    /// </summary>
    /// <param name="ids">The ids.</param>
    /// <returns></returns>
    Task<IEnumerable<TView>> GetByIdsAsync(IEnumerable<int> ids);

    ///// <summary>
    ///// Gets all the entities.
    ///// </summary>
    ///// <returns></returns>
    Task<IEnumerable<TView>> GetAllAsync();

    /// <summary>
    /// Gets the results paginated.
    /// </summary>
    /// <param name="parameters">The parameters.</param>
    /// <returns></returns>
    Task<PaginatedResultDto<TView>> SearchPaginatedAsync(FilterTextPaginatedParameters parameters);

    /// <summary>
    /// Gets the results paginated.
    /// </summary>
    /// <param name="parameters">The parameters.</param>
    /// <param name="userId">The user identifier.</param>
    /// <returns></returns>
    Task<PaginatedResultDto<TView>> SearchPaginatedAsync(FilterTextPaginatedParameters parameters, int userId);
}