using Paradigm.Enterprise.Data.Oracle.StoredProcedures.Mappers;
using System.Collections.Concurrent;

namespace Paradigm.Enterprise.Data.Oracle.StoredProcedures;

/// <summary>Stores parameter-mapper factories keyed by application parameter type.</summary>
/// <remarks>Register factories once at startup. Registration is first-wins; factories should create fresh mapper instances.</remarks>
public static class OracleParameterMapperFactory
{
    #region Fields

    private static readonly ConcurrentDictionary<Type, Func<IOracleParameterMapper>> _mapperFactories = new();

    #endregion

    #region Public Methods

    /// <summary>Registers a factory unless the application type is already registered.</summary>
    /// <typeparam name="T">The application parameter type.</typeparam>
    /// <param name="mapperFactory">A factory creating a mapper for each operation.</param>
    public static void RegisterMapper<T>(Func<IOracleParameterMapper> mapperFactory)
    {
        ArgumentNullException.ThrowIfNull(mapperFactory);
        _mapperFactories.TryAdd(typeof(T), mapperFactory);
    }

    /// <summary>Creates the registered mapper for an application type.</summary>
    /// <typeparam name="T">The application parameter type.</typeparam>
    /// <returns>The mapper created by the registered factory.</returns>
    public static IOracleParameterMapper GetMapper<T>() => GetMapper(typeof(T));

    /// <summary>Creates the registered mapper for an application type.</summary>
    /// <param name="type">The application parameter type.</param>
    /// <returns>The mapper created by the registered factory.</returns>
    /// <exception cref="InvalidOperationException">No mapper was registered or the factory returned null.</exception>
    public static IOracleParameterMapper GetMapper(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        if (_mapperFactories.TryGetValue(type, out var factory))
            return factory() ?? throw new InvalidOperationException($"The Oracle parameter mapper factory for '{type}' returned null.");

        throw new InvalidOperationException($"No OracleParameterMapper registered for type '{type}'.");
    }

    /// <summary>Registers a custom mapper factory with the same first-registration-wins behavior.</summary>
    /// <typeparam name="T">The application parameter type.</typeparam>
    /// <param name="customMapperFactory">A factory creating a custom mapper for each operation.</param>
    public static void AddCustomMapper<T>(Func<IOracleParameterMapper> customMapperFactory) => RegisterMapper<T>(customMapperFactory);

    #endregion
}