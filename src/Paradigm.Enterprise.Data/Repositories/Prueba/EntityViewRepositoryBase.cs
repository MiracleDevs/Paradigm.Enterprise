using Microsoft.EntityFrameworkCore;
using Paradigm.Enterprise.Data.Context;
using Paradigm.Enterprise.Domain.Dtos;
using Paradigm.Enterprise.Domain.Entities;
using Paradigm.Enterprise.Domain.Repositories;
using Paradigm.Enterprise.Domain.Repositories.Prueba;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paradigm.Enterprise.Data.Repositories.Prueba;

#region OPCION 2, QUE VA CON POLÉMICA POR USO DE AsNoTracking() pensando en performance (debatible)

public abstract class EntityRepositoryBase<TEntity, TView, TContext> : ReadRepositoryBase<TView, TContext>, IEntityRepository<TEntity, TView>
    where TEntity : EntityBase
    where TView : EntityBase
    where TContext : DbContextBase
{
    protected EntityRepositoryBase(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public async Task<TEntity> AddAsync(TEntity entity)
    {
        await GetDbSet().AddAsync(entity);
        return entity;
    }

    public async Task AddAsync(IEnumerable<TEntity> entities)
    {
        await GetDbSet().AddRangeAsync(entities);
    }

    public async Task DeleteAsync(TEntity entity)
    {
        GetDbSet().Remove(entity);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(int id)
    {
        var dbset = GetDbSet();
        var item = await dbset.FirstAsync(e => e.Id == id);
        dbset.Remove(item);

        await EntityContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(IEnumerable<TEntity> entities)
    {
        GetDbSet().RemoveRange(entities);
        await Task.CompletedTask;
    }

    public virtual async Task<TEntity> UpdateAsync(TEntity entity)
    {
        DeleteRemovedAggregates(entity);

        var tracked = await GetDbSet().FirstAsync(x => x.Id == entity.Id);

        if (tracked == null)
            throw new Exception("Entity not found"); 

        EntityContext.Entry(tracked).CurrentValues.SetValues(entity);
        await EntityContext.SaveChangesAsync();

        return tracked;
    }

    public async Task UpdateAsync(IEnumerable<TEntity> entities)
    {
        var ids = entities.Select(x => x.Id).ToList();

        var trackedEntities = await GetDbSet()
            .Where(x => ids.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id);

        foreach (var entity in entities)
        {
            DeleteRemovedAggregates(entity);

            if (!trackedEntities.TryGetValue(entity.Id, out var tracked))
                throw new Exception($"Entity {entity.Id} not found");

            EntityContext.Entry(tracked)
                .CurrentValues
                .SetValues(entity);
        }

        await EntityContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<TEntity>> GetEntitiesByIdsAsync(IEnumerable<int> ids)
    {
        return await GetDbSet()
            .Where(x => ids.Contains(x.Id))
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<bool> ExistsEntityAsync(int id)
    {
       return await GetDbSet()
            .AsNoTracking()
            .AnyAsync(x => x.Id == id);
    }

    public async Task<TEntity?> GetEntityByIdAsync(int id)
    {
        return await GetDbSet()
             .AsNoTracking()
             .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<TView?> GetViewByIdAsync(int id)
    {
        return await GetViewDbSet().AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<IEnumerable<TView>> GetViewsByIdsAsync(IEnumerable<int> ids)
    {
        return await GetViewDbSet()
            .Where(x => ids.Contains(x.Id))
            .AsNoTracking()
            .ToListAsync();
    }


    /// <summary>
    /// Gets the database set.
    /// </summary>
    /// <returns></returns>
    protected virtual DbSet<TEntity> GetDbSet() => EntityContext.Set<TEntity>();

    protected virtual DbSet<TView> GetViewDbSet() => EntityContext.Set<TView>();

    protected virtual void DeleteRemovedAggregates(TEntity entity)
    {
    }
}

#endregion