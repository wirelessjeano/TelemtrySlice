namespace TelemetrySlice.Domain.Interfaces;

public interface ICacheService
{
    Task<T> SetAsync<T>(string cacheKey, T item) where T : class;
    Task<T> SetAsync<T>(string cacheKey, T item, int minutes) where T : class;
    T? Get<T>(string cacheKey) where T : class;
    Task<T?> GetAsync<T>(string cacheKey) where T : class;
    Task<T?> GetAsync<T>(string cacheKey, Func<Task<T?>> getItemCallback, double minutesToCache = 60, double? millisecondsToCache = null) where T : class;
    Task<bool> KeyExists(string cacheKey);
    Task SetKeysAsync(IEnumerable<string> keys, TimeSpan? expiry = null);
}