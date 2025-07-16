using ApBox.Core.Models;
using ApBox.Core.OSDP;
using ApBox.Core.Services;

namespace ApBox.Core.Extensions;

/// <summary>
/// Extension methods for ReaderConfiguration
/// </summary>
public static class ReaderConfigurationExtensions
{
    /// <summary>
    /// Converts a ReaderConfiguration to an OsdpDeviceConfiguration
    /// </summary>
    /// <param name="reader">The reader configuration</param>
    /// <param name="securityService">The security service for key handling</param>
    /// <returns>OsdpDeviceConfiguration for use with OSDP communication</returns>
    public static OsdpDeviceConfiguration ToOsdpConfiguration(this ReaderConfiguration reader, IOsdpSecurityService securityService)
    {
        return new OsdpDeviceConfiguration
        {
            Id = reader.ReaderId,
            Name = reader.ReaderName,
            Address = reader.Address,
            ConnectionString = reader.SerialPort ?? string.Empty,
            BaudRate = reader.BaudRate,
            UseSecureChannel = reader.SecurityMode != OsdpSecurityMode.ClearText,
            SecureChannelKey = securityService.GetSecurityKey(reader.SecurityMode, reader.SecureChannelKey),
            PollInterval = TimeSpan.FromMilliseconds(50), // Default OSDP poll interval
            IsEnabled = reader.IsEnabled
        };
    }
}