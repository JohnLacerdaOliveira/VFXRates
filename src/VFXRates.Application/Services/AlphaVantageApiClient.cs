using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.Json;
using VFXRates.Application.DTOs;
using VFXRates.Application.Interfaces;

namespace VFXRates.Application.Services
{
    public class AlphaVantageApiClient : IExchangeRateApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<AlphaVantageApiClient> _logger;

        public AlphaVantageApiClient(HttpClient httpClient, IConfiguration configuration, ILogger<AlphaVantageApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = configuration["AlphaVantage:ApiKey"] ?? throw new ArgumentNullException("API key is missing");
        }

        public async Task<FxRateDto?> FetchRateAsync(string baseCurrency, string quoteCurrency)
        {
            var url = $"https://www.alphavantage.co/query?function=CURRENCY_EXCHANGE_RATE&from_currency={baseCurrency}&to_currency={quoteCurrency}&apikey={_apiKey}";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to retrieve exchange rate from AlphaVantage. Status code: {StatusCode}", response.StatusCode);
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
                _logger.LogError(ex, "Error deserializing AlphaVantage response.");
                return null;
            }

            if (exchangeRateResponse?.ExchangeRateData == null)
            {
                _logger.LogWarning("AlphaVantage response does not contain valid exchange rate data.");
                return null;
            }

            if (!decimal.TryParse(exchangeRateResponse.ExchangeRateData.Bid, CultureInfo.InvariantCulture, out var bid))
            {
                _logger.LogWarning("Failed to parse bid value: {BidValue}", exchangeRateResponse.ExchangeRateData.Bid);
                return null;
            }
            if (!decimal.TryParse(exchangeRateResponse.ExchangeRateData.Ask, CultureInfo.InvariantCulture, out var ask))
            {
                _logger.LogWarning("Failed to parse ask value: {AskValue}", exchangeRateResponse.ExchangeRateData.Ask);
                return null;
            }
            if (!DateTime.TryParse(exchangeRateResponse.ExchangeRateData.LastUpdated, CultureInfo.InvariantCulture, out var lastUpdated))
            {
                _logger.LogWarning("Failed to parse LastUpdated value: {LastUpdatedValue}", exchangeRateResponse.ExchangeRateData.LastUpdated);
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
