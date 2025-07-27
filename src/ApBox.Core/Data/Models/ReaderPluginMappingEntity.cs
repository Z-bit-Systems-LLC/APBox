namespace ApBox.Core.Data.Models;

/// <summary>
/// Entity representing the mapping between readers and plugins
/// </summary>
public class ReaderPluginMappingEntity
{
    /// <summary>
    /// The reader ID (stored as string in SQLite)
    /// </summary>
    public string ReaderId { get; set; } = string.Empty;
    
    /// <summary>
    /// The plugin ID
    /// </summary>
    public string PluginId { get; set; } = string.Empty;
    
    /// <summary>
    /// The execution order for this plugin on this reader
    /// </summary>
    public int ExecutionOrder { get; set; }
    
    /// <summary>
    /// Whether this plugin is enabled for this reader
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// When this mapping was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// When this mapping was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// Navigation property to the reader configuration
    /// </summary>
    public virtual ReaderConfigurationEntity? ReaderConfiguration { get; set; }
}