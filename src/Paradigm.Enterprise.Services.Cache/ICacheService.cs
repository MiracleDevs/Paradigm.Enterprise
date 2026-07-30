using Paradigm.Enterprise.Services.Core;
using System.Text.Json.Serialization.Metadata;

namespace Paradigm.Enterprise.Services.Cache;

/// <summary>
/// Provides JSON-serialized distributed-cache operations with source-generated type metadata.
/// </summary>
/// <remarks>
/// Callers supply <c>JsonTypeInfo&lt;T&gt;</c> from their generated
/// <see cref="System.Text.Json.Serialization.JsonSerializerContext"/>; reflection-based
/// serialization is not used by this API.
/// </remarks>
/// <example>
/// Cache a product for five minutes with generated JSON metadata:
/// <code>
/// var product = await cache.GetOrCreateAsync(
///     $"products:{productId}",
///     () => LoadProductAsync(productId),
///     AppJsonContext.Default.Product,
///     TimeSpan.FromMinutes(5));
/// </code>
/// <c>AppJsonContext</c> includes <c>[JsonSerializable(typeof(Product))]</c>.
/// </example>
public interface ICacheService : IService
{
    /// <summary>
    /// Gets the value from the cache or creates it.
    /// </summary>
    /// <typeparam name="T">The value type to deserialize or create.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">The asynchronous fallback used on a miss, when caching is disabled, or after a suppressed cache failure.</param>
    /// <param name="jsonTypeInfo">Source-generated JSON metadata for <typeparamref name="T"/>.</param>
    /// <param name="expiration">The absolute lifetime relative to now, or <see langword="null"/> to use configured/default expiration.</param>
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
    /// <param name="expiration">The absolute lifetime relative to now, or <see langword="null"/> to use configured/default expiration.</param>
    /// <param name="jsonTypeInfo">Source-generated JSON metadata for <typeparamref name="T"/>.</param>
    Task SetAsync<T>(string key, T value, JsonTypeInfo<T> jsonTypeInfo, TimeSpan? expiration = null);

    /// <summary>
    /// Removes the specified key from cache.
    /// </summary>
    /// <param name="key">The key.</param>
    Task RemoveAsync(string key);
}
