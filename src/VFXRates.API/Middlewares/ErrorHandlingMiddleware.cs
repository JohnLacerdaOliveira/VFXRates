using System.Net;
using System.Text.Json;

namespace VFXRates.API.Middlewares
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                // Continue processing the request pipeline.
                await _next(context);
            }
            catch (Exception ex)
            {
                // Log the exception and return a standardized error response.
                _logger.LogError(ex, "An unhandled exception occurred.");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = new { error = "An unexpected error occurred. Please try again later." };
            var payload = JsonSerializer.Serialize(response);

            return context.Response.WriteAsync(payload);
        }
    }
}
