using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace VFXRates.Domain.Entities
{
    [Index(nameof(BaseCurrency), nameof(QuoteCurrency), IsUnique = true)]
    public class FxRate
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string BaseCurrency { get; set; } = string.Empty;

        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string QuoteCurrency { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,6)")]
        public decimal Bid { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,6)")]
        public decimal Ask { get; set; }

        [Required]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
