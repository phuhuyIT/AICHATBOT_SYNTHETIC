using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace WebApplication1.Service
{
    public interface ICacheService
    {
        T? Get<T>(string key);
        void Set<T>(string key, T value, TimeSpan? absoluteExpiration = null, TimeSpan? slidingExpiration = null);
        void Remove(string key);
        void Clear();
    }

    public class MemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ConcurrentDictionary<string, bool> _keys = new();
        private readonly ILogger<MemoryCacheService> _logger;

        public MemoryCacheService(IMemoryCache cache, ILogger<MemoryCacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public T? Get<T>(string key)
        {
            _cache.TryGetValue(key, out T? value);
            if (value != null)
            {
                _logger.LogDebug("Cache hit: {Key}", key);
            }
            else
            {
                _logger.LogDebug("Cache miss: {Key}", key);
            }
            return value;
        }

        public void Set<T>(string key, T value, TimeSpan? absoluteExpiration = null, TimeSpan? slidingExpiration = null)
        {
            var options = new MemoryCacheEntryOptions();
            
            if (absoluteExpiration.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = absoluteExpiration;
            }
            else
            {
                options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30); // Default
            }
            
            if (slidingExpiration.HasValue)
            {
                options.SlidingExpiration = slidingExpiration;
            }
            
            _cache.Set(key, value, options);
            _keys.TryAdd(key, true);
            _logger.LogDebug("Cache set: {Key}", key);
        }

        public void Remove(string key)
        {
            _cache.Remove(key);
            _keys.TryRemove(key, out _);
            _logger.LogDebug("Cache removed: {Key}", key);
        }

        public void Clear()
        {
            foreach (var key in _keys.Keys)
            {
                _cache.Remove(key);
            }
            _keys.Clear();
            _logger.LogInformation("Cache cleared");
        }
    }
}
