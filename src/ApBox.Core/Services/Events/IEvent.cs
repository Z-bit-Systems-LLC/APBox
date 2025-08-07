namespace ApBox.Core.Services.Events;

/// <summary>
/// Marker interface for all events in the system
/// </summary>
public interface IEvent
{
    /// <summary>
    /// The timestamp when the event occurred
    /// </summary>
    DateTime Timestamp { get; }
}