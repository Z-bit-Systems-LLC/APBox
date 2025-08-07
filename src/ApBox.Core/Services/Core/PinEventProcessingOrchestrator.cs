using ApBox.Core.Models;
using ApBox.Core.Services.Persistence;
using ApBox.Core.Services.Reader;
using ApBox.Plugins;
using Microsoft.Extensions.Logging;

namespace ApBox.Core.Services.Core;

/// <summary>
/// Orchestrates PIN event processing workflow
/// </summary>
public class PinEventProcessingOrchestrator(
    IPinProcessingService pinProcessingService,
    IPinEventPersistenceService persistenceService,
    IReaderService readerService,
    ILogger<PinEventProcessingOrchestrator> logger)
    : BaseEventProcessingOrchestrator<PinReadEvent, PinReadResult, IPinProcessingService, IPinEventPersistenceService>(
        pinProcessingService, persistenceService, readerService, logger)
{
    protected override void LogProcessingStart(PinReadEvent eventData)
    {
        Logger.LogInformation("Processing PIN event for reader {ReaderId}, PIN length {PinLength}", 
            eventData.ReaderId, eventData.Pin.Length);
    }

    protected override void LogPluginProcessingComplete(PinReadEvent eventData, PinReadResult result)
    {
        Logger.LogDebug("Plugin processing completed for PIN: {Success}", result.Success);
    }

    protected override void LogPluginProcessingError(PinReadEvent eventData, Exception ex)
    {
        Logger.LogError(ex, "Error during plugin processing for PIN");
    }

    protected override void LogPersistenceComplete(PinReadEvent eventData, bool successful)
    {
        Logger.LogDebug("Event persistence completed for PIN: {Success}", successful);
    }

    protected override void LogPersistenceError(PinReadEvent eventData, Exception ex)
    {
        Logger.LogWarning(ex, "Failed to persist PIN event");
    }

    protected override void LogProcessingComplete(PinReadEvent eventData, PinReadResult result, bool persistenceSuccessful, bool feedbackDeliverySuccessful)
    {
        Logger.LogInformation("PIN event processing completed: Success={Success}, Persistence={Persistence}, Feedback={Feedback}", 
            result.Success, persistenceSuccessful, feedbackDeliverySuccessful);
    }

    protected override Guid GetReaderId(PinReadEvent eventData) => eventData.ReaderId;

    protected override async Task<PinReadResult> ProcessThroughPluginsAsync(PinReadEvent eventData)
    {
        return await ProcessingService.ProcessPinReadAsync(eventData);
    }

    protected override async Task<ReaderFeedback> GetFeedbackAsync(Guid readerId, PinReadResult result)
    {
        return await ProcessingService.GetFeedbackAsync(readerId, result);
    }

    protected override async Task<bool> PersistSuccessEventAsync(PinReadEvent eventData, PinReadResult result)
    {
        await PersistenceService.PersistPinEventAsync(eventData, result);
        return true; // PersistPinEventAsync returns void, so we assume success
    }

    protected override async Task PersistErrorEventAsync(PinReadEvent eventData, string errorMessage)
    {
        await PersistenceService.PersistPinEventErrorAsync(eventData, errorMessage);
    }
}