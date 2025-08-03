namespace ApBox.Core.Models;

/// <summary>
/// Plugin mapping for reader configuration export/import
/// </summary>
public class ReaderPluginMapping
{
    /// <summary>
    /// Unique identifier of the plugin
    /// </summary>
    public string PluginId { get; set; } = string.Empty;
    
    /// <summary>
    /// Plugin execution sequence (1-based)
    /// </summary>
    public int ExecutionOrder { get; set; }
    
    /// <summary>
    /// Whether plugin is active for this reader
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}