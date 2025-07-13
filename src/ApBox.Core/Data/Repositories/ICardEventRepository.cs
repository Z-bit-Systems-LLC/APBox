using ApBox.Core.Data.Models;
using ApBox.Plugins;

namespace ApBox.Core.Data.Repositories;

public interface ICardEventRepository
{
    Task<IEnumerable<CardEventEntity>> GetRecentAsync(int limit = 100);
    Task<IEnumerable<CardEventEntity>> GetByReaderAsync(Guid readerId, int limit = 100);
    Task<IEnumerable<CardEventEntity>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, int limit = 1000);
    Task<CardEventEntity> CreateAsync(CardReadEvent cardEvent, CardReadResult? result = null);
    Task<long> GetCountAsync();
    Task<long> GetCountByReaderAsync(Guid readerId);
}