using Paradigm.Enterprise.Domain.Dtos;
using Paradigm.Enterprise.Domain.Entities;

namespace Paradigm.Enterprise.Domain.Repositories;

/// <summary>
/// Defines read and paginated-search operations for an entity type.
/// </summary>
/// <typeparam name="TEntity">The entity returned by the repository.</typeparam>
/// <typeparam name="TId">The value type used for entity identifiers.</typeparam>
/// <example>
/// Repository queries are asynchronous and return <see langword="null"/> when an identifier is not found:
/// <code>
/// var order = await repository.GetByIdAsync(orderId);
/// var page = await repository.SearchAsync(
///     new OrderSearchParameters { PageNumber = 1, PageSize = 25 });
/// </code>
/// </example>
public interface IReadRepository<TEntity, TId> : IRepository
    where TEntity : EntityBase<TId>
    where TId : struct, IEquatable<TId>
{
    /// <summary>
    /// Gets the entity by identifier.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <returns>The matching entity, or <see langword="null"/> when no entity has that identifier.</returns>
    Task<TEntity?> GetByIdAsync(TId id);

    /// <summary>
    /// Gets the entities by their identifiers.
    /// </summary>
    /// <param name="ids">The identifiers to match.</param>
    /// <returns>The matching entities; identifiers with no match are omitted.</returns>
    Task<IEnumerable<TEntity>> GetByIdsAsync(IEnumerable<TId> ids);

    /// <summary>
    /// Gets all the entities.
    /// </summary>
    /// <returns>All entities exposed by this repository.</returns>
    Task<IEnumerable<TEntity>> GetAllAsync();

    /// <summary>
    /// Executes the search using the specified parameters.
    /// </summary>
    /// <typeparam name="TParameters">The type of the parameters.</typeparam>
    /// <param name="parameters">The parameters.</param>
    /// <returns>The requested results and their pagination metadata.</returns>
    Task<PaginatedResultDto<TEntity>> SearchAsync<TParameters>(TParameters parameters) where TParameters : PaginationParametersBase;

    /// <summary>
    /// Searches the results paginated.
    /// </summary>
    /// <param name="parameters">The parameters.</param>
    /// <returns>The requested results and their pagination metadata.</returns>
    [Obsolete("Use SearchAsync<TParameters> instead")]
    Task<PaginatedResultDto<TEntity>> SearchPaginatedAsync(FilterTextPaginatedParameters parameters);
}
