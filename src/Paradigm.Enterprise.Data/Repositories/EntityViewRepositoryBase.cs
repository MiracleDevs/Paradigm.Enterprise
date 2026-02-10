using Microsoft.EntityFrameworkCore;
using Paradigm.Enterprise.Data.Context;
using Paradigm.Enterprise.Domain.Entities;
using Paradigm.Enterprise.Domain.Repositories;

namespace Paradigm.Enterprise.Data.Repositories;

public abstract class EntityViewRepositoryBase<TEntity, TView, TContext>
    : ReadRepositoryBase<TView, TContext>, IEntityViewRepository<TEntity, TView>
    where TEntity : EntityBase<Interfaces.IEntity, TEntity, TView>, Interfaces.IEntity, new()
    where TView : EntityBase, Interfaces.IEntity, new()
    where TContext : DbContextBase
{
    protected EntityViewRepositoryBase(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    /// <summary>
    /// Gets an entity by its identifier.
    /// </summary>
    /// <param name="id">Entity identifier.</param>
    /// <returns>The entity if found; otherwise, null.</returns>
    public async Task<TEntity?> GetEntityByIdAsync(int id) => await AsQueryableEntity().FirstOrDefaultAsync(x => x.Id == id);

    /// <summary>
    /// Gets multiple entities by their identifiers.
    /// </summary>
    /// <param name="ids">Collection of entity identifiers.</param>
    /// <returns>Collection of matching entities.</returns>
    public async Task<IEnumerable<TEntity>> GetEntitiesByIdsAsync(IEnumerable<int> ids)
    {
        var idList = ids.ToList();
        if (idList.Count == 0)
            return Enumerable.Empty<TEntity>();

        // Determine chunk size based on database provider limits:
        var chunkSize = GetChunkSize();
        var results = new List<TEntity>();
        var queryable = AsQueryableEntity();

        foreach (var chunk in idList.Chunk(chunkSize))
        {
            var chunkList = chunk.ToList();
            var chunkResults = await queryable.Where(x => chunkList.Contains(x.Id)).ToListAsync();
            results.AddRange(chunkResults);
        }

        return results;
    }


    /// <summary>
    /// Determines whether an entity with the given identifier exists.
    /// </summary>
    /// <param name="id">Entity identifier.</param>
    /// <returns>True if the entity exists; otherwise, false.</returns>
    public async Task<bool> ExistsEntityAsync(int id) => await AsQueryableEntity().AnyAsync(x => x.Id == id);

    /// <summary>
    /// Adds a new entity to the context.
    /// </summary>
    /// <param name="entity">Entity to add.</param>
    /// <returns>The added entity.</returns>
    public async Task<TEntity> AddAsync(TEntity entity)
    {
        await GetDbSet().AddAsync(entity);
        return entity;
    }

    /// <summary>
    /// Adds multiple entities to the context.
    /// </summary>
    /// <param name="entities">Entities to add.</param>
    public async Task AddAsync(IEnumerable<TEntity> entities) => await GetDbSet().AddRangeAsync(entities);

    /// <summary>
    /// Deletes an entity by its identifier.
    /// </summary>
    /// <param name="id">Entity identifier.</param>
    public async Task DeleteAsync(int id) =>
        await AsQueryableEntity()
            .Where(x => x.Id == id)
            .ExecuteDeleteAsync();

    /// <summary>
    /// Deletes multiple entities by their identifiers.
    /// </summary>
    /// <param name="ids">Collection of entity identifiers.</param>
    public async Task DeleteAsync(IEnumerable<int> ids) =>
        await AsQueryableEntity()
            .Where(x => ids.Contains(x.Id))
            .ExecuteDeleteAsync();

    /// <summary>
    /// Deletes the specified entity.
    /// </summary>
    /// <param name="entity">Entity to delete.</param>
    public async Task DeleteAsync(TEntity entity)
    {
        GetDbSet().Remove(entity);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Deletes the specified entities.
    /// </summary>
    /// <param name="entities">Entities to delete.</param>
    public async Task DeleteAsync(IEnumerable<TEntity> entities)
    {
        GetDbSet().RemoveRange(entities);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Updates an existing entity.
    /// </summary>
    /// <param name="entity">Entity with updated values.</param>
    /// <returns>The updated tracked entity.</returns>
    /// <exception cref="Exception">Thrown when the entity is not found.</exception>
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

    /// <summary>
    /// Updates multiple existing entities.
    /// </summary>
    /// <param name="entities">Entities with updated values.</param>
    /// <exception cref="Exception">Thrown when any entity is not found.</exception>
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

    /// <summary>
    /// Gets a queryable for entities with no tracking enabled.
    /// </summary>
    /// <returns>Queryable entity set.</returns>
    protected virtual IQueryable<TEntity> AsQueryableEntity() =>
        EntityContext.Set<TEntity>().AsNoTracking();

    /// <summary>
    /// Gets the database set for the entity type.
    /// </summary>
    /// <returns>Entity DbSet.</returns>
    protected virtual DbSet<TEntity> GetDbSet() =>
        EntityContext.Set<TEntity>();

    /// <summary>
    /// Removes aggregate entities that are no longer associated with the given entity.
    /// </summary>
    /// <param name="entity">Entity being updated.</param>
    protected virtual void DeleteRemovedAggregates(TEntity entity)
    {
    }
}