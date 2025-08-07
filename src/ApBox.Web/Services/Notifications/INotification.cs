namespace ApBox.Web.Services.Notifications;

/// <summary>
/// Base interface for all notification types
/// </summary>
public interface INotification
{
    /// <summary>
    /// When the notification was created
    /// </summary>
    DateTime Timestamp { get; }
    
    /// <summary>
    /// Type identifier for the notification
    /// </summary>
    string NotificationType { get; }
}