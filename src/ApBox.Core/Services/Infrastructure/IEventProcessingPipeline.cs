using ApBox.Core.Services.Events;
using ApBox.Plugins;

namespace ApBox.Core.Services.Infrastructure;

/// <summary>
/// Unified pipeline for processing all types of events
/// </summary>
public interface IEventProcessingPipeline
{
    /// <summary>
    /// Process a card read event through the complete pipeline
    /// </summary>
    Task ProcessCardEventAsync(CardReadEvent cardRead);
    
    /// <summary>
    /// Process a pin read event through the complete pipeline
    /// </summary>
    Task ProcessPinEventAsync(PinReadEvent pinRead);
    
    /// <summary>
    /// Process a reader status change event
    /// </summary>
    Task ProcessReaderStatusChangedAsync(ReaderStatusChangedEvent statusEvent);
}