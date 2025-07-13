namespace ApBox.Plugins;

public class CardReadResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = new();
    public string? ProcessedByPlugin { get; set; }
}