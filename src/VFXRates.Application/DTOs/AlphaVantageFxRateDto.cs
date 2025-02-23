using System.Text.Json.Serialization;

namespace VFXRates.Application.DTOs
{

    public class AlphaVantageResponseDto
    {
        [JsonPropertyName("Realtime Currency Exchange Rate")]
        public AlphaVantageFxRateDto ExchangeRateData { get; set; } = new();
    }

    public class AlphaVantageFxRateDto : ICurrencyDto
    {
        [JsonPropertyName("1. From_Currency Code")]
        public string BaseCurrency { get; set; } = string.Empty;

        [JsonPropertyName("3. To_Currency Code")]
        public string QuoteCurrency { get; set; } = string.Empty;

        [JsonPropertyName("8. Bid Price")]
        public string Bid { get; set; } = string.Empty;

        [JsonPropertyName("9. Ask Price")]
        public string Ask { get; set; } = string.Empty;

        [JsonPropertyName("6. Last Refreshed")]
        public string LastUpdated { get; set; } = string.Empty;
    }

}
