namespace ApBox.Plugins;

/// <summary>
/// Represents the result of processing a card read event through the plugin system.
/// Contains status information and any data returned by plugins.
/// </summary>
public class CardReadResult : IProcessingResult
{
    /// <summary>
    /// Indicates whether the card read was processed successfully by at least one plugin
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Human-readable message describing the result or any errors
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Additional data returned by plugins during processing
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();
    
    /// <summary>
    /// Name of the plugin that processed this card read, if any
    /// </summary>
    public string? ProcessedByPlugin { get; set; }
}