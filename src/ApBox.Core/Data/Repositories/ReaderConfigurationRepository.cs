using ApBox.Core.Data.Models;
using ApBox.Core.Models;
using Dapper;

namespace ApBox.Core.Data.Repositories;

public class ReaderConfigurationRepository(IApBoxDbContext dbContext, ILogger<ReaderConfigurationRepository> logger)
    : IReaderConfigurationRepository
{
    public async Task<IEnumerable<ReaderConfiguration>> GetAllAsync()
    {
        using var connection = dbContext.CreateDbConnectionAsync();
        connection.Open();

        var sql = @"
            SELECT * FROM reader_configurations 
            ORDER BY reader_name";
        var entities = await connection.QueryAsync<ReaderConfigurationEntity>(sql);
        
        return entities.Select(e => e.ToReaderConfiguration());
    }

    public async Task<ReaderConfiguration?> GetByIdAsync(Guid readerId)
    {
        using var connection = dbContext.CreateDbConnectionAsync();
        connection.Open();

        var sql = @"
            SELECT * FROM reader_configurations 
            WHERE reader_id = @ReaderId";
        var entity = await connection.QueryFirstOrDefaultAsync<ReaderConfigurationEntity>(sql, new { ReaderId = readerId.ToString() });
        
        return entity?.ToReaderConfiguration();
    }

    public async Task<ReaderConfiguration> CreateAsync(ReaderConfiguration readerConfiguration)
    {
        using var connection = dbContext.CreateDbConnectionAsync();
        connection.Open();

        var entity = ReaderConfigurationEntity.FromReaderConfiguration(readerConfiguration);
        
        var sql = @"
            INSERT INTO reader_configurations 
            (reader_id, reader_name, address, is_enabled, created_at, updated_at)
            VALUES (@ReaderId, @ReaderName, @Address, @IsEnabled, @CreatedAt, @UpdatedAt)";
        
        await connection.ExecuteAsync(sql, entity);
        
        logger.LogInformation("Created reader configuration for {ReaderName} ({ReaderId})", 
            readerConfiguration.ReaderName, readerConfiguration.ReaderId);
        
        return readerConfiguration;
    }

    public async Task<ReaderConfiguration> UpdateAsync(ReaderConfiguration readerConfiguration)
    {
        using var connection = dbContext.CreateDbConnectionAsync();
        connection.Open();

        var entity = ReaderConfigurationEntity.FromReaderConfiguration(readerConfiguration);
        entity.UpdatedAt = DateTime.UtcNow;
        
        var sql = @"
            UPDATE reader_configurations 
            SET reader_name = @ReaderName,
                address = @Address,
                is_enabled = @IsEnabled,
                updated_at = @UpdatedAt
            WHERE reader_id = @ReaderId";
        
        var rowsAffected = await connection.ExecuteAsync(sql, entity);
        
        if (rowsAffected == 0)
        {
            throw new InvalidOperationException($"Reader configuration with ID {readerConfiguration.ReaderId} not found");
        }
        
        logger.LogInformation("Updated reader configuration for {ReaderName} ({ReaderId})", 
            readerConfiguration.ReaderName, readerConfiguration.ReaderId);
        
        return readerConfiguration;
    }

    public async Task<bool> DeleteAsync(Guid readerId)
    {
        using var connection = dbContext.CreateDbConnectionAsync();
        connection.Open();

        var sql = "DELETE FROM reader_configurations WHERE reader_id = @ReaderId";
        var rowsAffected = await connection.ExecuteAsync(sql, new { ReaderId = readerId.ToString() });
        
        if (rowsAffected > 0)
        {
            logger.LogInformation("Deleted reader configuration {ReaderId}", readerId);
        }
        
        return rowsAffected > 0;
    }

    public async Task<bool> ExistsAsync(Guid readerId)
    {
        using var connection = dbContext.CreateDbConnectionAsync();
        connection.Open();

        var sql = "SELECT COUNT(1) FROM reader_configurations WHERE reader_id = @ReaderId";
        var count = await connection.QuerySingleAsync<int>(sql, new { ReaderId = readerId.ToString() });
        
        return count > 0;
    }
}