using static ECBCurrencyRates.ECBIntegration.Models.ECBSeriesModel;
using System.Xml.Serialization;
using System.Xml;
using System.Text;
using ECBCurrencyRates.Models;
using ECBCurrencyRates.Cache;
using ECBCurrencyRates.Utility;
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
      // Validate base currency
      if (string.IsNullOrWhiteSpace(baseCurrency))
      {
        throw new ArgumentException("baseCurrency must be provided.", nameof(baseCurrency));
      }

      baseCurrency = baseCurrency.Trim().ToUpperInvariant();

      if (baseCurrency.Length != 3)
      {
        throw new InvalidDataException("currencyCode must be exactly 3 characters long.");
      }

      var utcNow = DateTime.UtcNow;
      if (dateToCheck == null) dateToCheck = utcNow;

      // Compare by date only
      if (dateToCheck.Value.Date > utcNow.Date) throw new InvalidDataException("Can not ask for a date in the future");
      if (dateToCheck.Value < utcNow.AddDays(-90)) throw new InvalidDataException("Can not go back further than 90 days");

      // If the requested date is Monday or Sunday, adjust to the previous Saturday (data availability reasons)
      if (dateToCheck.Value.DayOfWeek == DayOfWeek.Monday || dateToCheck.Value.DayOfWeek == DayOfWeek.Sunday)
      {
        dateToCheck = dateToCheck.Value.AddDays(dateToCheck.Value.DayOfWeek == DayOfWeek.Sunday ? -1 : -2);
      }

      // Use adjusted date for the API request
      var dateYesterday = dateToCheck.Value.AddDays(-1).ToString("yyyy-MM-dd");
      var currentDate = dateToCheck.Value.ToString("yyyy-MM-dd");

      // Normalize and validate currencies list
      var removeEuroEntry = false;
      List<string>? currenciesList = null;
      if (currenciesToCheckAgainst != null)
      {
        currenciesList = currenciesToCheckAgainst
          .Where(x => !string.IsNullOrWhiteSpace(x))
          .Select(x => x.Trim().ToUpperInvariant())
          .ToList();

        if (currenciesList.Count == 0) currenciesList = null;
      }

      if (currenciesList != null && currenciesList.Count > 0)
      {
        foreach (var currency in currenciesList)
        {
          if (currency.Length != 3)
          {
            throw new InvalidDataException($"Currency code '{currency}' must be exactly 3 characters long.");
          }
        }

        // If EUR is present in the list, replace it with baseCurrency (as original logic)
        var hasEur = currenciesList.Any(x => x == "EUR");
        removeEuroEntry = !hasEur; // original intent preserved

        if (hasEur)
        {
          for (int i = 0; i < currenciesList.Count; i++)
          {
            if (currenciesList[i] == "EUR") currenciesList[i] = baseCurrency;
          }
        }
        else
        {
          // ensure base currency is included for relative calculations
          if (!currenciesList.Contains(baseCurrency)) currenciesList.Add(baseCurrency);
        }
      }

      var cacheKey = CacheKeyUtility.GetKey(baseCurrency, dateYesterday, currenciesList);
      var cacheResult = _cacheProvider.GetCache<CurrencyResponseModel>(cacheKey);
      if (cacheResult != null) return cacheResult;

      HttpClient client = _httpClientFactory.CreateClient();

      StringBuilder baseUrlBuilder = new StringBuilder();
      baseUrlBuilder.Append("https://data-api.ecb.europa.eu/service/data/EXR/");

      if (currenciesList != null && currenciesList.Count > 0)
      {
        string currencies = string.Join("+", currenciesList);
        baseUrlBuilder.Append($"D.{currencies.Trim()}.EUR.SP00.A");
      }
      else
      {
        baseUrlBuilder.Append($"D..EUR.SP00.A");
      }

      baseUrlBuilder.Append($"?startPeriod={dateYesterday}&endPeriod={currentDate}");
      var apiUrl = baseUrlBuilder.ToString();

      HttpResponseMessage response;
      try
      {
        response = await client.GetAsync(apiUrl);
      }
      catch (HttpRequestException hx)
      {
        throw new HttpRequestException($"Error while calling ECB API: {hx.Message}", hx);
      }
      catch (TaskCanceledException tcx)
      {
        throw new HttpRequestException("Request to ECB API was canceled or timed out.", tcx);
      }

      if (!response.IsSuccessStatusCode)
      {
        var status = (int)response.StatusCode;
        var reason = response.ReasonPhrase;
        throw new HttpRequestException($"ECB API returned non-success status code {status}: {reason}");
      }

      // At this point we have a success response
      try
      {
        using (var stream = await response.Content.ReadAsStreamAsync())
        using (XmlReader reader = XmlReader.Create(stream))
        {
          XmlSerializer serializer = new XmlSerializer(typeof(GenericData));

          GenericData? genericData;
          try
          {
            genericData = serializer.Deserialize(reader) as GenericData;
          }
          catch (Exception ex)
          {
            throw new InvalidDataException("Failed to deserialize ECB response XML.", ex);
          }

          if (genericData == null || genericData.DataSet == null || genericData.DataSet.Series == null)
          {
            throw new InvalidDataException("ECB response contained no usable data.");
          }

          var responseModel = new CurrencyResponseModel { BaseCurrency = baseCurrency, CalculatedTime = DateTime.UtcNow, CurrencyRateResults = new() };

          foreach (var series in genericData.DataSet.Series)
          {
            if (series?.SeriesKey?.Values == null || series.Obs == null) continue;

            var currencyChosen = series.SeriesKey.Values.FirstOrDefault(x => x.Id == "CURRENCY");
            var exchangeRate = series.Obs.ObsValue;
            if (currencyChosen == null || exchangeRate == null || string.IsNullOrWhiteSpace(exchangeRate.Value)) continue;

            decimal parsedResult;
            var parsed = decimal.TryParse(exchangeRate.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out parsedResult)
                      || decimal.TryParse(exchangeRate.Value, NumberStyles.Any, CultureInfo.CurrentCulture, out parsedResult);

            if (!parsed) continue;

            var calcResult = new CurrencyCalcResult { Currency = currencyChosen.Value, Rate = parsedResult };

            // If the requested base currency is EUR, invert the rate (avoid division by zero)
            if (baseCurrency == "EUR")
            {
              if (calcResult.Rate == 0) continue;
              calcResult.Rate = decimal.Round(1m / calcResult.Rate, 3);
            }

            // Parse the date if present
            if (!string.IsNullOrWhiteSpace(series.Obs.ObsDimension?.Value))
            {
              if (DateTime.TryParse(series.Obs.ObsDimension.Value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsedDate))
              {
                calcResult.RateValidFrom = parsedDate;
              }
              else if (DateTime.TryParse(series.Obs.ObsDimension.Value, CultureInfo.CurrentCulture, DateTimeStyles.None, out parsedDate))
              {
                calcResult.RateValidFrom = parsedDate;
              }
            }

            responseModel.CurrencyRateResults.Add(calcResult);
          }

          if (baseCurrency != "EUR" && responseModel.CurrencyRateResults.Count > 0)
          {
            var baseEntry = responseModel.CurrencyRateResults.FirstOrDefault(x => x.Currency == baseCurrency);
            if (baseEntry == null)
            {
              throw new InvalidDataException($"{baseCurrency} rate not found in the exchange rates response.");
            }

            var baseCurrencyRate = baseEntry.Rate;
            if (baseCurrencyRate == 0) throw new InvalidDataException($"Base currency rate for {baseCurrency} is zero, cannot normalize other rates.");

            foreach (var result in responseModel.CurrencyRateResults)
            {
              if (result.Currency != baseCurrency)
              {
                // Prevent division by zero
                if (result.Rate == 0) continue;
                result.Rate = decimal.Round(baseCurrencyRate / result.Rate, 3);
              }
            }

            // Transform the base entry to EUR representation and optionally remove it
            baseEntry.Currency = "EUR";
            baseEntry.Rate = baseCurrencyRate;
            if (removeEuroEntry)
            {
              responseModel.CurrencyRateResults.Remove(baseEntry);
            }
          }

          responseModel.CurrencyRateResults = responseModel.CurrencyRateResults.OrderBy(x => x.Currency).ToList();

          // Cache only when we have results
          if (responseModel.CurrencyRateResults.Any())
          {
            _cacheProvider.CreateOrUpdateCache(cacheKey, responseModel);
          }

          return responseModel;
        }
      }
      catch (InvalidDataException)
      {
        // Let callers handle meaningful InvalidDataException
        throw;
      }
      catch (Exception ex)
      {
        throw new Exception("Failed to process response from ECB API.", ex);
      }

    }

  }
}
