using System.Collections.Concurrent;
using ApBox.Core.Services.Events;
using ApBox.Web.Hubs;
using ApBox.Web.Models.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace ApBox.Web.Services.Notifications;

/// <summary>
/// Unified notification service that handles both server-side (ViewModel) and client-side (SignalR) notifications
/// Combines the functionality of ServerSideNotificationAggregator and SignalREventSubscriber
/// </summary>
public class UnifiedNotificationService : INotificationAggregator, IHostedService
{
    private readonly IEventPublisher _eventPublisher;
    private readonly IHubContext<NotificationHub, INotificationClient> _hubContext;
    private readonly ILogger<UnifiedNotificationService> _logger;
    private readonly ConcurrentDictionary<Type, List<Delegate>> _serverSideHandlers = new();

    public UnifiedNotificationService(
        IEventPublisher eventPublisher,
        IHubContext<NotificationHub, INotificationClient> hubContext,
        ILogger<UnifiedNotificationService> logger)
    {
        _eventPublisher = eventPublisher;
        _hubContext = hubContext;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Unified Notification Service starting");

        // Subscribe to domain events - single point of subscription for all notifications
        _eventPublisher.Subscribe<CardProcessingCompletedEvent>(OnCardProcessingCompleted);
        _eventPublisher.Subscribe<PinProcessingCompletedEvent>(OnPinProcessingCompleted);
        _eventPublisher.Subscribe<ReaderStatusChangedEvent>(OnReaderStatusChanged);

        _logger.LogInformation("Unified Notification Service started - handling both server-side and SignalR notifications");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Unified Notification Service stopping");

        // Unsubscribe from domain events
        _eventPublisher.Unsubscribe<CardProcessingCompletedEvent>(OnCardProcessingCompleted);
        _eventPublisher.Unsubscribe<PinProcessingCompletedEvent>(OnPinProcessingCompleted);
        _eventPublisher.Unsubscribe<ReaderStatusChangedEvent>(OnReaderStatusChanged);

        _logger.LogInformation("Unified Notification Service stopped");
        return Task.CompletedTask;
    }

    #region INotificationAggregator Implementation (for ViewModels)

    public async Task BroadcastAsync<TNotification>(TNotification notification) where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);

        try
        {
            // Notify server-side subscribers (ViewModels)
            NotifyServerSideSubscribers(notification);
            
            _logger.LogDebug("Server-side notification broadcast of type {NotificationType}", 
                notification.NotificationType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast server-side notification of type {NotificationType}", 
                notification.NotificationType);
        }

        await Task.CompletedTask;
    }

    public void Subscribe<TNotification>(Action<TNotification> handler) where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(handler);

        var notificationType = typeof(TNotification);
        _serverSideHandlers.AddOrUpdate(
            notificationType,
            new List<Delegate> { handler },
            (key, existingHandlers) =>
            {
                lock (existingHandlers)
                {
                    existingHandlers.Add(handler);
                    return existingHandlers;
                }
            });

        _logger.LogDebug("Subscribed server-side handler for notification type {NotificationType}", 
            notificationType.Name);
    }

    public void Unsubscribe<TNotification>(Action<TNotification> handler) where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(handler);

        var notificationType = typeof(TNotification);
        if (_serverSideHandlers.TryGetValue(notificationType, out var handlers))
        {
            lock (handlers)
            {
                handlers.Remove(handler);
                if (handlers.Count == 0)
                {
                    _serverSideHandlers.TryRemove(notificationType, out _);
                }
            }
        }

        _logger.LogDebug("Unsubscribed server-side handler for notification type {NotificationType}", 
            notificationType.Name);
    }

    #endregion

    #region Domain Event Handlers (converts and distributes to both channels)

    private async Task OnCardProcessingCompleted(CardProcessingCompletedEvent completedEvent)
    {
        try
        {
            var notification = new CardEventNotification
            {
                ReaderId = completedEvent.CardRead.ReaderId,
                ReaderName = completedEvent.CardRead.ReaderName,
                CardNumber = completedEvent.CardRead.CardNumber,
                BitLength = completedEvent.CardRead.BitLength,
                Timestamp = completedEvent.CardRead.Timestamp,
                Success = completedEvent.Result.Success,
                Message = completedEvent.Result.Message,
                Feedback = completedEvent.Feedback
            };

            // Distribute to both channels
            await DistributeNotificationAsync(notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing card processing completion event for {CardNumber}", 
                completedEvent.CardRead.CardNumber);
        }
    }

    private async Task OnPinProcessingCompleted(PinProcessingCompletedEvent completedEvent)
    {
        try
        {
            var notification = new PinEventNotification
            {
                ReaderId = completedEvent.PinRead.ReaderId,
                ReaderName = completedEvent.PinRead.ReaderName,
                PinLength = completedEvent.PinRead.Pin.Length,
                CompletionReason = completedEvent.PinRead.CompletionReason,
                Timestamp = completedEvent.PinRead.Timestamp,
                Success = completedEvent.Result.Success,
                Message = completedEvent.Result.Message,
                Feedback = completedEvent.Feedback
            };

            // Distribute to both channels
            await DistributeNotificationAsync(notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing pin processing completion event for reader {ReaderId}", 
                completedEvent.PinRead.ReaderId);
        }
    }

    private async Task OnReaderStatusChanged(ReaderStatusChangedEvent statusEvent)
    {
        try
        {
            var notification = new ReaderStatusNotification
            {
                ReaderId = statusEvent.ReaderId,
                ReaderName = statusEvent.ReaderName,
                IsOnline = statusEvent.IsOnline,
                IsEnabled = statusEvent.IsEnabled,
                SecurityMode = statusEvent.SecurityMode,
                LastActivity = statusEvent.LastActivity,
                Status = statusEvent.Status,
                Timestamp = statusEvent.Timestamp
            };

            // Distribute to both channels
            await DistributeNotificationAsync(notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing reader status change event for {ReaderName}", 
                statusEvent.ReaderName);
        }
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Distributes notification to both server-side subscribers and SignalR clients
    /// </summary>
    private async Task DistributeNotificationAsync<TNotification>(TNotification notification) 
        where TNotification : INotification
    {
        // Send to server-side subscribers (ViewModels)
        await BroadcastAsync(notification);

        // Send to SignalR clients
        await BroadcastToSignalRAsync(notification);
    }

    /// <summary>
    /// Broadcasts notification to SignalR clients based on type
    /// </summary>
    private async Task BroadcastToSignalRAsync<TNotification>(TNotification notification) 
        where TNotification : INotification
    {
        try
        {
            switch (notification)
            {
                case CardEventNotification cardNotification:
                    await _hubContext.Clients.All.CardEventProcessed(cardNotification);
                    break;
                case PinEventNotification pinNotification:
                    await _hubContext.Clients.All.PinEventProcessed(pinNotification);
                    break;
                case ReaderStatusNotification statusNotification:
                    await _hubContext.Clients.All.ReaderStatusChanged(statusNotification);
                    break;
                default:
                    _logger.LogWarning("Unknown notification type for SignalR broadcast: {NotificationType}", 
                        notification.GetType().Name);
                    break;
            }
            
            _logger.LogDebug("SignalR notification broadcast completed for type {NotificationType}", 
                notification.NotificationType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting to SignalR clients for notification type {NotificationType}", 
                notification.NotificationType);
        }
    }

    /// <summary>
    /// Notifies server-side subscribers (ViewModels)
    /// </summary>
    private void NotifyServerSideSubscribers<TNotification>(TNotification notification) 
        where TNotification : INotification
    {
        var notificationType = typeof(TNotification);
        if (_serverSideHandlers.TryGetValue(notificationType, out var handlers))
        {
            foreach (var handler in handlers.ToList()) // ToList to avoid collection modified exception
            {
                try
                {
                    ((Action<TNotification>)handler)(notification);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in server-side notification handler for type {NotificationType}", 
                        notificationType.Name);
                }
            }
        }
    }

    #endregion
}