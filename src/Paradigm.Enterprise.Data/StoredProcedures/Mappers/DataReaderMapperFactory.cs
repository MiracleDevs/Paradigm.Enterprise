using System.Collections.Concurrent;

namespace Paradigm.Enterprise.Data.StoredProcedures.Mappers;

/// <summary>
/// Maintains a thread-safe registry of factories for result types materialized from data readers.
/// </summary>
/// <remarks>
/// Registration is process-wide and first-registration-wins. Register mappers during application
/// startup, before executing stored procedures. The delegate controls mapper lifetime; this matters
/// for <see cref="DataReaderMapperBase"/> because it caches the first result-set schema it observes.
/// Registry operations are thread-safe; mapper instances and factory delegates are thread-safe only
/// when their own implementations provide that guarantee.
/// </remarks>
/// <example>
/// Register a factory for every non-primitive stored-procedure result type:
/// <code>
/// DataReaderMapperFactory.RegisterMapper&lt;OrderSummary&gt;(
///     () =&gt; new OrderSummaryMapper());
///
/// // Stored-procedure result translation now resolves OrderSummaryMapper.
/// OrderSummary? order = await procedure.ExecuteAsync(
///     connection,
///     new GetOrderParameters(orderId),
///     unitOfWork);
/// </code>
/// Returning a new mapper from the factory isolates cached schema metadata between executions.
/// A shared mapper is suitable only when all uses have the same schema and its implementation is
/// safe for concurrent calls.
/// </example>
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
