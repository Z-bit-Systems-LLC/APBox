using ApBox.Core.Models;
using ApBox.Core.Extensions;
using ApBox.Core.OSDP;
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
    private readonly IOsdpCommunicationManager _osdpManager;
    private readonly ILogger<ReaderService> _logger;
    
    public ReaderService(
        IReaderConfigurationService configurationService,
        IOsdpSecurityService securityService,
        IOsdpCommunicationManager osdpManager,
        ILogger<ReaderService> logger)
    {
        _configurationService = configurationService;
        _securityService = securityService;
        _osdpManager = osdpManager;
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
            
            // Get the OSDP device for this reader
            var osdpDevice = await _osdpManager.GetDeviceAsync(readerId);
            if (osdpDevice == null)
            {
                _logger.LogWarning("OSDP device not found for reader {ReaderId}", readerId);
                return false;
            }
            
            // Send feedback to the device
            var result = await osdpDevice.SendFeedbackAsync(feedback);
            
            if (result)
            {
                _logger.LogDebug("Feedback sent successfully - Beeps: {BeepCount}, LED: {LedColor}, Duration: {Duration}ms", 
                    feedback.BeepCount, feedback.LedColor, feedback.LedDurationMs);
            }
            else
            {
                _logger.LogWarning("Failed to send feedback to OSDP device {ReaderId}", readerId);
            }
            
            return result;
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

            // Convert reader configuration to OSDP device configuration
            var osdpConfig = reader.ToOsdpConfiguration(_securityService);
            
            // Add device to communication manager
            var result = await _osdpManager.AddDeviceAsync(osdpConfig);
            
            if (result)
            {
                _logger.LogInformation("Connected to reader {ReaderName} ({ReaderId})", reader.ReaderName, readerId);
            }
            else
            {
                _logger.LogWarning("Failed to connect to reader {ReaderName} ({ReaderId})", reader.ReaderName, readerId);
            }
            
            return result;
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
            // Remove OSDP device from communication manager
            var result = await _osdpManager.RemoveDeviceAsync(readerId);
            
            if (result)
            {
                _logger.LogInformation("Disconnected from reader {ReaderId}", readerId);
            }
            else
            {
                _logger.LogWarning("Failed to disconnect from reader {ReaderId} (device not found)", readerId);
            }
            
            return result;
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

            _logger.LogInformation("Testing connection to reader {ReaderName} on {SerialPort}", 
                reader.ReaderName, reader.SerialPort);
            
            // Get the OSDP device for this reader
            var osdpDevice = await _osdpManager.GetDeviceAsync(readerId);
            if (osdpDevice == null)
            {
                _logger.LogWarning("OSDP device not found for reader {ReaderId}", readerId);
                return false;
            }
            
            // Check if device is online
            var isOnline = osdpDevice.IsOnline;
            
            if (isOnline)
            {
                _logger.LogInformation("Reader {ReaderName} is online", reader.ReaderName);
            }
            else
            {
                _logger.LogWarning("Reader {ReaderName} is offline", reader.ReaderName);
            }
            
            return isOnline;
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
            
            _logger.LogInformation("Refreshing {ReaderCount} readers", readers.Count());
            
            // Get current OSDP devices
            var currentDevices = await _osdpManager.GetDevicesAsync();
            var currentDeviceIds = currentDevices.Select(d => d.Id).ToHashSet();
            
            // Add new or changed readers
            foreach (var reader in readers.Where(r => r.IsEnabled))
            {
                if (!currentDeviceIds.Contains(reader.ReaderId))
                {
                    await ConnectReaderAsync(reader.ReaderId);
                }
            }
            
            // Remove disabled readers
            var enabledReaderIds = readers.Where(r => r.IsEnabled).Select(r => r.ReaderId).ToHashSet();
            foreach (var device in currentDevices)
            {
                if (!enabledReaderIds.Contains(device.Id))
                {
                    await DisconnectReaderAsync(device.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh readers");
        }
    }
}