using ApBox.Core.Data.Models;

namespace ApBox.Core.Models;

/// <summary>
/// Card event model with processing results for UI display
/// </summary>
public class CardEventDisplay
{
    public long Id { get; set; }
    public Guid ReaderId { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public int BitLength { get; set; }
    public string ReaderName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Whether the card processing was successful
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Processing result message
    /// </summary>
    public string? Message { get; set; }
    
    /// <summary>
    /// Comma-separated list of plugins that processed this card
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
    /// Create from CardEventEntity
    /// </summary>
    public static CardEventDisplay FromEntity(CardEventEntity entity)
    {
        return new CardEventDisplay
        {
            Id = entity.Id,
            ReaderId = Guid.Parse(entity.ReaderId),
            CardNumber = entity.CardNumber,
            BitLength = entity.BitLength,
            ReaderName = entity.ReaderName,
            Timestamp = entity.Timestamp,
            Success = entity.Success,
            Message = entity.Message,
            ProcessedByPlugins = entity.ProcessedByPlugin
        };
    }
}