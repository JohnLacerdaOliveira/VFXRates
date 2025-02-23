namespace VFXRates.Application.Interfaces;

public interface ILogService
{
    Task LogDebug(string message);
    Task LogInformation(string message);
    Task LogWarning(string message);
    Task LogError(string message, Exception? ex = null);
    Task LogCritical(string message, Exception? ex = null);
}