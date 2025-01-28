using Microsoft.Extensions.DependencyInjection;
using Paradigm.Enterprise.Domain.Entities;
using Paradigm.Enterprise.Domain.Repositories;
using Paradigm.Enterprise.Domain.Uow;
using Paradigm.Enterprise.Providers.Exceptions;

namespace Paradigm.Enterprise.Providers;

public abstract class EditProviderBase<TInterface, TEntity, TView, TRepository, TViewRepository> : ReadProviderBase<TInterface, TView, TViewRepository>, IEditProvider<TView>
    where TInterface : Interfaces.IEntity
    where TEntity : EntityBase<TInterface, TEntity, TView>, TInterface, new()
    where TView : EntityBase, TInterface, new()
    where TRepository : IEditRepository<TEntity>
    where TViewRepository : IReadRepository<TView>
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
    protected EditProviderBase(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        Repository = serviceProvider.GetRequiredService<TRepository>();
        UnitOfWork = serviceProvider.GetRequiredService<IUnitOfWork>();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Adds a new entity.
    /// </summary>
    /// <param name="view">The dto.</param>
    /// <returns></returns>
    public virtual async Task<TView> AddAsync(TView view)
    {
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
        var entity = await Repository.GetByIdAsync(view.Id)
            ?? throw new NotFoundException("Entity not found or you don't have the permissions to open it.");

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
            var entity = await Repository.GetByIdAsync(view.Id)
                ?? throw new NotFoundException("Entity not found or you don't have the permissions to open it.");

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
    /// Adds or updates the entity.
    /// </summary>
    /// <param name="view">The dto.</param>
    /// <returns></returns>
    public virtual async Task<TView> SaveAsync(TView view)
    {
        return view.IsNew() ?
            await AddAsync(view) :
            await UpdateAsync(view);
    }

    /// <summary>
    /// Adds or updates the entities.
    /// </summary>
    /// <param name="views">The dto.</param>
    /// <returns></returns>
    public virtual async Task<IEnumerable<TView>> SaveAsync(IEnumerable<TView> views)
    {
        var entities = new List<(bool, TEntity)>();

        foreach (var view in views)
        {
            var isNew = view.IsNew();
            var entity = isNew
                ? ServiceProvider.GetRequiredService<TEntity>()
                : await Repository.GetByIdAsync(view.Id)
                ?? throw new NotFoundException("Entity not found or you don't have the permissions to open it.");

            entity.MapFrom(ServiceProvider, view);
            entity.Validate();

            if (isNew)
            {
                await BeforeAddAsync(entity);
                await BeforeSaveAsync(entity);
                entity = await Repository.AddAsync(entity);
            }
            else
            {
                await BeforeUpdateAsync(entity);
                await BeforeSaveAsync(entity);
                entity = await Repository.UpdateAsync(entity);
            }

            entities.Add((isNew, entity));
        }

        await UnitOfWork.CommitChangesAsync();

        foreach (var (isNew, entity) in entities)
        {
            await AfterSaveAsync(entity);

            if (isNew)
                await AfterAddAsync(entity);
            else
                await AfterUpdateAsync(entity);
        }

        return entities.Select(x => x.Item2.MapTo(ServiceProvider)).ToList();
    }

    /// <summary>
    /// Deletes the entity.
    /// </summary>
    /// <param name="id">The identifier.</param>
    public virtual async Task DeleteAsync(int id)
    {
        var entity = await Repository.GetByIdAsync(id);
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
        var entities = await Repository.GetByIdsAsync(ids);

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
    /// Executes before an entity is added.
    /// </summary>
    /// <param name="entity">The entity.</param>
    protected virtual async Task BeforeAddAsync(TEntity entity)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Executes after an entity has been added.
    /// </summary>
    /// <param name="entity">The entity.</param>
    protected virtual async Task AfterAddAsync(TEntity entity)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Executes before an entity is updated.
    /// </summary>
    /// <param name="entity">The entity.</param>
    protected virtual async Task BeforeUpdateAsync(TEntity entity)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Executes after an entity has been updated.
    /// </summary>
    /// <param name="entity">The entity.</param>
    protected virtual async Task AfterUpdateAsync(TEntity entity)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Executes before an entity is saved.
    /// </summary>
    /// <param name="entity">The entity.</param>
    protected virtual async Task BeforeSaveAsync(TEntity entity)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Executes after an entity has been saved.
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