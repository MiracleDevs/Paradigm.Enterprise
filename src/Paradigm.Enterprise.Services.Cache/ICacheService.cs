using System.Text.Json.Serialization.Metadata;

namespace Paradigm.Enterprise.Providers.Cache;

public interface ICacheService
{
    /// <summary>
    /// Gets the value from the cache or creates it.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">The factory.</param>
    /// <param name="jsonTypeInfo">The json type information.</param>
    /// <param name="expiration">The cache expiration.</param>
    /// <returns></returns>
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, JsonTypeInfo<T> jsonTypeInfo, TimeSpan? expiration = null);

    /// <summary>
    /// Removes the specified key from cache.
    /// </summary>
    /// <param name="key">The key.</param>
    Task RemoveAsync(string key);
}