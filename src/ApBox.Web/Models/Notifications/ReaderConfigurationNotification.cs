using ApBox.Core.Models;
using ApBox.Web.Services.Notifications;

namespace ApBox.Web.Models.Notifications;

/// <summary>
/// Notification sent when reader configuration changes
/// </summary>
public class ReaderConfigurationNotification : INotification
{
    public Guid ReaderId { get; set; }
    public string ReaderName { get; set; } = string.Empty;
    public string? SerialPort { get; set; }
    public int BaudRate { get; set; }
    public byte Address { get; set; }
    public OsdpSecurityMode SecurityMode { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string ChangeType { get; set; } = string.Empty; // "Created", "Updated", "Deleted"
    
    public string NotificationType => "ReaderConfiguration";
}