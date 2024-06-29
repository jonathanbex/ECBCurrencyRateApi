namespace ECBCurrencyRates.Models
{
  public class CurrencyResponseModel
  {
    public string BaseCurrency { get; set; }
    public DateTime CalculatedTime { get; set; }
    public List<CurrencyCalcResult> CurrencyRateResults { get; set; }

  }
  public class CurrencyCalcResult
  {
    public string Currency { get; set; }
    public decimal Rate { get; set; }
    public DateTime RateValidFrom { get; set; }
  }
}
