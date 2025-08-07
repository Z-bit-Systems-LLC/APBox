using System.Collections.Concurrent;
using ApBox.Core.Services.Events;
using ApBox.Web.Models.Notifications;

namespace ApBox.Web.Services.Notifications;

/// <summary>
/// Server-side notification aggregator that forwards domain events to ViewModels
/// This bridges between the new domain event system and ViewModels that need notifications
/// </summary>
public class ServerSideNotificationAggregator : INotificationAggregator, IHostedService
{
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<ServerSideNotificationAggregator> _logger;
    private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new();

    public ServerSideNotificationAggregator(
        IEventPublisher eventPublisher,
        ILogger<ServerSideNotificationAggregator> logger)
    {
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Server-side notification aggregator starting");

        // Subscribe to domain events and forward them as notifications
        _eventPublisher.Subscribe<CardProcessingCompletedEvent>(OnCardProcessingCompleted);
        _eventPublisher.Subscribe<PinProcessingCompletedEvent>(OnPinProcessingCompleted);
        _eventPublisher.Subscribe<ReaderStatusChangedEvent>(OnReaderStatusChanged);

        _logger.LogInformation("Server-side notification aggregator started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Server-side notification aggregator stopping");

        // Unsubscribe from domain events
        _eventPublisher.Unsubscribe<CardProcessingCompletedEvent>(OnCardProcessingCompleted);
        _eventPublisher.Unsubscribe<PinProcessingCompletedEvent>(OnPinProcessingCompleted);
        _eventPublisher.Unsubscribe<ReaderStatusChangedEvent>(OnReaderStatusChanged);

        _logger.LogInformation("Server-side notification aggregator stopped");
        return Task.CompletedTask;
    }

    public async Task BroadcastAsync<TNotification>(TNotification notification) where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);

        try
        {
            // Notify server-side subscribers (ViewModels)
            NotifySubscribers(notification);
            
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
        _handlers.AddOrUpdate(
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

        _logger.LogDebug("Subscribed handler for server-side notification type {NotificationType}", 
            notificationType.Name);
    }

    public void Unsubscribe<TNotification>(Action<TNotification> handler) where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(handler);

        var notificationType = typeof(TNotification);
        if (_handlers.TryGetValue(notificationType, out var handlers))
        {
            lock (handlers)
            {
                handlers.Remove(handler);
                if (handlers.Count == 0)
                {
                    _handlers.TryRemove(notificationType, out _);
                }
            }
        }

        _logger.LogDebug("Unsubscribed handler for server-side notification type {NotificationType}", 
            notificationType.Name);
    }

    private async Task OnCardProcessingCompleted(CardProcessingCompletedEvent completedEvent)
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

        await BroadcastAsync(notification);
    }

    private async Task OnPinProcessingCompleted(PinProcessingCompletedEvent completedEvent)
    {
        var notification = new PinEventNotification
        {
            ReaderId = completedEvent.PinRead.ReaderId,
            ReaderName = completedEvent.PinRead.ReaderName,
            Timestamp = completedEvent.PinRead.Timestamp,
            Success = completedEvent.Result.Success,
            Message = completedEvent.Result.Message,
            Feedback = completedEvent.Feedback
        };

        await BroadcastAsync(notification);
    }

    private async Task OnReaderStatusChanged(ReaderStatusChangedEvent statusEvent)
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

        await BroadcastAsync(notification);
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
                    _logger.LogError(ex, "Error in server-side notification handler for type {NotificationType}", 
                        notificationType.Name);
                }
            }
        }
    }
}