using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using ApBox.Web.Hubs;
using ApBox.Web.Models.Notifications;

namespace ApBox.Web.Services.Notifications;

/// <summary>
/// Unified notification aggregator that broadcasts all types via SignalR
/// and manages server-side event subscriptions
/// </summary>
public class SignalRNotificationAggregator : INotificationAggregator
{
    private readonly IHubContext<NotificationHub, INotificationClient> _hubContext;
    private readonly ILogger<SignalRNotificationAggregator> _logger;
    private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new();

    public SignalRNotificationAggregator(
        IHubContext<NotificationHub, INotificationClient> hubContext,
        ILogger<SignalRNotificationAggregator> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task BroadcastAsync<TNotification>(TNotification notification) where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);

        try
        {
            // Route to appropriate SignalR method based on notification type
            await BroadcastToSignalR(notification);
            
            // Notify server-side subscribers
            NotifySubscribers(notification);
            
            _logger.LogDebug("Broadcast notification of type {NotificationType}", notification.NotificationType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast notification of type {NotificationType}", notification.NotificationType);
        }
    }

    public void Subscribe<TNotification>(Action<TNotification> handler) where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(handler);
        
        var notificationType = typeof(TNotification);
        _handlers.AddOrUpdate(
            notificationType,
            new List<Delegate> { handler },
            (key, existingHandlers) =>
            {
                existingHandlers.Add(handler);
                return existingHandlers;
            });
            
        _logger.LogDebug("Subscribed handler for notification type {NotificationType}", notificationType.Name);
    }

    public void Unsubscribe<TNotification>(Action<TNotification> handler) where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(handler);
        
        var notificationType = typeof(TNotification);
        if (_handlers.TryGetValue(notificationType, out var handlers))
        {
            handlers.Remove(handler);
            if (handlers.Count == 0)
            {
                _handlers.TryRemove(notificationType, out _);
            }
        }
        
        _logger.LogDebug("Unsubscribed handler for notification type {NotificationType}", notificationType.Name);
    }

    private async Task BroadcastToSignalR<TNotification>(TNotification notification) where TNotification : INotification
    {
        switch (notification)
        {
            case CardEventNotification cardEvent:
                await _hubContext.Clients.All.CardEventProcessed(cardEvent);
                break;
                
            case PinEventNotification pinEvent:
                await _hubContext.Clients.All.PinEventProcessed(pinEvent);
                break;
                
            case ReaderStatusNotification readerStatus:
                await _hubContext.Clients.All.ReaderStatusChanged(readerStatus);
                break;
                
            case ReaderConfigurationNotification readerConfig:
                await _hubContext.Clients.All.ReaderConfigurationChanged(readerConfig);
                break;
                
            case StatisticsNotification statistics:
                await _hubContext.Clients.All.StatisticsUpdated(statistics);
                break;
                
            default:
                _logger.LogWarning("Unknown notification type: {NotificationType}", notification.NotificationType);
                break;
        }
    }

    private void NotifySubscribers<TNotification>(TNotification notification) where TNotification : INotification
    {
        var notificationType = typeof(TNotification);
        if (_handlers.TryGetValue(notificationType, out var handlers))
        {
            foreach (var handler in handlers.ToList()) // ToList to avoid collection modified exception
            {
                try
                {
                    ((Action<TNotification>)handler)(notification);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in notification handler for type {NotificationType}", notificationType.Name);
                }
            }
        }
    }
}