using ApBox.Core.Models;
using ApBox.Core.Services.Core;
using ApBox.Core.Services.Reader;
using ApBox.Core.Services.Persistence;
using ApBox.Plugins;

namespace ApBox.Web.Services;

/// <summary>
/// Orchestrates the card processing workflow
/// </summary>
public class CardProcessingOrchestrator(
    ICardProcessingService cardProcessingService,
    ICardEventPersistenceService persistenceService,
    IReaderService readerService,
    ICardEventNotificationService notificationService,
    ILogger<CardProcessingOrchestrator> logger)
    : ICardProcessingOrchestrator
{
    /// <inheritdoc/>
    public async Task<CardReadResult> OrchestrateCardProcessingAsync(CardReadEvent cardRead)
    {
        logger.LogInformation("Orchestrating card processing for reader {ReaderId}, card {CardNumber}", 
            cardRead.ReaderId, cardRead.CardNumber);

        CardReadResult result;

        try
        {
            // Step 1: Process the card read through plugins
            result = await cardProcessingService.ProcessCardReadAsync(cardRead);

            // Step 2: Determine feedback based on result
            var feedback = await cardProcessingService.GetFeedbackAsync(cardRead.ReaderId, result);

            // Step 3: Persist the event (non-critical, don't fail the process)
            await persistenceService.PersistCardEventAsync(cardRead, result);

            // Step 4: Send feedback to the reader
            try
            {
                await readerService.SendFeedbackAsync(cardRead.ReaderId, feedback);
            }
            catch (Exception feedbackEx)
            {
                logger.LogError(feedbackEx, "Failed to send feedback to reader {ReaderId}", cardRead.ReaderId);
                // Continue with notification even if feedback fails
            }

            // Step 5: Broadcast notification (non-critical)
            try
            {
                await notificationService.BroadcastCardEventAsync(cardRead, result, feedback);
            }
            catch (Exception notificationEx)
            {
                logger.LogError(notificationEx, "Failed to broadcast card event notification");
                // Continue - notification failure shouldn't fail the process
            }

            logger.LogInformation("Card processing orchestration completed for reader {ReaderId}: {Success}", 
                cardRead.ReaderId, result.Success);

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error orchestrating card processing for reader {ReaderId}", cardRead.ReaderId);
            
            // Create error result
            result = new CardReadResult
            {
                Success = false,
                Message = "Processing error occurred"
            };

            // Try to persist the error
            await persistenceService.PersistCardEventErrorAsync(cardRead, ex.Message);

            // Try to notify about the error
            try
            {
                await notificationService.BroadcastCardEventAsync(cardRead, result);
            }
            catch (Exception notificationEx)
            {
                logger.LogError(notificationEx, "Failed to broadcast error notification");
            }

            return result;
        }
    }
}