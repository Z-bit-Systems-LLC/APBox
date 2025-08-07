using ApBox.Core.Models;
using ApBox.Core.Services.Core;
using ApBox.Plugins;
using ApBox.Web.Models.Notifications;
using ApBox.Web.Services.Notifications;
using Microsoft.Extensions.Logging;

namespace ApBox.Web.Services;

/// <summary>
/// Web adapter for PIN processing that handles notifications
/// </summary>
public class PinProcessingWebOrchestrator(
    PinEventProcessingOrchestrator coreOrchestrator,
    INotificationAggregator notificationAggregator,
    ILogger<PinProcessingWebOrchestrator> logger)
    : IPinProcessingOrchestrator
{
    /// <inheritdoc/>
    public async Task<PinReadResult> OrchestratePinProcessingAsync(PinReadEvent pinRead)
    {
        logger.LogInformation("Web orchestrator processing PIN event for reader {ReaderId}, PIN length {PinLength}", 
            pinRead.ReaderId, pinRead.Pin.Length);

        // Call core orchestrator
        var result = await coreOrchestrator.ProcessEventAsync(pinRead);

        // Handle web-specific concerns (notifications)
        await HandleNotificationAsync(pinRead, result);

        logger.LogInformation("Web PIN processing completed: {Success}", result.PluginResult.Success);

        return result.PluginResult;
    }

    private async Task HandleNotificationAsync(PinReadEvent pinRead, EventProcessingResult<PinReadResult> result)
    {
        try
        {
            var notification = new PinEventNotification
            {
                ReaderId = pinRead.ReaderId,
                ReaderName = pinRead.ReaderName,
                PinLength = pinRead.Pin.Length,
                CompletionReason = pinRead.CompletionReason,
                Timestamp = pinRead.Timestamp,
                Success = result.PluginResult.Success,
                Message = result.PluginResult.Message,
                Feedback = result.Feedback
            };

            await notificationAggregator.BroadcastAsync(notification);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to broadcast PIN event notification");
            // Don't fail the process due to notification issues
        }
    }
}