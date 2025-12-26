using ApBox.Core.Models;
using ApBox.Core.Services.Configuration;
using ApBox.Core.Services.Core;
using ApBox.Core.Services.Infrastructure;
using ApBox.Core.Services.Security;
using ApBox.Core.PacketTracing.Services;
using ApBox.Plugins;
using OSDP.Net;
using OSDP.Net.Tracing;

namespace ApBox.Core.OSDP;

public class OsdpCommunicationManager : IOsdpCommunicationManager
{
    private readonly Dictionary<Guid, IOsdpDevice> _devices = new();
    private readonly Dictionary<string, Guid> _connectionMappings = new(); // Maps connection strings to connection IDs
    private readonly Dictionary<(Guid ConnectionId, byte Address), (string ReaderId, string ReaderName)> _addressToReaderMap = new();
    private readonly ILogger<OsdpCommunicationManager> _logger;
    private readonly ISerialPortService _serialPortService;
    private readonly ISecurityModeUpdateService _securityModeUpdateService;
    private readonly IFeedbackConfigurationService _feedbackConfigurationService;
    private readonly IPinCollectionService _pinCollectionService;
    private readonly IPacketTraceService _packetTraceService;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private ControlPanel? _controlPanel;
    private bool _isRunning;


    public OsdpCommunicationManager(
        ISerialPortService serialPortService,
        ISecurityModeUpdateService securityModeUpdateService,
        IFeedbackConfigurationService feedbackConfigurationService,
        IPinCollectionService pinCollectionService,
        IPacketTraceService packetTraceService,
        ILogger<OsdpCommunicationManager> logger)
    {
        _logger = logger;
        _serialPortService = serialPortService;
        _securityModeUpdateService = securityModeUpdateService;
        _feedbackConfigurationService = feedbackConfigurationService;
        _pinCollectionService = pinCollectionService;
        _packetTraceService = packetTraceService;
        
        // Subscribe to PIN collection events
        _pinCollectionService.PinCollectionCompleted += OnPinCollectionCompleted;
    }
    
    public event EventHandler<CardReadEvent>? CardRead;
    public event EventHandler<PinReadEvent>? PinRead;
    public event EventHandler<PinDigitEvent>? PinDigitReceived;
    public event EventHandler<OsdpDeviceStatusEventArgs>? DeviceStatusChanged;
    public event EventHandler<ReaderStatusChangedEventArgs>? ReaderStatusChanged;
    
    public Task<IEnumerable<IOsdpDevice>> GetDevicesAsync()
    {
        return Task.FromResult<IEnumerable<IOsdpDevice>>(_devices.Values);
    }
    
    public Task<IOsdpDevice?> GetDeviceAsync(Guid deviceId)
    {
        _devices.TryGetValue(deviceId, out var device);
        return Task.FromResult(device);
    }
    
    public async Task<bool> AddDeviceAsync(OsdpDeviceConfiguration config)
    {
        try
        {
            // Ensure we have a shared ControlPanel
            if (_controlPanel == null)
            {
                _controlPanel = new ControlPanel();
                // Subscribe to global events
                _controlPanel.ConnectionStatusChanged += OnConnectionStatusChanged;
            }
            
            // Get or create connection for this device's connection string
            Guid connectionId;
            if (!_connectionMappings.TryGetValue(config.ConnectionString, out connectionId))
            {
                // Validate serial port configuration
                if (string.IsNullOrEmpty(config.ConnectionString))
                {
                    _logger.LogWarning("No serial port configured for OSDP device {DeviceName}", config.Name);
                    return false;
                }
                
                // Check if serial port exists
                if (!_serialPortService.PortExists(config.ConnectionString))
                {
                    _logger.LogWarning("Serial port {SerialPort} not found for OSDP device {DeviceName}", 
                        config.ConnectionString, config.Name);
                    return false;
                }
                
                // Create new connection
                var connection = _serialPortService.CreateConnection(config.ConnectionString, config.BaudRate);
                connectionId = _controlPanel.StartConnection(connection, TimeSpan.FromMilliseconds(100), TraceCallback);
                    
                _connectionMappings[config.ConnectionString] = connectionId;
                
                _logger.LogInformation("Created OSDP connection for {ConnectionString} at {BaudRate} baud", 
                    config.ConnectionString, config.BaudRate);
            }
            
            // Create device with shared ControlPanel and connection
            var device = new OsdpDevice(config, _logger, _controlPanel, connectionId, _feedbackConfigurationService);
            device.CardRead += OnDeviceCardRead;
            device.PinDigitReceived += OnDevicePinDigitReceived;
            device.StatusChanged += OnDeviceStatusChanged;
            device.SecurityModeChanged += OnDeviceSecurityModeChanged;
            
            _devices[config.Id] = device;

            // Map (connectionId, address) to reader info for efficient trace routing
            var key = (connectionId, config.Address);
            _addressToReaderMap[key] = (config.Id.ToString(), config.Name);

            if (_isRunning && config.IsEnabled)
            {
                await device.ConnectAsync();
            }

            _logger.LogInformation("Added OSDP device {DeviceName} with address {Address}",
                config.Name, config.Address);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add OSDP device {DeviceName}", config.Name);
            return false;
        }
    }
    
    public async Task<bool> RemoveDeviceAsync(Guid deviceId)
    {
        if (_devices.TryGetValue(deviceId, out var device))
        {
            device.CardRead -= OnDeviceCardRead;
            device.PinDigitReceived -= OnDevicePinDigitReceived;
            device.StatusChanged -= OnDeviceStatusChanged;

            // Remove from address mapping
            var keyToRemove = _addressToReaderMap
                .FirstOrDefault(kvp => kvp.Value.ReaderId == deviceId.ToString()).Key;
            if (keyToRemove != default)
            {
                _addressToReaderMap.Remove(keyToRemove);
            }

            await device.DisconnectAsync();
            _devices.Remove(deviceId);

            _logger.LogInformation("Removed OSDP device {DeviceId}", deviceId);
            return true;
        }

        return false;
    }
    
    public async Task StartAsync()
    {
        if (_isRunning) return;
        
        _isRunning = true;
        _logger.LogInformation("Starting OSDP Communication Manager");
        
        // Connect to all enabled devices
        var connectTasks = _devices.Values
            .Where(d => d.IsEnabled)
            .Select(d => d.ConnectAsync());
            
        await Task.WhenAll(connectTasks);
        
        _logger.LogInformation("OSDP Communication Manager started with {DeviceCount} devices", 
            _devices.Count);
    }
    
    public async Task StopAsync()
    {
        if (!_isRunning) return;
        
        _isRunning = false;
        _cancellationTokenSource.Cancel();
        
        _logger.LogInformation("Stopping OSDP Communication Manager");
        
        // Disconnect all devices
        var disconnectTasks = _devices.Values.Select(d => d.DisconnectAsync());
        await Task.WhenAll(disconnectTasks);
        
        // Unsubscribe from PIN collection events
        _pinCollectionService.PinCollectionCompleted -= OnPinCollectionCompleted;
        
        // Stop all connections and dispose ControlPanel
        if (_controlPanel != null)
        {
            try
            {
                // Unsubscribe from events
                _controlPanel.ConnectionStatusChanged -= OnConnectionStatusChanged;
                
                // Stop all connections
                var stopTasks = _connectionMappings.Values.Select(connId =>
                    _controlPanel.StopConnection(connId));
                await Task.WhenAll(stopTasks);

                _connectionMappings.Clear();
                _addressToReaderMap.Clear();
                _controlPanel = null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error stopping OSDP ControlPanel");
            }
        }
        
        _logger.LogInformation("OSDP Communication Manager stopped");
    }
    
    private void OnDeviceCardRead(object? sender, CardReadEvent e)
    {
        CardRead?.Invoke(this, e);
    }
    
    
    private void OnDevicePinDigitReceived(object? sender, PinDigitEvent e)
    {
        // Forward to our PIN collection service for processing
        _ = Task.Run(async () => await _pinCollectionService.AddDigitAsync(e.ReaderId, e.Digit));
        
        // Also raise the raw event for any other subscribers
        PinDigitReceived?.Invoke(this, e);
    }
    
    private void OnPinCollectionCompleted(object? sender, PinReadEvent e)
    {
        PinRead?.Invoke(this, e);
    }
    
    private void OnDeviceStatusChanged(object? sender, OsdpStatusChangedEventArgs e)
    {
        if (sender is IOsdpDevice device)
        {
            DeviceStatusChanged?.Invoke(this, new OsdpDeviceStatusEventArgs
            {
                DeviceId = device.Id,
                IsOnline = e.IsOnline,
                Message = e.Message,
                Timestamp = e.Timestamp
            });
            
            // Also trigger the reader status changed event
            ReaderStatusChanged?.Invoke(this, new ReaderStatusChangedEventArgs
            {
                ReaderId = device.Id,
                ReaderName = device.Name,
                IsOnline = e.IsOnline,
                ErrorMessage = e.IsOnline ? null : e.Message
            });
        }
    }
    
    private void OnDeviceSecurityModeChanged(object? sender, SecurityModeChangedEventArgs e)
    {
        // Fire and forget with proper error handling
        _ = Task.Run(async () => await ProcessSecurityModeChangedAsync(e));
    }
    
    private async Task ProcessSecurityModeChangedAsync(SecurityModeChangedEventArgs e)
    {
        try
        {
            _logger.LogInformation("Handling security mode change for device {DeviceName} to {SecurityMode}", 
                e.DeviceName, e.NewMode);
            
            var updateSuccess = await _securityModeUpdateService.UpdateSecurityModeAsync(
                e.DeviceId, 
                e.NewMode, 
                e.SecureChannelKey);
            
            if (updateSuccess)
            {
                _logger.LogInformation("Successfully updated database with new security mode for device {DeviceName}", 
                    e.DeviceName);
            }
            else
            {
                _logger.LogWarning("Failed to update database with new security mode for device {DeviceName}", 
                    e.DeviceName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling security mode change for device {DeviceName}", e.DeviceName);
        }
    }
    
    private void OnConnectionStatusChanged(object? sender, EventArgs args)
    {
        // Global connection status changes will be handled by individual devices
        // This is a placeholder for any manager-level connection monitoring
        _logger.LogDebug("Global OSDP connection status changed");
    }
    
    private void TraceCallback(TraceEntry traceEntry)
    {
        try
        {
            // Try direct lookup using TraceEntry.Address and ConnectionId
            if (traceEntry.Address is { } address)
            {
                var key = (traceEntry.ConnectionId, address);
                if (_addressToReaderMap.TryGetValue(key, out var readerInfo))
                {
                    if (_packetTraceService.IsTracingReader(readerInfo.ReaderId))
                    {
                        _packetTraceService.CapturePacket(traceEntry, readerInfo.ReaderId, readerInfo.ReaderName);
                    }
                    return;
                }
            }

            // Fallback: iterate through devices if address lookup fails
            foreach (var device in _devices.Values)
            {
                if (_packetTraceService.IsTracingReader(device.Id.ToString()))
                {
                    _packetTraceService.CapturePacket(traceEntry, device.Id.ToString(), device.Name);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error processing OSDP trace entry");
        }
    }
    
}