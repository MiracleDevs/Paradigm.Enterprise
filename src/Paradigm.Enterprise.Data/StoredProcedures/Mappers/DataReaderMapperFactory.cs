using System.Collections.Concurrent;

namespace Paradigm.Enterprise.Data.StoredProcedures.Mappers;
public static class DataReaderMapperFactory
{
    private static readonly ConcurrentDictionary<Type, IDataReaderMapper> Mappers = new ConcurrentDictionary<Type, IDataReaderMapper>();

    public static void RegisterMapper<T>(IDataReaderMapper mapper)
    {
        Mappers.TryAdd(typeof(T), mapper);
    }

    public static IDataReaderMapper GetMapper<T>()
    {
        return GetMapper(typeof(T));
    }

    public static IDataReaderMapper GetMapper(Type type)
    {
        if (Mappers.TryGetValue(type, out var mapper))
            return mapper;

        throw new InvalidOperationException($"No DataReaderMapper registered for type {type}");
    }
}