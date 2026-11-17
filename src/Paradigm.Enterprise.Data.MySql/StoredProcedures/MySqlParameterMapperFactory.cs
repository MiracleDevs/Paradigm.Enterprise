using Paradigm.Enterprise.Data.MySql.StoredProcedures.Mappers;
using System.Collections.Concurrent;

namespace Paradigm.Enterprise.Data.MySql.StoredProcedures;

/// <summary>Stores parameter-mapper factories keyed by application parameter type.</summary>
/// <remarks>Register factories once at startup. Registration is first-wins; factories should create fresh mapper instances.</remarks>
public static class MySqlParameterMapperFactory
{
    #region Fields

    private static readonly ConcurrentDictionary<Type, Func<IMySqlParameterMapper>> _mapperFactories = new();

    #endregion

    #region Public Methods

    /// <summary>Registers a factory unless the application type is already registered.</summary>
    /// <typeparam name="T">The application parameter type.</typeparam>
    /// <param name="mapperFactory">A factory creating a mapper for each operation.</param>
    public static void RegisterMapper<T>(Func<IMySqlParameterMapper> mapperFactory)
    {
        ArgumentNullException.ThrowIfNull(mapperFactory);
        _mapperFactories.TryAdd(typeof(T), mapperFactory);
    }

    /// <summary>Creates the registered mapper for an application type.</summary>
    /// <typeparam name="T">The application parameter type.</typeparam>
    /// <returns>The mapper created by the registered factory.</returns>
    public static IMySqlParameterMapper GetMapper<T>() => GetMapper(typeof(T));

    /// <summary>Creates the registered mapper for an application type.</summary>
    /// <param name="type">The application parameter type.</param>
    /// <returns>The mapper created by the registered factory.</returns>
    /// <exception cref="InvalidOperationException">No mapper was registered or the factory returned null.</exception>
    public static IMySqlParameterMapper GetMapper(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        if (_mapperFactories.TryGetValue(type, out var factory))
            return factory() ?? throw new InvalidOperationException($"The MySQL parameter mapper factory for '{type}' returned null.");

        throw new InvalidOperationException($"No MySqlParameterMapper registered for type '{type}'.");
    }

    /// <summary>Registers a custom mapper factory with the same first-registration-wins behavior.</summary>
    /// <typeparam name="T">The application parameter type.</typeparam>
    /// <param name="customMapperFactory">A factory creating a custom mapper for each operation.</param>
    public static void AddCustomMapper<T>(Func<IMySqlParameterMapper> customMapperFactory) => RegisterMapper<T>(customMapperFactory);

    #endregion
}