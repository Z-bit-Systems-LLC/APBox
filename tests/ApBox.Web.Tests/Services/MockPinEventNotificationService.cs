using ApBox.Core.Models;
using ApBox.Core.Services;
using ApBox.Plugins;
using ApBox.Web.Services;

namespace ApBox.Web.Tests.Services;

/// <summary>
/// Mock implementation of IPinEventNotificationService for unit testing
/// </summary>
public class MockPinEventNotificationService : IPinEventNotificationService
{
    public event Action<PinEventNotification>? OnPinEventProcessed;

    public Task BroadcastPinEventAsync(PinReadEvent pinRead, PinReadResult result, ReaderFeedback? feedback = null)
    {
        var notification = new PinEventNotification
        {
            ReaderId = pinRead.ReaderId,
            ReaderName = pinRead.ReaderName,
            PinLength = pinRead.Pin.Length,
            CompletionReason = pinRead.CompletionReason,
            Timestamp = pinRead.Timestamp,
            Success = result.Success,
            Message = result.Message,
            Feedback = feedback
        };

        // Fire the event for testing
        OnPinEventProcessed?.Invoke(notification);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Helper method for testing - simulates receiving a PIN event
    /// </summary>
    public void SimulatePinEventProcessed(PinEventNotification notification)
    {
        OnPinEventProcessed?.Invoke(notification);
    }
}