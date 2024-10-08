﻿using static ECBCurrencyRates.ECBIntegration.Models.ECBSeriesModel;
using System.Xml.Serialization;
using System.Xml;
using System.Text;
using ECBCurrencyRates.Models;
using ECBCurrencyRates.Cache;
using ECBCurrencyRates.Utility;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Globalization;

namespace ECBCurrencyRates.ECBIntegration
{
  public class ECBApplicationLayer
  {
    IHttpClientFactory _httpClientFactory;
    ICacheProvider _cacheProvider;
    public ECBApplicationLayer(IHttpClientFactory httpclientFactory, ICacheProvider cacheProvider)
    {
      _httpClientFactory = httpclientFactory;
      _cacheProvider = cacheProvider;
    }

    public async Task<CurrencyResponseModel?> RelayCurrencyRequest
      (string baseCurrency,
      IEnumerable<string>? currenciesToCheckAgainst = null,
      DateTime? dateToCheck = null)
    {
      if (baseCurrency.Length != 3)
      {
        throw new InvalidDataException("currencyCode must be exactly 3 characters long.");
      }

      if (dateToCheck == null) dateToCheck = DateTime.Now;
      if (dateToCheck.Value.Date > DateTime.UtcNow.Date) throw new InvalidDataException("Can not ask for a date in the future");
      if (dateToCheck < DateTime.UtcNow.AddDays(-90)) throw new Exception("Can not go back further than 90 days");
      // Check if dateToCheck is Monday or Sunday
      if (dateToCheck.Value.DayOfWeek == DayOfWeek.Monday || dateToCheck.Value.DayOfWeek == DayOfWeek.Sunday)
      {
        // Adjust dateToCheck to the previous Saturday
        dateToCheck = dateToCheck.Value.AddDays(dateToCheck.Value.DayOfWeek == DayOfWeek.Sunday ? -1 : -2);
      }

      var dateYesterday = dateToCheck.Value.AddDays(-1).ToString("yyyy-MM-dd");

      var cacheKey = CacheKeyUtility.GetKey(baseCurrency, dateYesterday, currenciesToCheckAgainst);
      var cacheResult = _cacheProvider.GetCache<CurrencyResponseModel>(cacheKey);
      if (cacheResult != null) return cacheResult;

      var currentDate = dateToCheck.Value.ToString("yyyy-MM-dd");


      HttpClient client = _httpClientFactory.CreateClient();

      // Replace with your actual API endpoint URL
      StringBuilder baseUrlBuilder = new StringBuilder();
      baseUrlBuilder.Append("https://data-api.ecb.europa.eu/service/data/EXR/");

      var removeEuroEntry = false;
      if (currenciesToCheckAgainst != null && currenciesToCheckAgainst.Count() > 0)
      {
        removeEuroEntry = !currenciesToCheckAgainst.Any(x => x == "EUR");
        foreach (var currency in currenciesToCheckAgainst)
        {
          if (currency.Length != 3)
          {
            throw new InvalidDataException($"Currency code '{currency}' must be exactly 3 characters long.");
          }
        }

        var euroEntry = currenciesToCheckAgainst.FirstOrDefault(x => x == "EUR");

        // If "EUR" is found, replace it with baseCurrency
        if (euroEntry != null)
        {
          currenciesToCheckAgainst = currenciesToCheckAgainst.Select(x => x == "EUR" ? baseCurrency : x);
        }
        else
        {
          currenciesToCheckAgainst = currenciesToCheckAgainst.Concat(new[] { baseCurrency });
        }
        string currencies = string.Join("+", currenciesToCheckAgainst);
        baseUrlBuilder.Append($"D.{currencies.Trim()}.EUR.SP00.A");
      }
      else baseUrlBuilder.Append($"D..EUR.SP00.A");
      // string apiUrl = $"https://data-api.ecb.europa.eu/service/data/EXR/D+M..EUR.SP00.A?startPeriod={dateYesterday}&endPeriod={currentDate}";

      baseUrlBuilder.Append($"?startPeriod={dateYesterday}&endPeriod={currentDate}");
      var apiUrl = baseUrlBuilder.ToString();
      HttpResponseMessage response = await client.GetAsync(apiUrl);

      if (response.IsSuccessStatusCode)
      {
        using (XmlReader reader = XmlReader.Create(await response.Content.ReadAsStreamAsync()))
        {
          XmlSerializer serializer = new XmlSerializer(typeof(GenericData));


          GenericData genericData = (GenericData)serializer.Deserialize(reader);
          var responseModel = new CurrencyResponseModel { BaseCurrency = baseCurrency, CalculatedTime = DateTime.UtcNow, CurrencyRateResults = new() };

          foreach (var series in genericData.DataSet.Series)
          {
            var currencyChosen = series.SeriesKey.Values.FirstOrDefault(x => x.Id == "CURRENCY");
            var exchangeRate = series.Obs.ObsValue;
            if (currencyChosen == null || exchangeRate == null) continue;
            var calcResult = new CurrencyCalcResult { Currency = currencyChosen.Value, Rate = decimal.Parse(exchangeRate.Value, CultureInfo.InvariantCulture) };
            if (baseCurrency == "EUR") calcResult.Rate = decimal.Round(1 / calcResult.Rate, 3);
            calcResult.RateValidFrom = DateTime.Parse(series.Obs.ObsDimension.Value);
            responseModel.CurrencyRateResults.Add(calcResult);
          }

          if (baseCurrency != "EUR" && responseModel.CurrencyRateResults.Count() > 0)
          {

            var baseEntry = responseModel.CurrencyRateResults.FirstOrDefault(x => x.Currency == baseCurrency);
            if (baseEntry == null)
            {
              throw new Exception($"{baseCurrency} rate not found in the exchange rates response.");
            }
            var baseCurrencyRate = baseEntry.Rate;


            // Calculate rates relative to baseCurrency
            foreach (var result in responseModel.CurrencyRateResults)
            {
              if (result.Currency != baseCurrency)
              {
                result.Rate = decimal.Round(baseCurrencyRate / result.Rate, 3);
              }
            }

            if (baseEntry != null)
            {
              baseEntry.Currency = "EUR";
              baseEntry.Rate = baseCurrencyRate;
              if (removeEuroEntry) responseModel.CurrencyRateResults.Remove(baseEntry);
            }

          }
          responseModel.CurrencyRateResults = responseModel.CurrencyRateResults.OrderBy(x => x.Currency).ToList();


          _cacheProvider.CreateOrUpdateCache(cacheKey, responseModel);
          return responseModel;
        }
      }

      throw new Exception("Invalid Request");

    }

  }
}