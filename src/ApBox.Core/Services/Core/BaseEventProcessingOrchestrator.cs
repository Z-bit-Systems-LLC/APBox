using ApBox.Core.Models;
using ApBox.Core.Services.Configuration;
using ApBox.Core.Services.Reader;
using ApBox.Plugins;
using Microsoft.Extensions.Logging;

namespace ApBox.Core.Services.Core;

/// <summary>
/// Base class for event processing orchestrators that eliminates code duplication
/// </summary>
/// <typeparam name="TEvent">The event type (CardReadEvent, PinReadEvent, etc.)</typeparam>
/// <typeparam name="TResult">The result type (CardReadResult, PinReadResult, etc.)</typeparam>
/// <typeparam name="TProcessingService">The processing service interface</typeparam>
/// <typeparam name="TPersistenceService">The persistence service interface</typeparam>
public abstract class BaseEventProcessingOrchestrator<TEvent, TResult, TProcessingService, TPersistenceService>(
    TProcessingService processingService,
    TPersistenceService persistenceService,
    IReaderService readerService,
    IFeedbackConfigurationService feedbackConfigurationService,
    ILogger logger)
    where TEvent : class
    where TResult : class, IProcessingResult, new()
{
    protected readonly TProcessingService ProcessingService = processingService;
    protected readonly TPersistenceService PersistenceService = persistenceService;
    protected readonly IReaderService ReaderService = readerService;
    protected readonly IFeedbackConfigurationService FeedbackConfigurationService = feedbackConfigurationService;
    protected readonly ILogger Logger = logger;

    /// <summary>
    /// Processes an event through the complete orchestration workflow
    /// </summary>
    /// <param name="eventData">The event to process</param>
    /// <returns>The processing result including plugin result and feedback</returns>
    public virtual async Task<EventProcessingResult<TResult>> ProcessEventAsync(TEvent eventData)
    {
        // Log start of processing
        LogProcessingStart(eventData);

        TResult pluginResult;
        ReaderFeedback feedback;
        bool persistenceSuccessful = false;
        bool feedbackDeliverySuccessful = false;
        var readerId = GetReaderId(eventData);

        try
        {
            // Step 1: Process through plugins
            pluginResult = await ProcessThroughPluginsAsync(eventData);
            LogPluginProcessingComplete(eventData, pluginResult);

            // Step 2: Determine feedback
            feedback = pluginResult.Success 
                ? await FeedbackConfigurationService.GetSuccessFeedbackAsync()
                : await FeedbackConfigurationService.GetFailureFeedbackAsync();
            Logger.LogDebug("Feedback determined for reader {ReaderId}: {FeedbackType}", 
                readerId, feedback.Type);
        }
        catch (Exception ex)
        {
            LogPluginProcessingError(eventData, ex);
            
            pluginResult = CreateErrorResult();
            feedback = CreateErrorFeedback();
        }

        // Step 3: Persist event (non-critical)
        try
        {
            if (pluginResult.Success)
            {
                persistenceSuccessful = await PersistSuccessEventAsync(eventData, pluginResult);
            }
            else
            {
                await PersistErrorEventAsync(eventData, GetErrorMessage(pluginResult));
                persistenceSuccessful = true; // Error persistence counts as successful
            }
            
            LogPersistenceComplete(eventData, persistenceSuccessful);
        }
        catch (Exception ex)
        {
            LogPersistenceError(eventData, ex);
        }

        // Step 4: Send feedback to reader (non-critical)
        try
        {
            await ReaderService.SendFeedbackAsync(readerId, feedback);
            feedbackDeliverySuccessful = true;
            Logger.LogDebug("Feedback sent to reader {ReaderId}", readerId);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to send feedback to reader {ReaderId}", readerId);
        }

        LogProcessingComplete(eventData, pluginResult, persistenceSuccessful, feedbackDeliverySuccessful);

        return new EventProcessingResult<TResult>
        {
            PluginResult = pluginResult,
            Feedback = feedback,
            PersistenceSuccessful = persistenceSuccessful,
            FeedbackDeliverySuccessful = feedbackDeliverySuccessful
        };
    }

    // Abstract methods that derived classes must implement
    protected abstract void LogProcessingStart(TEvent eventData);
    protected abstract void LogPluginProcessingComplete(TEvent eventData, TResult result);
    protected abstract void LogPluginProcessingError(TEvent eventData, Exception ex);
    protected abstract void LogPersistenceComplete(TEvent eventData, bool successful);
    protected abstract void LogPersistenceError(TEvent eventData, Exception ex);
    protected abstract void LogProcessingComplete(TEvent eventData, TResult result, bool persistenceSuccessful, bool feedbackDeliverySuccessful);
    protected abstract Guid GetReaderId(TEvent eventData);
    protected abstract Task<TResult> ProcessThroughPluginsAsync(TEvent eventData);
    protected abstract Task<bool> PersistSuccessEventAsync(TEvent eventData, TResult result);
    protected abstract Task PersistErrorEventAsync(TEvent eventData, string errorMessage);

    // Virtual methods that can be overridden if needed
    protected virtual TResult CreateErrorResult() => new()
    {
        Success = false,
        Message = "Plugin processing error occurred"
    };

    protected virtual ReaderFeedback CreateErrorFeedback() => new()
    {
        Type = ReaderFeedbackType.Failure,
        DisplayMessage = "Processing failed",
        LedColor = LedColor.Red,
        BeepCount = 3
    };

    // Now we can directly access Success property from the interface
    protected string GetErrorMessage(TResult result) => result.Message;
}