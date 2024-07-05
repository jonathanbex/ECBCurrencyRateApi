using System.Text;

namespace ECBCurrencyRates.Utility
{
  public static class CacheKeyUtility
  {
    public static string GetKey(string baseCurrency, string date, IEnumerable<string>? currenciesToCheckAgainst = null)
    {
      var stringBuilder = new StringBuilder();
      stringBuilder.Append($"{baseCurrency}:{date}");
      if (currenciesToCheckAgainst != null && currenciesToCheckAgainst.Count() > 0)
      {
          stringBuilder.Append($":{string.Join(",", currenciesToCheckAgainst)}");
      }

      return stringBuilder.ToString();
    }
  }
}
