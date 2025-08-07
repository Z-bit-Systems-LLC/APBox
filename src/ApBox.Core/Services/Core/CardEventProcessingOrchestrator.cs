using ApBox.Core.Models;
using ApBox.Core.Services.Configuration;
using ApBox.Core.Services.Persistence;
using ApBox.Core.Services.Reader;
using ApBox.Plugins;
using Microsoft.Extensions.Logging;

namespace ApBox.Core.Services.Core;

/// <summary>
/// Orchestrates card event processing workflow
/// </summary>
public class CardEventProcessingOrchestrator(
    ICardProcessingService cardProcessingService,
    ICardEventPersistenceService persistenceService,
    IReaderService readerService,
    IFeedbackConfigurationService feedbackConfigurationService,
    ILogger<CardEventProcessingOrchestrator> logger)
    : BaseEventProcessingOrchestrator<CardReadEvent, CardReadResult, ICardProcessingService, ICardEventPersistenceService>(
        cardProcessingService, persistenceService, readerService, feedbackConfigurationService, logger)
{
    protected override void LogProcessingStart(CardReadEvent eventData)
    {
        Logger.LogInformation("Processing card event for reader {ReaderId}, card {CardNumber}", 
            eventData.ReaderId, eventData.CardNumber);
    }

    protected override void LogPluginProcessingComplete(CardReadEvent eventData, CardReadResult result)
    {
        Logger.LogDebug("Plugin processing completed for card {CardNumber}: {Success}", 
            eventData.CardNumber, result.Success);
    }

    protected override void LogPluginProcessingError(CardReadEvent eventData, Exception ex)
    {
        Logger.LogError(ex, "Error during plugin processing for card {CardNumber}", eventData.CardNumber);
    }

    protected override void LogPersistenceComplete(CardReadEvent eventData, bool successful)
    {
        Logger.LogDebug("Event persistence completed for card {CardNumber}: {Success}", 
            eventData.CardNumber, successful);
    }

    protected override void LogPersistenceError(CardReadEvent eventData, Exception ex)
    {
        Logger.LogWarning(ex, "Failed to persist card event for {CardNumber}", eventData.CardNumber);
    }

    protected override void LogProcessingComplete(CardReadEvent eventData, CardReadResult result, bool persistenceSuccessful, bool feedbackDeliverySuccessful)
    {
        Logger.LogInformation("Card event processing completed for {CardNumber}: Success={Success}, Persistence={Persistence}, Feedback={Feedback}", 
            eventData.CardNumber, result.Success, persistenceSuccessful, feedbackDeliverySuccessful);
    }

    protected override Guid GetReaderId(CardReadEvent eventData) => eventData.ReaderId;

    protected override async Task<CardReadResult> ProcessThroughPluginsAsync(CardReadEvent eventData)
    {
        return await ProcessingService.ProcessCardReadAsync(eventData);
    }


    protected override async Task<bool> PersistSuccessEventAsync(CardReadEvent eventData, CardReadResult result)
    {
        return await PersistenceService.PersistCardEventAsync(eventData, result);
    }

    protected override async Task PersistErrorEventAsync(CardReadEvent eventData, string errorMessage)
    {
        await PersistenceService.PersistCardEventErrorAsync(eventData, errorMessage);
    }
}