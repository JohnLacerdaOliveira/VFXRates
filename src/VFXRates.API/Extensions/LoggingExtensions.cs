namespace VFXRates.API.Extensions
{
    public static class LoggingExtensions
    {
        public static IServiceCollection ConfigureCustomLogging(this IServiceCollection services)
        {
            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.AddConsole();
            });

            return services;
        }
    }
}
