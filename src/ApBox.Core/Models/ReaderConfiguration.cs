namespace ApBox.Core.Models;

/// <summary>
/// Core reader configuration model
/// </summary>
public class ReaderConfiguration
{
    public Guid ReaderId { get; set; }
    public string ReaderName { get; set; } = string.Empty;
    public byte Address { get; set; } = 0;
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // OSDP Communication Settings
    public string? SerialPort { get; set; }
    public int BaudRate { get; set; } = 9600;
    public OsdpSecurityMode SecurityMode { get; set; } = OsdpSecurityMode.ClearText;
    public byte[]? SecureChannelKey { get; set; }
    
    // Plugin Mappings
    public List<ReaderPluginMapping> PluginMappings { get; set; } = new();
}