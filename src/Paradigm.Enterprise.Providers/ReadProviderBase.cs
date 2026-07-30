using Microsoft.Extensions.DependencyInjection;
using Paradigm.Enterprise.Domain.Dtos;
using Paradigm.Enterprise.Domain.Entities;
using Paradigm.Enterprise.Domain.Repositories;
using Paradigm.Enterprise.Providers.Exceptions;

namespace Paradigm.Enterprise.Providers;

/// <summary>
/// Implements application-facing reads by delegating to a view repository.
/// </summary>
/// <typeparam name="TInterface">The interface shared by the domain entity and view.</typeparam>
/// <typeparam name="TView">The view model returned to callers.</typeparam>
/// <typeparam name="TViewRepository">The repository used to retrieve views.</typeparam>
/// <typeparam name="TId">The value type used for identifiers.</typeparam>
public abstract class ReadProviderBase<TInterface, TView, TViewRepository, TId> : ProviderBase, IReadProvider<TView, TId>
    where TId : struct, IEquatable<TId>
    where TInterface : Interfaces.IEntity<TId>
    where TView : EntityBase<TId>, TInterface, new()
    where TViewRepository : IReadRepository<TView, TId>
{
    #region Properties

    /// <summary>
    /// Gets the view repository.
    /// </summary>
    /// <value>
    /// The view repository.
    /// </value>
    protected TViewRepository ViewRepository { get; }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadProviderBase{TInterface, TView, TViewRepository, TId}"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    protected ReadProviderBase(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        ViewRepository = serviceProvider.GetRequiredService<TViewRepository>();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Gets the entity identifier.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <returns>The matching view.</returns>
    /// <exception cref="NotFoundException">The view does not exist or is not visible to the caller.</exception>
    public virtual async Task<TView> GetByIdAsync(TId id)
    {
        return await ViewRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Entity not found or you don't have the permissions to open it.");
    }

    /// <summary>
    /// Gets the entities by ids.
    /// </summary>
    /// <param name="ids">The ids.</param>
    /// <returns>The matching views; identifiers with no match are omitted.</returns>
    public virtual async Task<IEnumerable<TView>> GetByIdsAsync(IEnumerable<TId> ids)
    {
        return await ViewRepository.GetByIdsAsync(ids);
    }

    /// <summary>
    /// Gets all the entities.
    /// </summary>
    /// <returns>All views exposed by the repository.</returns>
    public virtual async Task<IEnumerable<TView>> GetAllAsync()
    {
        return await ViewRepository.GetAllAsync();
    }

    /// <summary>
    /// Executes the search using the specified parameters.
    /// </summary>
    /// <typeparam name="TParameters">The type of the parameters.</typeparam>
    /// <param name="parameters">The parameters.</param>
    /// <returns>The requested views and their pagination metadata.</returns>
    public virtual async Task<PaginatedResultDto<TView>> SearchAsync<TParameters>(TParameters parameters)
        where TParameters : PaginationParametersBase
    {
        return await ViewRepository.SearchAsync(parameters);
    }

    /// <summary>
    /// Gets the results paginated.
    /// </summary>
    /// <param name="parameters">The parameters.</param>
    /// <returns>The requested views and their pagination metadata.</returns>
    [Obsolete("Use SearchAsync<TParameters> instead")]
    public virtual async Task<PaginatedResultDto<TView>> SearchPaginatedAsync(FilterTextPaginatedParameters parameters)
    {
        return await ViewRepository.SearchPaginatedAsync(parameters);
    }

    #endregion
}
