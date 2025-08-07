using ApBox.Plugins;
using ApBox.Core.Models;
using ApBox.Web.Services.Notifications;

namespace ApBox.Web.Models.Notifications;

/// <summary>
/// Notification sent when a card event is processed
/// </summary>
public class CardEventNotification : INotification
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public Guid ReaderId { get; set; }
    public string ReaderName { get; set; } = string.Empty;
    public string CardNumber { get; set; } = string.Empty;
    public int BitLength { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public ReaderFeedback? Feedback { get; set; }
    
    public string NotificationType => "CardEvent";
}