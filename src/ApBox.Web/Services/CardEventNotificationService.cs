using ApBox.Core.Models;
using Microsoft.AspNetCore.SignalR;
using ApBox.Web.Hubs;
using ApBox.Plugins;
using ApBox.Core.Services;

namespace ApBox.Web.Services;

/// <summary>
/// Service for broadcasting card events and system updates via SignalR
/// </summary>
public interface ICardEventNotificationService
{
    /// <summary>
    /// Broadcast a card event to all connected clients
    /// </summary>
    Task BroadcastCardEventAsync(CardReadEvent cardEvent, CardReadResult result, ReaderFeedback? feedback = null);

    /// <summary>
    /// Broadcast reader status change to all connected clients
    /// </summary>
    Task BroadcastReaderStatusAsync(Guid readerId, string readerName, bool isOnline, DateTime? lastActivity = null);

    /// <summary>
    /// Broadcast system statistics update to all connected clients
    /// </summary>
    Task BroadcastStatisticsAsync(int activeReaders, int loadedPlugins, int totalEventsToday, int totalEvents);
}

public class CardEventNotificationService : ICardEventNotificationService
{
    private readonly IHubContext<CardEventsHub, ICardEventsClient> _hubContext;
    private readonly ILogger<CardEventNotificationService> _logger;

    public CardEventNotificationService(
        IHubContext<CardEventsHub, ICardEventsClient> hubContext,
        ILogger<CardEventNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task BroadcastCardEventAsync(CardReadEvent cardEvent, CardReadResult result, ReaderFeedback? feedback = null)
    {
        try
        {
            var notification = new CardEventNotification
            {
                ReaderId = cardEvent.ReaderId,
                ReaderName = cardEvent.ReaderName,
                CardNumber = cardEvent.CardNumber,
                BitLength = cardEvent.BitLength,
                Timestamp = cardEvent.Timestamp,
                Success = result.Success,
                Message = result.Message,
                Feedback = feedback
            };

            await _hubContext.Clients.Group("CardEvents").CardEventProcessed(notification);

            _logger.LogDebug("Broadcasted card event for reader {ReaderName}: {CardNumber}", 
                cardEvent.ReaderName, cardEvent.CardNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast card event for reader {ReaderId}", cardEvent.ReaderId);
        }
    }

    public async Task BroadcastReaderStatusAsync(Guid readerId, string readerName, bool isOnline, DateTime? lastActivity = null)
    {
        try
        {
            var notification = new ReaderStatusNotification
            {
                ReaderId = readerId,
                ReaderName = readerName,
                IsOnline = isOnline,
                LastActivity = lastActivity,
                Status = isOnline ? "Online" : "Offline"
            };

            await _hubContext.Clients.Group("CardEvents").ReaderStatusChanged(notification);

            _logger.LogDebug("Broadcasted reader status change for {ReaderName}: {Status}", 
                readerName, notification.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast reader status for reader {ReaderId}", readerId);
        }
    }

    public async Task BroadcastStatisticsAsync(int activeReaders, int loadedPlugins, int totalEventsToday, int totalEvents)
    {
        try
        {
            var notification = new StatisticsNotification
            {
                ActiveReaders = activeReaders,
                LoadedPlugins = loadedPlugins,
                TotalEventsToday = totalEventsToday,
                TotalEvents = totalEvents,
                SystemStatus = "Online"
            };

            await _hubContext.Clients.Group("CardEvents").StatisticsUpdated(notification);

            _logger.LogDebug("Broadcasted statistics update: {ActiveReaders} readers, {LoadedPlugins} plugins", 
                activeReaders, loadedPlugins);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast statistics update");
        }
    }
}