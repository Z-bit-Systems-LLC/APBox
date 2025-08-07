using ApBox.Web.Services.Notifications;
using System.Collections.Concurrent;

namespace ApBox.Web.Tests.Services;

/// <summary>
/// Mock implementation of INotificationAggregator for unit testing
/// </summary>
public class MockNotificationAggregator : INotificationAggregator
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new();
    public List<INotification> BroadcastedNotifications { get; } = new();

    public Task BroadcastAsync<TNotification>(TNotification notification) where TNotification : INotification
    {
        // Store the notification for verification in tests
        BroadcastedNotifications.Add(notification);
        
        // Notify server-side subscribers
        NotifySubscribers(notification);
        
        return Task.CompletedTask;
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
    }

    private void NotifySubscribers<TNotification>(TNotification notification) where TNotification : INotification
    {
        var notificationType = typeof(TNotification);
        if (_handlers.TryGetValue(notificationType, out var handlers))
        {
            foreach (var handler in handlers.ToList())
            {
                ((Action<TNotification>)handler)(notification);
            }
        }
    }

    /// <summary>
    /// Helper method to get notifications of a specific type for testing
    /// </summary>
    public List<TNotification> GetNotifications<TNotification>() where TNotification : INotification
    {
        return BroadcastedNotifications.OfType<TNotification>().ToList();
    }

    /// <summary>
    /// Clear all recorded notifications
    /// </summary>
    public void ClearNotifications()
    {
        BroadcastedNotifications.Clear();
    }
}