using System.Net;
using System.Security.Authentication;
using System.Text.Json;
using VFXRates.Application.Interfaces;

namespace VFXRates.API.Middlewares
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogService _logService;
        private readonly IWebHostEnvironment _appEnv;

        public ErrorHandlingMiddleware(
            RequestDelegate next, 
            ILogService logService, 
            IWebHostEnvironment appEnv)
        {
            _next = next;
            _logService = logService;
            _appEnv = appEnv;
        }

        public async Task InvokeAsync(HttpContext context)
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

        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            HttpStatusCode status;
            string message;
            string? stackTrace = string.Empty;

            await _logService.LogError("An unhandled exception has occurred.", ex);

            if (ex is DirectoryNotFoundException ||
                ex is DllNotFoundException ||
                ex is EntryPointNotFoundException ||
                ex is FileNotFoundException ||
                ex is KeyNotFoundException)
            {
                status = HttpStatusCode.NotFound;
            }
            else if (ex is NotImplementedException)
            {
                status = HttpStatusCode.NotImplemented;
            }
            else if (ex is UnauthorizedAccessException || 
                    ex is AuthenticationException)
            {
                status = HttpStatusCode.Unauthorized;
            }
            else if (ex is InvalidOperationException)
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
                message = ex.Message;
                stackTrace = ex.StackTrace?.ToString();
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

            await context.Response.WriteAsync(jsonResponse);
        }
    }
}
