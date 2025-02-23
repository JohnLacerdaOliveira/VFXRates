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
        public DbSet<User> Users { get; set; }
        public DbSet<Log> Logs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Bypass seeding in test scenarios
            if (Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
            {
                return;
            }

            ConfigureFxRates(modelBuilder);
            ConfigureUsers(modelBuilder);
            ConfigureLogs(modelBuilder);
        }

        private static void ConfigureFxRates(ModelBuilder modelBuilder)
        {
            var fixedDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            modelBuilder.Entity<FxRate>(entity =>
            {
                entity.HasKey(fxRate => fxRate.Id);
                entity.Property(fxRate => fxRate.BaseCurrency).IsRequired().HasMaxLength(3);
                entity.Property(fxRate => fxRate.QuoteCurrency).IsRequired().HasMaxLength(3);
                entity.Property(fxRate => fxRate.Bid).IsRequired().HasColumnType("decimal(18,6)");
                entity.Property(fxRate => fxRate.Ask).IsRequired().HasColumnType("decimal(18,6)");
                entity.Property(fxRate => fxRate.LastUpdated).IsRequired();
                entity.HasIndex(fxRate => 
                new { fxRate.BaseCurrency, fxRate.QuoteCurrency }).IsUnique();
            });

            // Seed data
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

        private static void ConfigureUsers(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(user => user.Id);
                entity.HasIndex(user => user.Username).IsUnique();
                entity.Property(user => user.Username).IsRequired().HasMaxLength(50);
                entity.Property(user => user.PasswordHash).IsRequired();
            });

            // Seed data
            modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 1,
                Username = "testuser",             
                PasswordHash = "$2a$11$9jzz74pa6aZj/omFYbXETOknRyLNz2YcdNDrpk1bx2UeDkHOjPY.S" //testpassword
            });
           
        }

        private static void ConfigureLogs(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Log>(entity =>
            {
                entity.HasKey(log => log.Id);
                entity.Property(log => log.Timestamp).HasColumnType("datetime2").IsRequired();
                entity.Property(log => log.Level).HasMaxLength(50).IsRequired();
                entity.Property(log => log.Category).HasMaxLength(1000).IsRequired();
                entity.Property(log => log.Message).HasMaxLength(4000).IsRequired();
                entity.Property(log => log.Exception).HasMaxLength(4000);
            });                        
        }
    }
}