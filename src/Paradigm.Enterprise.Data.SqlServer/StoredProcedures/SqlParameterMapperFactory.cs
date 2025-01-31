using Paradigm.Enterprise.Data.SqlServer.StoredProcedures.Mappers;
using System.Collections.Concurrent;

namespace Paradigm.Enterprise.Data.SqlServer.StoredProcedures;

public static class SqlParameterMapperFactory
{
    private static readonly ConcurrentDictionary<Type, Func<ISqlParameterMapper>> MapperFactories = new ConcurrentDictionary<Type, Func<ISqlParameterMapper>>();

    public static void RegisterMapper<T>(Func<ISqlParameterMapper> mapperFactory)
    {
        MapperFactories.TryAdd(typeof(T), mapperFactory);
    }

    public static ISqlParameterMapper GetMapper<T>()
    {
        return GetMapper(typeof(T));
    }

    public static ISqlParameterMapper GetMapper(Type type)
    {
        if (MapperFactories.TryGetValue(type, out var mapperFactory))
            return mapperFactory();

        throw new InvalidOperationException($"No SqlParameterMapper registered for type {type}");
    }

    public static void AddCustomMapper<T>(Func<ISqlParameterMapper> customMapperFactory)
    {
        MapperFactories.TryAdd(typeof(T), customMapperFactory);
    }
}