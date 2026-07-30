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
/// <remarks>
/// The interface overloads copy into caller-supplied objects and return those objects. They do not
/// call the mapping or validation hooks declared by <see cref="EntityBase{TId,TInterface,TEntity,TView}"/>.
/// Configure Mapster rules in <see cref="MapperBase{TFrom,TTo}.RegisterCustomConfigurations"/> when
/// interface members need transformations rather than convention-based copying.
/// </remarks>
/// <example>
/// <code>
/// public sealed class ProductMapper
///     : EntityMapperBase&lt;int, IProduct, Product, ProductView&gt;
/// {
///     protected override void RegisterCustomConfigurations()
///     {
///         if (!HasCustomConfigurationRegistered())
///         {
///             TypeAdapterConfig&lt;IProduct, Product&gt;.NewConfig()
///                 .Ignore(product =&gt; product.Id);
///         }
///     }
/// }
///
/// var mapper = new ProductMapper();
///
/// // Update an entity that was loaded and is already tracked by the repository.
/// Product entity = mapper.MapFromInterface(existingProduct, incomingView);
/// entity.Validate(); // Mapping itself does not validate.
/// await repository.UpdateAsync(entity); // Stages the update.
/// await unitOfWork.CommitChangesAsync(); // Persists the staged update.
///
/// ProductView view = mapper.MapFromInterface(new ProductView(), entity);
/// </code>
/// </example>
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
