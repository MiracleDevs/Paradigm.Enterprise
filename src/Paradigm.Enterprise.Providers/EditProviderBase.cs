using Microsoft.Extensions.DependencyInjection;
using Paradigm.Enterprise.Domain.Entities;
using Paradigm.Enterprise.Domain.Repositories;
using Paradigm.Enterprise.Domain.Uow;
using Paradigm.Enterprise.Providers.Exceptions;

namespace Paradigm.Enterprise.Providers;

/// <summary>
/// Coordinates mapping, validation, repository changes, commits, and lifecycle hooks for editable views.
/// </summary>
/// <typeparam name="TInterface">The interface shared by entity and view.</typeparam>
/// <typeparam name="TEntity">The persisted domain entity.</typeparam>
/// <typeparam name="TView">The application-facing view model.</typeparam>
/// <typeparam name="TRepository">The editable entity repository.</typeparam>
/// <typeparam name="TViewRepository">The repository used to return views.</typeparam>
/// <typeparam name="TId">The value type used for identifiers.</typeparam>
/// <remarks>
/// For add and update operations, the view hooks run before mapping; the entity hooks run after
/// mapping and validation but before repository staging. The unit of work then commits once before
/// the after-save and operation-specific after hooks run. For deletes, <see cref="BeforeDeleteAsync"/>
/// runs before repository staging, <see cref="AfterDeleteAsync"/> runs after staging, and the unit
/// of work commits last.
/// </remarks>
public abstract class EditProviderBase<TInterface, TEntity, TView, TRepository, TViewRepository, TId>
    : ReadProviderBase<TInterface, TView, TViewRepository, TId>, IEditProvider<TView, TId>
    where TId : struct, IEquatable<TId>
    where TInterface : Interfaces.IEntity<TId>
    where TEntity : EntityBase<TId, TInterface, TEntity, TView>, TInterface, new()
    where TView : EntityBase<TId>, TInterface, new()
    where TRepository : IEditRepository<TEntity, TId>
    where TViewRepository : IReadRepository<TView, TId>
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
    /// Initializes a new instance of the <see cref="EditProviderBase{TInterface, TEntity, TView, TRepository, TViewRepository, TId}"/> class.
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
    /// <returns>The committed view loaded back from the view repository.</returns>
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
    /// <returns>The committed views mapped from the added entities.</returns>
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
    /// <returns>The committed view loaded back from the view repository.</returns>
    /// <exception cref="NotFoundException">The view does not identify an accessible entity.</exception>
    public virtual async Task<TView> UpdateAsync(TView view)
    {
        var entity = await Repository.GetByIdAsync(view.Id)
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
    /// <returns>The committed views mapped from the updated entities.</returns>
    /// <exception cref="NotFoundException">A view does not identify an accessible entity.</exception>
    public virtual async Task<IEnumerable<TView>> UpdateAsync(List<TView> views)
    {
        var entities = new List<TEntity>();

        foreach (var view in views)
        {
            var entity = await Repository.GetByIdAsync(view.Id)
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
    /// Adds or updates the entity.
    /// </summary>
    /// <param name="view">The dto.</param>
    /// <returns>The view returned by the selected add or update operation.</returns>
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
    /// <returns>The committed views mapped from the added and updated entities.</returns>
    /// <exception cref="NotFoundException">An existing view does not identify an accessible entity.</exception>
    public virtual async Task<IEnumerable<TView>> SaveAsync(IEnumerable<TView> views)
    {
        var entities = new List<(bool, TEntity)>();

        foreach (var view in views)
        {
            var isNew = view.IsNew();
            TEntity entity = isNew
                ? ServiceProvider.GetRequiredService<TEntity>()
                : (await Repository.GetByIdAsync(view.Id)
                    ?? throw new NotFoundException("Entity not found or you don't have the permissions to open it."));

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
    public virtual async Task DeleteAsync(TId id)
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
    public virtual async Task DeleteAsync(IEnumerable<TId> ids)
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
    /// Executed on add operation before the <typeparamref name="TView"/> is mapped to <typeparamref name="TEntity"/>.
    /// </summary>
    /// <param name="view">The view.</param>
    protected virtual async Task BeforeAddAsync(TView view)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Executed on add operation after the <typeparamref name="TEntity"/> is mapped from <typeparamref name="TView"/>.
    /// </summary>
    /// <param name="entity">The entity.</param>
    protected virtual async Task BeforeAddAsync(TEntity entity)
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
    /// Executed on update operation before the <typeparamref name="TView"/> is mapped to <typeparamref name="TEntity"/>.
    /// </summary>
    /// <param name="view">The view.</param>
    protected virtual async Task BeforeUpdateAsync(TView view)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Executed on update operation after the <typeparamref name="TEntity"/> is mapped from <typeparamref name="TView"/>.
    /// </summary>
    /// <param name="entity">The entity.</param>
    protected virtual async Task BeforeUpdateAsync(TEntity entity)
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
    /// Executed on save operation before the <typeparamref name="TView"/> is mapped to <typeparamref name="TEntity"/>.
    /// </summary>
    /// <param name="view">The view.</param>
    protected virtual async Task BeforeSaveAsync(TView view)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Executed on save operation after the <typeparamref name="TEntity"/> is mapped from <typeparamref name="TView"/>.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <example>
    /// A derived provider can stamp an entity immediately before it is staged:
    /// <code>
    /// protected override Task BeforeSaveAsync(Order entity)
    /// {
    ///     entity.ModifiedAt = DateTimeOffset.UtcNow;
    ///     return Task.CompletedTask;
    /// }
    /// </code>
    /// </example>
    protected virtual async Task BeforeSaveAsync(TEntity entity)
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
    /// Executes after deletion is staged in the repository and before the unit of work commits.
    /// </summary>
    /// <param name="entity">The entity.</param>
    protected virtual async Task AfterDeleteAsync(TEntity entity)
    {
        await Task.CompletedTask;
    }

    #endregion
}
