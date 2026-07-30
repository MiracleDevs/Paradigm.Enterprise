using Paradigm.Enterprise.Domain.Entities;

namespace Paradigm.Enterprise.Domain.Repositories;

/// <summary>
/// Defines staged add, update, and delete operations for an entity type.
/// </summary>
/// <typeparam name="TEntity">The entity managed by the repository.</typeparam>
/// <typeparam name="TId">The value type used for entity identifiers.</typeparam>
/// <remarks>
/// These methods stage changes in the backing persistence context. Call
/// <c>IUnitOfWork.CommitChangesAsync</c> to persist them.
/// </remarks>
/// <example>
/// Stage several changes and commit them together through the scoped unit of work:
/// <code>
/// Order? order = await repository.GetByIdAsync(orderId);
/// if (order is null)
///     return;
///
/// order.ChangeShippingAddress(address);
/// await repository.UpdateAsync(order);
///
/// await auditRepository.AddAsync(new OrderAudit
/// {
///     OrderId = order.Id,
///     Message = "Shipping address changed"
/// });
///
/// await unitOfWork.CommitChangesAsync();
/// </code>
/// Repository calls do not guarantee durability by themselves. If all changes must be atomic, create
/// and explicitly commit an <c>ITransaction</c> as shown in the <c>UnitOfWork</c> documentation.
/// </example>
public interface IEditRepository<TEntity, TId> : IReadRepository<TEntity, TId>
    where TEntity : EntityBase<TId>
    where TId : struct, IEquatable<TId>
{
    /// <summary>
    /// Stages a new entity for insertion.
    /// </summary>
    /// <param name="entity">The entity to stage.</param>
    /// <returns>The entity staged for addition.</returns>
    Task<TEntity> AddAsync(TEntity entity);

    /// <summary>
    /// Stages a sequence of new entities for insertion.
    /// </summary>
    /// <param name="entities">The entities to stage.</param>
    /// <returns>A task that completes when all entities have been staged.</returns>
    Task AddAsync(IEnumerable<TEntity> entities);

    /// <summary>
    /// Stages an existing entity for update.
    /// </summary>
    /// <param name="entity">The entity to stage.</param>
    /// <returns>The entity staged for update.</returns>
    Task<TEntity> UpdateAsync(TEntity entity);

    /// <summary>
    /// Stages a sequence of existing entities for update.
    /// </summary>
    /// <param name="entities">The entities to stage.</param>
    /// <returns>A task that completes when all entities have been staged.</returns>
    Task UpdateAsync(IEnumerable<TEntity> entities);

    /// <summary>
    /// Stages an entity for deletion.
    /// </summary>
    /// <param name="entity">The entity to stage for deletion.</param>
    /// <returns>A task that completes when the entity has been staged for deletion.</returns>
    Task DeleteAsync(TEntity entity);

    /// <summary>
    /// Stages a sequence of entities for deletion.
    /// </summary>
    /// <param name="entities">The entities to stage for deletion.</param>
    /// <returns>A task that completes when all entities have been staged for deletion.</returns>
    Task DeleteAsync(IEnumerable<TEntity> entities);
}
