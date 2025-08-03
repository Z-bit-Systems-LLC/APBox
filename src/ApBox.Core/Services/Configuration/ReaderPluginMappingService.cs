using ApBox.Core.Data.Models;
using ApBox.Core.Data.Repositories;
using Microsoft.Extensions.Logging;

namespace ApBox.Core.Services.Configuration;

/// <summary>
/// Service implementation for managing reader-plugin mappings
/// </summary>
public class ReaderPluginMappingService : IReaderPluginMappingService
{
    private readonly IReaderPluginMappingRepository _repository;
    private readonly ILogger<ReaderPluginMappingService> _logger;

    public ReaderPluginMappingService(
        IReaderPluginMappingRepository repository,
        ILogger<ReaderPluginMappingService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IEnumerable<string>> GetPluginsForReaderAsync(Guid readerId)
    {
        _logger.LogDebug("Getting plugins for reader {ReaderId}", readerId);
        
        var mappings = await _repository.GetMappingsForReaderAsync(readerId);
        var enabledPlugins = mappings
            .Where(m => m.IsEnabled)
            .OrderBy(m => m.ExecutionOrder)
            .Select(m => m.PluginId);
            
        _logger.LogInformation("Retrieved {Count} enabled plugins for reader {ReaderId}", 
            enabledPlugins.Count(), readerId);
            
        return enabledPlugins;
    }

    public async Task SetPluginsForReaderAsync(Guid readerId, IEnumerable<string> pluginIds)
    {
        _logger.LogInformation("Setting plugins for reader {ReaderId}: {PluginIds}", 
            readerId, string.Join(", ", pluginIds));
        
        // Delete existing mappings
        await _repository.DeleteMappingsForReaderAsync(readerId);
        
        // Create new mappings with execution order based on list order
        var executionOrder = 1;
        foreach (var pluginId in pluginIds)
        {
            var mapping = new ReaderPluginMappingEntity
            {
                ReaderId = readerId.ToString(),
                PluginId = pluginId,
                ExecutionOrder = executionOrder++,
                IsEnabled = true
            };
            
            await _repository.CreateMappingAsync(mapping);
        }
        
        _logger.LogInformation("Successfully set {Count} plugins for reader {ReaderId}", 
            pluginIds.Count(), readerId);
    }

    public async Task UpdatePluginOrderAsync(Guid readerId, string pluginId, int newOrder)
    {
        _logger.LogDebug("Updating plugin {PluginId} order to {Order} for reader {ReaderId}", 
            pluginId, newOrder, readerId);
            
        await _repository.UpdateExecutionOrderAsync(readerId, pluginId, newOrder);
    }

    public async Task EnablePluginForReaderAsync(Guid readerId, string pluginId)
    {
        _logger.LogInformation("Enabling plugin {PluginId} for reader {ReaderId}", 
            pluginId, readerId);
            
        await _repository.SetPluginEnabledAsync(readerId, pluginId, true);
    }

    public async Task DisablePluginForReaderAsync(Guid readerId, string pluginId)
    {
        _logger.LogInformation("Disabling plugin {PluginId} for reader {ReaderId}", 
            pluginId, readerId);
            
        await _repository.SetPluginEnabledAsync(readerId, pluginId, false);
    }

    public async Task<IEnumerable<(Guid ReaderId, string PluginId, int ExecutionOrder, bool IsEnabled)>> GetAllMappingsAsync()
    {
        _logger.LogDebug("Getting all reader-plugin mappings");
        
        var mappings = await _repository.GetAllMappingsAsync();
        var result = mappings.Select(m => (
            ReaderId: Guid.Parse(m.ReaderId),
            m.PluginId,
            m.ExecutionOrder,
            m.IsEnabled
        ));
        
        _logger.LogInformation("Retrieved {Count} total reader-plugin mappings", result.Count());
        return result;
    }

    public async Task CopyMappingsAsync(Guid sourceReaderId, Guid targetReaderId)
    {
        _logger.LogInformation("Copying plugin mappings from reader {SourceId} to {TargetId}", 
            sourceReaderId, targetReaderId);
        
        // Get source mappings
        var sourceMappings = await _repository.GetMappingsForReaderAsync(sourceReaderId);
        
        // Delete target's existing mappings
        await _repository.DeleteMappingsForReaderAsync(targetReaderId);
        
        // Copy mappings to target
        foreach (var sourceMapping in sourceMappings)
        {
            var targetMapping = new ReaderPluginMappingEntity
            {
                ReaderId = targetReaderId.ToString(),
                PluginId = sourceMapping.PluginId,
                ExecutionOrder = sourceMapping.ExecutionOrder,
                IsEnabled = sourceMapping.IsEnabled
            };
            
            await _repository.CreateMappingAsync(targetMapping);
        }
        
        _logger.LogInformation("Successfully copied {Count} plugin mappings from reader {SourceId} to {TargetId}", 
            sourceMappings.Count(), sourceReaderId, targetReaderId);
    }

    public async Task<IEnumerable<Guid>> GetReadersUsingPluginAsync(string pluginId)
    {
        _logger.LogDebug("Getting readers using plugin {PluginId}", pluginId);
        
        var mappings = await _repository.GetMappingsForPluginAsync(pluginId);
        var readerIds = mappings
            .Where(m => m.IsEnabled)
            .Select(m => Guid.Parse(m.ReaderId))
            .Distinct();
            
        _logger.LogInformation("Found {Count} readers using plugin {PluginId}", 
            readerIds.Count(), pluginId);
            
        return readerIds;
    }
}