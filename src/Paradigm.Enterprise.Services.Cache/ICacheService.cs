using Paradigm.Enterprise.Services.Core;
using System.Text.Json.Serialization.Metadata;

namespace Paradigm.Enterprise.Services.Cache;

public interface ICacheService : IService
{
    /// <summary>
    /// Gets a value from the cache.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="expiration"></param>
    /// <param name="tags"></param>
    /// <returns></returns>
    Task<T> GetAsync<T>(string key, T value, TimeSpan? expiration = null, IEnumerable<string>? tags = null)

    /// <summary>
    /// Gets the value from the cache or creates it.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">The factory.</param>
    /// <param name="jsonTypeInfo">The json type information.</param>
    /// <param name="expiration">The cache expiration.</param>
    /// <param name="tags">The tags.</param>
    /// <returns></returns>
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, JsonTypeInfo<T> jsonTypeInfo, TimeSpan? expiration = null, IEnumerable<string>? tags = null);

    /// <summary>
    /// Removes the specified key from cache.
    /// </summary>
    /// <param name="key">The key.</param>
    Task RemoveAsync(string key);

    /// <summary>
    /// Removes the by tag asynchronous.
    /// </summary>
    /// <param name="tag">The tag.</param>
    /// <returns></returns>
    Task RemoveByTagAsync(string tag);
}