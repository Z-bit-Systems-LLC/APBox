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
    event EventHandler<PinDigitEvent>? PinDigitReceived;
    event EventHandler<OsdpDeviceStatusEventArgs>? DeviceStatusChanged;
}

public class OsdpDeviceStatusEventArgs : EventArgs
{
    public Guid DeviceId { get; set; }
    public bool IsOnline { get; set; }
    public string? Message { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}