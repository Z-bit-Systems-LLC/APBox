using System.Collections.Concurrent;
using ApBox.Core.Models;
using ApBox.Core.Services.Infrastructure;
using ApBox.Core.Services.Infrastructure;
using Microsoft.Extensions.Logging;

namespace ApBox.Core.Logging;

/// <summary>
/// Custom log provider that captures application logs and feeds them to the LogService
/// </summary>
public class ApBoxLogProvider : ILoggerProvider
{
    private readonly ILogService _logService;
    private readonly ConcurrentDictionary<string, ApBoxLogger> _loggers = new();

    public ApBoxLogProvider(ILogService logService)
    {
        _logService = logService;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new ApBoxLogger(name, _logService));
    }

    public void Dispose()
    {
        _loggers.Clear();
    }
}

/// <summary>
/// Custom logger implementation that captures logs and sends them to LogService
/// </summary>
public class ApBoxLogger : ILogger
{
    private readonly string _categoryName;
    private readonly ILogService _logService;

    public ApBoxLogger(string categoryName, ILogService logService)
    {
        _categoryName = categoryName;
        _logService = logService;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default!;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        if (string.IsNullOrEmpty(message))
            return;

        var logEntry = new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = logLevel,
            Source = GetSourceName(_categoryName),
            Message = message,
            Exception = exception?.ToString()
        };

        // Fire and forget - don't block the calling thread
        _ = Task.Run(async () =>
        {
            try
            {
                await _logService.AddLogEntryAsync(logEntry);
            }
            catch
            {
                // Swallow exceptions to prevent infinite logging loops
            }
        });
    }

    private static string GetSourceName(string categoryName)
    {
        // Simplify category names for better display
        return categoryName switch
        {
            var name when name.StartsWith("ApBox.Core") => "ApBox.Core",
            var name when name.StartsWith("ApBox.Web") => "ApBox.Web",
            var name when name.StartsWith("ApBox.Plugins") => "ApBox.Plugins",
            var name when name.StartsWith("Microsoft.AspNetCore") => "AspNetCore",
            var name when name.StartsWith("Microsoft.EntityFrameworkCore") => "EntityFramework",
            var name when name.StartsWith("Microsoft.Extensions") => "Extensions",
            var name when name.StartsWith("System") => "System",
            _ => categoryName.Split('.').LastOrDefault() ?? categoryName
        };
    }
}