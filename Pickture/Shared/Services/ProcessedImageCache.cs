using System.Collections.Concurrent;

namespace Pickture.Shared.Services;

/// <summary>
/// In-memory cache for processed images with LRU eviction and timeout support
/// </summary>
public class ProcessedImageCache
{
    private class CacheEntry
    {
        public byte[]? Data { get; set; }
        public DateTime ExpirationTime { get; set; }
        public DateTime LastAccessTime { get; set; }
    }

    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly TimeSpan _timeout;
    private readonly int _maxSize;

    public ProcessedImageCache(TimeSpan? timeout = null, int maxSize = 100)
    {
        _timeout = timeout ?? TimeSpan.FromMinutes(5);
        _maxSize = maxSize;
    }

    /// <summary>
    /// Get a cached processed image if available and not expired
    /// </summary>
    public byte[]? Get(string cacheKey)
    {
        if (_cache.TryGetValue(cacheKey, out var entry))
        {
            // Check if expired
            if (DateTime.UtcNow > entry.ExpirationTime)
            {
                _cache.TryRemove(cacheKey, out _);
                return null;
            }

            // Update last access time for LRU
            entry.LastAccessTime = DateTime.UtcNow;
            return entry.Data;
        }

        return null;
    }

    /// <summary>
    /// Set a processed image in the cache
    /// </summary>
    public void Set(string cacheKey, byte[] data)
    {
        // Clean up expired entries and enforce size limit
        CleanupIfNeeded();

        var entry = new CacheEntry
        {
            Data = data,
            ExpirationTime = DateTime.UtcNow.Add(_timeout),
            LastAccessTime = DateTime.UtcNow
        };

        _cache[cacheKey] = entry;
    }

    /// <summary>
    /// Clear the entire cache
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
    }

    /// <summary>
    /// Remove expired entries and enforce size limit using LRU
    /// </summary>
    private void CleanupIfNeeded()
    {
        // Remove expired entries
        var now = DateTime.UtcNow;
        var expiredKeys = _cache
            .Where(kvp => now > kvp.Value.ExpirationTime)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _cache.TryRemove(key, out _);
        }

        // If cache exceeds max size, remove least recently used items
        if (_cache.Count >= _maxSize)
        {
            var orderedByAccess = _cache
                .OrderBy(kvp => kvp.Value.LastAccessTime)
                .Take(_cache.Count - _maxSize + 1)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in orderedByAccess)
            {
                _cache.TryRemove(key, out _);
            }
        }
    }
}
