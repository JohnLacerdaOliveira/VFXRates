using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.Text.Json;
using VFXRates.Application.DTOs;
using VFXRates.Application.Interfaces;

namespace VFXRates.Infrastructure.ApiClients
{
    public class AlphaVantageApiClient : IExchangeRateApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogService _logService;
        private readonly string _apiKey;

        public AlphaVantageApiClient(HttpClient httpClient, IConfiguration configuration, ILogService logService)
        {
            _httpClient = httpClient;
            _logService = logService;
            _apiKey = configuration["AlphaVantage:ApiKey"] ??
                throw new ArgumentNullException("API key is missing");
        }

        public async Task<FxRateDto?> FetchRateAsync(string baseCurrency, string quoteCurrency)
        {
            var url = $"https://www.alphavantage.co/query?function=CURRENCY_EXCHANGE_RATE&from_currency={baseCurrency}&to_currency={quoteCurrency}&apikey={_apiKey}";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                await _logService.LogWarning($"Failed to retrieve exchange rate from AlphaVantage. Status code: {response.StatusCode}");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();

            AlphaVantageResponseDto? exchangeRateResponse;
            try
            {
                exchangeRateResponse = JsonSerializer.Deserialize<AlphaVantageResponseDto>(json);
            }
            catch (Exception ex)
            {
                await _logService.LogError("Error deserializing AlphaVantage response.", ex);
                return null;
            }

            if (exchangeRateResponse?.ExchangeRateData == null)
            {
                await _logService.LogWarning("AlphaVantage response does not contain valid exchange rate data.");
                return null;
            }

            if (!decimal.TryParse(exchangeRateResponse.ExchangeRateData.Bid, CultureInfo.InvariantCulture, out var bid))
            {
                await _logService.LogWarning($"Failed to parse bid value: {exchangeRateResponse.ExchangeRateData.Bid}");
                return null;
            }
            if (!decimal.TryParse(exchangeRateResponse.ExchangeRateData.Ask, CultureInfo.InvariantCulture, out var ask))
            {
                await _logService.LogWarning($"Failed to parse ask value: {exchangeRateResponse.ExchangeRateData.Ask}");
                return null;
            }
            if (!DateTime.TryParse(exchangeRateResponse.ExchangeRateData.LastUpdated, CultureInfo.InvariantCulture, out var lastUpdated))
            {
                await _logService.LogWarning("Failed to parse LastUpdated value: {exchangeRateResponse.ExchangeRateData.LastUpdated}");
                return null;
            }

            return new FxRateDto
            {
                BaseCurrency = exchangeRateResponse.ExchangeRateData.BaseCurrency,
                QuoteCurrency = exchangeRateResponse.ExchangeRateData.QuoteCurrency,
                Bid = bid,
                Ask = ask,
                LastUpdated = lastUpdated
            };
        }
    }
}
