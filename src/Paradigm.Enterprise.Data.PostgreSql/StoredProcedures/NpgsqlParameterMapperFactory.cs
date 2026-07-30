using Paradigm.Enterprise.Data.PostgreSql.StoredProcedures.Mappers;
using System.Collections.Concurrent;

namespace Paradigm.Enterprise.Data.PostgreSql.StoredProcedures;

/// <summary>
/// Stores factories used to create PostgreSQL parameter mappers for application parameter types.
/// </summary>
/// <example>
/// Register mappers during application startup, before any procedure uses the parameter type:
/// <code>
/// NpgsqlParameterMapperFactory.RegisterMapper&lt;ArchiveOrderParameters&gt;(
///     () =&gt; new ArchiveOrderParameterMapper());
///
/// using INpgsqlParameterMapper mapper =
///     NpgsqlParameterMapperFactory.GetMapper&lt;ArchiveOrderParameters&gt;();
/// NpgsqlParameter[] parameters = mapper.Map(new ArchiveOrderParameters(42, null));
/// </code>
/// Registration is first-wins. Register the intended mapper once instead of attempting to replace it later.
/// </example>
public static class NpgsqlParameterMapperFactory
{
    private static readonly ConcurrentDictionary<Type, Func<INpgsqlParameterMapper>> MapperFactories = new ConcurrentDictionary<Type, Func<INpgsqlParameterMapper>>();

    /// <summary>Registers a mapper factory for an application parameter type if one is not already registered.</summary>
    /// <typeparam name="T">The application parameter type.</typeparam>
    /// <param name="mapperFactory">The delegate invoked for each request; it controls whether mapper instances are new or shared.</param>
    public static void RegisterMapper<T>(Func<INpgsqlParameterMapper> mapperFactory)
    {
        MapperFactories.TryAdd(typeof(T), mapperFactory);
    }

    /// <summary>Creates the registered mapper for an application parameter type.</summary>
    /// <typeparam name="T">The application parameter type.</typeparam>
    /// <returns>The mapper returned by the registered factory.</returns>
    /// <exception cref="InvalidOperationException">No mapper is registered for <typeparamref name="T"/>.</exception>
    public static INpgsqlParameterMapper GetMapper<T>()
    {
        return GetMapper(typeof(T));
    }

    /// <summary>Creates the registered mapper for an application parameter type.</summary>
    /// <param name="type">The application parameter type.</param>
    /// <returns>The mapper returned by the registered factory.</returns>
    /// <exception cref="InvalidOperationException">No mapper is registered for <paramref name="type"/>.</exception>
    public static INpgsqlParameterMapper GetMapper(Type type)
    {
        if (MapperFactories.TryGetValue(type, out var mapperFactory))
            return mapperFactory();

        throw new InvalidOperationException($"No SqlParameterMapper registered for type {type}");
    }

    /// <summary>Registers a custom mapper factory for an application parameter type if one is not already registered.</summary>
    /// <typeparam name="T">The application parameter type.</typeparam>
    /// <param name="customMapperFactory">A factory that creates the custom mapper.</param>
    /// <remarks>Registration is first-wins; this method does not replace an existing factory.</remarks>
    public static void AddCustomMapper<T>(Func<INpgsqlParameterMapper> customMapperFactory)
    {
        MapperFactories.TryAdd(typeof(T), customMapperFactory);
    }
}
