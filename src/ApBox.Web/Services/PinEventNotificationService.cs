using ApBox.Core.Models;
using ApBox.Core.Services;
using ApBox.Plugins;
using ApBox.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace ApBox.Web.Services;

/// <summary>
/// Service that broadcasts PIN events to connected SignalR clients
/// </summary>
public class PinEventNotificationService : IPinEventNotificationService
{
    private readonly IHubContext<NotificationHub, INotificationClient> _hubContext;
    private readonly ILogger<PinEventNotificationService> _logger;

    public event Action<PinEventNotification>? OnPinEventProcessed;

    public PinEventNotificationService(
        IHubContext<NotificationHub, INotificationClient> hubContext,
        ILogger<PinEventNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task BroadcastPinEventAsync(PinReadEvent pinRead, PinReadResult result, ReaderFeedback? feedback = null)
    {
        try
        {
            var notification = new PinEventNotification
            {
                ReaderId = pinRead.ReaderId,
                ReaderName = pinRead.ReaderName,
                PinLength = pinRead.Pin.Length,
                CompletionReason = pinRead.CompletionReason,
                Timestamp = pinRead.Timestamp,
                Success = result.Success,
                Message = result.Message,
                Feedback = feedback
            };

            // Send to SignalR clients (browsers)
            await _hubContext.Clients.Group("CardEvents").PinEventProcessed(notification);

            // Fire event for server-side subscribers (ViewModels)
            OnPinEventProcessed?.Invoke(notification);

            _logger.LogDebug("PIN event broadcasted via SignalR: Reader={ReaderName}, Success={Success}", 
                pinRead.ReaderName, result.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting PIN event for reader {ReaderName}", pinRead.ReaderName);
            throw;
        }
    }
}