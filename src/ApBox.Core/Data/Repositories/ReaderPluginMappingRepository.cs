using Dapper;
using ApBox.Core.Data.Models;

namespace ApBox.Core.Data.Repositories;

/// <summary>
/// Repository implementation for managing reader-plugin mappings
/// </summary>
public class ReaderPluginMappingRepository : IReaderPluginMappingRepository
{
    private readonly IApBoxDbContext _dbContext;
    private readonly ILogger<ReaderPluginMappingRepository> _logger;

    public ReaderPluginMappingRepository(IApBoxDbContext dbContext, ILogger<ReaderPluginMappingRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task CreateMappingAsync(ReaderPluginMappingEntity mapping)
    {
        const string sql = @"
            INSERT INTO reader_plugin_mappings 
            (reader_id, plugin_id, execution_order, is_enabled, created_at, updated_at)
            VALUES 
            (@ReaderId, @PluginId, @ExecutionOrder, @IsEnabled, @CreatedAt, @UpdatedAt)";

        mapping.CreatedAt = DateTime.UtcNow;
        mapping.UpdatedAt = DateTime.UtcNow;

        using var connection = _dbContext.CreateDbConnectionAsync();
        await connection.ExecuteAsync(sql, mapping);
        
        _logger.LogInformation("Created reader-plugin mapping for reader {ReaderId} and plugin {PluginId}", 
            mapping.ReaderId, mapping.PluginId);
    }

    public async Task<IEnumerable<ReaderPluginMappingEntity>> GetMappingsForReaderAsync(Guid readerId)
    {
        const string sql = @"
            SELECT 
                reader_id as ReaderId,
                plugin_id as PluginId,
                execution_order as ExecutionOrder,
                is_enabled as IsEnabled,
                created_at as CreatedAt,
                updated_at as UpdatedAt
            FROM reader_plugin_mappings
            WHERE reader_id = @ReaderId
            ORDER BY execution_order";

        using var connection = _dbContext.CreateDbConnectionAsync();
        var mappings = await connection.QueryAsync<ReaderPluginMappingEntity>(sql, new { ReaderId = readerId.ToString() });
        
        _logger.LogDebug("Retrieved {Count} plugin mappings for reader {ReaderId}", mappings.Count(), readerId);
        return mappings;
    }

    public async Task<IEnumerable<ReaderPluginMappingEntity>> GetMappingsForPluginAsync(string pluginId)
    {
        const string sql = @"
            SELECT 
                reader_id as ReaderId,
                plugin_id as PluginId,
                execution_order as ExecutionOrder,
                is_enabled as IsEnabled,
                created_at as CreatedAt,
                updated_at as UpdatedAt
            FROM reader_plugin_mappings
            WHERE plugin_id = @PluginId
            ORDER BY reader_id";

        using var connection = _dbContext.CreateDbConnectionAsync();
        var mappings = await connection.QueryAsync<ReaderPluginMappingEntity>(sql, new { PluginId = pluginId });
        
        _logger.LogDebug("Retrieved {Count} reader mappings for plugin {PluginId}", mappings.Count(), pluginId);
        return mappings;
    }

    public async Task<IEnumerable<ReaderPluginMappingEntity>> GetAllMappingsAsync()
    {
        const string sql = @"
            SELECT 
                reader_id as ReaderId,
                plugin_id as PluginId,
                execution_order as ExecutionOrder,
                is_enabled as IsEnabled,
                created_at as CreatedAt,
                updated_at as UpdatedAt
            FROM reader_plugin_mappings
            ORDER BY reader_id, execution_order";

        using var connection = _dbContext.CreateDbConnectionAsync();
        var mappings = await connection.QueryAsync<ReaderPluginMappingEntity>(sql);
        
        _logger.LogDebug("Retrieved {Count} total reader-plugin mappings", mappings.Count());
        return mappings;
    }

    public async Task DeleteMappingsForReaderAsync(Guid readerId)
    {
        const string sql = "DELETE FROM reader_plugin_mappings WHERE reader_id = @ReaderId";

        using var connection = _dbContext.CreateDbConnectionAsync();
        var deleted = await connection.ExecuteAsync(sql, new { ReaderId = readerId.ToString() });
        
        _logger.LogInformation("Deleted {Count} plugin mappings for reader {ReaderId}", deleted, readerId);
    }

    public async Task DeleteMappingAsync(Guid readerId, string pluginId)
    {
        const string sql = "DELETE FROM reader_plugin_mappings WHERE reader_id = @ReaderId AND plugin_id = @PluginId";

        using var connection = _dbContext.CreateDbConnectionAsync();
        var deleted = await connection.ExecuteAsync(sql, new { ReaderId = readerId.ToString(), PluginId = pluginId });
        
        if (deleted > 0)
        {
            _logger.LogInformation("Deleted mapping between reader {ReaderId} and plugin {PluginId}", readerId, pluginId);
        }
    }

    public async Task UpdateExecutionOrderAsync(Guid readerId, string pluginId, int newOrder)
    {
        const string sql = @"
            UPDATE reader_plugin_mappings 
            SET execution_order = @NewOrder, updated_at = @UpdatedAt
            WHERE reader_id = @ReaderId AND plugin_id = @PluginId";

        using var connection = _dbContext.CreateDbConnectionAsync();
        var updated = await connection.ExecuteAsync(sql, new 
        { 
            ReaderId = readerId.ToString(), 
            PluginId = pluginId, 
            NewOrder = newOrder,
            UpdatedAt = DateTime.UtcNow
        });
        
        if (updated > 0)
        {
            _logger.LogInformation("Updated execution order for reader {ReaderId} plugin {PluginId} to {Order}", 
                readerId, pluginId, newOrder);
        }
    }

    public async Task SetPluginEnabledAsync(Guid readerId, string pluginId, bool isEnabled)
    {
        const string sql = @"
            UPDATE reader_plugin_mappings 
            SET is_enabled = @IsEnabled, updated_at = @UpdatedAt
            WHERE reader_id = @ReaderId AND plugin_id = @PluginId";

        using var connection = _dbContext.CreateDbConnectionAsync();
        var updated = await connection.ExecuteAsync(sql, new 
        { 
            ReaderId = readerId.ToString(), 
            PluginId = pluginId, 
            IsEnabled = isEnabled ? 1 : 0,
            UpdatedAt = DateTime.UtcNow
        });
        
        if (updated > 0)
        {
            _logger.LogInformation("Set plugin {PluginId} {Status} for reader {ReaderId}", 
                pluginId, isEnabled ? "enabled" : "disabled", readerId);
        }
    }

    public async Task<bool> ExistsAsync(Guid readerId, string pluginId)
    {
        const string sql = @"
            SELECT COUNT(1) 
            FROM reader_plugin_mappings 
            WHERE reader_id = @ReaderId AND plugin_id = @PluginId";

        using var connection = _dbContext.CreateDbConnectionAsync();
        var count = await connection.ExecuteScalarAsync<int>(sql, new { ReaderId = readerId.ToString(), PluginId = pluginId });
        
        return count > 0;
    }
}