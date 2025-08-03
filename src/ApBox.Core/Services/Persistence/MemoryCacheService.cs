using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ApBox.Core.Services.Persistence;

public class MemoryCacheService : ICacheService
{
    private readonly ILogger<MemoryCacheService> _logger;
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly Timer _cleanupTimer;
    private readonly object _lockObject = new();

    private class CacheEntry
    {
        public object Value { get; set; } = null!;
        public DateTime? ExpiryTime { get; set; }
        public SemaphoreSlim Lock { get; } = new(1, 1);
    }

    public MemoryCacheService(ILogger<MemoryCacheService> logger)
    {
        _logger = logger;
        // Run cleanup every minute
        _cleanupTimer = new Timer(CleanupExpiredEntries, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    public Task<T?> GetAsync<T>(string key) where T : class
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            if (entry.ExpiryTime.HasValue && entry.ExpiryTime.Value < DateTime.UtcNow)
            {
                // Entry has expired
                _cache.TryRemove(key, out _);
                return Task.FromResult<T?>(null);
            }
            
            _logger.LogDebug("Cache hit for key: {Key}", key);
            return Task.FromResult((T?)entry.Value);
        }

        _logger.LogDebug("Cache miss for key: {Key}", key);
        return Task.FromResult<T?>(null);
    }

    public async Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null) where T : class
    {
        // Try to get existing entry
        if (_cache.TryGetValue(key, out var existingEntry))
        {
            if (!existingEntry.ExpiryTime.HasValue || existingEntry.ExpiryTime.Value > DateTime.UtcNow)
            {
                _logger.LogDebug("Cache hit for key: {Key}", key);
                return (T)existingEntry.Value;
            }
        }

        // Get or create entry with lock
        var entry = _cache.GetOrAdd(key, _ => new CacheEntry());
        
        await entry.Lock.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (entry.Value != null && (!entry.ExpiryTime.HasValue || entry.ExpiryTime.Value > DateTime.UtcNow))
            {
                return (T)entry.Value;
            }

            _logger.LogDebug("Cache miss for key: {Key}, invoking factory", key);
            var value = await factory();
            
            entry.Value = value;
            entry.ExpiryTime = expiry.HasValue ? DateTime.UtcNow.Add(expiry.Value) : null;
            
            return value;
        }
        finally
        {
            entry.Lock.Release();
        }
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
    {
        var entry = _cache.AddOrUpdate(key, 
            _ => new CacheEntry 
            { 
                Value = value, 
                ExpiryTime = expiry.HasValue ? DateTime.UtcNow.Add(expiry.Value) : null 
            },
            (_, existing) =>
            {
                existing.Value = value;
                existing.ExpiryTime = expiry.HasValue ? DateTime.UtcNow.Add(expiry.Value) : null;
                return existing;
            });

        _logger.LogDebug("Set cache entry for key: {Key}", key);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        if (_cache.TryRemove(key, out _))
        {
            _logger.LogDebug("Removed cache entry for key: {Key}", key);
        }
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        _cache.Clear();
        _logger.LogInformation("Cleared all cache entries");
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key)
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            return Task.FromResult(!entry.ExpiryTime.HasValue || entry.ExpiryTime.Value > DateTime.UtcNow);
        }
        return Task.FromResult(false);
    }

    private void CleanupExpiredEntries(object? state)
    {
        var expiredKeys = new List<string>();
        var now = DateTime.UtcNow;

        foreach (var kvp in _cache)
        {
            if (kvp.Value.ExpiryTime.HasValue && kvp.Value.ExpiryTime.Value < now)
            {
                expiredKeys.Add(kvp.Key);
            }
        }

        foreach (var key in expiredKeys)
        {
            _cache.TryRemove(key, out _);
        }

        if (expiredKeys.Count > 0)
        {
            _logger.LogDebug("Cleaned up {Count} expired cache entries", expiredKeys.Count);
        }
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
    }
}