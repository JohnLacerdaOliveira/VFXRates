using Microsoft.EntityFrameworkCore;
using VFXRates.Infrastructure.Data.dbContext;

namespace VFXRates.API.Extensions
{
    public static class DbContextExtensions
    {
        public static IServiceCollection ConfigureDatabase(
            this IServiceCollection services, 
            IConfiguration configuration, 
            string environment)
        {
            if (environment == "IntegrationTest")
            {
                services.AddDbContext<FxRatesDbContext>(options =>
                    options.UseInMemoryDatabase("FxRatesTestDb"));
            }
            else
            {
                var connectionString = configuration.GetConnectionString("FxRatesDb");
                if (string.IsNullOrEmpty(connectionString))
                    throw new Exception("Database connection string is not configured.");

                services.AddDbContext<FxRatesDbContext>(options =>
                    options.UseSqlServer(connectionString, sqlOptions => sqlOptions.EnableRetryOnFailure()));
            }
            return services;
        }
    }
}
