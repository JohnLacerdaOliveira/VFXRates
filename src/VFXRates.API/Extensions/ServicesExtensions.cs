using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using VFXRates.Application.Interfaces;
using VFXRates.Application.Services;
using VFXRates.Infrastructure.ApiClients;
using VFXRates.Infrastructure.Data.dbContext;
using VFXRates.Infrastructure.Logging;
using VFXRates.Infrastructure.Repositories;
using VFXRates.TestUtilities;
using AuthenticationService = VFXRates.Application.Services.AuthenticationService;

namespace VFXRates.API.Extensions
{
    public static class ServicesExtensions
    {
        public static IServiceCollection ConfigureApiServices(this IServiceCollection services)
        {
            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "VFXRates API",
                    Version = "v1"
                });
            });

            return services;
        }
        public static IServiceCollection ConfigureApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IFxRatesRepository, FxRatesRepository>();
            services.AddScoped<IAuthRepository, AuthRepository>();
            services.AddScoped<IFxRateService, FxRateService>();
            services.AddScoped<IAuthService, AuthenticationService>();

            return services;
        }

        public static IServiceCollection ConfigureAuthenticationServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {

            if (configuration["Environment"] == "IntegrationTest")
            {
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
                services.Configure<AuthenticationOptions>(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                });

                return services;
            }

            var jwtSettings = configuration.GetSection("Jwt");
            var secretKey = jwtSettings["Secret"] ??
                throw new ArgumentNullException("Jwt secret is null or empty");

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secretKey)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSettings["Audience"],
                    ClockSkew = TimeSpan.FromMinutes(5)
                };
            });

            return services;
        }

        public static IServiceCollection ConfigureExternalServices(this IServiceCollection services,
          string environment)
        {
            services.AddScoped<ILogService, DbLogService>();

            if (environment == "Development")
            {
                services.AddLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.AddConsole();
                });
            }

            services.AddHttpClient<IExchangeRateApiClient, AlphaVantageApiClient>();
            services.AddScoped<IRabbitMqPublisher, RabbitMqPublisher>();

            return services;
        }

        public static IServiceCollection ConfigureDatabaseServices(
      this IServiceCollection services,
      IConfiguration configuration,
      string environment)
        {
            ArgumentNullException.ThrowIfNull(environment, nameof(environment));

            string? connectionString = environment switch
            {
                "IntegrationTest" => null,
                "Development" => configuration.GetConnectionString("FxRatesDevDb"),
                "Production" => configuration.GetConnectionString("FxRatesProdDb"),
                "Docker" => configuration.GetConnectionString("FxRatesDockerDb"),
                _ => throw new ArgumentException($"Unsupported environment: {environment}", nameof(environment))
            };

            if (environment != "IntegrationTest" && string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException($"Database connection string for environment '{environment}' is not configured.");
            }

            services.AddDbContext<FxRatesDbContext>(options =>
            {
                if (environment == "IntegrationTest")
                {
                    options.UseInMemoryDatabase("FxRatesTestDb");
                }
                else
                {
                    options.UseSqlServer(connectionString, sqlOptions =>
                        sqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(30), null));
                }
            });

            return services;
        }
    }
}
