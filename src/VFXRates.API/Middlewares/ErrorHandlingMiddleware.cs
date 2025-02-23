using System.Net;
using System.Text.Json;

namespace VFXRates.API.Middlewares
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ErrorHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var path = context.Request.Path;
            var query = context.Request.QueryString.Value;
            var response = new { error = $"An unexpected error occurred at {path}{query}. Please try again later." };
            var payload = JsonSerializer.Serialize(response);

            return context.Response.WriteAsync(payload);
        }
    }
}
