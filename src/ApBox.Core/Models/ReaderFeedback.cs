namespace ApBox.Core.Models;

public class ReaderFeedback
{
    public ReaderFeedbackType Type { get; set; }
    public int? BeepCount { get; set; }
    public LedColor? LedColor { get; set; }
    public int? LedDurationMs { get; set; }
    public string? DisplayMessage { get; set; }
}

public enum ReaderFeedbackType
{
    None,
    Success,
    Failure,
    Custom
}

public enum LedColor
{
    Off,
    Red,
    Green,
    Amber,
    Blue
}