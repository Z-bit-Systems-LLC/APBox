using Dapper;
using ApBox.Core.Data.Models;

namespace ApBox.Core.Data.Repositories;

/// <summary>
/// Repository implementation for managing PIN events
/// </summary>
public class PinEventRepository : IPinEventRepository
{
    private readonly IApBoxDbContext _dbContext;
    private readonly ILogger<PinEventRepository> _logger;

    public PinEventRepository(IApBoxDbContext dbContext, ILogger<PinEventRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task CreatePinEventAsync(PinEventEntity pinEvent)
    {
        const string sql = @"
            INSERT INTO pin_events 
            (reader_id, reader_name, encrypted_pin, pin_length, completion_reason, success, message, processed_by_plugin, timestamp)
            VALUES 
            (@ReaderId, @ReaderName, @EncryptedPin, @PinLength, @CompletionReason, @Success, @Message, @ProcessedByPlugin, @Timestamp)";

        using var connection = _dbContext.CreateDbConnectionAsync();
        await connection.ExecuteAsync(sql, pinEvent);
        
        _logger.LogInformation("Created PIN event for reader {ReaderName} ({ReaderId}), Success={Success}", 
            pinEvent.ReaderName, pinEvent.ReaderId, pinEvent.Success);
    }

    public async Task<IEnumerable<PinEventEntity>> GetPinEventsForReaderAsync(Guid readerId, int limit = 100)
    {
        const string sql = @"
            SELECT 
                id as Id,
                reader_id as ReaderId,
                reader_name as ReaderName,
                encrypted_pin as EncryptedPin,
                pin_length as PinLength,
                completion_reason as CompletionReason,
                success as Success,
                message as Message,
                processed_by_plugin as ProcessedByPlugin,
                timestamp as Timestamp
            FROM pin_events
            WHERE reader_id = @ReaderId
            ORDER BY timestamp DESC
            LIMIT @Limit";

        using var connection = _dbContext.CreateDbConnectionAsync();
        var events = await connection.QueryAsync<PinEventEntity>(sql, new { ReaderId = readerId.ToString(), Limit = limit });
        
        _logger.LogDebug("Retrieved {Count} PIN events for reader {ReaderId}", events.Count(), readerId);
        return events;
    }

    public async Task<IEnumerable<PinEventEntity>> GetPinEventsByDateRangeAsync(DateTime startDate, DateTime endDate, int limit = 100)
    {
        const string sql = @"
            SELECT 
                id as Id,
                reader_id as ReaderId,
                reader_name as ReaderName,
                encrypted_pin as EncryptedPin,
                pin_length as PinLength,
                completion_reason as CompletionReason,
                success as Success,
                message as Message,
                processed_by_plugin as ProcessedByPlugin,
                timestamp as Timestamp
            FROM pin_events
            WHERE timestamp >= @StartDate AND timestamp <= @EndDate
            ORDER BY timestamp DESC
            LIMIT @Limit";

        using var connection = _dbContext.CreateDbConnectionAsync();
        var events = await connection.QueryAsync<PinEventEntity>(sql, new { StartDate = startDate, EndDate = endDate, Limit = limit });
        
        _logger.LogDebug("Retrieved {Count} PIN events between {StartDate} and {EndDate}", events.Count(), startDate, endDate);
        return events;
    }

    public async Task<IEnumerable<PinEventEntity>> GetRecentPinEventsAsync(int limit = 50)
    {
        const string sql = @"
            SELECT 
                id as Id,
                reader_id as ReaderId,
                reader_name as ReaderName,
                encrypted_pin as EncryptedPin,
                pin_length as PinLength,
                completion_reason as CompletionReason,
                success as Success,
                message as Message,
                processed_by_plugin as ProcessedByPlugin,
                timestamp as Timestamp
            FROM pin_events
            ORDER BY timestamp DESC
            LIMIT @Limit";

        using var connection = _dbContext.CreateDbConnectionAsync();
        var events = await connection.QueryAsync<PinEventEntity>(sql, new { Limit = limit });
        
        _logger.LogDebug("Retrieved {Count} recent PIN events", events.Count());
        return events;
    }

    public async Task<int> GetPinEventCountAsync(Guid readerId, DateTime startDate, DateTime endDate)
    {
        const string sql = @"
            SELECT COUNT(1) 
            FROM pin_events 
            WHERE reader_id = @ReaderId AND timestamp >= @StartDate AND timestamp <= @EndDate";

        using var connection = _dbContext.CreateDbConnectionAsync();
        var count = await connection.ExecuteScalarAsync<int>(sql, new { ReaderId = readerId.ToString(), StartDate = startDate, EndDate = endDate });
        
        _logger.LogDebug("PIN event count for reader {ReaderId} between {StartDate} and {EndDate}: {Count}", readerId, startDate, endDate, count);
        return count;
    }

    public async Task DeleteOldPinEventsAsync(DateTime cutoffDate)
    {
        const string sql = "DELETE FROM pin_events WHERE timestamp < @CutoffDate";

        using var connection = _dbContext.CreateDbConnectionAsync();
        var deleted = await connection.ExecuteAsync(sql, new { CutoffDate = cutoffDate });
        
        _logger.LogInformation("Deleted {Count} PIN events older than {CutoffDate}", deleted, cutoffDate);
    }
}