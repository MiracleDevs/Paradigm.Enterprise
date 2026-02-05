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

#region OPCION 1 QUE NO ME GUSTA
//--> Ineritance from both interfaces is not possible because IEditRepository inherits from IReadRepository and generates compilation error (CS0695)
public interface IJoinRepository<TEntity, TView> : IRepository
    where TEntity : EntityBase
    where TView : EntityBase
{
    // Read operations (formerly in ViewRepository)
    Task<TView?> GetViewByIdAsync(int id);
    Task<IEnumerable<TView>> GetAllAsync();
    Task<PaginatedResultDto<TView>> SearchViewPaginatedAsync(FilterTextPaginatedParameters parameters);
    Task<IEnumerable<TView>> GetViewsByIdsAsync(IEnumerable<int> ids);

    //Entity methods
    Task<IEnumerable<TEntity>> GetEntityByIdsAsync(IEnumerable<int> ids);
    Task<TEntity?> GetEntityByIdAsync(int id);
    Task<TEntity> AddAsync(TEntity entity);
    Task AddAsync(IEnumerable<TEntity> entities);
    Task<TEntity> UpdateAsync(TEntity entity);
    Task UpdateAsync(IEnumerable<TEntity> entities);
    Task DeleteAsync(TEntity entity);
    Task DeleteAsync(IEnumerable<TEntity> entities);
}

public abstract class EntityViewRepositoryBase<TEntity, TView, TContext> : RepositoryBase<TContext>, IJoinRepository<TEntity, TView>
    where TEntity : EntityBase
    where TView : EntityBase
    where TContext : DbContextBase
{
    protected EntityViewRepositoryBase(IServiceProvider serviceProvider) : base(serviceProvider)
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

    public async Task DeleteAsync(IEnumerable<TEntity> entities)
    {
        GetDbSet().RemoveRange(entities);
        await Task.CompletedTask;
    }

    public virtual async Task<TEntity> UpdateAsync(TEntity entity)
    {
        DeleteRemovedAggregates(entity);
        GetDbSet().Update(entity);
        return await Task.FromResult(entity);
    }

    public async Task UpdateAsync(IEnumerable<TEntity> entities)
    {
        foreach (var entity in entities)
            DeleteRemovedAggregates(entity);

        GetDbSet().UpdateRange(entities);
    }

    public async Task<IEnumerable<TEntity>> GetEntityByIdsAsync(IEnumerable<int> ids) => await GetDbSet().Where(x => ids.Contains(x.Id)).ToListAsync();

    public async Task<TEntity?> GetEntityByIdAsync(int id) => await GetDbSet().FirstOrDefaultAsync(x=> x.Id == id);


    /*******************/

    /// <summary>
    /// Gets all the entities.
    /// </summary>
    /// <returns></returns>
    public async Task<IEnumerable<TView>> GetAllAsync() => await AsQueryable().ToListAsync();

    /// <summary>
    /// Gets the entity by identifier.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <returns></returns>
    public virtual async Task<TView?> GetViewByIdAsync(int id) => await AsQueryable().FirstOrDefaultAsync(x => x.Id == id);

    public async Task<IEnumerable<TView>> GetViewsByIdsAsync(IEnumerable<int> ids) => await AsQueryable().Where(x => ids.Contains(x.Id)).ToListAsync();
    
    public Task<PaginatedResultDto<TView>> SearchViewPaginatedAsync(FilterTextPaginatedParameters parameters) => throw new NotImplementedException();

    #region Protected Methods

    /// <summary>
    /// Gets the database set.
    /// </summary>
    /// <returns></returns>
    //Dont put in ReadRepositoryBase because generates a Warning in EditRepositoryBase becasuse the inheritance
    protected virtual DbSet<TEntity> GetDbSet() => EntityContext.Set<TEntity>();

    /// <summary>
    /// Gets the entity set as queryable.
    /// </summary>
    /// <returns></returns>
    protected virtual IQueryable<TView> AsQueryable() => EntityContext.Set<TView>();

    /// <summary>
    /// Removes an aggregated entity from the database context.
    /// This method allows aggregate root repositories to delete child entities
    /// without requiring separate repositories, maintaining aggregate boundaries.
    /// If the entity is null, no operation is performed.
    /// </summary>
    /// <typeparam name="TAggregatedEntity">The type of the aggregated entity.</typeparam>
    /// <param name="entity">The aggregated entity to remove.</param>
    protected void RemoveAggregate<TAggregatedEntity>(TAggregatedEntity? entity)
        where TAggregatedEntity : EntityBase
    {
        if (entity is null)
            return;

        EntityContext.Set<TAggregatedEntity>().Remove(entity);
    }

    /// <summary>
    /// Removes multiple aggregated entities from the database context.
    /// This method allows aggregate root repositories to delete child entities
    /// without requiring separate repositories, maintaining aggregate boundaries.
    /// If the collection is null or empty, no operation is performed.
    /// </summary>
    /// <typeparam name="TAggregatedEntity">The type of the aggregated entity.</typeparam>
    /// <param name="entities">The aggregated entities to remove.</param>
    protected void RemoveAggregate<TAggregatedEntity>(IEnumerable<TAggregatedEntity>? entities)
        where TAggregatedEntity : EntityBase
    {
        if (entities is null)
            return;

        var entityList = entities.ToList();
        if (entityList.Count == 0)
            return;

        EntityContext.Set<TAggregatedEntity>().RemoveRange(entityList);
    }

    protected virtual void DeleteRemovedAggregates(TEntity entity)
    {
    }

    #endregion
}

#endregion

#region OPCION 2, QUE VA CON POLÉMICA POR USO DE AsNoTracking() pensando en performance (debatible)

public abstract class EntityRepositoryBase<TEntity, TContext> : RepositoryBase<TContext>, IEntityRepository<TEntity>
    where TEntity : EntityBase
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


    public async Task<IEnumerable<TEntity>> GetByIdsAsync(IEnumerable<int> ids)
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

    public async Task<TEntity?> GetByIdAsync(int id)
    {
        return await GetDbSet()
             .AsNoTracking()
             .FirstOrDefaultAsync(x => x.Id == id);
    }


    /// <summary>
    /// Gets the database set.
    /// </summary>
    /// <returns></returns>
    protected virtual DbSet<TEntity> GetDbSet() => EntityContext.Set<TEntity>();

    protected virtual void DeleteRemovedAggregates(TEntity entity)
    {
    }
}

#endregion