using Microsoft.EntityFrameworkCore;

namespace VFXRates.Domain.Entities
{
    [Index(nameof(BaseCurrency), nameof(QuoteCurrency), IsUnique = true)]
    public class FxRate
    {
        public int Id { get; set; }
        public required string BaseCurrency { get; set; }
        public required string QuoteCurrency { get; set; }
        public required decimal Bid { get; set; }
        public required decimal Ask { get; set; }
        public required DateTime LastUpdated { get; set; }
    }
}
