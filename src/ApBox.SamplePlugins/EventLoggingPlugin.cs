using ApBox.Plugins;
using Microsoft.Extensions.Logging;

namespace ApBox.SamplePlugins;

/// <summary>
/// Sample event logging plugin that demonstrates how to log card events
/// to the standard .NET logging infrastructure
/// </summary>
public class EventLoggingPlugin : IApBoxPlugin
{
    private readonly ILogger<EventLoggingPlugin>? _logger;
    private int _totalEvents = 0;
    private int _successfulEvents = 0;
    private int _failedEvents = 0;

    public EventLoggingPlugin()
    {
    }

    public EventLoggingPlugin(ILogger<EventLoggingPlugin> logger)
    {
        _logger = logger;
    }

    public string Name => "Event Logging Plugin";
    public string Version => "1.0.0";
    public string Description => "Logs card events to the standard .NET logging infrastructure with statistics tracking";

    public async Task<bool> ProcessCardReadAsync(CardReadEvent cardRead)
    {
        await Task.CompletedTask; // Async signature for future extensibility

        // Increment counters
        Interlocked.Increment(ref _totalEvents);

        // Log detailed event information
        _logger?.LogInformation(
            "Card read event: Reader={ReaderName} ({ReaderId}), Card={CardNumber}, Bits={BitLength}, Time={Timestamp:yyyy-MM-dd HH:mm:ss.fff}",
            cardRead.ReaderName,
            cardRead.ReaderId,
            cardRead.CardNumber,
            cardRead.BitLength,
            cardRead.Timestamp);

        // Log additional data if present
        if (cardRead.AdditionalData?.Any() == true)
        {
            foreach (var kvp in cardRead.AdditionalData)
            {
                _logger?.LogDebug("Card read additional data: {Key}={Value}", kvp.Key, kvp.Value);
            }
        }

        // Event logging plugin is passive - it doesn't make access decisions
        // It just logs and tracks statistics
        Interlocked.Increment(ref _successfulEvents);
        
        return true;
    }

    public Task InitializeAsync()
    {
        _logger?.LogInformation("Event Logging Plugin initialized");
        _logger?.LogInformation("Plugin will log all card read events at Information level");
        
        // Log configuration information
        _logger?.LogDebug("Event Logging Plugin configuration: TotalEvents={TotalEvents}, SuccessfulEvents={SuccessfulEvents}, FailedEvents={FailedEvents}",
            _totalEvents, _successfulEvents, _failedEvents);
            
        return Task.CompletedTask;
    }

    public Task ShutdownAsync()
    {
        // Log final statistics
        _logger?.LogInformation("Event Logging Plugin shutting down. Final statistics: Total={Total}, Successful={Successful}, Failed={Failed}",
            _totalEvents, _successfulEvents, _failedEvents);
            
        return Task.CompletedTask;
    }

    /// <summary>
    /// Get current event statistics
    /// </summary>
    public EventStatistics GetStatistics()
    {
        return new EventStatistics
        {
            TotalEvents = _totalEvents,
            SuccessfulEvents = _successfulEvents,
            FailedEvents = _failedEvents,
            SuccessRate = _totalEvents > 0 ? (double)_successfulEvents / _totalEvents : 0.0
        };
    }

    /// <summary>
    /// Reset event counters (for testing or maintenance)
    /// </summary>
    public void ResetStatistics()
    {
        _totalEvents = 0;
        _successfulEvents = 0;
        _failedEvents = 0;
        
        _logger?.LogInformation("Event statistics reset to zero");
    }

    /// <summary>
    /// Log a custom event (for demonstration of plugin extensibility)
    /// </summary>
    public void LogCustomEvent(string eventType, string message, object? data = null)
    {
        if (data != null)
        {
            _logger?.LogInformation("Custom event [{EventType}]: {Message}. Data: {@Data}", eventType, message, data);
        }
        else
        {
            _logger?.LogInformation("Custom event [{EventType}]: {Message}", eventType, message);
        }
    }

    /// <summary>
    /// Log an error event
    /// </summary>
    public void LogError(string message, Exception? exception = null)
    {
        if (exception != null)
        {
            _logger?.LogError(exception, "Plugin error: {Message}", message);
        }
        else
        {
            _logger?.LogError("Plugin error: {Message}", message);
        }
    }

    /// <summary>
    /// Log a warning event
    /// </summary>
    public void LogWarning(string message, object? data = null)
    {
        if (data != null)
        {
            _logger?.LogWarning("Plugin warning: {Message}. Data: {@Data}", message, data);
        }
        else
        {
            _logger?.LogWarning("Plugin warning: {Message}", message);
        }
    }
}

/// <summary>
/// Statistics tracked by the Event Logging Plugin
/// </summary>
public class EventStatistics
{
    /// <summary>
    /// Total number of events processed
    /// </summary>
    public int TotalEvents { get; set; }
    
    /// <summary>
    /// Number of successful events
    /// </summary>
    public int SuccessfulEvents { get; set; }
    
    /// <summary>
    /// Number of failed events
    /// </summary>
    public int FailedEvents { get; set; }
    
    /// <summary>
    /// Success rate as a percentage (0.0 to 1.0)
    /// </summary>
    public double SuccessRate { get; set; }
}