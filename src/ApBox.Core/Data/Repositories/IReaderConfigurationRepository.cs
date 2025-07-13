using ApBox.Core.Data.Models;
using ApBox.Plugins;

namespace ApBox.Core.Data.Repositories;

public interface IReaderConfigurationRepository
{
    Task<IEnumerable<ReaderConfiguration>> GetAllAsync();
    Task<ReaderConfiguration?> GetByIdAsync(Guid readerId);
    Task<ReaderConfiguration> CreateAsync(ReaderConfiguration readerConfiguration);
    Task<ReaderConfiguration> UpdateAsync(ReaderConfiguration readerConfiguration);
    Task<bool> DeleteAsync(Guid readerId);
    Task<bool> ExistsAsync(Guid readerId);
}