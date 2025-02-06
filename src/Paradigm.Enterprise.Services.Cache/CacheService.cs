using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Paradigm.Enterprise.Services.Cache.Configuration;
using ZiggyCreatures.Caching.Fusion;

namespace Paradigm.Enterprise.Services.Cache;

public class CacheService : ICacheService
{
    #region Properties

    /// <summary>
    /// The cache configuration
    /// </summary>
    private readonly RedisCacheConfiguration _cacheConfiguration;

    /// <summary>
    /// The fusion cache
    /// </summary>
    private readonly IFusionCache _fusionCache;

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
    /// <param name="fusionCache">The fusion cache.</param>
    /// <param name="logger">The logger.</param>
    public CacheService(IConfiguration configuration, IFusionCache fusionCache, ILogger<CacheService> logger)
    {
        _cacheConfiguration = new();
        configuration.Bind("RedisCacheConfiguration", _cacheConfiguration);

        _fusionCache = fusionCache;
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
    /// <param name="expiration">The cache expiration.</param>
    /// <param name="tags">The tags.</param>
    /// <returns></returns>
    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, IEnumerable<string>? tags = null)
    {
        if (_cacheConfiguration.Disabled) return await factory();

        T? data = default;

        try
        {
            var entryOptions = new FusionCacheEntryOptions(expiration ?? TimeSpan.FromMinutes(_cacheConfiguration.ExpirationInMinutes ?? 10));
            return await _fusionCache.GetOrSetAsync(key, async _ => await factory(), entryOptions, tags);
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
    /// <returns></returns>
    public async Task<T?> GetAsync<T>(string key)
    {
        if (_cacheConfiguration.Disabled) return default;

        try
        {
            var result = await _fusionCache.TryGetAsync<T>(key);
            return result.HasValue ? result.Value : default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return default;
        }
    }

    /// <summary>
    /// Sets the value in the cache.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <param name="expiration">The expiration.</param>
    /// <param name="tags">The tags.</param>
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, IEnumerable<string>? tags = null)
    {
        if (_cacheConfiguration.Disabled) return;

        try
        {
            var entryOptions = new FusionCacheEntryOptions(expiration ?? TimeSpan.FromMinutes(_cacheConfiguration.ExpirationInMinutes ?? 10));
            await _fusionCache.SetAsync(key, value, entryOptions, tags);
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
            await _fusionCache.RemoveAsync(key);
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
            await _fusionCache.RemoveByTagAsync(tag);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
    }

    #endregion
}