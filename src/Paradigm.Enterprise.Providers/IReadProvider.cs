using Paradigm.Enterprise.Domain.Dtos;

namespace Paradigm.Enterprise.Providers;

/// <summary>
/// Defines application-facing read and search operations for a view model.
/// </summary>
/// <typeparam name="TView">The view model returned to callers.</typeparam>
/// <typeparam name="TId">The value type used for identifiers.</typeparam>
/// <example>
/// A controller or application service depends on the closed provider interface rather than a
/// repository:
/// <code>
/// public sealed class ProductQueries(IReadProvider&lt;ProductView, int&gt; products)
/// {
///     public Task&lt;ProductView&gt; GetAsync(int id) =&gt; products.GetByIdAsync(id);
///
///     public Task&lt;PaginatedResultDto&lt;ProductView&gt;&gt; SearchAsync(
///         ProductSearchParameters parameters) =&gt; products.SearchAsync(parameters);
/// }
/// </code>
/// </example>
public interface IReadProvider<TView, TId> : IProvider
    where TId : struct, IEquatable<TId>
{
    /// <summary>
    /// Gets the entity identifier.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <returns>The matching view.</returns>
    /// <exception cref="Exceptions.NotFoundException">The view does not exist or is not visible to the caller.</exception>
    Task<TView> GetByIdAsync(TId id);

    /// <summary>
    /// Gets the entities by ids.
    /// </summary>
    /// <param name="ids">The ids.</param>
    /// <returns>The matching views; identifiers with no match are omitted.</returns>
    Task<IEnumerable<TView>> GetByIdsAsync(IEnumerable<TId> ids);

    /// <summary>
    /// Gets all view models available to the caller.
    /// </summary>
    /// <returns>All view models exposed by the provider.</returns>
    Task<IEnumerable<TView>> GetAllAsync();

    /// <summary>
    /// Executes the search using the specified parameters.
    /// </summary>
    /// <typeparam name="TParameters">The type of the parameters.</typeparam>
    /// <param name="parameters">The parameters.</param>
    /// <returns>The requested views and their pagination metadata.</returns>
    Task<PaginatedResultDto<TView>> SearchAsync<TParameters>(TParameters parameters) where TParameters : PaginationParametersBase;

    /// <summary>
    /// Gets the results paginated.
    /// </summary>
    /// <param name="parameters">The parameters.</param>
    /// <returns>The requested views and their pagination metadata.</returns>
    [Obsolete("Use SearchAsync<TParameters> instead")]
    Task<PaginatedResultDto<TView>> SearchPaginatedAsync(FilterTextPaginatedParameters parameters);
}
