using Microsoft.EntityFrameworkCore;
using VFXRates.Domain.Entities;

namespace VFXRates.Infrastructure.Data.dbContext
{
    public class FxRatesDbContext : DbContext
    {
        public FxRatesDbContext(DbContextOptions<FxRatesDbContext> options)
           : base(options)
        {
        }

        public DbSet<FxRate> FxRates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Bypass seeding in test scenarios
            if (Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory") return;

            // Use a fixed date value instead of DateTime.UtcNow to seed data
            var fixedDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            modelBuilder.Entity<FxRate>().HasData(
                new FxRate
                {
                    Id = 1,
                    BaseCurrency = "USD",
                    QuoteCurrency = "EUR",
                    Bid = 0.9000m,
                    Ask = 0.9100m,
                    LastUpdated = fixedDate
                },
                new FxRate
                {
                    Id = 2,
                    BaseCurrency = "EUR",
                    QuoteCurrency = "GBP",
                    Bid = 0.8000m,
                    Ask = 0.8100m,
                    LastUpdated = fixedDate
                },
                new FxRate
                {
                    Id = 3,
                    BaseCurrency = "USD",
                    QuoteCurrency = "JPY",
                    Bid = 110.5000m,
                    Ask = 110.6000m,
                    LastUpdated = fixedDate
                },
                new FxRate
                {
                    Id = 4,
                    BaseCurrency = "AUD",
                    QuoteCurrency = "USD",
                    Bid = 0.7000m,
                    Ask = 0.7100m,
                    LastUpdated = fixedDate
                },
                new FxRate
                {
                    Id = 5,
                    BaseCurrency = "CAD",
                    QuoteCurrency = "USD",
                    Bid = 0.7500m,
                    Ask = 0.7600m,
                    LastUpdated = fixedDate
                },
                new FxRate
                {
                    Id = 6,
                    BaseCurrency = "CHF",
                    QuoteCurrency = "EUR",
                    Bid = 0.9200m,
                    Ask = 0.9300m,
                    LastUpdated = fixedDate
                }
            );
        }
    }
}
