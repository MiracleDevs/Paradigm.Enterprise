using System.Collections.Concurrent;

namespace Paradigm.Enterprise.Data.StoredProcedures.Mappers;

public static class DataReaderMapperFactory
{
    private static readonly ConcurrentDictionary<Type, Func<IDataReaderMapper>> MapperFactories = new ConcurrentDictionary<Type, Func<IDataReaderMapper>>();

    public static void RegisterMapper<T>(Func<IDataReaderMapper> mapperFactory)
    {
        MapperFactories.TryAdd(typeof(T), mapperFactory);
    }

    public static IDataReaderMapper GetMapper<T>()
    {
        return GetMapper(typeof(T));
    }

    public static IDataReaderMapper GetMapper(Type type)
    {
        if (MapperFactories.TryGetValue(type, out var mapperFactory))
            return mapperFactory();

        throw new InvalidOperationException($"No DataReaderMapper registered for type {type}");
    }
}