using Paradigm.Enterprise.Data.PostgreSql.StoredProcedures.Mappers;
using System.Collections.Concurrent;

namespace Paradigm.Enterprise.Data.PostgreSql.StoredProcedures;

public static class NpgsqlParameterMapperFactory
{
    private static readonly ConcurrentDictionary<Type, Func<INpgsqlParameterMapper>> MapperFactories = new ConcurrentDictionary<Type, Func<INpgsqlParameterMapper>>();

    public static void RegisterMapper<T>(Func<INpgsqlParameterMapper> mapperFactory)
    {
        MapperFactories.TryAdd(typeof(T), mapperFactory);
    }

    public static INpgsqlParameterMapper GetMapper<T>()
    {
        return GetMapper(typeof(T));
    }

    public static INpgsqlParameterMapper GetMapper(Type type)
    {
        if (MapperFactories.TryGetValue(type, out var mapperFactory))
            return mapperFactory();

        throw new InvalidOperationException($"No SqlParameterMapper registered for type {type}");
    }

    public static void AddCustomMapper<T>(Func<INpgsqlParameterMapper> customMapperFactory)
    {
        MapperFactories.TryAdd(typeof(T), customMapperFactory);
    }
}