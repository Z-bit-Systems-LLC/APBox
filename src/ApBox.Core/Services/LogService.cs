using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using ApBox.Core.Models;
using Microsoft.Extensions.Logging;

namespace ApBox.Core.Services;

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
        // Seed with some sample log entries for demonstration
        SeedSampleLogs();
    }

    private void SeedSampleLogs()
    {
        var sampleLogs = new[]
        {
            new LogEntry 
            { 
                Timestamp = DateTime.UtcNow.AddMinutes(-30), 
                Level = LogLevel.Information, 
                Source = "ApBox.Core", 
                Message = "Application started successfully" 
            },
            new LogEntry 
            { 
                Timestamp = DateTime.UtcNow.AddMinutes(-25), 
                Level = LogLevel.Information, 
                Source = "ApBox.OSDP", 
                Message = "OSDP communication manager initialized" 
            },
            new LogEntry 
            { 
                Timestamp = DateTime.UtcNow.AddMinutes(-20), 
                Level = LogLevel.Debug, 
                Source = "ApBox.Plugins", 
                Message = "Scanning plugins directory: /plugins" 
            },
            new LogEntry 
            { 
                Timestamp = DateTime.UtcNow.AddMinutes(-15), 
                Level = LogLevel.Warning, 
                Source = "ApBox.OSDP", 
                Message = "Reader connection timeout on port COM3, retrying..." 
            },
            new LogEntry 
            { 
                Timestamp = DateTime.UtcNow.AddMinutes(-10), 
                Level = LogLevel.Information, 
                Source = "ApBox.Web", 
                Message = "Blazor SignalR hub connected" 
            },
            new LogEntry 
            { 
                Timestamp = DateTime.UtcNow.AddMinutes(-5), 
                Level = LogLevel.Error, 
                Source = "ApBox.OSDP", 
                Message = "Failed to establish connection with reader ID 2",
                Exception = "System.TimeoutException: The operation has timed out.\n   at ApBox.OSDP.OsdpReader.ConnectAsync()\n   at ApBox.Core.Services.ReaderService.InitializeReader()"
            },
            new LogEntry 
            { 
                Timestamp = DateTime.UtcNow.AddMinutes(-2), 
                Level = LogLevel.Information, 
                Source = "ApBox.Core", 
                Message = "Card read event processed: Card ID 123456789" 
            },
            new LogEntry 
            { 
                Timestamp = DateTime.UtcNow.AddMinutes(-1), 
                Level = LogLevel.Critical, 
                Source = "ApBox.Database", 
                Message = "Database connection lost, attempting reconnection",
                Exception = "Microsoft.Data.Sqlite.SqliteException: Database is locked\n   at Microsoft.Data.Sqlite.SqliteConnection.Open()"
            }
        };

        foreach (var log in sampleLogs)
        {
            _logBuffer.Enqueue(log);
        }
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