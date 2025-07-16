namespace ApBox.Core.Models;

/// <summary>
/// OSDP security mode enumeration
/// </summary>
public enum OsdpSecurityMode
{
    /// <summary>
    /// No security - clear text communication
    /// </summary>
    ClearText = 0,
    
    /// <summary>
    /// Use default key to install random key on reader
    /// </summary>
    Install = 1,
    
    /// <summary>
    /// Use installed random key for secure communication
    /// </summary>
    Secure = 2
}