using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Paradigm.Enterprise.Services.Cache.Configuration;
using System.Text.Json.Serialization.Metadata;

namespace Paradigm.Enterprise.Services.Cache;

public class CacheService : ICacheService
{
    #region Properties

    /// <summary>
    /// The cache configuration
    /// </summary>
    private readonly RedisCacheConfiguration _cacheConfiguration;

    /// <summary>
    /// The hybrid cache
    /// </summary>
    private readonly HybridCache _hybridCache;

    /// <summary>
    /// The logger
    /// </summary>
    private readonly ILogger _logger;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheService" /> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="hybridCache">The hybrid cache.</param>
    /// <param name="logger">The logger.</param>
    public CacheService(IConfiguration configuration, HybridCache hybridCache, ILogger<CacheService> logger)
    {
        _cacheConfiguration = new();
        configuration.Bind("RedisCacheConfiguration", _cacheConfiguration);

        _hybridCache = hybridCache;
        _logger = logger;
    }

    #endregion

    #region Public Methods

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
    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, JsonTypeInfo<T> jsonTypeInfo, TimeSpan? expiration = null, IEnumerable<string>? tags = null)
    {
        if (_cacheConfiguration.Disabled) return await factory();

        T? data = default;

        try
        {
            return await _hybridCache.GetOrCreateAsync(key, async _ => await factory(), new HybridCacheEntryOptions
            {
                Expiration = expiration ?? TimeSpan.FromMinutes(_cacheConfiguration.ExpirationInMinutes ?? 60)
            }, tags);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return data ?? await factory();
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
            await _hybridCache.RemoveAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
    }

    /// <summary>
    /// Removes the cache entries by tag.
    /// </summary>
    /// <param name="tag">The tag.</param>
    public async Task RemoveByTagAsync(string tag)
    {
        if (_cacheConfiguration.Disabled) return;

        try
        {
            await _hybridCache.RemoveByTagAsync(tag);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
    }

    #endregion
}