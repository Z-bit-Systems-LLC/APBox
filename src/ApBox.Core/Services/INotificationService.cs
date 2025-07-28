using ApBox.Core.Models;

namespace ApBox.Core.Services;

/// <summary>
/// Service for broadcasting notifications to connected clients
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Broadcast reader configuration change to all connected clients
    /// </summary>
    Task BroadcastReaderConfigurationAsync(ReaderConfiguration reader, string changeType);
}