using ApBox.Core.Services;
using ApBox.Plugins;

namespace ApBox.Web.Services;

/// <summary>
/// Enhanced card processing service that includes SignalR notifications
/// </summary>
public interface IEnhancedCardProcessingService : ICardProcessingService
{
    /// <summary>
    /// Process a card read with real-time notifications
    /// </summary>
    Task<CardReadResult> ProcessCardReadWithNotificationAsync(CardReadEvent cardRead);
}

public class EnhancedCardProcessingService : IEnhancedCardProcessingService
{
    private readonly ICardProcessingService _coreProcessingService;
    private readonly ICardEventNotificationService _notificationService;
    private readonly IReaderService _readerService;
    private readonly ILogger<EnhancedCardProcessingService> _logger;

    public EnhancedCardProcessingService(
        ICardProcessingService coreProcessingService,
        ICardEventNotificationService notificationService,
        IReaderService readerService,
        ILogger<EnhancedCardProcessingService> logger)
    {
        _coreProcessingService = coreProcessingService;
        _notificationService = notificationService;
        _readerService = readerService;
        _logger = logger;
    }

    public async Task<CardReadResult> ProcessCardReadAsync(CardReadEvent cardRead)
    {
        return await _coreProcessingService.ProcessCardReadAsync(cardRead);
    }

    public async Task<ReaderFeedback> GetFeedbackAsync(Guid readerId, CardReadResult result)
    {
        return await _coreProcessingService.GetFeedbackAsync(readerId, result);
    }

    public async Task<CardReadResult> ProcessCardReadWithNotificationAsync(CardReadEvent cardRead)
    {
        try
        {
            _logger.LogInformation("Processing card read with notification for reader {ReaderId}, card {CardNumber}", 
                cardRead.ReaderId, cardRead.CardNumber);

            // Process the card read using the core service
            var result = await _coreProcessingService.ProcessCardReadAsync(cardRead);

            // Get feedback for the reader
            var feedback = await _coreProcessingService.GetFeedbackAsync(cardRead.ReaderId, result);

            // Send feedback to the reader
            await _readerService.SendFeedbackAsync(cardRead.ReaderId, feedback);

            // Broadcast the event via SignalR
            await _notificationService.BroadcastCardEventAsync(cardRead, result, feedback);

            _logger.LogInformation("Card read processed and broadcasted for reader {ReaderId}: {Success}", 
                cardRead.ReaderId, result.Success);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing card read with notification for reader {ReaderId}", cardRead.ReaderId);
            
            // Still try to broadcast the error
            var errorResult = new CardReadResult
            {
                Success = false,
                Message = "Processing error occurred"
            };

            try
            {
                await _notificationService.BroadcastCardEventAsync(cardRead, errorResult);
            }
            catch (Exception notificationEx)
            {
                _logger.LogError(notificationEx, "Failed to broadcast error notification");
            }

            return errorResult;
        }
    }
}