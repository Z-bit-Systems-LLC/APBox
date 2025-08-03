using ApBox.Plugins;

namespace ApBox.Core.Models;

/// <summary>
/// PIN event model for UI display
/// </summary>
public class PinEventDisplay
{
    public Guid ReaderId { get; set; }
    public string ReaderName { get; set; } = string.Empty;
    public int PinLength { get; set; }
    public PinCompletionReason CompletionReason { get; set; }
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Whether the PIN processing was successful
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Processing result message
    /// </summary>
    public string? Message { get; set; }
    
    /// <summary>
    /// Comma-separated list of plugins that processed this PIN
    /// </summary>
    public string? ProcessedByPlugins { get; set; }
    
    /// <summary>
    /// Get list of individual plugin names
    /// </summary>
    public IEnumerable<string> GetPluginNames()
    {
        if (string.IsNullOrWhiteSpace(ProcessedByPlugins))
            return Enumerable.Empty<string>();
            
        return ProcessedByPlugins.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                 .Select(p => p.Trim());
    }
    
    /// <summary>
    /// Get detailed plugin results with success/failure status
    /// </summary>
    public PluginResultCollection GetPluginResults()
    {
        return PluginResultCollection.Parse(ProcessedByPlugins);
    }
    
    /// <summary>
    /// Create from PinReadEvent
    /// </summary>
    public static PinEventDisplay FromPinReadEvent(PinReadEvent pinReadEvent)
    {
        return new PinEventDisplay
        {
            ReaderId = pinReadEvent.ReaderId,
            ReaderName = pinReadEvent.ReaderName,
            PinLength = pinReadEvent.Pin.Length,
            CompletionReason = pinReadEvent.CompletionReason,
            Timestamp = pinReadEvent.Timestamp,
            Success = false, // Will be updated by processing service
            Message = null,
            ProcessedByPlugins = null
        };
    }
}