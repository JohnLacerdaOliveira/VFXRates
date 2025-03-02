using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using VFXRates.Application.Interfaces;
using VFXRates.Domain.Entities;
using VFXRates.Infrastructure.Data.dbContext;

namespace VFXRates.Infrastructure.Logging;

public class DbLogService : ILogService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DbLogService> _logger;

    public DbLogService(IServiceScopeFactory scopeFactory, ILogger<DbLogService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task LogDebug(string message)
    {
        await LogAsync(LogLevel.Debug, message);
    }

    public async Task LogInformation(string message)
    {
        await LogAsync(LogLevel.Information, message);
    }

    public async Task LogWarning(string message)
    {
        await LogAsync(LogLevel.Warning, message);
    }

    public async Task LogError(string message, Exception? ex = null)
    {
        await LogAsync(LogLevel.Error, message, ex);
    }

    public async Task LogCritical(string message, Exception? ex = null)
    {
        await LogAsync(LogLevel.Critical, message, ex);
    }

    private async Task LogAsync(LogLevel logLevel, string message, Exception? ex = null)
    {
        var category = GetCategory();

        var log = new Log
        {
            Timestamp = DateTime.UtcNow,
            Level = logLevel.ToString(),
            Category = category,
            Message = message,
            Exception = ex?.ToString(),
        };

        using (var scope = _scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<FxRatesDbContext>();
            dbContext.Logs.Add(log);
            await dbContext.SaveChangesAsync();
        }
    }

    private string CleanCategory(string category)
    {
        var regex = new Regex(@"^(?<caller>.+)\+<(?<method>\w+)>d__\d+\.MoveNext$");
        var match = regex.Match(category);
        if (match.Success)
        {
            string caller = match.Groups["caller"].Value;
            string method = match.Groups["method"].Value;
            return $"{caller}.{method}";
        }
        return category;
    }

    private string GetCategory()
    {
        var stackTrace = new StackTrace();
        MethodBase? callerMethod = null;

        // Filter out internal frames: skip those in the logging class and system/framework methods.
        for (int i = 0; i < stackTrace.FrameCount; i++)
        {
            var frame = stackTrace.GetFrame(i);
            var method = frame?.GetMethod();
            if (method == null)
                continue;

            var declaringType = method.DeclaringType;
            if (declaringType == null)
                continue;

            // Skip frames from our logging class
            if (declaringType.FullName.StartsWith(typeof(DbLogService).FullName))
                continue;

            // Skip system or Microsoft namespaces
            if (!string.IsNullOrEmpty(declaringType.Namespace) &&
                (declaringType.Namespace.StartsWith("System") || declaringType.Namespace.StartsWith("Microsoft")))
                continue;

            callerMethod = method;
            break;
        }

        // Build the raw category string
        var rawCategory = callerMethod != null
            ? $"{callerMethod.DeclaringType?.FullName}.{callerMethod.Name}"
            : "Unknown";

        return CleanCategory(rawCategory);
    }
}