using Microsoft.EntityFrameworkCore;
using VFXRates.API.Middlewares;
using VFXRates.Infrastructure.Data.dbContext;

namespace VFXRates.API.Extensions
{
    public static class WebApplicationExtensions
    {
        public static void ApplyDatabaseMigrations(this WebApplication app)
        {
            bool isIntegrationTest = app.Environment.IsEnvironment("IntegrationTest");
            bool isDevelopment = app.Environment.IsDevelopment();
            var logger = app.Logger;

            if (!isIntegrationTest)
            {
                using (var scope = app.Services.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<FxRatesDbContext>();

                    if (isDevelopment)
                    {
                        logger.LogInformation("Development environment detected. Dropping existing database.");
                        dbContext.Database.EnsureDeleted();
                    }

                    try
                    {
                        logger.LogInformation("Applying database migrations.");
                        dbContext.Database.Migrate();
                        logger.LogInformation("Database migration applied successfully.");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error during database migration.");
                        throw;
                    }
                }
            }
        }

        public static void ConfigureMiddleware(this WebApplication app)
        {
            app.UseMiddleware<ErrorHandlingMiddleware>();
        }

        public static void ConfigureDevelopmentEnvironment(this WebApplication app)
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        public static void ConfigureProductionEnvironment(this WebApplication app)
        {

            app.UseHttpsRedirection();

        }
    }
}
