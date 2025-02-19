using DotNetEnv;
using VFXRates.API.Extensions;
using VFXRates.API.Middlewares;

namespace VFXRates.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @"..\.."));
            Env.Load(Path.Combine(projectRoot, ".env"));

            var builder = WebApplication.CreateBuilder(args);

            builder.Services.ConfigureCustomLogging();
            builder.Services.ConfigureApiServices();
            builder.Services.ConfigureApplicationServices();
            builder.Services.ConfigureExternalServices();
            builder.Services.ConfigureDatabase(builder.Configuration, builder.Environment.EnvironmentName);

            var app = builder.Build();

            // Apply database migrations using your HostExtensions method
            app.ApplyDatabaseMigrations();

            app.UseMiddleware<ErrorHandlingMiddleware>();
            app.UseAuthorization();
            app.MapControllers();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.Run();

        }
    }
}