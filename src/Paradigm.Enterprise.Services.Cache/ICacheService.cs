using Paradigm.Enterprise.Services.Core;
using System.Text.Json.Serialization.Metadata;

namespace Paradigm.Enterprise.Services.Cache;

/// <summary>
/// Provides JSON-serialized distributed-cache operations with source-generated type metadata.
/// </summary>
public interface ICacheService : IService
{
    /// <summary>
    /// Gets the value from the cache or creates it.
    /// </summary>
    /// <typeparam name="T">The value type to deserialize or create.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">The factory.</param>
    /// <param name="jsonTypeInfo">The json type information.</param>
    /// <param name="expiration">The cache expiration.</param>
    /// <returns>The cached value, or the value produced by <paramref name="factory"/>.</returns>
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, JsonTypeInfo<T> jsonTypeInfo, TimeSpan? expiration = null);

    /// <summary>
    /// Gets a value from the cache.
    /// </summary>
    /// <typeparam name="T">The cached value type.</typeparam>
    /// <param name="key">The key.</param>
    /// <param name="jsonTypeInfo">Source-generated JSON metadata for <typeparamref name="T"/>.</param>
    /// <returns>The cached value, or <see langword="null"/>/the default value when it is absent or unavailable.</returns>
    Task<T?> GetAsync<T>(string key, JsonTypeInfo<T> jsonTypeInfo);

    /// <summary>
    /// Sets the value in the cache.
    /// </summary>
    /// <typeparam name="T">The cached value type.</typeparam>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <param name="expiration">The expiration.</param>
    /// <param name="jsonTypeInfo">Source-generated JSON metadata for <typeparamref name="T"/>.</param>
    Task SetAsync<T>(string key, T value, JsonTypeInfo<T> jsonTypeInfo, TimeSpan? expiration = null);

    /// <summary>
    /// Removes the specified key from cache.
    /// </summary>
    /// <param name="key">The key.</param>
    Task RemoveAsync(string key);
}
