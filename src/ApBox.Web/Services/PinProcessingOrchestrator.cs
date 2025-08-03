using ApBox.Core.Services;
using ApBox.Plugins;

namespace ApBox.Web.Services;

/// <summary>
/// Orchestrates the PIN processing workflow
/// </summary>
public class PinProcessingOrchestrator(
    IPinProcessingService pinProcessingService,
    IPinEventPersistenceService persistenceService,
    IReaderService readerService,
    IPinEventNotificationService notificationService,
    ILogger<PinProcessingOrchestrator> logger)
    : IPinProcessingOrchestrator
{
    /// <inheritdoc/>
    public async Task<PinReadResult> OrchestratePinProcessingAsync(PinReadEvent pinRead)
    {
        logger.LogInformation("Orchestrating PIN processing for reader {ReaderId}, PIN length {PinLength}", 
            pinRead.ReaderId, pinRead.Pin.Length);

        PinReadResult result;

        try
        {
            // Step 1: Process the PIN read through plugins
            result = await pinProcessingService.ProcessPinReadAsync(pinRead);

            // Step 2: Determine feedback based on result
            var feedback = await pinProcessingService.GetFeedbackAsync(pinRead.ReaderId, result);

            // Step 3: Persist the event (non-critical, don't fail the process)
            try
            {
                await persistenceService.PersistPinEventAsync(pinRead, result);
            }
            catch (Exception persistenceEx)
            {
                logger.LogError(persistenceEx, "Failed to persist PIN event for reader {ReaderId}", pinRead.ReaderId);
                // Continue with feedback even if persistence fails
            }

            // Step 4: Send feedback to the reader
            try
            {
                await readerService.SendFeedbackAsync(pinRead.ReaderId, feedback);
            }
            catch (Exception feedbackEx)
            {
                logger.LogError(feedbackEx, "Failed to send feedback to reader {ReaderId}", pinRead.ReaderId);
                // Continue with notification even if feedback fails
            }

            // Step 5: Broadcast notification (non-critical)
            try
            {
                await notificationService.BroadcastPinEventAsync(pinRead, result, feedback);
            }
            catch (Exception notificationEx)
            {
                logger.LogError(notificationEx, "Failed to broadcast PIN event notification");
                // Continue - notification failure shouldn't fail the process
            }

            logger.LogInformation("PIN processing orchestration completed for reader {ReaderId}: {Success}", 
                pinRead.ReaderId, result.Success);

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error orchestrating PIN processing for reader {ReaderId}", pinRead.ReaderId);
            
            // Create error result
            result = new PinReadResult
            {
                Success = false,
                Message = "PIN processing error occurred"
            };

            // Try to persist the error
            try
            {
                await persistenceService.PersistPinEventErrorAsync(pinRead, ex.Message);
            }
            catch (Exception persistenceEx)
            {
                logger.LogError(persistenceEx, "Failed to persist PIN error for reader {ReaderId}", pinRead.ReaderId);
            }

            // Try to notify about the error
            try
            {
                await notificationService.BroadcastPinEventAsync(pinRead, result);
            }
            catch (Exception notificationEx)
            {
                logger.LogError(notificationEx, "Failed to broadcast PIN error notification");
            }

            return result;
        }
    }
}