using Microsoft.Extensions.Caching.Distributed;
using TelemetrySlice.Lib.Extensions;

namespace TelemetrySlice.Services.Extensions;

public static class DistributedCachingExtensions  
{  
    public static async Task SetAsync<T>(this IDistributedCache distributedCache, string key, T value, DistributedCacheEntryOptions options, CancellationToken token = default(CancellationToken))  
    {  
        await distributedCache.SetAsync(key, value!.ToByteArray(), options, token);  
    }  
  
    public static async Task<T?> GetAsync<T>(this IDistributedCache distributedCache, string key, CancellationToken token = default(CancellationToken)) where T : class  
    {  
        var result = await distributedCache.GetAsync(key, token);  
        return result?.FromByteArray<T>();  
    }  
        
    public static T? Get<T>(this IDistributedCache distributedCache, string key, CancellationToken token = default(CancellationToken)) where T : class  
    { 
        var result = distributedCache.Get(key);
        return result?.FromByteArray<T>();  
    }  
}  