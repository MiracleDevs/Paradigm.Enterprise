using System.ComponentModel.DataAnnotations;

namespace Paradigm.Enterprise.Domain.Entities
{
    /// <summary>
    /// Provides a base contract for domain entities that can report whether they have a persistent identity.
    /// </summary>
    /// <remarks>
    /// Providers use <see cref="IsNew"/> to choose between add and update operations. Derived types
    /// should therefore base the result on the identifier or persistence convention used by their repository.
    /// </remarks>
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
    /// <remarks>
    /// An identifier equal to <c>default(TId)</c> is treated as unassigned. For example, an entity with
    /// an <see cref="int"/> identifier is new while its identifier is zero, and an entity with a
    /// <see cref="Guid"/> identifier is new while its identifier is <see cref="Guid.Empty"/>.
    /// Override <see cref="EntityBase.IsNew"/> when the persistence store uses a different convention.
    /// </remarks>
    /// <example>
    /// <code>
    /// public sealed class InvoiceView : EntityBase&lt;Guid&gt;
    /// {
    ///     public decimal Total { get; set; }
    ///
    ///     public void Initialize() => Id = Guid.NewGuid();
    /// }
    ///
    /// var draft = new InvoiceView();
    /// bool add = draft.IsNew();        // true: Id is Guid.Empty
    ///
    /// draft.Initialize();
    /// bool update = !draft.IsNew();    // true: an identity has been assigned
    /// </code>
    /// </example>
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
    /// they use. Provider operations call <see cref="MapFrom"/> on an existing entity and then call
    /// <see cref="Validate"/>; the return value from <see cref="MapFrom"/> is not used by
    /// <c>EditProviderBase</c>, so an override must mutate and normally return the current instance.
    /// The <see cref="BeforeMapping"/> and <see cref="AfterMapping"/> hooks are not invoked automatically
    /// by this class or by the provider bases.
    /// </remarks>
    /// <example>
    /// A domain entity can own mapping and validation while resolving only the services it actually needs:
    /// <code>
    /// public interface IOrderModel : IEntity&lt;Guid&gt;
    /// {
    ///     string Number { get; }
    ///     decimal Total { get; }
    /// }
    ///
    /// public sealed class OrderView : EntityBase&lt;Guid&gt;, IOrderModel
    /// {
    ///     public string Number { get; set; } = "";
    ///     public decimal Total { get; set; }
    /// }
    ///
    /// public sealed class Order
    ///     : EntityBase&lt;Guid, IOrderModel, Order, OrderView&gt;, IOrderModel
    /// {
    ///     public string Number { get; private set; } = "";
    ///     public decimal Total { get; private set; }
    ///
    ///     public override Order MapFrom(IServiceProvider services, IOrderModel model)
    ///     {
    ///         Id = model.Id;
    ///         Number = model.Number.Trim();
    ///         Total = model.Total;
    ///         return this; // The provider continues using this instance.
    ///     }
    ///
    ///     public override OrderView MapTo(IServiceProvider services) => new()
    ///     {
    ///         Id = Id,
    ///         Number = Number,
    ///         Total = Total
    ///     };
    ///
    ///     public override void Validate()
    ///     {
    ///         var rules = new DomainValidator();
    ///         rules.Assert(Number.Length != 0, "An order number is required.");
    ///         rules.Assert(Total &gt;= 0, "The order total cannot be negative.");
    ///         rules.ThrowIfAny();
    ///     }
    /// }
    /// </code>
    /// </example>
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
        /// <remarks>
        /// Override this method before using an edit provider. Mutate the current entity rather than
        /// returning an unrelated instance because provider implementations ignore the returned value.
        /// </remarks>
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
        /// Runs after a custom mapping operation when invoked explicitly by application code.
        /// The base implementation performs no work.
        /// </summary>
        /// <remarks>This hook is not called automatically by <see cref="MapFrom"/> or <see cref="MapTo"/>.</remarks>
        public virtual void AfterMapping() { }

        /// <summary>
        /// Runs before a custom mapping operation when invoked explicitly by application code.
        /// The base implementation performs no work.
        /// </summary>
        /// <remarks>This hook is not called automatically by <see cref="MapFrom"/> or <see cref="MapTo"/>.</remarks>
        public virtual void BeforeMapping() { }

        /// <summary>
        /// Validates the entity after mapping. The base implementation accepts the entity unchanged.
        /// </summary>
        public virtual void Validate() { }
    }
}
