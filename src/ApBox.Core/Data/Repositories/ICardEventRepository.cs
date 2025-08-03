using ApBox.Core.Data.Models;
using ApBox.Plugins;

namespace ApBox.Core.Data.Repositories;

/// <summary>
/// Repository interface for managing card event data persistence.
/// Provides methods for querying and storing card read events.
/// </summary>
public interface ICardEventRepository
{
    /// <summary>
    /// Get the most recent card events across all readers.
    /// </summary>
    /// <param name="limit">Maximum number of events to return (default: 100)</param>
    /// <returns>Collection of recent card events ordered by timestamp descending</returns>
    Task<IEnumerable<CardEventEntity>> GetRecentAsync(int limit = 100);
    
    /// <summary>
    /// Get card events for a specific reader.
    /// </summary>
    /// <param name="readerId">The ID of the reader to get events for</param>
    /// <param name="limit">Maximum number of events to return (default: 100)</param>
    /// <returns>Collection of card events for the specified reader ordered by timestamp descending</returns>
    Task<IEnumerable<CardEventEntity>> GetByReaderAsync(Guid readerId, int limit = 100);
    
    /// <summary>
    /// Get card events within a specific date range.
    /// </summary>
    /// <param name="startDate">Start of the date range (inclusive)</param>
    /// <param name="endDate">End of the date range (inclusive)</param>
    /// <param name="limit">Maximum number of events to return (default: 1000)</param>
    /// <returns>Collection of card events within the date range ordered by timestamp descending</returns>
    Task<IEnumerable<CardEventEntity>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, int limit = 1000);
    
    /// <summary>
    /// Create a new card event record from a card read event and its processing result.
    /// </summary>
    /// <param name="cardEvent">The card read event that occurred</param>
    /// <param name="result">The result of processing the card read (optional)</param>
    /// <returns>The created card event entity</returns>
    Task<CardEventEntity> CreateAsync(CardReadEvent cardEvent, CardReadResult? result = null);
    
    /// <summary>
    /// Get the total count of card events in the system.
    /// </summary>
    /// <returns>Total number of card events</returns>
    Task<long> GetCountAsync();
    
    /// <summary>
    /// Get the count of card events for a specific reader.
    /// </summary>
    /// <param name="readerId">The ID of the reader to count events for</param>
    /// <returns>Number of card events for the specified reader</returns>
    Task<long> GetCountByReaderAsync(Guid readerId);
}