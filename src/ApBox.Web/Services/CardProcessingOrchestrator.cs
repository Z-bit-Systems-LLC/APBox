using ApBox.Core.Models;
using ApBox.Core.Services;
using ApBox.Plugins;
using Microsoft.Extensions.Logging;

namespace ApBox.Web.Services;

/// <summary>
/// Orchestrates the card processing workflow
/// </summary>
public class CardProcessingOrchestrator : ICardProcessingOrchestrator
{
    private readonly ICardProcessingService _cardProcessingService;
    private readonly ICardEventPersistenceService _persistenceService;
    private readonly IReaderService _readerService;
    private readonly ICardEventNotificationService _notificationService;
    private readonly ILogger<CardProcessingOrchestrator> _logger;

    public CardProcessingOrchestrator(
        ICardProcessingService cardProcessingService,
        ICardEventPersistenceService persistenceService,
        IReaderService readerService,
        ICardEventNotificationService notificationService,
        ILogger<CardProcessingOrchestrator> logger)
    {
        _cardProcessingService = cardProcessingService;
        _persistenceService = persistenceService;
        _readerService = readerService;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<CardReadResult> OrchestrateCardProcessingAsync(CardReadEvent cardRead)
    {
        _logger.LogInformation("Orchestrating card processing for reader {ReaderId}, card {CardNumber}", 
            cardRead.ReaderId, cardRead.CardNumber);

        CardReadResult result;
        ReaderFeedback feedback;

        try
        {
            // Step 1: Process the card read through plugins
            result = await _cardProcessingService.ProcessCardReadAsync(cardRead);

            // Step 2: Determine feedback based on result
            feedback = await _cardProcessingService.GetFeedbackAsync(cardRead.ReaderId, result);

            // Step 3: Persist the event (non-critical, don't fail the process)
            await _persistenceService.PersistCardEventAsync(cardRead, result);

            // Step 4: Send feedback to the reader
            try
            {
                await _readerService.SendFeedbackAsync(cardRead.ReaderId, feedback);
            }
            catch (Exception feedbackEx)
            {
                _logger.LogError(feedbackEx, "Failed to send feedback to reader {ReaderId}", cardRead.ReaderId);
                // Continue with notification even if feedback fails
            }

            // Step 5: Broadcast notification (non-critical)
            try
            {
                await _notificationService.BroadcastCardEventAsync(cardRead, result, feedback);
            }
            catch (Exception notificationEx)
            {
                _logger.LogError(notificationEx, "Failed to broadcast card event notification");
                // Continue - notification failure shouldn't fail the process
            }

            _logger.LogInformation("Card processing orchestration completed for reader {ReaderId}: {Success}", 
                cardRead.ReaderId, result.Success);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error orchestrating card processing for reader {ReaderId}", cardRead.ReaderId);
            
            // Create error result
            result = new CardReadResult
            {
                Success = false,
                Message = "Processing error occurred"
            };

            // Try to persist the error
            await _persistenceService.PersistCardEventErrorAsync(cardRead, ex.Message);

            // Try to notify about the error
            try
            {
                await _notificationService.BroadcastCardEventAsync(cardRead, result);
            }
            catch (Exception notificationEx)
            {
                _logger.LogError(notificationEx, "Failed to broadcast error notification");
            }

            return result;
        }
    }
}