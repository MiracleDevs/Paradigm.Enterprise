using Paradigm.Enterprise.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paradigm.Enterprise.Domain.Repositories.Prueba;

public interface IEntityRepository<TEntity> : IRepository
    where TEntity : EntityBase
{
    Task<TEntity?> GetByIdAsync(int id);
    Task<IEnumerable<TEntity>> GetByIdsAsync(IEnumerable<int> ids);
    Task<bool> ExistsEntityAsync(int id);
    Task<TEntity> AddAsync(TEntity entity);
    Task AddAsync(IEnumerable<TEntity> entities);
    Task<TEntity> UpdateAsync(TEntity entity);
    Task UpdateAsync(IEnumerable<TEntity> entities);
    Task DeleteAsync(TEntity entity);
    Task DeleteAsync(IEnumerable<TEntity> entities);
}