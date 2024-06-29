namespace ECBCurrencyRates.Utility
{
  public static class CacheKeyUtility
  {
    public static string GetKey(string baseCurrency, string date)
    {
      return $"{baseCurrency}:{date}";
    }
  }
}
