using ApBox.Core.Models;

namespace ApBox.Core.Services;

/// <summary>
/// Service for handling security mode updates when keys are installed
/// </summary>
public interface ISecurityModeUpdateService
{
    /// <summary>
    /// Updates the reader configuration when security mode changes
    /// </summary>
    /// <param name="readerId">The reader ID</param>
    /// <param name="newSecurityMode">The new security mode</param>
    /// <param name="secureChannelKey">The new secure channel key (if applicable)</param>
    /// <returns>True if the update was successful</returns>
    Task<bool> UpdateSecurityModeAsync(Guid readerId, OsdpSecurityMode newSecurityMode, byte[]? secureChannelKey = null);
}