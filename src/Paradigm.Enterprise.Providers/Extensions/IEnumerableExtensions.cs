namespace Paradigm.Enterprise.Providers.Extensions;

public static class IEnumerableExtensions
{
    /// <summary>
    /// Splits the provided source into chunks of the specified size.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source">The source.</param>
    /// <param name="chunkSize">Size of the chunk.</param>
    /// <returns></returns>
    public static IEnumerable<IEnumerable<T>> ChunkBy<T>(this IEnumerable<T> source, int chunkSize)
    {
        return source
            .Select((x, i) => new { Index = i, Value = x })
            .GroupBy(x => x.Index / chunkSize)
            .Select(x => x.Select(v => v.Value).ToList())
            .ToList();
    }
}