namespace ApBox.Plugins;

public class PinReadEvent
{
    public Guid ReaderId { get; set; }
    public string Pin { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string ReaderName { get; set; } = string.Empty;
    public PinCompletionReason CompletionReason { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

public enum PinCompletionReason
{
    Timeout,        // 3-second timeout reached
    PoundKey,       // Pound (#) key pressed
    MaxLength       // Maximum PIN length reached (if configured)
}