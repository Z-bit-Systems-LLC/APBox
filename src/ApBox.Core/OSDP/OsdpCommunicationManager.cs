using ApBox.Core.Models;
using ApBox.Core.Services.Configuration;
using ApBox.Core.Services.Core;
using ApBox.Core.Services.Infrastructure;
using ApBox.Core.Services.Security;
using ApBox.Core.PacketTracing.Services;
using ApBox.Core.PacketTracing.Models;
using ApBox.Plugins;
using OSDP.Net;
using OSDP.Net.Model;
using OSDP.Net.Model.ReplyData;
using OSDP.Net.Tracing;

namespace ApBox.Core.OSDP;

public class OsdpCommunicationManager : IOsdpCommunicationManager
{
    private readonly Dictionary<Guid, IOsdpDevice> _devices = new();
    private readonly Dictionary<string, Guid> _connectionMappings = new(); // Maps connection strings to connection IDs
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
            // Convert OSDP.Net TraceEntry to our PacketTraceEntry and capture it
            // Try to determine direction from TraceEntry properties
            var direction = DeterminePacketDirection(traceEntry);
            
            // For now, capture all packets and let the PacketTraceService handle filtering by reader
            // We'll use a generic approach since we may not have direct access to device address from TraceEntry
            foreach (var device in _devices.Values)
            {
                if (_packetTraceService.IsTracingReader(device.Id.ToString()))
                {
                    _packetTraceService.CapturePacket(
                        traceEntry.Data, 
                        direction, 
                        device.Id.ToString(), 
                        device.Name, 
                        device.Address);
                    
                    // For now, just capture to the first device being traced
                    // In a real scenario, we'd need to determine which device the packet belongs to
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error processing OSDP trace entry");
        }
    }
    
    private PacketDirection DeterminePacketDirection(TraceEntry traceEntry)
    {
        try
        {
            // Method 1: Try to use TraceEntry.Direction property
            var direction = traceEntry.Direction;
            _logger.LogDebug("TraceEntry Direction: {Direction} (type: {Type})", direction, direction.GetType().Name);
            
            // Convert based on common enum naming patterns
            var directionString = direction.ToString().ToLowerInvariant();
            if (directionString.Contains("out") || directionString.Contains("tx") || directionString.Contains("send") || 
                directionString.Contains("transmit") || directionString == "outbound")
            {
                return PacketDirection.Outgoing;
            }
            else if (directionString.Contains("in") || directionString.Contains("rx") || directionString.Contains("recv") || 
                     directionString.Contains("receive") || directionString == "inbound")
            {
                return PacketDirection.Incoming;
            }
            
            // Method 2: Analyze OSDP packet structure to determine direction
            if (traceEntry.Data?.Length >= 6) // Minimum OSDP packet size
            {
                // OSDP packet structure: SOM | ADDR | LEN | CTRL | DATA... | CHKSUM
                // SOM = 0xFF (Start of Message)
                // ADDR = Address byte (bit 7 indicates command/reply)
                // Commands: ADDR bit 7 = 0 (0x00-0x7F)
                // Replies:  ADDR bit 7 = 1 (0x80-0xFF)
                
                var data = traceEntry.Data;
                if (data[0] == 0xFF && data.Length >= 4) // Valid OSDP packet
                {
                    var addr = data[1];
                    var isReply = (addr & 0x80) != 0; // Check bit 7
                    
                    var detectedDirection = isReply ? PacketDirection.Incoming : PacketDirection.Outgoing;
                    _logger.LogDebug("OSDP packet analysis: SOM=0x{SOM:X2}, ADDR=0x{ADDR:X2}, IsReply={IsReply}, Direction={Direction}", 
                        data[0], addr, isReply, detectedDirection);
                    
                    return detectedDirection;
                }
            }
            
            // Method 3: Log packet data for manual analysis if we can't determine
            if (traceEntry.Data?.Length > 0)
            {
                var hexData = string.Join(" ", traceEntry.Data.Take(Math.Min(12, traceEntry.Data.Length)).Select(b => $"{b:X2}"));
                _logger.LogWarning("Unable to determine packet direction from TraceEntry.Direction='{Direction}' or packet analysis. " +
                                   "First {Count} bytes: {HexData}", direction, Math.Min(12, traceEntry.Data.Length), hexData);
            }
            
            // Default to incoming if we can't determine
            return PacketDirection.Incoming;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error determining packet direction, defaulting to Incoming");
            return PacketDirection.Incoming;
        }
    }
}