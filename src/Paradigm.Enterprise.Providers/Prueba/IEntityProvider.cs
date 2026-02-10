using Paradigm.Enterprise.Data.Repositories;
using Paradigm.Enterprise.Domain.Dtos;
using Paradigm.Enterprise.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paradigm.Enterprise.Providers.Prueba;

public interface IEntityProvider<TEntity, TView>
    where TEntity : EntityBase
    where TView : EntityBase
{
    Task<PaginatedResultDto<TView>> SearchAsync<TParameters>(TParameters parameters) where TParameters : PaginationParametersBase;
    Task<TView> SaveAsync(TView view);
    Task<IEnumerable<TEntity>> GetEntitiesByIdsAsync(IEnumerable<int> ids);
    Task<IEnumerable<TView>> GetViewsByIdsAsync(IEnumerable<int> ids);
    Task<TView?> GetViewByIdAsync(int id);
    Task<TEntity?> GetEntityByIdAsync(int id);
    Task<TView> AddAsync(TView view);
    Task<IEnumerable<TView>> AddAsync(List<TView> entitys);
    Task<TView> UpdateAsync(TView entity);
    Task<IEnumerable<TView>> UpdateAsync(List<TView> entities);

    Task DeleteAsync(int id);
    Task DeleteAsync(IEnumerable<int> ids);
}
