using Paradigm.Enterprise.Data.SqlServer.StoredProcedures.Mappers;
using System.Collections.Concurrent;

namespace Paradigm.Enterprise.Data.SqlServer.StoredProcedures;

/// <summary>
/// Stores factories used to create SQL Server parameter mappers for application parameter types.
/// </summary>
public static class SqlParameterMapperFactory
{
    private static readonly ConcurrentDictionary<Type, Func<ISqlParameterMapper>> MapperFactories = new ConcurrentDictionary<Type, Func<ISqlParameterMapper>>();

    /// <summary>Registers a mapper factory for an application parameter type if one is not already registered.</summary>
    /// <typeparam name="T">The application parameter type.</typeparam>
    /// <param name="mapperFactory">The delegate invoked for each request; it controls whether mapper instances are new or shared.</param>
    public static void RegisterMapper<T>(Func<ISqlParameterMapper> mapperFactory)
    {
        MapperFactories.TryAdd(typeof(T), mapperFactory);
    }

    /// <summary>Creates the registered mapper for an application parameter type.</summary>
    /// <typeparam name="T">The application parameter type.</typeparam>
    /// <returns>The mapper returned by the registered factory.</returns>
    /// <exception cref="InvalidOperationException">No mapper is registered for <typeparamref name="T"/>.</exception>
    public static ISqlParameterMapper GetMapper<T>()
    {
        return GetMapper(typeof(T));
    }

    /// <summary>Creates the registered mapper for an application parameter type.</summary>
    /// <param name="type">The application parameter type.</param>
    /// <returns>The mapper returned by the registered factory.</returns>
    /// <exception cref="InvalidOperationException">No mapper is registered for <paramref name="type"/>.</exception>
    public static ISqlParameterMapper GetMapper(Type type)
    {
        if (MapperFactories.TryGetValue(type, out var mapperFactory))
            return mapperFactory();

        throw new InvalidOperationException($"No SqlParameterMapper registered for type {type}");
    }

    /// <summary>Registers a custom mapper factory for an application parameter type if one is not already registered.</summary>
    /// <typeparam name="T">The application parameter type.</typeparam>
    /// <param name="customMapperFactory">A factory that creates the custom mapper.</param>
    /// <remarks>Registration is first-wins; this method does not replace an existing factory.</remarks>
    public static void AddCustomMapper<T>(Func<ISqlParameterMapper> customMapperFactory)
    {
        MapperFactories.TryAdd(typeof(T), customMapperFactory);
    }
}
