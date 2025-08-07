namespace ApBox.Web.Services.Notifications;

/// <summary>
/// Unified notification service that handles all types of notifications
/// </summary>
public interface INotificationAggregator
{
    /// <summary>
    /// Broadcast a notification to all connected SignalR clients
    /// </summary>
    /// <typeparam name="TNotification">Type of notification</typeparam>
    /// <param name="notification">The notification to broadcast</param>
    Task BroadcastAsync<TNotification>(TNotification notification) where TNotification : INotification;
    
    /// <summary>
    /// Subscribe to server-side notification events
    /// </summary>
    /// <typeparam name="TNotification">Type of notification to subscribe to</typeparam>
    /// <param name="handler">Handler to call when notification is broadcast</param>
    void Subscribe<TNotification>(Action<TNotification> handler) where TNotification : INotification;
    
    /// <summary>
    /// Unsubscribe from server-side notification events
    /// </summary>
    /// <typeparam name="TNotification">Type of notification to unsubscribe from</typeparam>
    /// <param name="handler">Handler to remove</param>
    void Unsubscribe<TNotification>(Action<TNotification> handler) where TNotification : INotification;
}