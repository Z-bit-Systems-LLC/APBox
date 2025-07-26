using ApBox.Core.Models;
using Microsoft.AspNetCore.SignalR;

namespace ApBox.Web.Hubs;

/// <summary>
/// SignalR hub for real-time card event notifications
/// </summary>
public class CardEventsHub : Hub<ICardEventsClient>
{
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    public override async Task OnConnectedAsync()
    {
        // Automatically join the "CardEvents" group for all connections
        await Groups.AddToGroupAsync(Context.ConnectionId, "CardEvents");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "CardEvents");
        await base.OnDisconnectedAsync(exception);
    }
}

/// <summary>
/// Client interface for SignalR card events hub
/// </summary>
public interface ICardEventsClient
{
    /// <summary>
    /// Called when a new card event is processed
    /// </summary>
    Task CardEventProcessed(CardEventNotification notification);

    /// <summary>
    /// Called when reader status changes
    /// </summary>
    Task ReaderStatusChanged(ReaderStatusNotification notification);

    /// <summary>
    /// Called when statistics are updated
    /// </summary>
    Task StatisticsUpdated(StatisticsNotification notification);
}

/// <summary>
/// Notification sent when a card event is processed
/// </summary>
public class CardEventNotification
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public Guid ReaderId { get; set; }
    public string ReaderName { get; set; } = string.Empty;
    public string CardNumber { get; set; } = string.Empty;
    public int BitLength { get; set; }
    public DateTime Timestamp { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public ReaderFeedback? Feedback { get; set; }
}


/// <summary>
/// Notification sent when system statistics are updated
/// </summary>
public class StatisticsNotification
{
    public int ActiveReaders { get; set; }
    public int LoadedPlugins { get; set; }
    public int TotalEventsToday { get; set; }
    public int TotalEvents { get; set; }
    public string SystemStatus { get; set; } = "Online";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}