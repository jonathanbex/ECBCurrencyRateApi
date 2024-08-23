# ECB Currency REST API

This project provides a simple REST API to fetch currency exchange rates from the European Central Bank (ECB). The ECB sets currency rates at 16:00 CET on the previous day.

## Endpoints

### 1. Get Exchange Rates

**Endpoint**: `/GetExchangeRates`

**Description**: Fetches the latest exchange rates for a given base currency. Optionally, you can filter the rates to only include specific currencies.

**Method**: `GET`

**Parameters**:
- `currencyCode` (string, required): The base currency code (ISO 4217, 3 letters).
- `currenciesToCheck` (string, optional): A comma-separated list of currency codes (ISO 4217, 3 letters) to filter the results. Example: `SEK,EUR,USD`.

**Example Request**:
/GetExchangeRates?currencyCode=EUR&currenciesToCheck=USD,GBP,JPY
**Example Response**:
```json
{
    "baseCurrency": "EUR",
    "calculatedTime": "2024-08-23T14:19:12.0373205Z",
    "currencyRateResults": [
        {
            "currency": "USD",
            "rate": 10.231,
            "rateValidFrom": "2024-08-22T00:00:00"
        },
        {
            "currency": "GBP",
            "rate": 13.411,
            "rateValidFrom": "2024-08-22T00:00:00"
        },
        {
            "currency": "JPY",
            "rate": 0.070,
            "rateValidFrom": "2024-08-22T00:00:00"
        }
    ]
}
```
### 2. Get Exchange Rates for a Specific Day

**Endpoint**: `/GetExchangeRatesForSpecificDay`

**Description**: Fetches exchange rates for a given base currency on a specific day. Optionally, you can filter the rates to only include specific currencies.

**Method**: `GET`

**Parameters**:
- `currencyCode` (string, required): The base currency code (ISO 4217, 3 letters).
- `day` (DateTime, required): The specific day to fetch the rates for. The time part is ignored.
- `currenciesToCheck` (string, optional): A comma-separated list of currency codes (ISO 4217, 3 letters) to filter the results. Example: `SEK,EUR,USD`.

**Example Request**:

Certainly! Here's the markdown for the second endpoint, "Get Exchange Rates for a Specific Day":

markdown
Kopiera kod
### 2. Get Exchange Rates for a Specific Day

**Endpoint**: `/GetExchangeRatesForSpecificDay`

**Description**: Fetches exchange rates for a given base currency on a specific day. Optionally, you can filter the rates to only include specific currencies.

**Method**: `GET`

**Parameters**:
- `currencyCode` (string, required): The base currency code (ISO 4217, 3 letters).
- `day` (DateTime, required): The specific day to fetch the rates for. The time part is ignored.
- `currenciesToCheck` (string, optional): A comma-separated list of currency codes (ISO 4217, 3 letters) to filter the results. Example: `SEK,EUR,USD`.

**Example Request**:
/GetExchangeRatesForSpecificDay?currencyCode=EUR&day=2023-08-22&currenciesToCheck=USD,GBP,JPY

**Example Response**:
```json
{
    "baseCurrency": "EUR",
    "day": "2023-08-22",
    "currencyRateResults": [
        {
            "currency": "USD",
            "rate": 10.231,
            "rateValidFrom": "2024-08-22T00:00:00"
        },
        {
            "currency": "GBP",
            "rate": 13.411,
            "rateValidFrom": "2024-08-22T00:00:00"
        },
        {
            "currency": "JPY",
            "rate": 0.070,
            "rateValidFrom": "2024-08-22T00:00:00"
        }
    ]
}
```
