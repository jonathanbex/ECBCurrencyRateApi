using Microsoft.Extensions.Caching.Memory;

namespace ECBCurrencyRates.Cache
{
  public class CacheResolver : ICacheProvider
  {
    private readonly IMemoryCache _cache;
    MemoryCacheEntryOptions cacheOptions;
    public CacheResolver(IMemoryCache cache)
    {
      _cache = cache;
      cacheOptions = new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2) };
    }

    public void CreateOrUpdateCache<T>(string key, T value, DateTimeOffset? expiryInDateTimeOffset = null)
    {
      _cache.Set(key, value, cacheOptions);
    }

    public void DeleteCache(string key)
    {
      if (_cache.TryGetValue(key, out _))
      {
        _cache.Remove(key);
      }
    }

    public T? GetCache<T>(string key)
    {
      T res = default;
      _cache.TryGetValue(key, out res);

      return res;
    }

  }
}
