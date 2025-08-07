using ApBox.Web.Models.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace ApBox.Web.Hubs;

/// <summary>
/// SignalR hub for real-time card event notifications
/// </summary>
public class NotificationHub : Hub<INotificationClient>
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
public interface INotificationClient
{
    /// <summary>
    /// Called when a new card event is processed
    /// </summary>
    Task CardEventProcessed(CardEventNotification notification);

    /// <summary>
    /// Called when a new PIN event is processed
    /// </summary>
    Task PinEventProcessed(PinEventNotification notification);

    /// <summary>
    /// Called when reader status changes
    /// </summary>
    Task ReaderStatusChanged(ReaderStatusNotification notification);

    /// <summary>
    /// Called when reader configuration changes
    /// </summary>
    Task ReaderConfigurationChanged(ReaderConfigurationNotification notification);

    /// <summary>
    /// Called when statistics are updated
    /// </summary>
    Task StatisticsUpdated(StatisticsNotification notification);
}

