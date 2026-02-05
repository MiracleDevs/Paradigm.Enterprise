using Paradigm.Enterprise.Data.Repositories;
using Paradigm.Enterprise.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paradigm.Enterprise.Providers.Prueba;

public interface IEntityProvider<TEntity>
    where TEntity : EntityBase
{
    Task<IEnumerable<TEntity>> GetByIdsAsync(IEnumerable<int> ids);
    Task<TEntity?> GetByIdAsync(int id);
    Task<TEntity> AddAsync(TEntity entity);
    Task<IEnumerable<TEntity>> AddAsync(List<TEntity> entitys);
    Task<TEntity> UpdateAsync(TEntity entity);
    Task<IEnumerable<TEntity>> UpdateAsync(List<TEntity> entities);
    Task DeleteAsync(int id);
    Task DeleteAsync(IEnumerable<int> ids);
}
