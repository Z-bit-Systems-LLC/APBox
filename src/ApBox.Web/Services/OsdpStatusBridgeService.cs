using ApBox.Core.OSDP;
using ApBox.Core.Models;
using ApBox.Core.Data.Repositories;

namespace ApBox.Web.Services;

/// <summary>
/// Service that bridges OSDP communication status events to the notification system
/// </summary>
public class OsdpStatusBridgeService : IHostedService
{
    private readonly IOsdpCommunicationManager _osdpManager;
    private readonly ICardEventNotificationService _notificationService;
    private readonly IReaderConfigurationRepository _readerConfigRepository;
    private readonly ILogger<OsdpStatusBridgeService> _logger;

    public OsdpStatusBridgeService(
        IOsdpCommunicationManager osdpManager,
        ICardEventNotificationService notificationService,
        IReaderConfigurationRepository readerConfigRepository,
        ILogger<OsdpStatusBridgeService> logger)
    {
        _osdpManager = osdpManager;
        _notificationService = notificationService;
        _readerConfigRepository = readerConfigRepository;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting OSDP status bridge service");
        
        // Subscribe to OSDP device status changes
        _osdpManager.DeviceStatusChanged += OnDeviceStatusChanged;
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping OSDP status bridge service");
        
        // Unsubscribe from events
        _osdpManager.DeviceStatusChanged -= OnDeviceStatusChanged;
        
        return Task.CompletedTask;
    }

    private void OnDeviceStatusChanged(object? sender, OsdpDeviceStatusEventArgs e)
    {
        // Fire and forget with proper error handling
        _ = Task.Run(async () => await ProcessDeviceStatusChangedAsync(e));
    }
    
    private async Task ProcessDeviceStatusChangedAsync(OsdpDeviceStatusEventArgs e)
    {
        try
        {
            _logger.LogDebug("Device status changed: {DeviceId} - {Status}", 
                e.DeviceId, e.IsOnline ? "Online" : "Offline");

            // Get reader configuration from repository
            var readerName = "Unknown Reader";
            var isEnabled = false;
            var securityMode = OsdpSecurityMode.ClearText;
            
            var readers = await _readerConfigRepository.GetAllAsync();
            var reader = readers.FirstOrDefault(r => r.ReaderId == e.DeviceId);
            if (reader != null)
            {
                readerName = reader.ReaderName;
                isEnabled = reader.IsEnabled;
                securityMode = reader.SecurityMode;
            }
            
            await _notificationService.BroadcastReaderStatusAsync(
                e.DeviceId,
                readerName,
                e.IsOnline,
                isEnabled,
                securityMode,
                e.Timestamp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting reader status change for device {DeviceId}", e.DeviceId);
        }
    }
}