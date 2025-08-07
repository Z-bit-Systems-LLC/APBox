using ApBox.Core.Models;

namespace ApBox.Core.Services.Core;

/// <summary>
/// Result of event processing that includes both the plugin result and feedback
/// </summary>
/// <typeparam name="TResult">The plugin result type</typeparam>
public class EventProcessingResult<TResult>
{
    /// <summary>
    /// The result from plugin processing
    /// </summary>
    public required TResult PluginResult { get; init; }
    
    /// <summary>
    /// The feedback to be sent to the reader
    /// </summary>
    public required ReaderFeedback Feedback { get; init; }
    
    /// <summary>
    /// Whether the event was successfully persisted
    /// </summary>
    public bool PersistenceSuccessful { get; init; }
    
    /// <summary>
    /// Whether the feedback was successfully sent to the reader
    /// </summary>
    public bool FeedbackDeliverySuccessful { get; init; }
}