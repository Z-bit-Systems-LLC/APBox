using ApBox.Core.Data.Models;
using ApBox.Plugins;
using Dapper;
using Microsoft.Extensions.Logging;

namespace ApBox.Core.Data.Repositories;

public class CardEventRepository : ICardEventRepository
{
    private readonly IApBoxDbContext _dbContext;
    private readonly ILogger<CardEventRepository> _logger;

    public CardEventRepository(IApBoxDbContext dbContext, ILogger<CardEventRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IEnumerable<CardEventEntity>> GetRecentAsync(int limit = 100)
    {
        using var connection = await _dbContext.CreateConnectionAsync();
        
        var sql = @"
            SELECT * FROM card_events 
            ORDER BY timestamp DESC 
            LIMIT @Limit";
        
        return await connection.QueryAsync<CardEventEntity>(sql, new { Limit = limit });
    }

    public async Task<IEnumerable<CardEventEntity>> GetByReaderAsync(Guid readerId, int limit = 100)
    {
        using var connection = await _dbContext.CreateConnectionAsync();
        
        var sql = @"
            SELECT * FROM card_events 
            WHERE reader_id = @ReaderId 
            ORDER BY timestamp DESC 
            LIMIT @Limit";
        
        return await connection.QueryAsync<CardEventEntity>(sql, new { ReaderId = readerId.ToString(), Limit = limit });
    }

    public async Task<IEnumerable<CardEventEntity>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, int limit = 1000)
    {
        using var connection = await _dbContext.CreateConnectionAsync();
        
        var sql = @"
            SELECT * FROM card_events 
            WHERE timestamp >= @StartDate AND timestamp <= @EndDate 
            ORDER BY timestamp DESC 
            LIMIT @Limit";
        
        return await connection.QueryAsync<CardEventEntity>(sql, new 
        { 
            StartDate = startDate.ToString("yyyy-MM-dd HH:mm:ss"),
            EndDate = endDate.ToString("yyyy-MM-dd HH:mm:ss"),
            Limit = limit 
        });
    }

    public async Task<CardEventEntity> CreateAsync(CardReadEvent cardEvent, CardReadResult? result = null)
    {
        using var connection = await _dbContext.CreateConnectionAsync();
        
        var entity = CardEventEntity.FromCardReadEvent(cardEvent, result);
        
        var sql = @"
            INSERT INTO card_events 
            (reader_id, card_number, bit_length, reader_name, success, message, processed_by_plugin, timestamp)
            VALUES (@ReaderId, @CardNumber, @BitLength, @ReaderName, @Success, @Message, @ProcessedByPlugin, @Timestamp)
            RETURNING id";
        
        var id = await connection.QuerySingleAsync<long>(sql, new
        {
            ReaderId = entity.ReaderId,
            CardNumber = entity.CardNumber,
            BitLength = entity.BitLength,
            ReaderName = entity.ReaderName,
            Success = entity.Success ? 1 : 0,
            Message = entity.Message,
            ProcessedByPlugin = entity.ProcessedByPlugin,
            Timestamp = entity.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff")
        });
        
        entity.Id = id;
        
        _logger.LogDebug("Created card event {Id} for card {CardNumber} on reader {ReaderName}", 
            id, cardEvent.CardNumber, cardEvent.ReaderName);
        
        return entity;
    }

    public async Task<long> GetCountAsync()
    {
        using var connection = await _dbContext.CreateConnectionAsync();
        
        var sql = "SELECT COUNT(*) FROM card_events";
        return await connection.QuerySingleAsync<long>(sql);
    }

    public async Task<long> GetCountByReaderAsync(Guid readerId)
    {
        using var connection = await _dbContext.CreateConnectionAsync();
        
        var sql = "SELECT COUNT(*) FROM card_events WHERE reader_id = @ReaderId";
        return await connection.QuerySingleAsync<long>(sql, new { ReaderId = readerId.ToString() });
    }
}