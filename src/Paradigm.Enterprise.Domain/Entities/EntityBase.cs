using System.ComponentModel.DataAnnotations;

namespace Paradigm.Enterprise.Domain.Entities
{
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

    public abstract class EntityBase<TId> : EntityBase, Interfaces.IEntity<TId>
        where TId : struct, IEquatable<TId>
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
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

    public abstract class EntityBase<TId, TInterface, TEntity, TView> : EntityBase<TId>
        where TId : struct, IEquatable<TId>
        where TInterface : Interfaces.IEntity<TId>
        where TEntity : EntityBase<TId, TInterface, TEntity, TView>, TInterface
        where TView : EntityBase<TId>, TInterface, new()
    {
        /// <summary>
        /// Maps from.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        public virtual TEntity? MapFrom(IServiceProvider serviceProvider, TInterface model)
        {
            return default;
        }

        /// <summary>
        /// Maps to.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public virtual TView MapTo(IServiceProvider serviceProvider)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Afters the mapping.
        /// </summary>
        public virtual void AfterMapping() { }

        /// <summary>
        /// Befores the mapping.
        /// </summary>
        public virtual void BeforeMapping() { }

        /// <summary>
        /// Validates this instance.
        /// </summary>
        public virtual void Validate() { }
    }
}