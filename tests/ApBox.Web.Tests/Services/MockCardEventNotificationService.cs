using ApBox.Core.Models;
using ApBox.Plugins;
using ApBox.Core.Services.Plugins;
using ApBox.Web.Hubs;
using ApBox.Web.Services;

namespace ApBox.Web.Tests.Services;

/// <summary>
/// Mock implementation of ICardEventNotificationService for unit testing
/// </summary>
public class MockCardEventNotificationService : ICardEventNotificationService
{
    public event Action<CardEventNotification>? OnCardEventProcessed;
    public event Action<ReaderStatusNotification>? OnReaderStatusChanged;

    public Task BroadcastCardEventAsync(CardReadEvent cardEvent, CardReadResult result, ReaderFeedback? feedback = null)
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

        // Fire the event for testing
        OnCardEventProcessed?.Invoke(notification);
        return Task.CompletedTask;
    }

    public Task BroadcastReaderStatusAsync(Guid readerId, string readerName, bool isOnline, bool isEnabled, OsdpSecurityMode securityMode, DateTime? lastActivity = null)
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

        // Fire the event for testing
        OnReaderStatusChanged?.Invoke(notification);
        return Task.CompletedTask;
    }

    public Task BroadcastReaderConfigurationAsync(ReaderConfiguration reader, string changeType)
    {
        return Task.CompletedTask;
    }

    public Task BroadcastStatisticsAsync(int activeReaders, int loadedPlugins, int totalEventsToday, int totalEvents)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Helper method for testing - simulates receiving a card event
    /// </summary>
    public void SimulateCardEventProcessed(CardEventNotification notification)
    {
        OnCardEventProcessed?.Invoke(notification);
    }

    /// <summary>
    /// Helper method for testing - simulates a reader status change
    /// </summary>
    public void SimulateReaderStatusChanged(ReaderStatusNotification notification)
    {
        OnReaderStatusChanged?.Invoke(notification);
    }
}