using System.ComponentModel.DataAnnotations;

namespace VFXRates.Application.DTOs
{
    public class UpdateFxRateDto : ICurrencyDto
    {
        [Required]
        [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency codes must be exactly 3 characters.")]
        public string BaseCurrency { get; set; } = string.Empty;

        [Required]
        [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency codes must be exactly 3 characters.")]
        public string QuoteCurrency { get; set; } = string.Empty;

        [Range(0.000001, double.MaxValue, ErrorMessage = "Bid price must be positive.")]
        public decimal Bid { get; set; }

        [Range(0.000001, double.MaxValue, ErrorMessage = "Ask price must be positive.")]
        public decimal Ask { get; set; }
    }
}
