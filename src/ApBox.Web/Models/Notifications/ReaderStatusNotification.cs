using ApBox.Core.Models;
using ApBox.Web.Services.Notifications;

namespace ApBox.Web.Models.Notifications;

/// <summary>
/// Notification sent when reader status changes
/// </summary>
public class ReaderStatusNotification : INotification
{
    public Guid ReaderId { get; set; }
    public string ReaderName { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public bool IsEnabled { get; set; }
    public OsdpSecurityMode SecurityMode { get; set; }
    public DateTime? LastActivity { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public string NotificationType => "ReaderStatus";
}