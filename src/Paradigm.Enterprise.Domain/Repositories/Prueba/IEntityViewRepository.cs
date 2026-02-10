using Paradigm.Enterprise.Domain.Entities;

namespace Paradigm.Enterprise.Domain.Repositories.Prueba;

public interface IEntityViewRepository<TEntity, TView> : IReadRepository<TView>
    where TEntity : EntityBase
    where TView : EntityBase
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