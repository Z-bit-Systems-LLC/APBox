namespace ApBox.Core.Models;

/// <summary>
/// Event arguments for security mode changes in OSDP devices
/// </summary>
public class SecurityModeChangedEventArgs : EventArgs
{
    /// <summary>
    /// The unique identifier of the device that changed security mode
    /// </summary>
    public Guid DeviceId { get; init; }
    
    /// <summary>
    /// The new security mode that was applied
    /// </summary>
    public OsdpSecurityMode NewMode { get; init; }
    
    /// <summary>
    /// The secure channel key used (if applicable)
    /// </summary>
    public byte[]? SecureChannelKey { get; init; }
    
    /// <summary>
    /// The name of the device for logging purposes
    /// </summary>
    public string DeviceName { get; init; } = string.Empty;
    
    /// <summary>
    /// Timestamp when the security mode change occurred
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}