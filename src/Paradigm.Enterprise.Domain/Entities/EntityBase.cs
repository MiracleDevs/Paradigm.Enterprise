using System.ComponentModel.DataAnnotations;

namespace Paradigm.Enterprise.Domain.Entities
{
    /// <summary>
    /// Provides a base contract for domain entities that can report whether they are new.
    /// </summary>
    public abstract class EntityBase : Interfaces.IEntity
    {
        /// <summary>
        /// Determines whether this instance is new.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance is new; otherwise, <c>false</c>.
        /// </returns>
        public abstract bool IsNew();
    }

    /// <summary>
    /// Provides a strongly typed identifier and the conventional default-identifier newness check.
    /// </summary>
    /// <typeparam name="TId">The value type used for the entity identifier.</typeparam>
    public abstract class EntityBase<TId> : EntityBase, Interfaces.IEntity<TId>
        where TId : struct, IEquatable<TId>
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The default value indicates a new entity; any other value indicates an existing entity.
        /// </value>
        [Key]
        public TId Id { get; set; }

        /// <summary>
        /// Determines whether this instance is new.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance is new; otherwise, <c>false</c>.
        /// </returns>
        public override bool IsNew() => Id.Equals(default(TId));
    }

    /// <summary>
    /// Provides overridable mapping and validation hooks for an entity and its public view model.
    /// </summary>
    /// <typeparam name="TId">The value type used for entity identifiers.</typeparam>
    /// <typeparam name="TInterface">The interface shared by the entity and view.</typeparam>
    /// <typeparam name="TEntity">The concrete domain entity type.</typeparam>
    /// <typeparam name="TView">The concrete view type.</typeparam>
    /// <remarks>
    /// The default mapping methods are deliberately inert: <see cref="MapFrom"/> returns
    /// <see langword="null"/> and <see cref="MapTo"/> throws. Applications must override the operations
    /// they use. Provider operations call <see cref="Validate"/> after mapping.
    /// </remarks>
    public abstract class EntityBase<TId, TInterface, TEntity, TView> : EntityBase<TId>
        where TId : struct, IEquatable<TId>
        where TInterface : Interfaces.IEntity<TId>
        where TEntity : EntityBase<TId, TInterface, TEntity, TView>, TInterface
        where TView : EntityBase<TId>, TInterface, new()
    {
        /// <summary>
        /// Maps values from an interface model into this entity.
        /// </summary>
        /// <param name="serviceProvider">The application service provider available to custom mapping logic.</param>
        /// <param name="model">The source model.</param>
        /// <returns>The mapped entity, or <see langword="null"/> in the default implementation.</returns>
        public virtual TEntity? MapFrom(IServiceProvider serviceProvider, TInterface model)
        {
            return default;
        }

        /// <summary>
        /// Maps this entity to its public view representation.
        /// </summary>
        /// <param name="serviceProvider">The application service provider available to custom mapping logic.</param>
        /// <returns>The mapped view.</returns>
        /// <exception cref="NotImplementedException">The method is not overridden by a concrete entity.</exception>
        public virtual TView MapTo(IServiceProvider serviceProvider)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Runs after a custom mapping operation when invoked by application code.
        /// The base implementation performs no work.
        /// </summary>
        public virtual void AfterMapping() { }

        /// <summary>
        /// Runs before a custom mapping operation when invoked by application code.
        /// The base implementation performs no work.
        /// </summary>
        public virtual void BeforeMapping() { }

        /// <summary>
        /// Validates the entity after mapping. The base implementation accepts the entity unchanged.
        /// </summary>
        public virtual void Validate() { }
    }
}
