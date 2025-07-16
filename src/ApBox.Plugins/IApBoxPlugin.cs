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
    
    Task InitializeAsync();
    Task ShutdownAsync();
}