namespace ApBox.Core.Services;

/// <summary>
/// Service for managing reader-plugin mappings
/// </summary>
public interface IReaderPluginMappingService
{
    /// <summary>
    /// Gets the list of plugin IDs assigned to a specific reader
    /// </summary>
    Task<IEnumerable<string>> GetPluginsForReaderAsync(Guid readerId);
    
    /// <summary>
    /// Sets the plugins for a reader (replaces existing mappings)
    /// </summary>
    Task SetPluginsForReaderAsync(Guid readerId, IEnumerable<string> pluginIds);
    
    /// <summary>
    /// Updates the execution order for a specific plugin on a reader
    /// </summary>
    Task UpdatePluginOrderAsync(Guid readerId, string pluginId, int newOrder);
    
    /// <summary>
    /// Enables a plugin for a specific reader
    /// </summary>
    Task EnablePluginForReaderAsync(Guid readerId, string pluginId);
    
    /// <summary>
    /// Disables a plugin for a specific reader
    /// </summary>
    Task DisablePluginForReaderAsync(Guid readerId, string pluginId);
    
    /// <summary>
    /// Gets all reader-plugin mappings
    /// </summary>
    Task<IEnumerable<(Guid ReaderId, string PluginId, int ExecutionOrder, bool IsEnabled)>> GetAllMappingsAsync();
    
    /// <summary>
    /// Copies plugin mappings from one reader to another
    /// </summary>
    Task CopyMappingsAsync(Guid sourceReaderId, Guid targetReaderId);
    
    /// <summary>
    /// Gets all readers that use a specific plugin
    /// </summary>
    Task<IEnumerable<Guid>> GetReadersUsingPluginAsync(string pluginId);
}