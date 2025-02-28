using System.Net;
using System.Security.Authentication;
using System.Text.Json;

namespace VFXRates.API.Middlewares
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;
        private readonly IWebHostEnvironment _appEnv;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger, IWebHostEnvironment appEnv)
        {
            _next = next;
            _logger = logger;
            _appEnv = appEnv;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                // If an exception occurs, handle it.
                await HandleExceptionAsync(context, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            HttpStatusCode status;
            string message;
            string? stackTrace = string.Empty;

            _logger.LogError(exception, "An unhandled exception has occurred.");

            if (exception is DirectoryNotFoundException ||
                exception is DllNotFoundException ||
                exception is EntryPointNotFoundException ||
                exception is FileNotFoundException ||
                exception is KeyNotFoundException)
            {
                status = HttpStatusCode.NotFound;
            }
            else if (exception is NotImplementedException)
            {
                status = HttpStatusCode.NotImplemented;
            }
            else if (exception is UnauthorizedAccessException || 
                    exception is AuthenticationException)
            {
                status = HttpStatusCode.Unauthorized;
            }
            else if (exception is InvalidOperationException)
            {
                status = HttpStatusCode.BadRequest;
            }
            else
            {
                status = HttpStatusCode.InternalServerError;
            }

            // In development, return the full exception message and stack trace.
            if (_appEnv.IsDevelopment())
            {
                message = exception.Message;
                stackTrace = exception.StackTrace?.ToString();
            }
            else
            {
                // In production, send a generic message.
                message = "An unexpected error occurred. Please try again later.";
            }

            var responseObj = new
            {
                error = message,
                // Include the stack trace only in development.
                stackTrace = stackTrace?.ToString()
            };

            var jsonResponse = JsonSerializer.Serialize(responseObj);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)status;

            return context.Response.WriteAsync(jsonResponse);
        }
    }
}
