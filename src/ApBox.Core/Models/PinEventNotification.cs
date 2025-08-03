using ApBox.Plugins;

namespace ApBox.Core.Models;

/// <summary>
/// Notification for PIN events sent via SignalR
/// </summary>
public class PinEventNotification
{
    public Guid ReaderId { get; set; }
    public string ReaderName { get; set; } = string.Empty;
    public int PinLength { get; set; }
    public PinCompletionReason CompletionReason { get; set; }
    public DateTime Timestamp { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public ReaderFeedback? Feedback { get; set; }
}