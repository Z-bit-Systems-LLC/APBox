using ApBox.Core.Data.Models;

namespace ApBox.Core.Data.Repositories;

/// <summary>
/// Repository interface for managing reader-plugin mappings
/// </summary>
public interface IReaderPluginMappingRepository
{
    /// <summary>
    /// Creates a new reader-plugin mapping
    /// </summary>
    Task CreateMappingAsync(ReaderPluginMappingEntity mapping);
    
    /// <summary>
    /// Gets all mappings for a specific reader
    /// </summary>
    Task<IEnumerable<ReaderPluginMappingEntity>> GetMappingsForReaderAsync(Guid readerId);
    
    /// <summary>
    /// Gets all mappings for a specific plugin
    /// </summary>
    Task<IEnumerable<ReaderPluginMappingEntity>> GetMappingsForPluginAsync(string pluginId);
    
    /// <summary>
    /// Gets all mappings in the system
    /// </summary>
    Task<IEnumerable<ReaderPluginMappingEntity>> GetAllMappingsAsync();
    
    /// <summary>
    /// Deletes all mappings for a specific reader
    /// </summary>
    Task DeleteMappingsForReaderAsync(Guid readerId);
    
    /// <summary>
    /// Deletes a specific reader-plugin mapping
    /// </summary>
    Task DeleteMappingAsync(Guid readerId, string pluginId);
    
    /// <summary>
    /// Updates the execution order for a mapping
    /// </summary>
    Task UpdateExecutionOrderAsync(Guid readerId, string pluginId, int newOrder);
    
    /// <summary>
    /// Updates the enabled status for a mapping
    /// </summary>
    Task SetPluginEnabledAsync(Guid readerId, string pluginId, bool isEnabled);
    
    /// <summary>
    /// Checks if a mapping exists
    /// </summary>
    Task<bool> ExistsAsync(Guid readerId, string pluginId);
}