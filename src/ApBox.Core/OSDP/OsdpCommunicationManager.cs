using ApBox.Core.Models;
using ApBox.Core.Services;
using ApBox.Plugins;
using Microsoft.Extensions.DependencyInjection;
using OSDP.Net;
using OSDP.Net.Connections;

namespace ApBox.Core.OSDP;

public class OsdpCommunicationManager : IOsdpCommunicationManager
{
    private readonly Dictionary<Guid, IOsdpDevice> _devices = new();
    private readonly Dictionary<string, Guid> _connectionMappings = new(); // Maps connection strings to connection IDs
    private readonly ILogger<OsdpCommunicationManager> _logger;
    private readonly ISerialPortService _serialPortService;
    private readonly ISecurityModeUpdateService _securityModeUpdateService;
    private readonly IFeedbackConfigurationService _feedbackConfigurationService;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private ControlPanel? _controlPanel;
    private bool _isRunning;


    public OsdpCommunicationManager(
        ISerialPortService serialPortService,
        ISecurityModeUpdateService securityModeUpdateService,
        IFeedbackConfigurationService feedbackConfigurationService,
        ILogger<OsdpCommunicationManager> logger)
    {
        _logger = logger;
        _serialPortService = serialPortService;
        _securityModeUpdateService = securityModeUpdateService;
        _feedbackConfigurationService = feedbackConfigurationService;
    }
    
    public event EventHandler<CardReadEvent>? CardRead;
    public event EventHandler<OsdpDeviceStatusEventArgs>? DeviceStatusChanged;
    
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
                // _controlPanel.RawCardDataReplyReceived += OnCardRead; // TODO: Enable when OSDP.Net API is confirmed
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
                connectionId = _controlPanel.StartConnection(connection);
                    
                _connectionMappings[config.ConnectionString] = connectionId;
                
                _logger.LogInformation("Created OSDP connection for {ConnectionString} at {BaudRate} baud", 
                    config.ConnectionString, config.BaudRate);
            }
            
            // Create device with shared ControlPanel and connection
            var device = new OsdpDevice(config, _logger, _controlPanel, connectionId, _feedbackConfigurationService);
            device.CardRead += OnDeviceCardRead;
            device.StatusChanged += OnDeviceStatusChanged;
            device.SecurityModeChanged += OnDeviceSecurityModeChanged;
            
            _devices[config.Id] = device;
            
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
            device.StatusChanged -= OnDeviceStatusChanged;
            
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
        }
    }
    
    private async void OnDeviceSecurityModeChanged(object? sender, SecurityModeChangedEventArgs e)
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
}