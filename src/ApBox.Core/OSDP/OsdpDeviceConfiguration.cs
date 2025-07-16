using ApBox.Core.Models;

namespace ApBox.Core.OSDP;

public class OsdpDeviceConfiguration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public string Name { get; set; } = string.Empty;
    
    public byte Address { get; set; }
    
    public string ConnectionString { get; set; } = string.Empty; // Serial port or IP address
    
    public int BaudRate { get; set; } = 9600;
    
    public bool UseSecureChannel { get; set; }
    
    public byte[]? SecureChannelKey { get; set; }
    
    public OsdpSecurityMode SecurityMode { get; set; } = OsdpSecurityMode.ClearText;

    public bool IsEnabled { get; set; } = true;
}