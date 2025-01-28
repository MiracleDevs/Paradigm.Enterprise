using Paradigm.Enterprise.Domain.Entities;

namespace Paradigm.Enterprise.Domain.Repositories;

public interface IEditRepository<TEntity> : IReadRepository<TEntity>
    where TEntity : EntityBase
{
    /// <summary>
    /// Adds a new entity.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns></returns>
    Task<TEntity> AddAsync(TEntity entity);

    /// <summary>
    /// Adds the new entities.
    /// </summary>
    /// <param name="entities">The entities.</param>
    /// <returns></returns>
    Task AddAsync(IEnumerable<TEntity> entities);

    /// <summary>
    /// Updates the entity.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns></returns>
    Task<TEntity> UpdateAsync(TEntity entity);

    /// <summary>
    /// Updates the entities.
    /// </summary>
    /// <param name="entities">The entities.</param>
    /// <returns></returns>
    Task UpdateAsync(IEnumerable<TEntity> entities);

    /// <summary>
    /// Deletes the entity.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns></returns>
    Task DeleteAsync(TEntity entity);

    /// <summary>
    /// Deletes the entities.
    /// </summary>
    /// <param name="entities">The entities.</param>
    /// <returns></returns>
    Task DeleteAsync(IEnumerable<TEntity> entities);
}