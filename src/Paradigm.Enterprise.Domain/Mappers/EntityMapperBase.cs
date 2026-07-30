using Mapster;
using Paradigm.Enterprise.Domain.Entities;

namespace Paradigm.Enterprise.Domain.Mappers;

/// <summary>
/// Adds interface-to-entity and interface-to-view mapping to the standard bidirectional mapper.
/// </summary>
/// <typeparam name="TId">The value type used for entity identifiers.</typeparam>
/// <typeparam name="TInterface">The interface shared by the entity and view.</typeparam>
/// <typeparam name="TEntity">The concrete entity type.</typeparam>
/// <typeparam name="TView">The concrete view type.</typeparam>
public abstract class EntityMapperBase<TId, TInterface, TEntity, TView> : MapperBase<TEntity, TView>
    where TId : struct, IEquatable<TId>
    where TInterface : Interfaces.IEntity<TId>
    where TEntity : EntityBase<TId, TInterface, TEntity, TView>, TInterface, new()
    where TView : EntityBase<TId>, TInterface, new()
{
    /// <summary>
    /// Maps values from the shared interface into an existing entity.
    /// </summary>
    /// <param name="destination">The entity instance to update.</param>
    /// <param name="source">The interface value to read.</param>
    /// <returns>The mapped <paramref name="destination"/> entity.</returns>
    public virtual TEntity MapFromInterface(TEntity destination, TInterface source)
    {
        return source.Adapt(destination);
    }

    /// <summary>
    /// Maps values from the shared interface into an existing view.
    /// </summary>
    /// <param name="destination">The view instance to update.</param>
    /// <param name="source">The interface value to read.</param>
    /// <returns>The mapped <paramref name="destination"/> view.</returns>
    public virtual TView MapFromInterface(TView destination, TInterface source)
    {
        return source.Adapt(destination);
    }

    /// <summary>
    /// Determines whether Mapster has a global mapping rule from the shared interface to the entity.
    /// </summary>
    /// <returns><see langword="true"/> when a rule is registered; otherwise, <see langword="false"/>.</returns>
    protected bool HasCustomConfigurationRegistered()
    {
        return HasCustomConfigurationRegistered<TInterface, TEntity>();
    }
}
