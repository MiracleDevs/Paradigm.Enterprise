using Microsoft.Extensions.DependencyInjection;
using Paradigm.Enterprise.Domain.Dtos;
using Paradigm.Enterprise.Domain.Entities;
using Paradigm.Enterprise.Domain.Repositories;
using Paradigm.Enterprise.Domain.Uow;
using Paradigm.Enterprise.Providers.Exceptions;

namespace Paradigm.Enterprise.Providers;

internal class EntityViewProvider<TInterface, TEntity, TView, TRepository> : ProviderBase, IEntityViewProvider<TEntity, TView>
    where TInterface : Interfaces.IEntity
    where TEntity : EntityBase<TInterface, TEntity, TView>, TInterface, new()
    where TView : EntityBase, TInterface, new()
    where TRepository : IEntityViewRepository<TEntity, TView>
{
    #region Properties

    /// <summary>
    /// Gets the repository.
    /// </summary>
    /// <value>
    /// The repository.
    /// </value>
    protected TRepository Repository { get; }

    /// <summary>
    /// Gets the unit of work.
    /// </summary>
    /// <value>
    /// The unit of work.
    /// </value>
    protected IUnitOfWork UnitOfWork { get; }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="EditProviderBase{TEntity, TDto, TRepository}"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    protected EntityViewProvider(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        Repository = serviceProvider.GetRequiredService<TRepository>();
        UnitOfWork = serviceProvider.GetRequiredService<IUnitOfWork>();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Executes the search using the specified parameters.
    /// </summary>
    /// <typeparam name="TParameters">The type of the parameters.</typeparam>
    /// <param name="parameters">The parameters.</param>
    /// <returns></returns>
    public virtual async Task<PaginatedResultDto<TView>> SearchAsync<TParameters>(TParameters parameters)
        where TParameters : PaginationParametersBase
    {
        return await Repository.SearchAsync(parameters);
    }

    /// <summary>
    /// Add or update a TView item
    /// </summary>
    /// <param name="view"></param>
    /// <returns></returns>
    public virtual async Task<TView> SaveAsync(TView view)
    {
        return view.IsNew() ?
            await AddAsync(view) :
            await UpdateAsync(view);
    }

    /// <summary>
    /// Returns all the TView items
    /// </summary>
    /// <returns></returns>
    public async Task<IEnumerable<TView>> GetAllAsync() => await Repository.GetAllAsync();

    /// <summary>
    /// Returns a list of TView
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns></returns>
    public async Task<PaginatedResultDto<TView>> SearchPaginatedAsync(FilterTextPaginatedParameters parameters) => await Repository.SearchAsync(parameters);

    /// <summary>
    /// Returns a list of TEntity by the given ids
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    public async Task<IEnumerable<TEntity>> GetEntitiesByIdsAsync(IEnumerable<int> ids) => await Repository.GetEntitiesByIdsAsync(ids);

    /// <summary>
    /// Returns a TEntity by Id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <exception cref="NotFoundException"></exception>
    public virtual async Task<TEntity?> GetEntityByIdAsync(int id)
    {
        return await Repository.GetEntityByIdAsync(id)
            ?? throw new NotFoundException("Entity not found or you don't have the permissions to open it.");
    }

    /// <summary>
    /// Returns a list of TView items by the given Ids
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    public async Task<IEnumerable<TView>> GetByIdsAsync(IEnumerable<int> ids) => await Repository.GetByIdsAsync(ids);

    /// <summary>
    /// Returns a TView item by id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <exception cref="NotFoundException"></exception>
    public virtual async Task<TView> GetByIdAsync(int id)
    {
        return await Repository.GetByIdAsync(id)
            ?? throw new NotFoundException("Entity not found or you don't have the permissions to open it.");
    }

    /// <summary>
    /// Adds a TView item
    /// </summary>
    /// <param name="view"></param>
    /// <returns></returns>
    public virtual async Task<TView> AddAsync(TView view)
    {
        await BeforeAddAsync(view);
        await BeforeSaveAsync(view);

        var entity = ServiceProvider.GetRequiredService<TEntity>();
        entity.MapFrom(ServiceProvider, view);
        entity.Validate();

        await BeforeAddAsync(entity);
        await BeforeSaveAsync(entity);

        await Repository.AddAsync(entity);
        await UnitOfWork.CommitChangesAsync();

        await AfterSaveAsync(entity);
        await AfterAddAsync(entity);

        return await GetByIdAsync(entity.Id);
    }

    /// <summary>
    /// Adds new entities.
    /// </summary>
    /// <param name="views">The dtos.</param>
    /// <returns></returns>
    public virtual async Task<IEnumerable<TView>> AddAsync(List<TView> views)
    {
        var entities = new List<TEntity>();

        foreach (var view in views)
        {
            await BeforeAddAsync(view);
            await BeforeSaveAsync(view);

            var entity = ServiceProvider.GetRequiredService<TEntity>();
            entity.MapFrom(ServiceProvider, view);
            entity.Validate();

            await BeforeAddAsync(entity);
            await BeforeSaveAsync(entity);

            await Repository.AddAsync(entity);
            entities.Add(entity);
        }

        await UnitOfWork.CommitChangesAsync();

        foreach (var entity in entities)
        {
            await AfterSaveAsync(entity);
            await AfterAddAsync(entity);
        }

        return entities.Select(x => x.MapTo(ServiceProvider)).ToList();
    }

    /// <summary>
    /// Updates the entity.
    /// </summary>
    /// <param name="view">The dto.</param>
    /// <returns></returns>
    public virtual async Task<TView> UpdateAsync(TView view)
    {
        var entity = await Repository.GetEntityByIdAsync(view.Id)
            ?? throw new NotFoundException("Entity not found or you don't have the permissions to open it.");

        await BeforeUpdateAsync(view);
        await BeforeSaveAsync(view);

        entity.MapFrom(ServiceProvider, view);
        entity.Validate();

        await BeforeUpdateAsync(entity);
        await BeforeSaveAsync(entity);

        entity = await Repository.UpdateAsync(entity);
        await UnitOfWork.CommitChangesAsync();

        await AfterSaveAsync(entity);
        await AfterUpdateAsync(entity);

        return await GetByIdAsync(entity.Id);
    }

    /// <summary>
    /// Updates a new entities.
    /// </summary>
    /// <param name="views">The dtos.</param>
    /// <returns></returns>
    public virtual async Task<IEnumerable<TView>> UpdateAsync(List<TView> views)
    {
        var entities = new List<TEntity>();

        foreach (var view in views)
        {
            var entity = await Repository.GetEntityByIdAsync(view.Id)
                ?? throw new NotFoundException("Entity not found or you don't have the permissions to open it.");

            await BeforeUpdateAsync(view);
            await BeforeSaveAsync(view);

            entity.MapFrom(ServiceProvider, view);
            entity.Validate();

            await BeforeUpdateAsync(entity);
            await BeforeSaveAsync(entity);

            entity = await Repository.UpdateAsync(entity);
            entities.Add(entity);
        }

        await UnitOfWork.CommitChangesAsync();

        foreach (var entity in entities)
        {
            await AfterSaveAsync(entity);
            await AfterUpdateAsync(entity);
        }

        return entities.Select(x => x.MapTo(ServiceProvider)).ToList();
    }

    /// <summary>
    /// Deletes the entity.
    /// </summary>
    /// <param name="id">The identifier.</param>
    public virtual async Task DeleteAsync(int id)
    {
        var entity = await Repository.GetEntityByIdAsync(id);
        if (entity is not null)
        {
            await BeforeDeleteAsync(entity);
            await Repository.DeleteAsync(entity);
            await AfterDeleteAsync(entity);
            await UnitOfWork.CommitChangesAsync();
        }
    }

    /// <summary>
    /// Deletes the entities.
    /// </summary>
    /// <param name="ids">The identifiers.</param>
    public virtual async Task DeleteAsync(IEnumerable<int> ids)
    {
        var entities = await Repository.GetEntitiesByIdsAsync(ids);

        foreach (var entity in entities)
        {
            await BeforeDeleteAsync(entity);
            await Repository.DeleteAsync(entity);
            await AfterDeleteAsync(entity);
        }

        await UnitOfWork.CommitChangesAsync();
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// Executed on add operation before the <see cref="TEntity"/> is mapped to <see cref="TEntity"/>.
    /// </summary>
    /// <param name="entity">The entity.</param>
    protected virtual async Task BeforeAddAsync(TEntity entity)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Executed on add operation before the <see cref="TView"/> is mapped to <see cref="TEntity"/>.
    /// </summary>
    /// <param name="view">The view.</param>
    protected virtual async Task BeforeAddAsync(TView view)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Executed on add operation after an entity has been added.
    /// </summary>
    /// <param name="entity">The entity.</param>
    protected virtual async Task AfterAddAsync(TEntity entity)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Executed on update operation before the <see cref="TEntity"/> is mapped to <see cref="TEntity"/>.
    /// </summary>
    /// <param name="entity">The entity.</param>
    protected virtual async Task BeforeUpdateAsync(TEntity entity)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Executed on update operation before the <see cref="TEntity"/> is mapped to <see cref="TEntity"/>.
    /// </summary>
    /// <param name="entity">The entity.</param>
    protected virtual async Task BeforeUpdateAsync(TView entity)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Executed on update operation after an entity has been updated.
    /// </summary>
    /// <param name="entity">The entity.</param>
    protected virtual async Task AfterUpdateAsync(TEntity entity)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Executed on save operation before the <see cref="TEntity"/> is mapped to <see cref="TEntity"/>.
    /// </summary>
    /// <param name="entity">The entity.</param>
    protected virtual async Task BeforeSaveAsync(TEntity entity)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Executed on save operation before the <see cref="TView"/> is mapped to <see cref="TEntity"/>.
    /// </summary>
    /// <param name="view">The view.</param>
    protected virtual async Task BeforeSaveAsync(TView view)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Executed on save operation after an entity has been saved.
    /// </summary>
    /// <param name="entity">The entity.</param>
    protected virtual async Task AfterSaveAsync(TEntity entity)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Executes before an entity is deleted.
    /// </summary>
    /// <param name="entity">The entity.</param>
    protected virtual async Task BeforeDeleteAsync(TEntity entity)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Executes after an entity has been deleted.
    /// </summary>
    /// <param name="entity">The entity.</param>
    protected virtual async Task AfterDeleteAsync(TEntity entity)
    {
        await Task.CompletedTask;
    }

    #endregion
}
