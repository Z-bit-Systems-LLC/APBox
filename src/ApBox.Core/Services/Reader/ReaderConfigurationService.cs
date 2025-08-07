using ApBox.Core.Data.Repositories;
using ApBox.Core.Models;

namespace ApBox.Core.Services.Reader;

public class ReaderConfigurationService : IReaderConfigurationService
{
    private readonly IReaderConfigurationRepository _repository;
    private readonly ILogger<ReaderConfigurationService> _logger;
    private readonly Lazy<IReaderService> _readerService;
    
    public ReaderConfigurationService(
        IReaderConfigurationRepository repository,
        ILogger<ReaderConfigurationService> logger,
        Lazy<IReaderService> readerService)
    {
        _repository = repository;
        _logger = logger;
        _readerService = readerService;
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
            var oldConfiguration = exists ? await _repository.GetByIdAsync(reader.ReaderId) : null;
            
            if (exists)
            {
                await _repository.UpdateAsync(reader);
                _logger.LogInformation("Updated reader configuration for {ReaderId}", reader.ReaderId);
                
                // Note: Configuration change notifications are handled at the Web layer
            }
            else
            {
                await _repository.CreateAsync(reader);
                _logger.LogInformation("Created new reader configuration for {ReaderId}", reader.ReaderId);
                
                // Note: Configuration change notifications are handled at the Web layer
            }

            // Handle connection management
            if (exists && oldConfiguration != null && ShouldRestartConnection(oldConfiguration, reader))
            {
                // Existing reader with configuration changes - restart connection
                await RestartReaderConnectionAsync(reader);
            }
            else if (!exists && reader.IsEnabled)
            {
                // New enabled reader - connect immediately using RefreshAllReadersAsync to ensure consistency
                await RefreshAllReadersAsync();
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
            // Get reader configuration before deleting for notification
            var readerToDelete = await _repository.GetByIdAsync(readerId);
            
            // Disconnect from OSDP bus before deleting from database
            try
            {
                var readerService = _readerService.Value;
                await readerService.DisconnectReaderAsync(readerId);
                _logger.LogDebug("Disconnected reader {ReaderId} from OSDP bus before deletion", readerId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to disconnect reader {ReaderId} from OSDP bus during deletion", readerId);
                // Continue with database deletion even if disconnect fails
            }
            
            var deleted = await _repository.DeleteAsync(readerId);
            
            if (deleted)
            {
                _logger.LogInformation("Deleted reader configuration for {ReaderId}", readerId);
                
                // Note: Configuration change notifications are handled at the Web layer
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

    /// <summary>
    /// Determines if a reader connection should be restarted due to configuration changes
    /// </summary>
    private static bool ShouldRestartConnection(ReaderConfiguration oldConfig, ReaderConfiguration newConfig)
    {
        // Check if connection-critical settings have changed
        return oldConfig.SerialPort != newConfig.SerialPort ||
               oldConfig.BaudRate != newConfig.BaudRate ||
               oldConfig.Address != newConfig.Address ||
               oldConfig.SecurityMode != newConfig.SecurityMode ||
               !AreSecureChannelKeysEqual(oldConfig.SecureChannelKey, newConfig.SecureChannelKey) ||
               oldConfig.IsEnabled != newConfig.IsEnabled;
    }

    /// <summary>
    /// Compares two secure channel key arrays for equality
    /// </summary>
    private static bool AreSecureChannelKeysEqual(byte[]? key1, byte[]? key2)
    {
        if (key1 == null && key2 == null) return true;
        if (key1 == null || key2 == null) return false;
        if (key1.Length != key2.Length) return false;
        
        for (int i = 0; i < key1.Length; i++)
        {
            if (key1[i] != key2[i]) return false;
        }
        
        return true;
    }

    /// <summary>
    /// Refreshes all readers to sync OSDP manager with database changes
    /// </summary>
    private async Task RefreshAllReadersAsync()
    {
        try
        {
            var readerService = _readerService.Value;
            await readerService.RefreshAllReadersAsync();
            _logger.LogDebug("Refreshed all readers after configuration change");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing readers after configuration change");
            // Don't rethrow - we don't want configuration save to fail due to refresh issues
        }
    }

    /// <summary>
    /// Restarts the reader connection with new configuration
    /// </summary>
    private async Task RestartReaderConnectionAsync(ReaderConfiguration reader)
    {
        try
        {
            _logger.LogInformation("Restarting connection for reader {ReaderName} ({ReaderId}) due to configuration changes", 
                reader.ReaderName, reader.ReaderId);

            var readerService = _readerService.Value;
            
            // Disconnect the existing connection (natural OSDP status change will handle offline notification)
            await readerService.DisconnectReaderAsync(reader.ReaderId);
            
            // Wait a brief moment for the disconnection to complete
            await Task.Delay(500);
            
            // Reconnect with new configuration if enabled
            if (reader.IsEnabled)
            {
                var connected = await readerService.ConnectReaderAsync(reader.ReaderId);
                if (connected)
                {
                    _logger.LogInformation("Successfully restarted connection for reader {ReaderName}", reader.ReaderName);
                    // Natural OSDP status events will handle online notification
                }
                else
                {
                    _logger.LogWarning("Failed to reconnect reader {ReaderName} with new configuration", reader.ReaderName);
                    // Device remains offline from disconnect - natural OSDP events handle this
                }
            }
            else
            {
                _logger.LogInformation("Reader {ReaderName} is disabled, keeping disconnected", reader.ReaderName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restarting connection for reader {ReaderId}", reader.ReaderId);
            // Natural OSDP status events will reflect the actual device state
            // Don't rethrow - we don't want configuration save to fail due to connection issues
        }
    }
}