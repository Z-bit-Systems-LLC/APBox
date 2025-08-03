namespace ApBox.Plugins;

public interface IApBoxPlugin
{
    /// <summary>
    /// Unique identifier for this plugin instance
    /// </summary>
    Guid Id { get; }
    
    string Name { get; }
    string Version { get; }
    string Description { get; }
    
    Task<bool> ProcessCardReadAsync(CardReadEvent cardRead);
    
    /// <summary>
    /// Process a completed PIN read. Returns true if the plugin successfully handled the PIN read.
    /// </summary>
    /// <param name="pinRead">The PIN read event</param>
    /// <returns>True if the plugin handled the PIN read successfully, false otherwise</returns>
    Task<bool> ProcessPinReadAsync(PinReadEvent pinRead);
    
    Task InitializeAsync();
    Task ShutdownAsync();
}