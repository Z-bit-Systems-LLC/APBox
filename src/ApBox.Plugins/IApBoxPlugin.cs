namespace ApBox.Plugins;

public interface IApBoxPlugin
{
    string Name { get; }
    string Version { get; }
    string Description { get; }
    
    Task<bool> ProcessCardReadAsync(CardReadEvent cardRead);
    
    Task InitializeAsync();
    Task ShutdownAsync();
}