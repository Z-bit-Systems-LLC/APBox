namespace ApBox.Core.Models;

/// <summary>
/// Represents a log entry in the system
/// </summary>
public class LogEntry
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public LogLevel Level { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public Dictionary<string, object>? Properties { get; set; }
    
    /// <summary>
    /// Get display-friendly level name
    /// </summary>
    public string LevelDisplay => Level switch
    {
        LogLevel.Trace => "Trace",
        LogLevel.Debug => "Debug", 
        LogLevel.Information => "Info",
        LogLevel.Warning => "Warning",
        LogLevel.Error => "Error",
        LogLevel.Critical => "Critical",
        _ => Level.ToString()
    };
    
    /// <summary>
    /// Get CSS class for log level styling
    /// </summary>
    public string LevelCssClass => Level switch
    {
        LogLevel.Trace => "text-muted",
        LogLevel.Debug => "text-secondary",
        LogLevel.Information => "text-info", 
        LogLevel.Warning => "text-warning",
        LogLevel.Error => "text-danger",
        LogLevel.Critical => "text-danger fw-bold",
        _ => "text-dark"
    };
}