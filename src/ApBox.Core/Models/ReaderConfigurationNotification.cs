namespace ApBox.Core.Models;

/// <summary>
/// Notification sent when reader configuration changes
/// </summary>
public class ReaderConfigurationNotification
{
    public Guid ReaderId { get; set; }
    public string ReaderName { get; set; } = string.Empty;
    public string? SerialPort { get; set; }
    public int BaudRate { get; set; }
    public byte Address { get; set; }
    public OsdpSecurityMode SecurityMode { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string ChangeType { get; set; } = string.Empty; // "Created", "Updated", "Deleted"
}