using DotNetEnv;
using VFXRates.API.Extensions;

namespace VFXRates.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @"..\.."));
            Env.Load(Path.Combine(projectRoot, ".env"));

            var builder = WebApplication.CreateBuilder(args);

            builder.Services.ConfigureApiServices();

            builder.Services.ConfigureApplicationServices(
                builder.Environment.EnvironmentName);

            builder.Services.ConfigureAuthenticationServices(
                builder.Configuration);

            builder.Services.ConfigureExternalServices();

            builder.Services.ConfigureDatabaseServices(
                builder.Configuration,
                builder.Environment.EnvironmentName);

            var app = builder.Build();

            app.ApplyDatabaseMigrations();
            app.ConfigureMiddleware();

            if (app.Environment.IsDevelopment())
            {
                app.ConfigureDevelopmentEnvironment();
            }

            if (app.Environment.IsProduction())
            {
                app.ConfigureProductionEnvironment();
            }

            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}