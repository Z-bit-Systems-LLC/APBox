namespace ApBox.Plugins;

/// <summary>
/// Represents a PIN read event from an OSDP reader.
/// Contains the PIN data and context about how the PIN entry was completed.
/// </summary>
public class PinReadEvent
{
    /// <summary>
    /// Unique identifier of the reader that generated this event
    /// </summary>
    public Guid ReaderId { get; set; }
    
    /// <summary>
    /// The PIN that was entered by the user
    /// </summary>
    public string Pin { get; set; } = string.Empty;
    
    /// <summary>
    /// UTC timestamp when the PIN entry was completed
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Human-readable name of the reader
    /// </summary>
    public string ReaderName { get; set; } = string.Empty;
    
    /// <summary>
    /// Reason why the PIN entry was completed
    /// </summary>
    public PinCompletionReason CompletionReason { get; set; }
    
    /// <summary>
    /// Additional metadata or context data for the PIN read
    /// </summary>
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

/// <summary>
/// Defines the reason why PIN entry was completed
/// </summary>
public enum PinCompletionReason
{
    /// <summary>
    /// PIN entry completed due to timeout (typically 3 seconds)
    /// </summary>
    Timeout,
    
    /// <summary>
    /// PIN entry completed when user pressed the pound (#) key
    /// </summary>
    PoundKey,
    
    /// <summary>
    /// PIN entry completed when maximum configured PIN length was reached
    /// </summary>
    MaxLength
}