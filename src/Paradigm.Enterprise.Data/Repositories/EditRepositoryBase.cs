using Microsoft.EntityFrameworkCore;
using Paradigm.Enterprise.Data.Context;
using Paradigm.Enterprise.Domain.Dtos;
using Paradigm.Enterprise.Domain.Entities;
using Paradigm.Enterprise.Domain.Repositories;

namespace Paradigm.Enterprise.Data.Repositories;

public abstract class EditRepositoryBase<TEntity, TContext> : EditRepositoryBase<TEntity, TContext, FilterTextPaginatedParameters>
     where TEntity : EntityBase
     where TContext : DbContextBase
{
    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="EditRepositoryBase{TEntity, TContext}"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    protected EditRepositoryBase(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    #endregion
}

public abstract class EditRepositoryBase<TEntity, TContext, TParameters> : ReadRepositoryBase<TEntity, TContext, TParameters>, IEditRepository<TEntity, TParameters>
     where TEntity : EntityBase
     where TContext : DbContextBase
     where TParameters : FilterTextPaginatedParameters
{
    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="EditRepositoryBase{TEntity, TContext, TParameters}"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    protected EditRepositoryBase(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Adds a new entity.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns></returns>
    public virtual async Task<TEntity> AddAsync(TEntity entity)
    {
        await GetDbSet().AddAsync(entity);
        return entity;
    }

    /// <summary>
    /// Adds the new entities.
    /// </summary>
    /// <param name="entities">The entities.</param>
    public virtual async Task AddAsync(IEnumerable<TEntity> entities)
    {
        await GetDbSet().AddRangeAsync(entities);
    }

    /// <summary>
    /// Updates the entity.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns></returns>
    public virtual async Task<TEntity> UpdateAsync(TEntity entity)
    {
        await DeleteRemovedAggregatesAsync(entity);
        GetDbSet().Update(entity);
        return await Task.FromResult(entity);
    }

    /// <summary>
    /// Updates the entities.
    /// </summary>
    /// <param name="entities">The entities.</param>
    public async Task UpdateAsync(IEnumerable<TEntity> entities)
    {
        foreach (var entity in entities)
            await DeleteRemovedAggregatesAsync(entity);

        GetDbSet().UpdateRange(entities);
    }

    /// <summary>
    /// Deletes the entity.
    /// </summary>
    /// <param name="entity">The entity.</param>
    public virtual async Task DeleteAsync(TEntity entity)
    {
        GetDbSet().Remove(entity);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Deletes the asynchronous.
    /// </summary>
    /// <param name="entities">The entities.</param>
    public virtual async Task DeleteAsync(IEnumerable<TEntity> entities)
    {
        GetDbSet().RemoveRange(entities);
        await Task.CompletedTask;
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// Gets the database set.
    /// </summary>
    /// <returns></returns>
    protected virtual DbSet<TEntity> GetDbSet() => EntityContext.Set<TEntity>();

    /// <summary>
    /// Deletes the removed aggregates.
    /// </summary>
    protected virtual async Task DeleteRemovedAggregatesAsync(TEntity entity)
    {
        await Task.CompletedTask;
    }

    #endregion
}