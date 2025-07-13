using ApBox.Plugins;

namespace ApBox.Core.OSDP;

public interface IOsdpCommunicationManager
{
    Task<IEnumerable<IOsdpDevice>> GetDevicesAsync();
    Task<IOsdpDevice?> GetDeviceAsync(Guid deviceId);
    Task<bool> AddDeviceAsync(OsdpDeviceConfiguration config);
    Task<bool> RemoveDeviceAsync(Guid deviceId);
    Task StartAsync();
    Task StopAsync();
    
    event EventHandler<CardReadEvent>? CardRead;
    event EventHandler<OsdpDeviceStatusEventArgs>? DeviceStatusChanged;
}

public class OsdpDeviceStatusEventArgs : EventArgs
{
    public Guid DeviceId { get; set; }
    public bool IsOnline { get; set; }
    public string? Message { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class OsdpDeviceConfiguration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public byte Address { get; set; }
    public string ConnectionString { get; set; } = string.Empty; // Serial port or IP address
    public int BaudRate { get; set; } = 9600;
    public bool UseSecureChannel { get; set; } = false;
    public byte[]? SecureChannelKey { get; set; }
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(1);
    public bool IsEnabled { get; set; } = true;
}

public class OsdpCommunicationManager : IOsdpCommunicationManager
{
    private readonly Dictionary<Guid, IOsdpDevice> _devices = new();
    private readonly ILogger<OsdpCommunicationManager> _logger;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private bool _isRunning;
    
    public OsdpCommunicationManager(ILogger<OsdpCommunicationManager> logger)
    {
        _logger = logger;
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
            var device = new MockOsdpDevice(config, _logger);
            device.CardRead += OnDeviceCardRead;
            device.StatusChanged += OnDeviceStatusChanged;
            
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
            .Where(d => d is MockOsdpDevice mockDevice && mockDevice.IsEnabled)
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
}