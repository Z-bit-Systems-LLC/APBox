using ApBox.Core.Models;
using Microsoft.Extensions.Logging;

namespace ApBox.Core.Services;

/// <summary>
/// Service for managing and retrieving system logs
/// </summary>
public interface ILogService
{
    /// <summary>
    /// Get recent log entries
    /// </summary>
    Task<IEnumerable<LogEntry>> GetRecentLogsAsync(int count = 100);
    
    /// <summary>
    /// Get logs by minimum level
    /// </summary>
    Task<IEnumerable<LogEntry>> GetLogsByLevelAsync(LogLevel level, DateTime? since = null);
    
    /// <summary>
    /// Search logs by message content
    /// </summary>
    Task<IEnumerable<LogEntry>> SearchLogsAsync(string searchTerm, DateTime? since = null);
    
    /// <summary>
    /// Export logs as downloadable file
    /// </summary>
    Task<byte[]> ExportLogsAsync(DateTime? since = null);
    
    /// <summary>
    /// Get real-time log stream
    /// </summary>
    IAsyncEnumerable<LogEntry> GetRealTimeLogsAsync();
    
    /// <summary>
    /// Add a log entry (used by custom log provider)
    /// </summary>
    Task AddLogEntryAsync(LogEntry logEntry);
    
    /// <summary>
    /// Get available log levels with counts
    /// </summary>
    Task<Dictionary<LogLevel, int>> GetLogLevelCountsAsync();
}