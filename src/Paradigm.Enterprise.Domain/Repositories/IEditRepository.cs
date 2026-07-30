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
public interface IEditRepository<TEntity, TId> : IReadRepository<TEntity, TId>
    where TEntity : EntityBase<TId>
    where TId : struct, IEquatable<TId>
{
    /// <summary>
    /// Adds a new entity.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns>The entity staged for addition.</returns>
    Task<TEntity> AddAsync(TEntity entity);

    /// <summary>
    /// Adds the new entities.
    /// </summary>
    /// <param name="entities">The entities.</param>
    /// <returns>A task that completes when all entities have been staged.</returns>
    Task AddAsync(IEnumerable<TEntity> entities);

    /// <summary>
    /// Updates the entity.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns>The entity staged for update.</returns>
    Task<TEntity> UpdateAsync(TEntity entity);

    /// <summary>
    /// Updates the entities.
    /// </summary>
    /// <param name="entities">The entities.</param>
    /// <returns>A task that completes when all entities have been staged.</returns>
    Task UpdateAsync(IEnumerable<TEntity> entities);

    /// <summary>
    /// Deletes the entity.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns>A task that completes when the entity has been staged for deletion.</returns>
    Task DeleteAsync(TEntity entity);

    /// <summary>
    /// Deletes the entities.
    /// </summary>
    /// <param name="entities">The entities.</param>
    /// <returns>A task that completes when all entities have been staged for deletion.</returns>
    Task DeleteAsync(IEnumerable<TEntity> entities);
}
