using ApBox.Web.Services.Notifications;
using ApBox.Core.PacketTracing.Models;
using ApBox.Core.PacketTracing;

namespace ApBox.Web.Models.Notifications;

/// <summary>
/// Notification sent when a packet is captured during tracing
/// </summary>
public class PacketTraceNotification : INotification
{
    public PacketTraceEntry TraceEntry { get; set; } = null!;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public string NotificationType => "PacketTrace";
}

/// <summary>
/// Notification sent when tracing statistics are updated
/// </summary>
public class TracingStatisticsNotification : INotification
{
    public TracingStatistics Statistics { get; set; } = null!;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public string NotificationType => "TracingStatistics";
}