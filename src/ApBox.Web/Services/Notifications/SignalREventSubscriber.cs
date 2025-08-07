using ApBox.Core.Services.Events;
using ApBox.Web.Hubs;
using ApBox.Web.Models.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace ApBox.Web.Services.Notifications;

/// <summary>
/// Subscribes to core domain events and broadcasts them via SignalR
/// Pure adapter pattern - no business logic
/// </summary>
public class SignalREventSubscriber : IHostedService
{
    private readonly IEventPublisher _eventPublisher;
    private readonly IHubContext<NotificationHub, INotificationClient> _hubContext;
    private readonly ILogger<SignalREventSubscriber> _logger;

    public SignalREventSubscriber(
        IEventPublisher eventPublisher,
        IHubContext<NotificationHub, INotificationClient> hubContext,
        ILogger<SignalREventSubscriber> logger)
    {
        _eventPublisher = eventPublisher;
        _hubContext = hubContext;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("SignalR Event Subscriber starting");

        // Subscribe to domain events
        _eventPublisher.Subscribe<CardProcessingCompletedEvent>(OnCardProcessingCompleted);
        _eventPublisher.Subscribe<PinProcessingCompletedEvent>(OnPinProcessingCompleted);
        _eventPublisher.Subscribe<ReaderStatusChangedEvent>(OnReaderStatusChanged);

        _logger.LogInformation("SignalR Event Subscriber started and subscribed to domain events");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("SignalR Event Subscriber stopping");

        // Unsubscribe from domain events
        _eventPublisher.Unsubscribe<CardProcessingCompletedEvent>(OnCardProcessingCompleted);
        _eventPublisher.Unsubscribe<PinProcessingCompletedEvent>(OnPinProcessingCompleted);
        _eventPublisher.Unsubscribe<ReaderStatusChangedEvent>(OnReaderStatusChanged);

        _logger.LogInformation("SignalR Event Subscriber stopped");
        return Task.CompletedTask;
    }

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

            await _hubContext.Clients.All.CardEventProcessed(notification);
            
            _logger.LogDebug("Card processing completion broadcasted via SignalR for {CardNumber}", 
                completedEvent.CardRead.CardNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting card processing completion for {CardNumber}", 
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
                Timestamp = completedEvent.PinRead.Timestamp,
                Success = completedEvent.Result.Success,
                Message = completedEvent.Result.Message,
                Feedback = completedEvent.Feedback
            };

            await _hubContext.Clients.All.PinEventProcessed(notification);
            
            _logger.LogDebug("Pin processing completion broadcasted via SignalR for reader {ReaderId}", 
                completedEvent.PinRead.ReaderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting pin processing completion for reader {ReaderId}", 
                completedEvent.PinRead.ReaderId);
        }
    }

    private async Task OnReaderStatusChanged(ReaderStatusChangedEvent statusEvent)
    {
        try
        {
            // Now using enriched data from the pipeline
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

            await _hubContext.Clients.All.ReaderStatusChanged(notification);
            
            _logger.LogDebug("Reader status change broadcasted via SignalR for {ReaderName}: {IsOnline}", 
                statusEvent.ReaderName, statusEvent.IsOnline);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting reader status change for {ReaderName}", 
                statusEvent.ReaderName);
        }
    }
}