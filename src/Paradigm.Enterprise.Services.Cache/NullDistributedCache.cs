using Microsoft.Extensions.Caching.Distributed;

namespace Paradigm.Enterprise.Services.Cache;

/// <summary>
/// Null-object implementation of <see cref="IDistributedCache"/> that performs no operations.
/// Used as a fallback when the Redis cache is unavailable at startup.
/// </summary>
internal class NullDistributedCache : IDistributedCache
{
    /// <summary>
    /// Does nothing.
    /// </summary>
    public void Dispose()
    {
    }

    /// <summary>
    /// Returns null.
    /// </summary>
    public byte[]? Get(string key) => null;

    /// <summary>
    /// Returns null.
    /// </summary>
    public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => Task.FromResult<byte[]?>(null);

    /// <summary>
    /// Returns null.
    /// </summary>
    public string? GetString(string key) => null;

    /// <summary>
    /// Returns null.
    /// </summary>
    public Task<string?> GetStringAsync(string key, CancellationToken token = default) => Task.FromResult<string?>(null);

    /// <summary>
    /// Does nothing.
    /// </summary>
    public void Refresh(string key)
    {
    }

    /// <summary>
    /// Does nothing.
    /// </summary>
    public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;

    /// <summary>
    /// Does nothing.
    /// </summary>
    public void Remove(string key)
    {
    }

    /// <summary>
    /// Does nothing.
    /// </summary>
    public Task RemoveAsync(string key, CancellationToken token = default) => Task.CompletedTask;

    /// <summary>
    /// Does nothing.
    /// </summary>
    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
    }

    /// <summary>
    /// Does nothing.
    /// </summary>
    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) => Task.CompletedTask;

    /// <summary>
    /// Does nothing.
    /// </summary>
    public void SetString(string key, string value, DistributedCacheEntryOptions? options = null)
    {
    }

    /// <summary>
    /// Does nothing.
    /// </summary>
    public Task SetStringAsync(string key, string value, DistributedCacheEntryOptions? options = null, CancellationToken token = default) => Task.CompletedTask;
}
