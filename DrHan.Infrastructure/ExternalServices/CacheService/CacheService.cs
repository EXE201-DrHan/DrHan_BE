using DrHan.Application.Interfaces.Services.CacheService;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DrHan.Infrastructure.ExternalServices.CacheService
{
    public class CacheService : ICacheService
    {
        private readonly IDistributedCache _distributedCache;
        private readonly IMemoryCache _memoryCache;
        private readonly IConnectionMultiplexer _redis;
        private readonly CacheSettings _cacheSettings;
        private readonly ILogger<CacheService> _logger;

        public CacheService(
            IDistributedCache distributedCache,
            IMemoryCache memoryCache,
            IConnectionMultiplexer redis,
            IOptions<CacheSettings> cacheSettings,
            ILogger<CacheService> logger)
        {
            _distributedCache = distributedCache;
            _memoryCache = memoryCache;
            _redis = redis;
            _cacheSettings = cacheSettings.Value;
            _logger = logger;
        }

        public async Task<T> GetAsync<T>(string key) where T : class
        {
            try
            {
                // Try memory cache first (faster)
                if (_memoryCache.TryGetValue(key, out T memoryCached))
                {
                    return memoryCached;
                }

                // Try Redis
                var json = await _distributedCache.GetStringAsync(key);
                if (json != null)
                {
                    var result = JsonSerializer.Deserialize<T>(json);

                    // Cache in memory for faster subsequent access (use short expiration)
                    _memoryCache.Set(key, result, _cacheSettings.ShortExpiration);

                    return result;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get cache key: {Key}", key);
                return null;
            }
        }

        public async Task<T> GetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null) where T : class
        {
            var cached = await GetAsync<T>(key);
            if (cached != null)
                return cached;

            // Cache miss - get from factory
            var result = await factory();
            if (result != null)
            {
                await SetAsync(key, result, expiration);
            }

            return result;
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            if (value == null) return;

            try
            {
                var exp = expiration ?? _cacheSettings.DefaultExpiration;

                // Set in memory cache (use shorter expiration for memory)
                var memoryExpiration = TimeSpan.FromMinutes(Math.Min(exp.TotalMinutes, _cacheSettings.ShortExpirationMinutes));
                _memoryCache.Set(key, value, memoryExpiration);

                // Set in Redis
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = exp
                };

                var json = JsonSerializer.Serialize(value);
                await _distributedCache.SetStringAsync(key, json, options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set cache key: {Key}", key);
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                _memoryCache.Remove(key);
                await _distributedCache.RemoveAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove cache key: {Key}", key);
            }
        }

        public async Task RemoveByPatternAsync(string pattern)
        {
            try
            {
                var server = _redis.GetServer(_redis.GetEndPoints().First());
                var keys = server.Keys(pattern: pattern);

                var tasks = keys.Select(key => _distributedCache.RemoveAsync(key.ToString()));
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove cache pattern: {Pattern}", pattern);
            }
        }

        public async Task<bool> ExistsAsync(string key)
        {
            try
            {
                if (_memoryCache.TryGetValue(key, out _))
                    return true;

                var result = await _distributedCache.GetAsync(key);
                return result != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check cache key existence: {Key}", key);
                return false;
            }
        }
    }
}
