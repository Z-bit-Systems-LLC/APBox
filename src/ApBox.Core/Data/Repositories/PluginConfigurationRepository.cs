using ApBox.Core.Data.Models;
using Dapper;
using Microsoft.Extensions.Logging;

namespace ApBox.Core.Data.Repositories;

public class PluginConfigurationRepository : IPluginConfigurationRepository
{
    private readonly IApBoxDbContext _dbContext;
    private readonly ILogger<PluginConfigurationRepository> _logger;

    public PluginConfigurationRepository(IApBoxDbContext dbContext, ILogger<PluginConfigurationRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<string?> GetConfigurationAsync(string pluginName, string key)
    {
        using var connection = _dbContext.CreateDbConnectionAsync();
        connection.Open();
        
        var sql = @"
            SELECT configuration_value 
            FROM plugin_configurations 
            WHERE plugin_name = @PluginName AND configuration_key = @Key";
        
        return await connection.QueryFirstOrDefaultAsync<string>(sql, new { PluginName = pluginName, Key = key });
    }

    public async Task SetConfigurationAsync(string pluginName, string key, string value)
    {
        using var connection = _dbContext.CreateDbConnectionAsync();
        connection.Open();

        var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        
        var sql = @"
            INSERT OR REPLACE INTO plugin_configurations 
            (plugin_name, configuration_key, configuration_value, created_at, updated_at)
            VALUES (@PluginName, @Key, @Value, 
                COALESCE((SELECT created_at FROM plugin_configurations 
                         WHERE plugin_name = @PluginName AND configuration_key = @Key), @Now),
                @Now)";
        
        await connection.ExecuteAsync(sql, new 
        { 
            PluginName = pluginName, 
            Key = key, 
            Value = value, 
            Now = now 
        });
        
        _logger.LogDebug("Set configuration {Key} for plugin {PluginName}", key, pluginName);
    }

    public async Task<Dictionary<string, string>> GetAllConfigurationAsync(string pluginName)
    {
        using var connection = _dbContext.CreateDbConnectionAsync();
        connection.Open();

        var sql = @"
            SELECT configuration_key, configuration_value 
            FROM plugin_configurations 
            WHERE plugin_name = @PluginName";
        
        var results = await connection.QueryAsync(sql, new { PluginName = pluginName });
        
        return results.ToDictionary(
            row => (string)row.configuration_key,
            row => (string)row.configuration_value
        );
    }

    public async Task<bool> DeleteConfigurationAsync(string pluginName, string key)
    {
        using var connection = _dbContext.CreateDbConnectionAsync();
        connection.Open();

        var sql = @"
            DELETE FROM plugin_configurations 
            WHERE plugin_name = @PluginName AND configuration_key = @Key";
        
        var rowsAffected = await connection.ExecuteAsync(sql, new { PluginName = pluginName, Key = key });
        
        if (rowsAffected > 0)
        {
            _logger.LogDebug("Deleted configuration {Key} for plugin {PluginName}", key, pluginName);
        }
        
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAllConfigurationAsync(string pluginName)
    {
        using var connection = _dbContext.CreateDbConnectionAsync();
        connection.Open();

        var sql = "DELETE FROM plugin_configurations WHERE plugin_name = @PluginName";
        var rowsAffected = await connection.ExecuteAsync(sql, new { PluginName = pluginName });
        
        if (rowsAffected > 0)
        {
            _logger.LogInformation("Deleted all configurations for plugin {PluginName}", pluginName);
        }
        
        return rowsAffected > 0;
    }
}