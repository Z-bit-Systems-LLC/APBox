using ApBox.Core.Data.Repositories;
using ApBox.Core.Models;
using Microsoft.Extensions.Logging;

namespace ApBox.Core.Services;

public class ReaderConfigurationService : IReaderConfigurationService
{
    private readonly IReaderConfigurationRepository _repository;
    private readonly ILogger<ReaderConfigurationService> _logger;
    
    public ReaderConfigurationService(
        IReaderConfigurationRepository repository,
        ILogger<ReaderConfigurationService> logger)
    {
        _repository = repository;
        _logger = logger;
    }
    
    public async Task<IEnumerable<ReaderConfiguration>> GetAllReadersAsync()
    {
        try
        {
            return await _repository.GetAllAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all reader configurations");
            throw;
        }
    }
    
    public async Task<ReaderConfiguration?> GetReaderAsync(Guid readerId)
    {
        try
        {
            return await _repository.GetByIdAsync(readerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reader configuration for {ReaderId}", readerId);
            throw;
        }
    }
    
    public async Task SaveReaderAsync(ReaderConfiguration reader)
    {
        try
        {
            var exists = await _repository.ExistsAsync(reader.ReaderId);
            
            if (exists)
            {
                await _repository.UpdateAsync(reader);
                _logger.LogInformation("Updated reader configuration for {ReaderId}", reader.ReaderId);
            }
            else
            {
                await _repository.CreateAsync(reader);
                _logger.LogInformation("Created new reader configuration for {ReaderId}", reader.ReaderId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving reader configuration for {ReaderId}", reader.ReaderId);
            throw;
        }
    }
    
    public async Task DeleteReaderAsync(Guid readerId)
    {
        try
        {
            var deleted = await _repository.DeleteAsync(readerId);
            
            if (deleted)
            {
                _logger.LogInformation("Deleted reader configuration for {ReaderId}", readerId);
            }
            else
            {
                _logger.LogWarning("Reader configuration {ReaderId} not found for deletion", readerId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting reader configuration for {ReaderId}", readerId);
            throw;
        }
    }
}