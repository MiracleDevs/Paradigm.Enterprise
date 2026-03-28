using Paradigm.Enterprise.Domain.Entities;
using Paradigm.Enterprise.Interfaces;

namespace Paradigm.Enterprise.Domain.Repositories;

public interface IEntityViewRepository<TInterface, TEntity, TView> : IReadRepository<TView>
    where TInterface : IEntity
    where TEntity : EntityBase<TInterface, TEntity, TView>, TInterface, new()
    where TView : EntityBase, TInterface, new()
{
    Task<TEntity?> GetEntityByIdAsync(int id);
    Task<IEnumerable<TEntity>> GetEntitiesByIdsAsync(IEnumerable<int> ids);
    Task<bool> ExistsEntityAsync(int id);
    Task<TEntity> AddAsync(TEntity entity);
    Task AddAsync(IEnumerable<TEntity> entities);
    Task<TEntity> UpdateAsync(TEntity entity);
    Task UpdateAsync(IEnumerable<TEntity> entities);
    Task DeleteAsync(int id);
    Task DeleteAsync(IEnumerable<int> ids);
    Task DeleteAsync(TEntity entity);
    Task DeleteAsync(IEnumerable<TEntity> entities);
}