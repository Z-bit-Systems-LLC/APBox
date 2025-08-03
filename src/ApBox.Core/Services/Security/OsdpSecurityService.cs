using System.Security.Cryptography;
using ApBox.Core.Models;

namespace ApBox.Core.Services.Security;

/// <summary>
/// Basic implementation of OSDP security service for key management
/// </summary>
public class OsdpSecurityService : IOsdpSecurityService
{
    private static readonly byte[] DefaultSecureChannelKey = new byte[] 
    { 
        0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37,
        0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F
    };

    public byte[]? GetSecurityKey(OsdpSecurityMode mode, byte[]? storedKey)
    {
        return mode switch
        {
            OsdpSecurityMode.ClearText => null,
            OsdpSecurityMode.Install => GetDefaultInstallationKey(),
            OsdpSecurityMode.Secure => storedKey ?? throw new InvalidOperationException("No secure key stored for secure mode"),
            _ => null
        };
    }

    public byte[] GenerateRandomKey()
    {
        var key = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(key);
        }
        return key;
    }

    public byte[] GetDefaultInstallationKey()
    {
        return (byte[])DefaultSecureChannelKey.Clone();
    }
}