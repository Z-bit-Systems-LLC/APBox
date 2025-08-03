namespace ApBox.Plugins;

/// <summary>
/// Represents a card read event from an OSDP reader.
/// Contains all relevant information about the card that was read.
/// </summary>
public class CardReadEvent
{
    /// <summary>
    /// Unique identifier of the reader that generated this event
    /// </summary>
    public Guid ReaderId { get; set; }
    
    /// <summary>
    /// The card number that was read (typically Wiegand format)
    /// </summary>
    public string CardNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// The bit length of the card data (e.g., 26-bit, 34-bit)
    /// </summary>
    public int BitLength { get; set; }
    
    /// <summary>
    /// UTC timestamp when the card was read
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Human-readable name of the reader
    /// </summary>
    public string ReaderName { get; set; } = string.Empty;
    
    /// <summary>
    /// Additional metadata or context data for the card read
    /// </summary>
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}