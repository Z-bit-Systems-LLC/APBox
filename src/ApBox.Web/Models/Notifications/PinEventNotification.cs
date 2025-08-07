using ApBox.Plugins;
using ApBox.Core.Models;
using ApBox.Web.Services.Notifications;

namespace ApBox.Web.Models.Notifications;

/// <summary>
/// Notification for PIN events sent via SignalR
/// </summary>
public class PinEventNotification : INotification
{
    public Guid ReaderId { get; set; }
    public string ReaderName { get; set; } = string.Empty;
    public int PinLength { get; set; }
    public PinCompletionReason CompletionReason { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public ReaderFeedback? Feedback { get; set; }
    
    public string NotificationType => "PinEvent";
}