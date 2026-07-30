using Mapster;
using Mapster.Models;

namespace Paradigm.Enterprise.Domain.Mappers;

/// <summary>
/// Implements bidirectional object mapping with Mapster and optional global configuration hooks.
/// </summary>
/// <typeparam name="TFrom">The first model type.</typeparam>
/// <typeparam name="TTo">The second model type.</typeparam>
/// <remarks>
/// <see cref="RegisterCustomConfigurations"/> runs from the base constructor before the derived
/// constructor body. Overrides must therefore avoid depending on derived-instance initialization.
/// Configuration is registered in <see cref="TypeAdapterConfig.GlobalSettings"/> and consequently
/// affects other Mapster mappings in the process. Use <see cref="HasCustomConfigurationRegistered{TSource,TDestination}"/>
/// to avoid registering the same pair repeatedly when mapper instances are scoped or transient.
/// </remarks>
/// <example>
/// A mapper can customize both directions once and then update existing objects or create destinations
/// with the <see cref="IMapperExtensions"/> helpers:
/// <code>
/// public sealed class OrderMapper : MapperBase&lt;Order, OrderView&gt;
/// {
///     protected override void RegisterCustomConfigurations()
///     {
///         if (!HasCustomConfigurationRegistered&lt;Order, OrderView&gt;())
///         {
///             TypeAdapterConfig&lt;Order, OrderView&gt;.NewConfig()
///                 .Map(view =&gt; view.DisplayNumber, order =&gt; order.Number);
///         }
///
///         if (!HasCustomConfigurationRegistered&lt;OrderView, Order&gt;())
///         {
///             TypeAdapterConfig&lt;OrderView, Order&gt;.NewConfig()
///                 .Map(order =&gt; order.Number, view =&gt; view.DisplayNumber);
///         }
///     }
/// }
///
/// IMapper&lt;Order, OrderView&gt; mapper = new OrderMapper();
/// OrderView existingView = mapper.MapTo(order, new OrderView());
/// OrderView newView = mapper.MapTo(order);
/// List&lt;OrderView&gt; views = mapper.MapTo(orders);
/// </code>
/// </example>
public abstract class MapperBase<TFrom, TTo> : IMapper<TFrom, TTo>
{
    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="MapperBase{TFrom, TTo}"/> class.
    /// </summary>
    public MapperBase()
    {
        RegisterCustomConfigurations();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Maps the first model into an existing second-model instance using Mapster.
    /// </summary>
    /// <param name="source">The first-model value to read.</param>
    /// <param name="destination">The second-model instance to update.</param>
    /// <returns>The mapped <paramref name="destination"/> instance.</returns>
    public virtual TTo MapTo(TFrom source, TTo destination)
    {
        return source.Adapt(destination);
    }

    /// <summary>
    /// Maps the second model into an existing first-model instance using Mapster.
    /// </summary>
    /// <param name="destination">The first-model instance to update.</param>
    /// <param name="source">The second-model value to read.</param>
    /// <returns>The mapped <paramref name="destination"/> instance.</returns>
    public virtual TFrom MapFrom(TFrom destination, TTo source)
    {
        return source.Adapt(destination);
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// Determines whether Mapster has a global mapping rule for the specified type pair.
    /// </summary>
    /// <typeparam name="TSource">The type of the source.</typeparam>
    /// <typeparam name="TDestination">The type of the destination.</typeparam>
    /// <returns>
    /// <see langword="true"/> when a rule is registered; otherwise, <see langword="false"/>.
    /// </returns>
    protected bool HasCustomConfigurationRegistered<TSource, TDestination>()
    {
        return TypeAdapterConfig.GlobalSettings.RuleMap.ContainsKey(new TypeTuple(typeof(TSource), typeof(TDestination)));
    }

    /// <summary>
    /// Registers custom Mapster configuration during mapper construction.
    /// The base implementation performs no work.
    /// </summary>
    /// <remarks>
    /// This virtual member is invoked by the base constructor. Do not read fields initialized by the
    /// derived constructor, and guard global registrations when multiple instances may be constructed.
    /// </remarks>
    protected virtual void RegisterCustomConfigurations()
    {
    }

    #endregion
}
