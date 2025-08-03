using ApBox.Core.Models;
using ApBox.Core.Services;
using ApBox.Plugins;

namespace ApBox.Web.Services;

/// <summary>
/// Service for broadcasting PIN events via SignalR
/// </summary>
public interface IPinEventNotificationService
{
    /// <summary>
    /// Event fired when a PIN event is processed - for server-side components
    /// </summary>
    event Action<PinEventNotification>? OnPinEventProcessed;
    
    /// <summary>
    /// Broadcast a PIN event to connected clients
    /// </summary>
    /// <param name="pinRead">PIN read event</param>
    /// <param name="result">Processing result</param>
    /// <param name="feedback">Reader feedback (optional)</param>
    Task BroadcastPinEventAsync(PinReadEvent pinRead, PinReadResult result, ReaderFeedback? feedback = null);
}