using ApBox.Web.Services.Notifications;

namespace ApBox.Web.Models.Notifications;

/// <summary>
/// Notification sent when system statistics are updated
/// </summary>
public class StatisticsNotification : INotification
{
    public int ActiveReaders { get; set; }
    public int LoadedPlugins { get; set; }
    public int TotalEventsToday { get; set; }
    public int TotalEvents { get; set; }
    public string SystemStatus { get; set; } = "Online";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public string NotificationType => "Statistics";
}