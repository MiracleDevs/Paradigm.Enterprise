using Mapster;
using Mapster.Models;

namespace Paradigm.Enterprise.Domain.Mappers;

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
    /// Maps to.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="destination">The destination.</param>
    /// <returns></returns>
    public virtual TTo MapTo(TFrom source, TTo destination)
    {
        return source.Adapt(destination);
    }

    /// <summary>
    /// Maps from.
    /// </summary>
    /// <param name="destination">The destination.</param>
    /// <param name="source">The source.</param>
    /// <returns></returns>
    public virtual TFrom MapFrom(TFrom destination, TTo source)
    {
        return source.Adapt(destination);
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// Determines whether [has custom configuration registered] [the specified destination].
    /// </summary>
    /// <typeparam name="TSource">The type of the source.</typeparam>
    /// <typeparam name="TDestination">The type of the destination.</typeparam>
    /// <returns>
    ///   <c>true</c> if [has custom configuration registered] [the specified destination]; otherwise, <c>false</c>.
    /// </returns>
    protected bool HasCustomConfigurationRegistered<TSource, TDestination>()
    {
        return TypeAdapterConfig.GlobalSettings.RuleMap.ContainsKey(new TypeTuple(typeof(TSource), typeof(TDestination)));
    }

    /// <summary>
    /// Registers the custom configurations.
    /// </summary>
    protected virtual void RegisterCustomConfigurations()
    {
    }

    #endregion
}