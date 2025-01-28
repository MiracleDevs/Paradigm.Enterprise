using Paradigm.Enterprise.Domain.Entities;

namespace Paradigm.Enterprise.Providers;

public interface IEditProvider<TView> : IReadProvider<TView>
    where TView : EntityBase, new()
{
    /// <summary>
    /// Adds a new entity.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns></returns>
    Task<TView> AddAsync(TView entity);

    /// <summary>
    /// Adds a new entities.
    /// </summary>
    /// <param name="dtos">The dtos.</param>
    /// <returns></returns>
    Task<IEnumerable<TView>> AddAsync(List<TView> dtos);

    /// <summary>
    /// Updates the entity.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns></returns>
    Task<TView> UpdateAsync(TView entity);

    /// <summary>
    /// Updates a new entities.
    /// </summary>
    /// <param name="dtos">The dtos.</param>
    /// <returns></returns>
    Task<IEnumerable<TView>> UpdateAsync(List<TView> dtos);

    /// <summary>
    /// Adds or updates the entity.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns></returns>
    Task<TView> SaveAsync(TView entity);

    /// <summary>
    /// Saves the entities.
    /// </summary>
    /// <param name="dtos">The dtos.</param>
    /// <returns></returns>
    Task<IEnumerable<TView>> SaveAsync(IEnumerable<TView> dtos);

    /// <summary>
    /// Deletes the entity.
    /// </summary>
    /// <param name="id">The identifier.</param>
    Task DeleteAsync(int id);

    /// <summary>
    /// Deletes the entities.
    /// </summary>
    /// <param name="ids">The ids.</param>
    /// <returns></returns>
    Task DeleteAsync(IEnumerable<int> ids);
}