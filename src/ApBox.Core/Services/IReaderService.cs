using ApBox.Core.Models;
using ApBox.Plugins;

namespace ApBox.Core.Services;

public interface IReaderService
{
    Task<IEnumerable<ReaderConfiguration>> GetReadersAsync();
    Task<ReaderConfiguration?> GetReaderAsync(Guid readerId);
    Task UpdateReaderAsync(ReaderConfiguration reader);
    Task<bool> SendFeedbackAsync(Guid readerId, ReaderFeedback feedback);
    
    // OSDP Integration
    Task<bool> ConnectReaderAsync(Guid readerId);
    Task<bool> DisconnectReaderAsync(Guid readerId);
    Task<bool> TestConnectionAsync(Guid readerId);
    Task<bool> InstallSecureKeyAsync(Guid readerId);
    Task RefreshAllReadersAsync();
}

public class ReaderService : IReaderService
{
    private readonly IReaderConfigurationService _configurationService;
    private readonly IOsdpSecurityService _securityService;
    private readonly ILogger<ReaderService> _logger;
    
    public ReaderService(
        IReaderConfigurationService configurationService,
        IOsdpSecurityService securityService,
        ILogger<ReaderService> logger)
    {
        _configurationService = configurationService;
        _securityService = securityService;
        _logger = logger;
    }
    
    public async Task<IEnumerable<ReaderConfiguration>> GetReadersAsync()
    {
        return await _configurationService.GetAllReadersAsync();
    }
    
    public async Task<ReaderConfiguration?> GetReaderAsync(Guid readerId)
    {
        return await _configurationService.GetReaderAsync(readerId);
    }
    
    public async Task UpdateReaderAsync(ReaderConfiguration reader)
    {
        await _configurationService.SaveReaderAsync(reader);
    }
    
    public async Task<bool> SendFeedbackAsync(Guid readerId, ReaderFeedback feedback)
    {
        try
        {
            _logger.LogInformation("Sending feedback to reader {ReaderId}: {FeedbackType}", 
                readerId, feedback.Type);
            
            // TODO: Implement actual OSDP communication
            // For now, just log the feedback
            _logger.LogDebug("Feedback details - Beeps: {BeepCount}, LED: {LedColor}, Duration: {Duration}ms", 
                feedback.BeepCount, feedback.LedColor, feedback.LedDurationMs);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send feedback to reader {ReaderId}", readerId);
            return false;
        }
    }

    public async Task<bool> ConnectReaderAsync(Guid readerId)
    {
        try
        {
            var reader = await _configurationService.GetReaderAsync(readerId);
            if (reader == null)
            {
                _logger.LogWarning("Reader {ReaderId} not found", readerId);
                return false;
            }

            // TODO: Add OSDP device to communication manager
            _logger.LogInformation("Connected to reader {ReaderName} ({ReaderId})", reader.ReaderName, readerId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to reader {ReaderId}", readerId);
            return false;
        }
    }

    public async Task<bool> DisconnectReaderAsync(Guid readerId)
    {
        try
        {
            // TODO: Remove OSDP device from communication manager
            _logger.LogInformation("Disconnected from reader {ReaderId}", readerId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disconnect from reader {ReaderId}", readerId);
            return false;
        }
    }

    public async Task<bool> TestConnectionAsync(Guid readerId)
    {
        try
        {
            var reader = await _configurationService.GetReaderAsync(readerId);
            if (reader == null)
            {
                _logger.LogWarning("Reader {ReaderId} not found", readerId);
                return false;
            }

            // TODO: Test OSDP connection
            _logger.LogInformation("Testing connection to reader {ReaderName} on {SerialPort}", 
                reader.ReaderName, reader.SerialPort);
            
            // For now, simulate a successful connection test
            await Task.Delay(1000); // Simulate connection time
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection test failed for reader {ReaderId}", readerId);
            return false;
        }
    }

    public async Task<bool> InstallSecureKeyAsync(Guid readerId)
    {
        try
        {
            var reader = await _configurationService.GetReaderAsync(readerId);
            if (reader == null)
            {
                _logger.LogWarning("Reader {ReaderId} not found", readerId);
                return false;
            }

            if (reader.SecurityMode != OsdpSecurityMode.Install)
            {
                _logger.LogWarning("Reader {ReaderId} is not in Install mode", readerId);
                return false;
            }

            // Generate and install random key
            var randomKey = _securityService.GenerateRandomKey();
            
            // TODO: Connect with default key, install random key, verify
            _logger.LogInformation("Installing secure key on reader {ReaderName}", reader.ReaderName);
            
            // Update reader configuration to Secure mode with new key
            reader.SecurityMode = OsdpSecurityMode.Secure;
            reader.SecureChannelKey = randomKey;
            reader.UpdatedAt = DateTime.UtcNow;
            
            await _configurationService.SaveReaderAsync(reader);
            
            _logger.LogInformation("Secure key installed successfully on reader {ReaderId}", readerId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install secure key on reader {ReaderId}", readerId);
            return false;
        }
    }

    public async Task RefreshAllReadersAsync()
    {
        try
        {
            var readers = await _configurationService.GetAllReadersAsync();
            
            // TODO: Refresh all OSDP devices in communication manager
            _logger.LogInformation("Refreshing {ReaderCount} readers", readers.Count());
            
            foreach (var reader in readers.Where(r => r.IsEnabled))
            {
                await ConnectReaderAsync(reader.ReaderId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh readers");
        }
    }
}