namespace ApBox.Plugins;

public class ReaderConfiguration
{
    public Guid ReaderId { get; set; }
    public string ReaderName { get; set; } = string.Empty;
    public ReaderFeedbackConfiguration? DefaultFeedback { get; set; }
    public Dictionary<string, ReaderFeedbackConfiguration> ResultFeedback { get; set; } = new();
}

public class ReaderFeedbackConfiguration
{
    public ReaderFeedbackType Type { get; set; }
    public int? BeepCount { get; set; }
    public LedColor? LedColor { get; set; }
    public int? LedDurationMs { get; set; }
    public string? DisplayMessage { get; set; }
    
    public ReaderFeedback ToReaderFeedback()
    {
        return new ReaderFeedback
        {
            Type = Type,
            BeepCount = BeepCount,
            LedColor = LedColor,
            LedDurationMs = LedDurationMs,
            DisplayMessage = DisplayMessage
        };
    }
}