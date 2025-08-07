using System.Collections.Concurrent;

namespace ApBox.Core.Services.Events;

/// <summary>
/// In-memory event publisher using concurrent collections for thread safety
/// </summary>
public class InMemoryEventPublisher : IEventPublisher
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new();
    private readonly ILogger<InMemoryEventPublisher> _logger;

    public InMemoryEventPublisher(ILogger<InMemoryEventPublisher> logger)
    {
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent eventData) where TEvent : IEvent
    {
        ArgumentNullException.ThrowIfNull(eventData);

        var eventType = typeof(TEvent);
        if (!_handlers.TryGetValue(eventType, out var handlers))
        {
            _logger.LogDebug("No handlers registered for event type {EventType}", eventType.Name);
            return;
        }

        _logger.LogDebug("Publishing event of type {EventType} to {HandlerCount} handlers", 
            eventType.Name, handlers.Count);

        var tasks = handlers.ToList()
            .Cast<Func<TEvent, Task>>()
            .Select(handler => InvokeHandlerSafely(handler, eventData));

        await Task.WhenAll(tasks);
    }

    public void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IEvent
    {
        ArgumentNullException.ThrowIfNull(handler);

        var eventType = typeof(TEvent);
        _handlers.AddOrUpdate(
            eventType,
            new List<Delegate> { handler },
            (key, existingHandlers) =>
            {
                lock (existingHandlers)
                {
                    existingHandlers.Add(handler);
                    return existingHandlers;
                }
            });

        _logger.LogDebug("Subscribed handler for event type {EventType}", eventType.Name);
    }

    public void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IEvent
    {
        ArgumentNullException.ThrowIfNull(handler);

        var eventType = typeof(TEvent);
        if (_handlers.TryGetValue(eventType, out var handlers))
        {
            lock (handlers)
            {
                handlers.Remove(handler);
                if (handlers.Count == 0)
                {
                    _handlers.TryRemove(eventType, out _);
                }
            }
        }

        _logger.LogDebug("Unsubscribed handler for event type {EventType}", eventType.Name);
    }

    private async Task InvokeHandlerSafely<TEvent>(Func<TEvent, Task> handler, TEvent eventData) 
        where TEvent : IEvent
    {
        try
        {
            await handler(eventData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in event handler for event type {EventType}", typeof(TEvent).Name);
        }
    }
}