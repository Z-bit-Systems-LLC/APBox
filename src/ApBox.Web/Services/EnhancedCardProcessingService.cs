using ApBox.Core.Services;
using ApBox.Core.Models;
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

/// <summary>
/// Simplified enhanced card processing service that delegates to the orchestrator
/// </summary>
public class EnhancedCardProcessingService : IEnhancedCardProcessingService
{
    private readonly ICardProcessingService _coreProcessingService;
    private readonly ICardProcessingOrchestrator _orchestrator;
    private readonly ILogger<EnhancedCardProcessingService> _logger;

    public EnhancedCardProcessingService(
        ICardProcessingService coreProcessingService,
        ICardProcessingOrchestrator orchestrator,
        ILogger<EnhancedCardProcessingService> logger)
    {
        _coreProcessingService = coreProcessingService;
        _orchestrator = orchestrator;
        _logger = logger;
    }

    public async Task<CardReadResult> ProcessCardReadAsync(CardReadEvent cardRead)
    {
        _logger.LogInformation("Processing card read: Card {CardNumber} on reader {ReaderName}", 
            cardRead.CardNumber, cardRead.ReaderName);
            
        try
        {
            var result = await _coreProcessingService.ProcessCardReadAsync(cardRead);
            
            _logger.LogInformation("Card read processed successfully: {Success}, Message: {Message}", 
                result.Success, result.Message);
                
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing card read for card {CardNumber} on reader {ReaderName}", 
                cardRead.CardNumber, cardRead.ReaderName);
            throw;
        }
    }

    public async Task<ReaderFeedback> GetFeedbackAsync(Guid readerId, CardReadResult result)
    {
        return await _coreProcessingService.GetFeedbackAsync(readerId, result);
    }

    public async Task<CardReadResult> ProcessCardReadWithNotificationAsync(CardReadEvent cardRead)
    {
        _logger.LogInformation("Processing card read with notification for reader {ReaderId}, card {CardNumber}", 
            cardRead.ReaderId, cardRead.CardNumber);
            
        // Delegate to the orchestrator which handles all the workflow steps
        return await _orchestrator.OrchestrateCardProcessingAsync(cardRead);
    }
}