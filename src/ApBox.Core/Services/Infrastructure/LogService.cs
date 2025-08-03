using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using ApBox.Core.Models;
using Microsoft.Extensions.Logging;

namespace ApBox.Core.Services.Infrastructure;

/// <summary>
/// Implementation of log service with in-memory buffer and real-time streaming
/// </summary>
public class LogService : ILogService
{
    private readonly ILogger<LogService> _logger;
    private readonly ConcurrentQueue<LogEntry> _logBuffer = new();
    private readonly List<TaskCompletionSource<LogEntry>> _realTimeSubscribers = new();
    private const int MaxBufferSize = 1000;

    public LogService(ILogger<LogService> logger)
    {
        _logger = logger;
        
        // Add an initial log entry to indicate the service has started
        var startupLog = new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = LogLevel.Information,
            Source = "ApBox.Core",
            Message = "Log service initialized and ready to capture application logs"
        };
        
        _logBuffer.Enqueue(startupLog);
        _logger.LogInformation("ApBox LogService initialized");
    }

    public async Task<IEnumerable<LogEntry>> GetRecentLogsAsync(int count = 100)
    {
        var logs = _logBuffer.ToArray()
            .OrderByDescending(l => l.Timestamp)
            .Take(count)
            .OrderBy(l => l.Timestamp);
        
        return await Task.FromResult(logs);
    }

    public async Task<IEnumerable<LogEntry>> GetLogsByLevelAsync(LogLevel level, DateTime? since = null)
    {
        var logs = _logBuffer.ToArray()
            .Where(l => l.Level >= level)
            .Where(l => since == null || l.Timestamp >= since)
            .OrderByDescending(l => l.Timestamp);
        
        return await Task.FromResult(logs);
    }

    public async Task<IEnumerable<LogEntry>> SearchLogsAsync(string searchTerm, DateTime? since = null)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return await GetRecentLogsAsync();
        }

        var logs = _logBuffer.ToArray()
            .Where(l => since == null || l.Timestamp >= since)
            .Where(l => l.Message.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                       l.Source.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                       (l.Exception?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false))
            .OrderByDescending(l => l.Timestamp);
        
        return await Task.FromResult(logs);
    }

    public async Task<byte[]> ExportLogsAsync(DateTime? since = null)
    {
        var logs = _logBuffer.ToArray()
            .Where(l => since == null || l.Timestamp >= since)
            .OrderBy(l => l.Timestamp);

        var exportData = new
        {
            ExportedAt = DateTime.UtcNow,
            Since = since,
            TotalEntries = logs.Count(),
            Logs = logs
        };

        var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        
        return await Task.FromResult(Encoding.UTF8.GetBytes(json));
    }

    public async IAsyncEnumerable<LogEntry> GetRealTimeLogsAsync()
    {
        // First, yield existing logs
        var existingLogs = await GetRecentLogsAsync(50);
        foreach (var log in existingLogs)
        {
            yield return log;
        }

        // Then yield new logs as they arrive
        while (true)
        {
            var tcs = new TaskCompletionSource<LogEntry>();
            lock (_realTimeSubscribers)
            {
                _realTimeSubscribers.Add(tcs);
            }

            var logEntry = await tcs.Task;
            yield return logEntry;
        }
    }

    public async Task AddLogEntryAsync(LogEntry logEntry)
    {
        // Add to buffer
        _logBuffer.Enqueue(logEntry);
        
        // Maintain buffer size
        while (_logBuffer.Count > MaxBufferSize)
        {
            _logBuffer.TryDequeue(out _);
        }

        // Notify real-time subscribers
        lock (_realTimeSubscribers)
        {
            for (int i = _realTimeSubscribers.Count - 1; i >= 0; i--)
            {
                var subscriber = _realTimeSubscribers[i];
                if (!subscriber.Task.IsCompleted)
                {
                    subscriber.SetResult(logEntry);
                    _realTimeSubscribers.RemoveAt(i);
                }
            }
        }

        await Task.CompletedTask;
    }

    public async Task<Dictionary<LogLevel, int>> GetLogLevelCountsAsync()
    {
        var logs = _logBuffer.ToArray();
        var counts = logs
            .GroupBy(l => l.Level)
            .ToDictionary(g => g.Key, g => g.Count());

        // Ensure all log levels are represented
        foreach (LogLevel level in Enum.GetValues<LogLevel>())
        {
            if (level != LogLevel.None && !counts.ContainsKey(level))
            {
                counts[level] = 0;
            }
        }

        return await Task.FromResult(counts);
    }
}