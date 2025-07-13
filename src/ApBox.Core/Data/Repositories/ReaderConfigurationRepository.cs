using ApBox.Core.Data.Models;
using ApBox.Plugins;
using Dapper;
using Microsoft.Extensions.Logging;

namespace ApBox.Core.Data.Repositories;

public class ReaderConfigurationRepository : IReaderConfigurationRepository
{
    private readonly IApBoxDbContext _dbContext;
    private readonly ILogger<ReaderConfigurationRepository> _logger;

    public ReaderConfigurationRepository(IApBoxDbContext dbContext, ILogger<ReaderConfigurationRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IEnumerable<ReaderConfiguration>> GetAllAsync()
    {
        using var connection = await _dbContext.CreateConnectionAsync();
        
        var sql = "SELECT * FROM reader_configurations ORDER BY reader_name";
        var entities = await connection.QueryAsync<ReaderConfigurationEntity>(sql);
        
        return entities.Select(e => e.ToReaderConfiguration());
    }

    public async Task<ReaderConfiguration?> GetByIdAsync(Guid readerId)
    {
        using var connection = await _dbContext.CreateConnectionAsync();
        
        var sql = "SELECT * FROM reader_configurations WHERE reader_id = @ReaderId";
        var entity = await connection.QueryFirstOrDefaultAsync<ReaderConfigurationEntity>(sql, new { ReaderId = readerId.ToString() });
        
        return entity?.ToReaderConfiguration();
    }

    public async Task<ReaderConfiguration> CreateAsync(ReaderConfiguration readerConfiguration)
    {
        using var connection = await _dbContext.CreateConnectionAsync();
        
        var entity = ReaderConfigurationEntity.FromReaderConfiguration(readerConfiguration);
        
        var sql = @"
            INSERT INTO reader_configurations 
            (reader_id, reader_name, default_feedback_json, result_feedback_json, created_at, updated_at)
            VALUES (@ReaderId, @ReaderName, @DefaultFeedbackJson, @ResultFeedbackJson, @CreatedAt, @UpdatedAt)";
        
        await connection.ExecuteAsync(sql, entity);
        
        _logger.LogInformation("Created reader configuration for {ReaderName} ({ReaderId})", 
            readerConfiguration.ReaderName, readerConfiguration.ReaderId);
        
        return readerConfiguration;
    }

    public async Task<ReaderConfiguration> UpdateAsync(ReaderConfiguration readerConfiguration)
    {
        using var connection = await _dbContext.CreateConnectionAsync();
        
        var entity = ReaderConfigurationEntity.FromReaderConfiguration(readerConfiguration);
        entity.UpdatedAt = DateTime.UtcNow;
        
        var sql = @"
            UPDATE reader_configurations 
            SET reader_name = @ReaderName, 
                default_feedback_json = @DefaultFeedbackJson, 
                result_feedback_json = @ResultFeedbackJson,
                updated_at = @UpdatedAt
            WHERE reader_id = @ReaderId";
        
        var rowsAffected = await connection.ExecuteAsync(sql, entity);
        
        if (rowsAffected == 0)
        {
            throw new InvalidOperationException($"Reader configuration with ID {readerConfiguration.ReaderId} not found");
        }
        
        _logger.LogInformation("Updated reader configuration for {ReaderName} ({ReaderId})", 
            readerConfiguration.ReaderName, readerConfiguration.ReaderId);
        
        return readerConfiguration;
    }

    public async Task<bool> DeleteAsync(Guid readerId)
    {
        using var connection = await _dbContext.CreateConnectionAsync();
        
        var sql = "DELETE FROM reader_configurations WHERE reader_id = @ReaderId";
        var rowsAffected = await connection.ExecuteAsync(sql, new { ReaderId = readerId.ToString() });
        
        if (rowsAffected > 0)
        {
            _logger.LogInformation("Deleted reader configuration {ReaderId}", readerId);
        }
        
        return rowsAffected > 0;
    }

    public async Task<bool> ExistsAsync(Guid readerId)
    {
        using var connection = await _dbContext.CreateConnectionAsync();
        
        var sql = "SELECT COUNT(1) FROM reader_configurations WHERE reader_id = @ReaderId";
        var count = await connection.QuerySingleAsync<int>(sql, new { ReaderId = readerId.ToString() });
        
        return count > 0;
    }
}