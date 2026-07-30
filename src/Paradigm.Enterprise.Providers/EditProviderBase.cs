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
/// mapping and validation but before repository staging. The unit of work's
/// <see cref="ICommiteable.CommitChangesAsync"/> method then runs once before
/// the after-save and operation-specific after hooks run. For deletes, <see cref="BeforeDeleteAsync"/>
/// runs before repository staging, <see cref="AfterDeleteAsync"/> runs after staging, and the unit
/// of work save runs last.
///
/// <para>
/// Repository methods stage work; <see cref="ICommiteable.CommitChangesAsync"/> is the participant-save
/// boundary. A failure before or during that save prevents the post-save add/update hooks from running.
/// A failure in an add/update post hook occurs after participant saves have completed. This class does
/// not undo those saves, but an active external transaction may still commit or roll them back.
/// Delete post hooks are different: <see cref="AfterDeleteAsync"/> runs before the unit-of-work save,
/// so an exception from that hook prevents the provider from invoking it.
/// </para>
/// <para>
/// The bulk overloads call <see cref="ICommiteable.CommitChangesAsync"/> once for the complete batch.
/// <see cref="SaveAsync(IEnumerable{TView})"/>
/// invokes only entity hooks; unlike the dedicated add and update overloads, it does not invoke the
/// view overloads of <see cref="BeforeSaveAsync(TView)"/>, <see cref="BeforeAddAsync(TView)"/>, or
/// <see cref="BeforeUpdateAsync(TView)"/>. Single add and update operations reload the returned view
/// through <c>GetByIdAsync</c>; bulk operations map saved entities directly with
/// <see cref="EntityBase{TId,TInterface,TEntity,TView}.MapTo"/>.
/// </para>
/// </remarks>
/// <example>
/// A provider commonly stamps audit information in an entity hook and publishes notifications from
/// a post-save hook:
/// <code>
/// public interface IOrderProvider : IEditProvider&lt;OrderView, Guid&gt;
/// {
/// }
///
/// public sealed class OrderProvider
///     : EditProviderBase&lt;IOrderModel, Order, OrderView,
///         IOrderRepository, IOrderViewRepository, Guid&gt;,
///       IOrderProvider
/// {
///     private readonly ILoggedUserService&lt;Guid&gt; users;
///     private readonly IOrderNotifications notifications;
///
///     public OrderProvider(
///         IServiceProvider services,
///         ILoggedUserService&lt;Guid&gt; users,
///         IOrderNotifications notifications)
///         : base(services)
///     {
///         this.users = users;
///         this.notifications = notifications;
///     }
///
///     protected override Task BeforeSaveAsync(Order entity)
///     {
///         Guid? userId = users.TryGetAuthenticatedUser&lt;User&gt;()?.Id;
///         entity.Audit(userId);
///         return Task.CompletedTask;
///     }
///
///     protected override Task AfterAddAsync(Order entity)
///     {
///         // CommitChangesAsync has completed, but an external transaction may still be active.
///         return notifications.OrderCreatedAsync(entity.Id);
///     }
/// }
///
/// services.AddTransient&lt;Order&gt;(); // Add operations resolve a new entity from DI.
/// services.AddTransient&lt;OrderView&gt;();
/// services.AddScoped&lt;IOrderRepository, OrderRepository&gt;();
/// services.AddScoped&lt;IOrderViewRepository, OrderViewRepository&gt;();
/// services.AddScoped&lt;IOrderProvider, OrderProvider&gt;();
///
/// OrderView created = await provider.AddAsync(new OrderView
/// {
///     Number = "SO-1042"
/// });
///
/// created.Number = "SO-1042-REV1";
/// OrderView updated = await provider.SaveAsync(created); // Non-default Id selects update.
/// </code>
/// Do not publish irreversible external effects directly from an after hook when an external
/// transaction can still roll back. Use an outbox saved in the same transaction, or publish only
/// after the transaction owner commits.
/// </example>
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
    /// Gets the editable entity repository resolved from the current service scope.
    /// </summary>
    /// <value>
    /// The repository used to stage entity additions, updates, and deletions.
    /// </value>
    protected TRepository Repository { get; }

    /// <summary>
    /// Gets the unit of work that saves changes staged by <see cref="Repository"/>.
    /// </summary>
    /// <value>
    /// The service-scope unit of work. Its lifetime is owned by dependency injection.
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
    /// Maps, validates, stages, and saves a new entity.
    /// </summary>
    /// <param name="view">The new view to map. Its identifier is normally the default value.</param>
    /// <returns>The saved view loaded back from the view repository.</returns>
    /// <remarks>
    /// Hook order is: view <c>BeforeAdd</c>, view <c>BeforeSave</c>, mapping, validation,
    /// entity <c>BeforeAdd</c>, entity <c>BeforeSave</c>, repository staging,
    /// <c>CommitChangesAsync</c>,
    /// entity <c>AfterSave</c>, entity <c>AfterAdd</c>, then reload.
    /// </remarks>
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
    /// Maps, validates, stages, and saves a batch of new entities.
    /// </summary>
    /// <param name="views">The new views to add in input order.</param>
    /// <returns>The saved views mapped from the added entities.</returns>
    /// <remarks>
    /// Each view is processed and staged before one <c>CommitChangesAsync</c> call. Post-save hooks
    /// then run in input order. The returned views come directly from each entity's <c>MapTo</c>
    /// implementation and are not reloaded from the view repository. If processing a later item fails,
    /// earlier items remain staged in the scoped context; a later save on that context can persist them.
    /// </remarks>
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
    /// Loads, maps, validates, stages, and saves an existing entity.
    /// </summary>
    /// <param name="view">The view whose identifier selects the entity to update.</param>
    /// <returns>The saved view loaded back from the view repository.</returns>
    /// <exception cref="NotFoundException">The view does not identify an accessible entity.</exception>
    /// <remarks>
    /// Hook order is: load, view <c>BeforeUpdate</c>, view <c>BeforeSave</c>, mapping, validation,
    /// entity <c>BeforeUpdate</c>, entity <c>BeforeSave</c>, repository staging,
    /// <c>CommitChangesAsync</c>,
    /// entity <c>AfterSave</c>, entity <c>AfterUpdate</c>, then reload.
    /// When the repository returns a tracked entity, mapping mutates it before explicit update staging.
    /// A mapping, validation, or hook failure can therefore leave changes tracked for a later save.
    /// </remarks>
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
    /// Loads, maps, validates, stages, and saves a batch of existing entities.
    /// </summary>
    /// <param name="views">The views to update in input order.</param>
    /// <returns>The saved views mapped from the updated entities.</returns>
    /// <exception cref="NotFoundException">A view does not identify an accessible entity.</exception>
    /// <remarks>
    /// All entities are staged before one <c>CommitChangesAsync</c> call. If any identifier cannot be
    /// loaded, this method throws before invoking that save. Earlier items remain staged in the scoped
    /// context and can be persisted by a later save. Returned views are mapped from the saved entities
    /// rather than reloaded through the view repository. A loaded tracked entity is mutated during
    /// mapping, so even a validation or pre-staging hook failure can leave changes pending.
    /// </remarks>
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
    /// Adds or updates one view according to <see cref="EntityBase.IsNew"/>.
    /// </summary>
    /// <param name="view">The view to add when its identifier is default, or update otherwise.</param>
    /// <returns>The view returned by the selected add or update operation.</returns>
    public virtual async Task<TView> SaveAsync(TView view)
    {
        return view.IsNew() ?
            await AddAsync(view) :
            await UpdateAsync(view);
    }

    /// <summary>
    /// Adds or updates a batch of views and asks the unit of work to save the complete batch once.
    /// </summary>
    /// <param name="views">The views to classify and save in enumeration order.</param>
    /// <returns>The saved views mapped from the added and updated entities.</returns>
    /// <exception cref="NotFoundException">An existing view does not identify an accessible entity.</exception>
    /// <remarks>
    /// This overload deliberately invokes entity hooks only. It does not call the view overloads of
    /// the before-add, before-update, or before-save hooks. Do not place required batch validation
    /// exclusively in a view hook. Each return value is mapped directly from its saved entity. If a
    /// later item fails, earlier items remain staged in the scoped context and can be persisted by a
    /// later save. Existing tracked entities are mutated during mapping, so their changes can also
    /// remain pending when validation or a hook fails before explicit update staging.
    /// </remarks>
    /// <example>
    /// <code>
    /// var batch = new[]
    /// {
    ///     new OrderView { Number = "SO-1043" },              // default Id: add
    ///     new OrderView { Id = existingId, Number = "SO-7" } // assigned Id: update
    /// };
    ///
    /// IEnumerable&lt;OrderView&gt; saved = await provider.SaveAsync(batch);
    /// // Both repository operations were staged before a single CommitChangesAsync call.
    /// </code>
    /// </example>
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
    /// Stages and saves deletion of the entity when the identifier is found.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <remarks>
    /// A missing entity is a no-op and does not save. For an existing entity, both delete hooks run
    /// before <c>CommitChangesAsync</c>; despite its name, <see cref="AfterDeleteAsync"/> means after repository
    /// staging, not after durable persistence. If that hook fails, the deletion remains staged in the
    /// scoped context and a later save can still persist it.
    /// </remarks>
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
    /// Stages deletion of all matching entities and invokes <c>CommitChangesAsync</c> once.
    /// </summary>
    /// <param name="ids">The identifiers.</param>
    /// <remarks>
    /// Missing identifiers are omitted according to repository behavior. The unit-of-work save is
    /// invoked once even when the repository returns no matching entities. A hook failure stops later
    /// entities, skips this method's save, and leaves earlier deletions staged for a possible later save.
    /// </remarks>
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
    /// Runs before an add view is mapped to a new <typeparamref name="TEntity"/>.
    /// </summary>
    /// <param name="view">The view.</param>
    protected virtual async Task BeforeAddAsync(TView view)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Runs after mapping and validation during add, before the entity is staged.
    /// </summary>
    /// <param name="entity">The entity.</param>
    protected virtual async Task BeforeAddAsync(TEntity entity)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Runs after <c>CommitChangesAsync</c> has completed for an added entity.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <remarks>
    /// This class does not undo completed participant saves when this hook throws. An active external
    /// transaction may still be committed or rolled back by its owner. In a bulk operation, a failure
    /// stops post-save hooks for later entities.
    /// </remarks>
    protected virtual async Task AfterAddAsync(TEntity entity)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Runs before an update view is mapped into its loaded <typeparamref name="TEntity"/>.
    /// </summary>
    /// <param name="view">The view.</param>
    protected virtual async Task BeforeUpdateAsync(TView view)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Runs after mapping and validation during update, before the entity is staged.
    /// </summary>
    /// <param name="entity">The entity.</param>
    protected virtual async Task BeforeUpdateAsync(TEntity entity)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Runs after <c>CommitChangesAsync</c> has completed for an updated entity.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <remarks>
    /// This class does not undo completed participant saves when this hook throws. An active external
    /// transaction may still be committed or rolled back by its owner. In a bulk operation, a failure
    /// stops post-save hooks for later entities.
    /// </remarks>
    protected virtual async Task AfterUpdateAsync(TEntity entity)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Runs before a view is mapped during the dedicated add and update overloads.
    /// </summary>
    /// <param name="view">The view.</param>
    /// <remarks>
    /// The batch <see cref="SaveAsync(IEnumerable{TView})"/> overload does not invoke this view hook.
    /// </remarks>
    protected virtual async Task BeforeSaveAsync(TView view)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Runs after mapping and validation and before the entity is staged.
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
    /// Runs after <c>CommitChangesAsync</c> has completed for an added or updated entity.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <remarks>
    /// This class does not undo completed participant saves when this hook throws. An active external
    /// transaction may still be committed or rolled back by its owner. If this hook throws, the
    /// operation-specific <see cref="AfterAddAsync(TEntity)"/> or <see cref="AfterUpdateAsync(TEntity)"/>
    /// hook is not invoked. In a bulk operation, a failure also stops hooks for later entities even
    /// though all participant saves have already completed.
    /// </remarks>
    protected virtual async Task AfterSaveAsync(TEntity entity)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Runs before an entity deletion is staged.
    /// </summary>
    /// <param name="entity">The entity.</param>
    protected virtual async Task BeforeDeleteAsync(TEntity entity)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Executes after deletion is staged in the repository and before the unit-of-work save.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <remarks>An exception prevents this provider from invoking <c>CommitChangesAsync</c>.</remarks>
    protected virtual async Task AfterDeleteAsync(TEntity entity)
    {
        await Task.CompletedTask;
    }

    #endregion
}
