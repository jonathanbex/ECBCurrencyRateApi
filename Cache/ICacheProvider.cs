namespace ECBCurrencyRates.Cache
{
  public interface ICacheProvider
  {
    void CreateOrUpdateCache<T>(string key, T value, DateTimeOffset? expiryInDateTimeOffset = null);
    void DeleteCache(string key);
    T? GetCache<T>(string key);
  }
}
