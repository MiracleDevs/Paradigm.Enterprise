using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Paradigm.Enterprise.Services.Cache.Configuration;
using System.Text.Json.Serialization.Metadata;

namespace Paradigm.Enterprise.Services.Cache;

public class CacheService : ICacheService, IDisposable
{
    #region Properties

    /// <summary>
    /// The cache configuration
    /// </summary>
    private readonly RedisCacheConfiguration _cacheConfiguration;

    /// <summary>
    /// The distributed cache
    /// </summary>
    private readonly IDistributedCache _distributedCache;

    /// <summary>
    /// The logger
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// The semaphore
    /// </summary>
    private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheService"/> class.
    /// </summary>
    /// <param name="cache">The cache.</param>
    public CacheService(IConfiguration configuration, IDistributedCache cache, ILogger<CacheService> logger)
    {
        _cacheConfiguration = new();
        configuration.Bind("RedisCacheConfiguration", _cacheConfiguration);

        // TODO: replace with Microsoft.Extensions.Caching.Hybrid when stable version is released, it prevents cache stampedes
        // by default and semaphore implementation won't be needed anymore. It also allows to tag the cached items to easily remove them by category if needed.
        _distributedCache = cache;
        _logger = logger;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        _semaphore.Dispose();
    }

    /// <summary>
    /// Gets the value from the cache or creates it.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">The factory.</param>
    /// <param name="jsonTypeInfo">The json type information.</param>
    /// <param name="expiration">The cache expiration.</param>
    /// <returns></returns>
    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, JsonTypeInfo<T> jsonTypeInfo, TimeSpan? expiration = null)
    {
        if (_cacheConfiguration.Disabled) return await factory();

        T? data = default;

        try
        {
            var cachedData = await _distributedCache.GetStringAsync(key);
            if (!string.IsNullOrWhiteSpace(cachedData))
            {
                var deserializedCachedData = System.Text.Json.JsonSerializer.Deserialize(cachedData, jsonTypeInfo);
                if (deserializedCachedData is not null)
                    return deserializedCachedData;
            }

            // If the data is not found, it waits for the semaphore to ensure only one thread can proceed to fetch and cache the data.
            await _semaphore.WaitAsync();

            try
            {
                // Double-check if the data was added to the cache while waiting for the semaphore
                cachedData = await _distributedCache.GetStringAsync(key);
                if (!string.IsNullOrWhiteSpace(cachedData))
                {
                    var deserializedCachedData = System.Text.Json.JsonSerializer.Deserialize(cachedData, jsonTypeInfo);
                    if (deserializedCachedData is not null)
                        return deserializedCachedData;
                }

                data = await factory();
                var serializedData = System.Text.Json.JsonSerializer.Serialize(data, jsonTypeInfo);

                if (expiration is null)
                    expiration = TimeSpan.FromMinutes(_cacheConfiguration.ExpirationInMinutes ?? 60);

                await _distributedCache.SetStringAsync(key, serializedData, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiration });

                return data;
            }
            finally
            {
                _semaphore.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return data ?? await factory();
        }
    }

    /// <summary>
    /// Gets a value from the cache.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key">The key.</param>
    /// <param name="jsonTypeInfo"></param>
    /// <returns></returns>
    public async Task<T?> GetAsync<T>(string key, JsonTypeInfo<T> jsonTypeInfo)
    {
        if (_cacheConfiguration.Disabled) return default;

        var cachedData = await _distributedCache.GetStringAsync(key);
        
        if (!string.IsNullOrWhiteSpace(cachedData))
        {
            var deserializedCachedData = System.Text.Json.JsonSerializer.Deserialize(cachedData, jsonTypeInfo);
            if (deserializedCachedData is not null)
                return deserializedCachedData;
        }

        return default;
    }

    /// <summary>
    /// Sets the value in the cache.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <param name="jsonTypeInfo"></param>
    /// <param name="expiration">The expiration.</param>
    public async Task SetAsync<T>(string key, T value, JsonTypeInfo<T> jsonTypeInfo, TimeSpan? expiration = null)
    {
        if (_cacheConfiguration.Disabled) return;

        try
        {
            var serializedData = System.Text.Json.JsonSerializer.Serialize(value, jsonTypeInfo);

            if (expiration is null)
                expiration = TimeSpan.FromMinutes(_cacheConfiguration.ExpirationInMinutes ?? 60);

            await _distributedCache.SetStringAsync(key, serializedData, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiration });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
    }

    /// <summary>
    /// Removes the specified key from cache.
    /// </summary>
    /// <param name="key">The key.</param>
    public async Task RemoveAsync(string key)
    {
        if (_cacheConfiguration.Disabled) return;

        try
        {
            await _distributedCache.RemoveAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
    }

    #endregion
}