using ApBox.Core.Models;

namespace ApBox.Core.Services.Reader;

public interface IReaderConfigurationService
{
    Task<IEnumerable<ReaderConfiguration>> GetAllReadersAsync();
    Task<ReaderConfiguration?> GetReaderAsync(Guid readerId);
    Task SaveReaderAsync(ReaderConfiguration reader);
    Task DeleteReaderAsync(Guid readerId);
}