using Microsoft.Extensions.Caching.Memory;

namespace RepportingApp.CoreSystem.ApiSystem;

public class CacheService : ICacheService
{
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());

    public T? Get<T>(string key)
    {
        _cache.TryGetValue(key, out T? value);
        return value;
    }

    public void Set<T>(string key, T value, TimeSpan expiration)
    {
        _cache.Set(key, value, expiration);
    }

    public void Remove(string key)
    {
        _cache.Remove(key);
    }
}