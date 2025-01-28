using Paradigm.Enterprise.Data.SqlServer.StoredProcedures.Mappers;
using System.Collections.Concurrent;

namespace Paradigm.Enterprise.Data.SqlServer.StoredProcedures;

internal static class SqlParameterMapperFactory
{
    private static readonly ConcurrentDictionary<Type, ISqlParameterMapper> Mappers = new ConcurrentDictionary<Type, ISqlParameterMapper>();

    public static void RegisterMapper<T>(ISqlParameterMapper mapper)
    {
        Mappers.TryAdd(typeof(T), mapper);
    }

    public static ISqlParameterMapper GetMapper<T>()
    {
        return GetMapper(typeof(T));
    }

    public static ISqlParameterMapper GetMapper(Type type)
    {
        if (Mappers.TryGetValue(type, out var mapper))
            return mapper;

        throw new InvalidOperationException($"No SqlParameterMapper registered for type {type}");
    }

    public static void AddCustomMapper<T>(ISqlParameterMapper customMapper)
    {
        Mappers.TryAdd(typeof(T), customMapper);
    }
}