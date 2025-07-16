using ApBox.Core.Models;
using Blazorise;

namespace ApBox.Web.Helpers;

/// <summary>
/// Helper class for displaying reader status with consistent styling and icons
/// </summary>
public static class ReaderStatusHelper
{
    /// <summary>
    /// Gets the display information for a reader's status including text, color, and icon
    /// </summary>
    /// <param name="reader">The reader configuration</param>
    /// <param name="readerStatuses">Dictionary of reader online statuses</param>
    /// <returns>Tuple containing status text, badge color, and icon (or null)</returns>
    public static (string statusText, Color statusColor, IconName? statusIcon) GetReaderStatusDisplay(
        ReaderConfiguration reader, 
        Dictionary<Guid, bool>? readerStatuses)
    {
        if (!reader.IsEnabled)
        {
            return ("Disabled", Color.Secondary, null);
        }

        var isOnline = readerStatuses?.TryGetValue(reader.ReaderId, out var status) == true && status;
        
        if (!isOnline)
        {
            return ("Offline", Color.Danger, null);
        }

        // Online - check security mode
        return reader.SecurityMode switch
        {
            OsdpSecurityMode.ClearText => ("Online", Color.Warning, IconName.Unlock),
            OsdpSecurityMode.Install => ("Online", Color.Warning, IconName.Wrench),
            OsdpSecurityMode.Secure => ("Online", Color.Success, IconName.Lock),
            _ => ("Online", Color.Warning, IconName.Unlock)
        };
    }
}