namespace ApBox.Plugins;

/// <summary>
/// Core interface that all ApBox plugins must implement.
/// Provides hooks for card reads, PIN reads, and plugin lifecycle management.
/// </summary>
public interface IApBoxPlugin
{
    /// <summary>
    /// Unique identifier for this plugin instance
    /// </summary>
    Guid Id { get; }
    
    /// <summary>
    /// Human-readable name of the plugin
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Version string of the plugin
    /// </summary>
    string Version { get; }
    
    /// <summary>
    /// Description of what the plugin does
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// Process a card read event. Returns true if the plugin successfully handled the card read.
    /// </summary>
    /// <param name="cardRead">The card read event to process</param>
    /// <returns>True if the plugin handled the card read successfully, false otherwise</returns>
    Task<bool> ProcessCardReadAsync(CardReadEvent cardRead);
    
    /// <summary>
    /// Process a completed PIN read. Returns true if the plugin successfully handled the PIN read.
    /// </summary>
    /// <param name="pinRead">The PIN read event</param>
    /// <returns>True if the plugin handled the PIN read successfully, false otherwise</returns>
    Task<bool> ProcessPinReadAsync(PinReadEvent pinRead);
    
    /// <summary>
    /// Initialize the plugin. Called once when the plugin is loaded.
    /// </summary>
    /// <returns>A task representing the initialization operation</returns>
    Task InitializeAsync();
    
    /// <summary>
    /// Shutdown the plugin. Called when the plugin is being unloaded.
    /// </summary>
    /// <returns>A task representing the shutdown operation</returns>
    Task ShutdownAsync();
}