namespace VFXRates.Application.DTOs
{
    public class FxRateDto
    {
        public required string BaseCurrency { get; init; }
        public required string QuoteCurrency { get; init; }
        public required decimal Bid { get; init; }
        public required decimal Ask { get; init; }
        public DateTime LastUpdated { get; init; }
    }
}
