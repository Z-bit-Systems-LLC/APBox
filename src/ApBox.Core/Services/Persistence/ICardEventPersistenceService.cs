using ApBox.Core.Models;
using ApBox.Plugins;

namespace ApBox.Core.Services.Persistence;

/// <summary>
/// Handles persistence of card events to the database
/// </summary>
public interface ICardEventPersistenceService
{
    /// <summary>
    /// Persists a card event to the database
    /// </summary>
    /// <param name="cardRead">The card read event</param>
    /// <param name="result">The processing result</param>
    /// <returns>True if persisted successfully, false otherwise</returns>
    Task<bool> PersistCardEventAsync(CardReadEvent cardRead, CardReadResult result);
    
    /// <summary>
    /// Persists a card event error to the database
    /// </summary>
    /// <param name="cardRead">The card read event</param>
    /// <param name="errorMessage">The error message</param>
    /// <returns>True if persisted successfully, false otherwise</returns>
    Task<bool> PersistCardEventErrorAsync(CardReadEvent cardRead, string errorMessage);
}