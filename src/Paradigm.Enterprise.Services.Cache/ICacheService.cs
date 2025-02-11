using Paradigm.Enterprise.Services.Core;
using System.Text.Json.Serialization.Metadata;

namespace Paradigm.Enterprise.Services.Cache;

public interface ICacheService : IService
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
    /// Gets a value from the cache.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key">The key.</param>
    /// <returns></returns>
    Task<T?> GetAsync<T>(string key, JsonTypeInfo<T> jsonTypeInfo);

    /// <summary>
    /// Sets the value in the cache.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <param name="expiration">The expiration.</param>
    /// <param name="tags">The tags.</param>
    Task SetAsync<T>(string key, T value, JsonTypeInfo<T> jsonTypeInfo, TimeSpan? expiration = null);

    /// <summary>
    /// Removes the specified key from cache.
    /// </summary>
    /// <param name="key">The key.</param>
    Task RemoveAsync(string key);
}