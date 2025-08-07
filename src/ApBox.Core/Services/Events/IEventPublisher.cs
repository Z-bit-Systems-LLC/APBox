namespace ApBox.Core.Services.Events;

/// <summary>
/// Service for publishing and subscribing to domain events
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publish an event to all subscribers
    /// </summary>
    /// <typeparam name="TEvent">The event type</typeparam>
    /// <param name="eventData">The event data</param>
    Task PublishAsync<TEvent>(TEvent eventData) where TEvent : IEvent;
    
    /// <summary>
    /// Subscribe to events of a specific type
    /// </summary>
    /// <typeparam name="TEvent">The event type to subscribe to</typeparam>
    /// <param name="handler">The handler to call when events are published</param>
    void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IEvent;
    
    /// <summary>
    /// Unsubscribe from events of a specific type
    /// </summary>
    /// <typeparam name="TEvent">The event type to unsubscribe from</typeparam>
    /// <param name="handler">The handler to remove</param>
    void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IEvent;
}