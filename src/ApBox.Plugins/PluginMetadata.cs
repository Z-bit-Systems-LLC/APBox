namespace ApBox.Plugins;

/// <summary>
/// Contains metadata information about a plugin, including identification,
/// configuration, and loading information.
/// </summary>
public class PluginMetadata
{
    /// <summary>
    /// Unique identifier for the plugin
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Human-readable name of the plugin
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Version string of the plugin
    /// </summary>
    public string Version { get; set; } = string.Empty;
    
    /// <summary>
    /// Description of what the plugin does
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Author or organization that created the plugin
    /// </summary>
    public string Author { get; set; } = string.Empty;
    
    /// <summary>
    /// File system path to the plugin assembly
    /// </summary>
    public string AssemblyPath { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether the plugin is currently enabled for execution
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// Configuration settings specific to this plugin
    /// </summary>
    public Dictionary<string, object> Configuration { get; set; } = new();
}