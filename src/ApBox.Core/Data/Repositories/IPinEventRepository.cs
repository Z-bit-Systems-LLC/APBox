using ApBox.Core.Data.Models;

namespace ApBox.Core.Data.Repositories;

/// <summary>
/// Repository interface for managing PIN events
/// </summary>
public interface IPinEventRepository
{
    /// <summary>
    /// Creates a new PIN event record
    /// </summary>
    Task CreatePinEventAsync(PinEventEntity pinEvent);
    
    /// <summary>
    /// Gets PIN events for a specific reader
    /// </summary>
    Task<IEnumerable<PinEventEntity>> GetPinEventsForReaderAsync(Guid readerId, int limit = 100);
    
    /// <summary>
    /// Gets PIN events within a date range
    /// </summary>
    Task<IEnumerable<PinEventEntity>> GetPinEventsByDateRangeAsync(DateTime startDate, DateTime endDate, int limit = 100);
    
    /// <summary>
    /// Gets the most recent PIN events across all readers
    /// </summary>
    Task<IEnumerable<PinEventEntity>> GetRecentPinEventsAsync(int limit = 50);
    
    /// <summary>
    /// Gets PIN event count for a specific reader within a date range
    /// </summary>
    Task<int> GetPinEventCountAsync(Guid readerId, DateTime startDate, DateTime endDate);
    
    /// <summary>
    /// Deletes PIN events older than the specified date
    /// </summary>
    Task DeleteOldPinEventsAsync(DateTime cutoffDate);
}