using ApBox.Core.Models;
using ApBox.Core.Data.Repositories;
using ApBox.Plugins;
using Microsoft.Extensions.Logging;

namespace ApBox.Core.Services;

/// <summary>
/// Implementation of card event persistence service
/// </summary>
public class CardEventPersistenceService : ICardEventPersistenceService
{
    private readonly ICardEventRepository _cardEventRepository;
    private readonly ILogger<CardEventPersistenceService> _logger;

    public CardEventPersistenceService(
        ICardEventRepository cardEventRepository,
        ILogger<CardEventPersistenceService> logger)
    {
        _cardEventRepository = cardEventRepository;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<bool> PersistCardEventAsync(CardReadEvent cardRead, CardReadResult result)
    {
        try
        {
            await _cardEventRepository.CreateAsync(cardRead, result);
            _logger.LogDebug("Card event saved to database for reader {ReaderId}, card {CardNumber}", 
                cardRead.ReaderId, cardRead.CardNumber);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save card event to database for reader {ReaderId}, card {CardNumber}", 
                cardRead.ReaderId, cardRead.CardNumber);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> PersistCardEventErrorAsync(CardReadEvent cardRead, string errorMessage)
    {
        try
        {
            var errorResult = new CardReadResult
            {
                Success = false,
                Message = errorMessage
            };
            
            await _cardEventRepository.CreateAsync(cardRead, errorResult);
            _logger.LogDebug("Card event error saved to database for reader {ReaderId}, card {CardNumber}", 
                cardRead.ReaderId, cardRead.CardNumber);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save card event error to database for reader {ReaderId}, card {CardNumber}", 
                cardRead.ReaderId, cardRead.CardNumber);
            return false;
        }
    }
}