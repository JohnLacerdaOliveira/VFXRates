using Microsoft.OpenApi.Models;
using VFXRates.Application.Interfaces;
using VFXRates.Application.Services;
using VFXRates.Infrastructure.Repositories;

namespace VFXRates.API.Extensions
{
    public static class ServicesExtensions
    {
        public static IServiceCollection ConfigureApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IFxRateRepository, FxRateRepository>();
            services.AddScoped<IFxRateService, FxRateService>();

            return services;
        }
        public static IServiceCollection ConfigureExternalServices(this IServiceCollection services)
        {
            services.AddHttpClient<IExchangeRateApiClient, AlphaVantageApiClient>();
            services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();

            return services;
        }
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
    }
}
