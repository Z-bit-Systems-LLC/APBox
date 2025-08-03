using ApBox.Plugins;

namespace ApBox.Core.Services.Plugins;

/// <summary>
/// Interface for loading and managing ApBox plugins from assemblies.
/// Provides methods for loading, reloading, and unloading plugins dynamically.
/// </summary>
public interface IPluginLoader
{
    /// <summary>
    /// Loads all available plugins from the configured plugin directory
    /// </summary>
    /// <returns>Collection of loaded and initialized plugins</returns>
    Task<IEnumerable<IApBoxPlugin>> LoadPluginsAsync();
    
    /// <summary>
    /// Forces a reload of all plugins, clearing cache and reloading from disk
    /// </summary>
    /// <returns>Collection of reloaded plugins</returns>
    Task<IEnumerable<IApBoxPlugin>> ReloadPluginsAsync();
    
    /// <summary>
    /// Unloads a specific plugin by its ID
    /// </summary>
    /// <param name="pluginId">The unique identifier of the plugin to unload</param>
    Task UnloadPluginAsync(string pluginId);
    
    /// <summary>
    /// Gets metadata for all available plugins
    /// </summary>
    /// <returns>Collection of plugin metadata</returns>
    IEnumerable<PluginMetadata> GetAvailablePlugins();
}