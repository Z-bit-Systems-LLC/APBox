namespace ApBox.Core.Services.Persistence;

public interface ICacheService
{
    /// <summary>
    /// Gets a cached value by key
    /// </summary>
    /// <typeparam name="T">The type of the cached value</typeparam>
    /// <param name="key">The cache key</param>
    /// <returns>The cached value if found and not expired, otherwise null</returns>
    Task<T?> GetAsync<T>(string key) where T : class;
    
    /// <summary>
    /// Gets a cached value or adds it using the factory if not present
    /// </summary>
    /// <typeparam name="T">The type of the cached value</typeparam>
    /// <param name="key">The cache key</param>
    /// <param name="factory">Factory function to create the value if not cached</param>
    /// <param name="expiry">Optional expiration time for the cache entry</param>
    /// <returns>The cached or newly created value</returns>
    Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null) where T : class;
    
    /// <summary>
    /// Sets a value in the cache
    /// </summary>
    /// <typeparam name="T">The type of the value to cache</typeparam>
    /// <param name="key">The cache key</param>
    /// <param name="value">The value to cache</param>
    /// <param name="expiry">Optional expiration time for the cache entry</param>
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class;
    
    /// <summary>
    /// Removes a cached value by key
    /// </summary>
    /// <param name="key">The cache key to remove</param>
    Task RemoveAsync(string key);
    
    /// <summary>
    /// Clears all cached values
    /// </summary>
    Task ClearAsync();
    
    /// <summary>
    /// Checks if a key exists in the cache and is not expired
    /// </summary>
    /// <param name="key">The cache key to check</param>
    /// <returns>True if the key exists and is not expired, otherwise false</returns>
    Task<bool> ExistsAsync(string key);
}