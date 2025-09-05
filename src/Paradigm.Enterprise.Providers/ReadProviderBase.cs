using Microsoft.Extensions.DependencyInjection;
using Paradigm.Enterprise.Domain.Dtos;
using Paradigm.Enterprise.Domain.Entities;
using Paradigm.Enterprise.Domain.Repositories;
using Paradigm.Enterprise.Providers.Exceptions;

namespace Paradigm.Enterprise.Providers;

public abstract class ReadProviderBase<TInterface, TView, TViewRepository> : ProviderBase, IReadProvider<TView>
      where TInterface : Interfaces.IEntity
      where TView : EntityBase, TInterface, new()
      where TViewRepository : IReadRepository<TView>
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
    /// Initializes a new instance of the <see cref="ReadProviderBase{TEntity, TDto, TRepository}"/> class.
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
    /// <returns></returns>
    public virtual async Task<TView> GetByIdAsync(int id)
    {
        return await ViewRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Entity not found or you don't have the permissions to open it.");
    }

    /// <summary>
    /// Gets the entities by ids.
    /// </summary>
    /// <param name="ids">The ids.</param>
    /// <returns></returns>
    public virtual async Task<IEnumerable<TView>> GetByIdsAsync(IEnumerable<int> ids)
    {
        return await ViewRepository.GetByIdsAsync(ids);
    }

    /// <summary>
    /// Gets all the entities.
    /// </summary>
    /// <returns></returns>
    public virtual async Task<IEnumerable<TView>> GetAllAsync()
    {
        return await ViewRepository.GetAllAsync();
    }

    /// <summary>
    /// Executes the search using the specified parameters.
    /// </summary>
    /// <typeparam name="TParameters">The type of the parameters.</typeparam>
    /// <param name="parameters">The parameters.</param>
    /// <returns></returns>
    public virtual async Task<PaginatedResultDto<TView>> SearchAsync<TParameters>(TParameters parameters)
        where TParameters : PaginationParametersBase
    {
        return await ViewRepository.SearchAsync(parameters);
    }

    /// <summary>
    /// Gets the results paginated.
    /// </summary>
    /// <param name="parameters">The parameters.</param>
    /// <returns></returns>
    [Obsolete("Use SearchAsync<TParameters> instead")]
    public virtual async Task<PaginatedResultDto<TView>> SearchPaginatedAsync(FilterTextPaginatedParameters parameters)
    {
        return await ViewRepository.SearchPaginatedAsync(parameters);
    }

    #endregion
}