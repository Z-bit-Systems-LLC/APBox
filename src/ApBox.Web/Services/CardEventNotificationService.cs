using Microsoft.AspNetCore.SignalR;

using ApBox.Core.Models;
using ApBox.Web.Hubs;
using ApBox.Plugins;

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
    Task BroadcastReaderStatusAsync(Guid readerId, string readerName, bool isOnline, bool isEnabled, OsdpSecurityMode securityMode, DateTime? lastActivity = null);

    /// <summary>
    /// Broadcast system statistics update to all connected clients
    /// </summary>
    Task BroadcastStatisticsAsync(int activeReaders, int loadedPlugins, int totalEventsToday, int totalEvents);
}

public class CardEventNotificationService(
    IHubContext<CardEventsHub, ICardEventsClient> hubContext,
    ILogger<CardEventNotificationService> logger)
    : ICardEventNotificationService
{
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

            await hubContext.Clients.Group("CardEvents").CardEventProcessed(notification);

            logger.LogDebug("Broadcasted card event for reader {ReaderName}: {CardNumber}", 
                cardEvent.ReaderName, cardEvent.CardNumber);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to broadcast card event for reader {ReaderId}", cardEvent.ReaderId);
        }
    }

    public async Task BroadcastReaderStatusAsync(Guid readerId, string readerName, bool isOnline, bool isEnabled, OsdpSecurityMode securityMode, DateTime? lastActivity = null)
    {
        try
        {
            var notification = new ReaderStatusNotification
            {
                ReaderId = readerId,
                ReaderName = readerName,
                IsOnline = isOnline,
                IsEnabled = isEnabled,
                SecurityMode = securityMode,
                LastActivity = lastActivity,
                Status = isOnline ? "Online" : "Offline"
            };

            await hubContext.Clients.Group("CardEvents").ReaderStatusChanged(notification);

            logger.LogDebug("Broadcasted reader status change for {ReaderName}: {Status}", 
                readerName, notification.Status);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to broadcast reader status for reader {ReaderId}", readerId);
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

            await hubContext.Clients.Group("CardEvents").StatisticsUpdated(notification);

            logger.LogDebug("Broadcasted statistics update: {ActiveReaders} readers, {LoadedPlugins} plugins", 
                activeReaders, loadedPlugins);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to broadcast statistics update");
        }
    }
}