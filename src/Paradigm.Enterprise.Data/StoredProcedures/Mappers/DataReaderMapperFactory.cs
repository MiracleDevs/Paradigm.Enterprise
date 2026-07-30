using System.Collections.Concurrent;

namespace Paradigm.Enterprise.Data.StoredProcedures.Mappers;

/// <summary>
/// Maintains thread-safe factories for result types materialized from data readers.
/// </summary>
public static class DataReaderMapperFactory
{
    private static readonly ConcurrentDictionary<Type, Func<IDataReaderMapper>> MapperFactories = new ConcurrentDictionary<Type, Func<IDataReaderMapper>>();

    /// <summary>
    /// Registers the mapper factory for a result type if that type is not already registered.
    /// </summary>
    /// <typeparam name="T">The result type handled by the mapper.</typeparam>
    /// <param name="mapperFactory">
    /// The delegate invoked for each mapper request. The delegate controls whether instances are new or shared.
    /// </param>
    /// <remarks>A later registration for the same type is ignored.</remarks>
    public static void RegisterMapper<T>(Func<IDataReaderMapper> mapperFactory)
    {
        MapperFactories.TryAdd(typeof(T), mapperFactory);
    }

    /// <summary>
    /// Gets the mapper supplied by the factory registered for a result type.
    /// </summary>
    /// <typeparam name="T">The result type whose mapper is requested.</typeparam>
    /// <returns>The mapper returned by the registered factory.</returns>
    /// <exception cref="InvalidOperationException">No mapper is registered for <typeparamref name="T"/>.</exception>
    public static IDataReaderMapper GetMapper<T>()
    {
        return GetMapper(typeof(T));
    }

    /// <summary>
    /// Gets the mapper supplied by the factory registered for a runtime result type.
    /// </summary>
    /// <param name="type">The result type whose mapper is requested.</param>
    /// <returns>The mapper returned by the registered factory.</returns>
    /// <exception cref="InvalidOperationException">No mapper is registered for <paramref name="type"/>.</exception>
    public static IDataReaderMapper GetMapper(Type type)
    {
        if (MapperFactories.TryGetValue(type, out var mapperFactory))
            return mapperFactory();

        throw new InvalidOperationException($"No DataReaderMapper registered for type {type}");
    }
}
