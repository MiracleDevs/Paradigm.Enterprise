using Paradigm.Enterprise.Domain.Entities;

namespace Paradigm.Enterprise.Providers;

/// <summary>
/// Defines application-facing creation, update, save, and deletion operations for a view model.
/// </summary>
/// <typeparam name="TView">The view model accepted and returned by the provider.</typeparam>
/// <typeparam name="TId">The value type used for identifiers.</typeparam>
/// <remarks>
/// Implementations decide their transaction and mapping boundaries. The standard
/// <see cref="EditProviderBase{TInterface,TEntity,TView,TRepository,TViewRepository,TId}"/>
/// stages repository changes and calls the unit of work's save method once per operation. When an
/// external transaction is active, those saves remain subject to its later commit or rollback.
/// </remarks>
/// <example>
/// <code>
/// public sealed class OrderApplicationService(IEditProvider&lt;OrderView, Guid&gt; orders)
/// {
///     public Task&lt;OrderView&gt; CreateAsync(OrderView draft) =&gt; orders.AddAsync(draft);
///
///     public Task&lt;OrderView&gt; RenameAsync(OrderView order, string number)
///     {
///         order.Number = number;
///         return orders.UpdateAsync(order);
///     }
///
///     public Task DeleteAsync(Guid id) =&gt; orders.DeleteAsync(id);
/// }
/// </code>
/// </example>
public interface IEditProvider<TView, TId> : IReadProvider<TView, TId>
    where TId : struct, IEquatable<TId>
    where TView : EntityBase<TId>, new()
{
    /// <summary>
    /// Adds a new entity.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns>The saved view, including its assigned identifier.</returns>
    Task<TView> AddAsync(TView entity);

    /// <summary>
    /// Adds a new entities.
    /// </summary>
    /// <param name="dtos">The dtos.</param>
    /// <returns>The saved views in input order.</returns>
    Task<IEnumerable<TView>> AddAsync(List<TView> dtos);

    /// <summary>
    /// Updates the entity.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns>The saved updated view.</returns>
    /// <exception cref="Exceptions.NotFoundException">The view does not identify an accessible entity.</exception>
    Task<TView> UpdateAsync(TView entity);

    /// <summary>
    /// Updates a new entities.
    /// </summary>
    /// <param name="dtos">The dtos.</param>
    /// <returns>The saved updated views in input order.</returns>
    /// <exception cref="Exceptions.NotFoundException">A view does not identify an accessible entity.</exception>
    Task<IEnumerable<TView>> UpdateAsync(List<TView> dtos);

    /// <summary>
    /// Adds or updates the entity.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns>The added or updated saved view.</returns>
    Task<TView> SaveAsync(TView entity);

    /// <summary>
    /// Saves the entities.
    /// </summary>
    /// <param name="dtos">The dtos.</param>
    /// <returns>The added or updated saved views in input order.</returns>
    /// <exception cref="Exceptions.NotFoundException">An existing view does not identify an accessible entity.</exception>
    Task<IEnumerable<TView>> SaveAsync(IEnumerable<TView> dtos);

    /// <summary>
    /// Deletes the entity.
    /// </summary>
    /// <param name="id">The identifier.</param>
    Task DeleteAsync(TId id);

    /// <summary>
    /// Deletes the entities.
    /// </summary>
    /// <param name="ids">The ids.</param>
    /// <returns>
    /// A task that completes after the matching deletions have been saved by the unit-of-work
    /// participants. An active transaction still requires an explicit commit.
    /// </returns>
    Task DeleteAsync(IEnumerable<TId> ids);
}
