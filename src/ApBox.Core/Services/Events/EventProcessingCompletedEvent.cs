using ApBox.Core.Models;
using ApBox.Core.Services.Core;
using ApBox.Plugins;

namespace ApBox.Core.Services.Events;

/// <summary>
/// Event raised when card processing is completed
/// </summary>
public class CardProcessingCompletedEvent : IEvent
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public required CardReadEvent CardRead { get; init; }
    public required CardReadResult Result { get; init; }
    public required ReaderFeedback Feedback { get; init; }
    public bool PersistenceSuccessful { get; init; }
    public bool FeedbackDeliverySuccessful { get; init; }
}

/// <summary>
/// Event raised when pin processing is completed
/// </summary>
public class PinProcessingCompletedEvent : IEvent
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public required PinReadEvent PinRead { get; init; }
    public required PinReadResult Result { get; init; }
    public required ReaderFeedback Feedback { get; init; }
    public bool PersistenceSuccessful { get; init; }
    public bool FeedbackDeliverySuccessful { get; init; }
}

/// <summary>
/// Event raised when reader status changes - enriched with database data
/// </summary>
public class ReaderStatusChangedEvent : IEvent
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public required Guid ReaderId { get; init; }
    public required string ReaderName { get; init; }
    public required bool IsOnline { get; init; }
    public string? ErrorMessage { get; init; }
    
    // Enriched properties from database
    public bool IsEnabled { get; init; }
    public OsdpSecurityMode SecurityMode { get; init; }
    public DateTime? LastActivity { get; init; }
    public string Status { get; init; } = string.Empty;
}