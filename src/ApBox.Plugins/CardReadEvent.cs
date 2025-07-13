namespace ApBox.Plugins;

public class CardReadEvent
{
    public Guid ReaderId { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public int BitLength { get; set; }
    public DateTime Timestamp { get; set; }
    public string ReaderName { get; set; } = string.Empty;
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}