using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.DependencyInjection;
using Paradigm.Enterprise.Data.Repositories.Prueba;
using Paradigm.Enterprise.Data.Uow;
using Paradigm.Enterprise.Domain.Entities;
using Paradigm.Enterprise.Domain.Repositories.Prueba;
using Paradigm.Enterprise.Domain.Uow;
using Paradigm.Enterprise.Providers.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paradigm.Enterprise.Providers.Prueba;

internal class EntityProvider<TEntity, TRepository> : ProviderBase, IEntityProvider<TEntity>
    where TEntity : EntityBase
    where TRepository : IEntityRepository<TEntity>
{
    #region Properties

    /// <summary>
    /// Gets the repository.
    /// </summary>
    /// <value>
    /// The repository.
    /// </value>
    protected TRepository Repository { get; }

    /// <summary>
    /// Gets the unit of work.
    /// </summary>
    /// <value>
    /// The unit of work.
    /// </value>
    protected IUnitOfWork UnitOfWork { get; }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="EditProviderBase{TEntity, TDto, TRepository}"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    protected EntityProvider(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        Repository = serviceProvider.GetRequiredService<TRepository>();
        UnitOfWork = serviceProvider.GetRequiredService<IUnitOfWork>();
    }

    #endregion

    #region Public Methods


    public async Task<IEnumerable<TEntity>> GetByIdsAsync(IEnumerable<int> ids) => await Repository.GetByIdsAsync(ids);
    
    public async Task<TEntity?> GetByIdAsync(int id) => await Repository.GetByIdAsync(id);
    
    public async Task<TEntity> AddAsync(TEntity entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        await BeforeAddAsync(entity);
        await BeforeSaveAsync(entity);

        entity = await Repository.AddAsync(entity);
        await UnitOfWork.CommitChangesAsync();

        await AfterSaveAsync(entity);
        await AfterAddAsync(entity);

        return entity;//await GetByIdAsync(entity.Id);
    }

    public async Task<TEntity> UpdateAsync(TEntity entity)
    {
        var exists = await Repository.ExistsEntityAsync(entity.Id);
        if (!exists)
            throw new Exception();

        entity.Validate();

        await BeforeUpdateAsync(entity);
        await BeforeSaveAsync(entity);

        entity = await Repository.UpdateAsync(entity);
        await UnitOfWork.CommitChangesAsync();

        await AfterSaveAsync(entity);
        await AfterUpdateAsync(entity);

        return entity;//await GetByIdAsync(entity.Id);
    }

    /// <summary>
    /// Adds or updates the entity.
    /// </summary>
    /// <param name="entity">The dto.</param>
    /// <returns></returns>
    public virtual async Task<TEntity> SaveAsync(TEntity entity)
    {
        return entity.IsNew() ?
            await AddAsync(entity) :
            await UpdateAsync(entity);
    }

    /// <summary>
    /// Adds new entities.
    /// </summary>
    /// <param name="entitys">The dtos.</param>
    /// <returns></returns>
    public virtual async Task<IEnumerable<TEntity>> AddAsync(List<TEntity> entitys)
    {
        var entities = new List<TEntity>();

        foreach (var entity in entitys)
        {
            var newEntity = ServiceProvider.GetRequiredService<TEntity>();
            newEntity.Validate();

            await BeforeAddAsync(newEntity);
            await BeforeSaveAsync(newEntity);

            await Repository.AddAsync(newEntity);
            entities.Add(newEntity);
        }

        await UnitOfWork.CommitChangesAsync();

        foreach (var entity in entities)
        {
            await AfterSaveAsync(entity);
            await AfterAddAsync(entity);
        }

        return entities;
    }

    /// <summary>
    /// Updates a new entities.
    /// </summary>
    /// <param name="entities">The dtos.</param>
    /// <returns></returns>
    public virtual async Task<IEnumerable<TEntity>> UpdateAsync(List<TEntity> entities)
    {
        var updatedEntities = new List<TEntity>();

        foreach (var entity in entities)
        {
            var existingEntity = await Repository.ExistsEntityAsync(entity.Id);
            if(!existingEntity)
                throw new NotFoundException("Entity not found or you don't have the permissions to open it.");

            entity.Validate();

            await BeforeUpdateAsync(entity);
            await BeforeSaveAsync(entity);

            await Repository.UpdateAsync(entity);
            updatedEntities.Add(entity);
        }

        await UnitOfWork.CommitChangesAsync();

        foreach (var entity in updatedEntities)
        {
            await AfterSaveAsync(entity);
            await AfterUpdateAsync(entity);
        }

        return updatedEntities;
    }

    /// <summary>
    /// Deletes the entity.
    /// </summary>
    /// <param name="id">The identifier.</param>
    public virtual async Task DeleteAsync(int id)
    {
        var entity = await Repository.GetByIdAsync(id);
        if (entity is not null)
        {
            await BeforeDeleteAsync(entity);
            await Repository.DeleteAsync(entity);
            await AfterDeleteAsync(entity);
            await UnitOfWork.CommitChangesAsync();
        }
    }

    /// <summary>
    /// Deletes the entities.
    /// </summary>
    /// <param name="ids">The identifiers.</param>
    public virtual async Task DeleteAsync(IEnumerable<int> ids)
    {
        var entities = await Repository.GetByIdsAsync(ids);

        foreach (var entity in entities)
        {
            await BeforeDeleteAsync(entity);
            await Repository.DeleteAsync(entity);
            await AfterDeleteAsync(entity);
        }

        await UnitOfWork.CommitChangesAsync();
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// Executed on add operation before the <see cref="TEntity"/> is mapped to <see cref="TEntity"/>.
    /// </summary>
    /// <param name="entity">The entity.</param>
    protected virtual async Task BeforeAddAsync(TEntity entity)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Executed on add operation after an entity has been added.
    /// </summary>
    /// <param name="entity">The entity.</param>
    protected virtual async Task AfterAddAsync(TEntity entity)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Executed on update operation before the <see cref="TEntity"/> is mapped to <see cref="TEntity"/>.
    /// </summary>
    /// <param name="entity">The entity.</param>
    protected virtual async Task BeforeUpdateAsync(TEntity entity)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Executed on update operation after an entity has been updated.
    /// </summary>
    /// <param name="entity">The entity.</param>
    protected virtual async Task AfterUpdateAsync(TEntity entity)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Executed on save operation before the <see cref="TEntity"/> is mapped to <see cref="TEntity"/>.
    /// </summary>
    /// <param name="entity">The entity.</param>
    protected virtual async Task BeforeSaveAsync(TEntity entity)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Executed on save operation after an entity has been saved.
    /// </summary>
    /// <param name="entity">The entity.</param>
    protected virtual async Task AfterSaveAsync(TEntity entity)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Executes before an entity is deleted.
    /// </summary>
    /// <param name="entity">The entity.</param>
    protected virtual async Task BeforeDeleteAsync(TEntity entity)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Executes after an entity has been deleted.
    /// </summary>
    /// <param name="entity">The entity.</param>
    protected virtual async Task AfterDeleteAsync(TEntity entity)
    {
        await Task.CompletedTask;
    }

    #endregion
}
