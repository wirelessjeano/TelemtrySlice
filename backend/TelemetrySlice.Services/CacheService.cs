using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using TelemetrySlice.Domain.Interfaces;
using TelemetrySlice.Services.Extensions;

namespace TelemetrySlice.Services;

public class CacheService(IDistributedCache distributedCache, IConnectionMultiplexer connectionMultiplexer) : ICacheService
{
    public async Task<T> SetAsync<T>(string cacheKey, T item) where T : class
    {
        await distributedCache.SetAsync<T>(cacheKey, item, new DistributedCacheEntryOptions());

        var itemUpdated = await distributedCache.GetAsync<T>(cacheKey);

        if (itemUpdated == null)
        {
            throw new Exception("Item inserted into cache but not retrievable immediately: " + cacheKey);
        }

        return itemUpdated;
    }

    public async Task<T> SetAsync<T>(string cacheKey, T item, int minutes) where T : class
    {
        await distributedCache.SetAsync<T>(cacheKey, item, new DistributedCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(minutes) });

        var itemUpdated = await distributedCache.GetAsync<T>(cacheKey);

        if (itemUpdated == null)
        {
            throw new Exception("Item inserted into cache but not retrievable immediately: " + cacheKey);
        }

        return itemUpdated;
    }

    public T? Get<T>(string cacheKey) where T : class
    {
        return distributedCache.Get<T>(cacheKey);
    }

    public async Task<T?> GetAsync<T>(string cacheKey) where T : class
    {
        return await distributedCache.GetAsync<T>(cacheKey);
    }

    public async Task<T?> GetAsync<T>(string cacheKey, Func<Task<T?>> getItemCallback, double minutesToCache = 60, double? millisecondsToCache = null) where T : class?
    {
        T? item = default(T);
        var objItem = await distributedCache.GetAsync<T>(cacheKey);

        if (objItem == null)
        {
            objItem = await getItemCallback();

            if (objItem != null && objItem is T)
            {
                item = (T)objItem;

                if (millisecondsToCache.HasValue && millisecondsToCache.Value >= 0)
                {
                    await distributedCache.SetAsync<T>(cacheKey, item, new DistributedCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(millisecondsToCache.Value) });
                }
                else if (minutesToCache <= 0)
                {
                    await distributedCache.SetAsync<T>(cacheKey, item, new DistributedCacheEntryOptions());
                }
                else
                {
                    await distributedCache.SetAsync<T>(cacheKey, item, new DistributedCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(minutesToCache) });
                }

                var itemTest = await distributedCache.GetAsync<T>(cacheKey);

                if (itemTest == null)
                {
                    throw new Exception("Item inserted into cache but not retrievable immediately: " + cacheKey);
                }
            }
        }
        else
        {
            item = (T)objItem;
        }

        return item;
    }
    
    public async Task<bool> KeyExists(string cacheKey)
    {
        var db = connectionMultiplexer.GetDatabase();
        return await db.KeyExistsAsync(cacheKey);
    }

    public async Task SetKeysAsync(IEnumerable<string> keys, TimeSpan? expiry = null)
    {
        var db = connectionMultiplexer.GetDatabase();
        var batch = db.CreateBatch();
        var tasks = new List<Task>();

        foreach (var key in keys)
        {
            tasks.Add(batch.StringSetAsync(key, RedisValue.EmptyString, expiry));
        }

        batch.Execute();
        await Task.WhenAll(tasks);
    }
}