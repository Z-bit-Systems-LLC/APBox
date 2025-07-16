using ApBox.Core.Models;

namespace ApBox.Core.Services;

/// <summary>
/// Service for managing OSDP security keys and operations
/// </summary>
public interface IOsdpSecurityService
{
    /// <summary>
    /// Gets the security key for the specified mode and stored key
    /// </summary>
    /// <param name="mode">The security mode</param>
    /// <param name="storedKey">The stored encrypted key (if any)</param>
    /// <returns>The decrypted key for use or null if no security</returns>
    byte[]? GetSecurityKey(OsdpSecurityMode mode, byte[]? storedKey);
    
    /// <summary>
    /// Generates a random 16-byte security key
    /// </summary>
    /// <returns>Random security key</returns>
    byte[] GenerateRandomKey();
    
    /// <summary>
    /// Gets the default OSDP installation key
    /// </summary>
    /// <returns>Default OSDP installation key</returns>
    byte[] GetDefaultInstallationKey();
}