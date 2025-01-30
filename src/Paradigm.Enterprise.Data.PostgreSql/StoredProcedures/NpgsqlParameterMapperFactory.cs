using Paradigm.Enterprise.Data.PostgreSql.StoredProcedures.Mappers;
using System.Collections.Concurrent;

namespace Paradigm.Enterprise.Data.PostgreSql.StoredProcedures;

public static class NpgsqlParameterMapperFactory
{
    private static readonly ConcurrentDictionary<Type, INpgsqlParameterMapper> Mappers = new ConcurrentDictionary<Type, INpgsqlParameterMapper>();

    public static void RegisterMapper<T>(INpgsqlParameterMapper mapper)
    {
        Mappers.TryAdd(typeof(T), mapper);
    }

    public static INpgsqlParameterMapper GetMapper<T>()
    {
        return GetMapper(typeof(T));
    }

    public static INpgsqlParameterMapper GetMapper(Type type)
    {
        if (Mappers.TryGetValue(type, out var mapper))
            return mapper;

        throw new InvalidOperationException($"No NpgsqlParameterMapper registered for type {type}");
    }

    public static void AddCustomMapper<T>(INpgsqlParameterMapper customMapper)
    {
        Mappers.TryAdd(typeof(T), customMapper);
    }
}