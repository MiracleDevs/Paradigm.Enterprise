using System.ComponentModel.DataAnnotations;

namespace Paradigm.Enterprise.Domain.Entities
{
    public abstract class EntityBase : Interfaces.IEntity
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Determines whether this instance is new.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance is new; otherwise, <c>false</c>.
        /// </returns>
        public bool IsNew() => Id == default;
    }

    public abstract class EntityBase<TInterface, TEntity, TView> : EntityBase
        where TInterface : Interfaces.IEntity
        where TEntity : EntityBase<TInterface, TEntity, TView>, TInterface
        where TView : EntityBase, TInterface, new()
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
        /// <exception cref="System.NotImplementedException"></exception>
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