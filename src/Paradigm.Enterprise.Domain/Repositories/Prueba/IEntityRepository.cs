using Paradigm.Enterprise.Domain.Dtos;
using Paradigm.Enterprise.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paradigm.Enterprise.Domain.Repositories.Prueba;

public interface IEntityRepository<TEntity, TView> : IRepository
    where TEntity : EntityBase
{
    /// <summary>
    /// Executes the search using the specified parameters.
    /// </summary>
    /// <typeparam name="TParameters">The type of the parameters.</typeparam>
    /// <param name="parameters">The parameters.</param>
    /// <returns></returns>
    Task<PaginatedResultDto<TView>> SearchAsync<TParameters>(TParameters parameters)
        where TParameters : PaginationParametersBase;

    Task<TView?> GetViewByIdAsync(int id);
    Task<IEnumerable<TView>> GetViewsByIdsAsync(IEnumerable<int> ids);
    Task<TEntity?> GetEntityByIdAsync(int id);
    Task<IEnumerable<TEntity>> GetEntitiesByIdsAsync(IEnumerable<int> ids);
    Task<bool> ExistsEntityAsync(int id);
    Task<TEntity> AddAsync(TEntity entity);
    Task AddAsync(IEnumerable<TEntity> entities);
    Task<TEntity> UpdateAsync(TEntity entity);
    Task UpdateAsync(IEnumerable<TEntity> entities);
    Task DeleteAsync(int id);
    Task DeleteAsync(TEntity entity);
    Task DeleteAsync(IEnumerable<TEntity> entities);
}