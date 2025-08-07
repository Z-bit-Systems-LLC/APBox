using ApBox.Core.Models;
using ApBox.Core.Services.Core;
using ApBox.Plugins;
using ApBox.Web.Models.Notifications;
using ApBox.Web.Services.Notifications;
using Microsoft.Extensions.Logging;

namespace ApBox.Web.Services;

/// <summary>
/// Web adapter for card processing that handles notifications
/// </summary>
public class CardProcessingWebOrchestrator(
    CardEventProcessingOrchestrator coreOrchestrator,
    INotificationAggregator notificationAggregator,
    ILogger<CardProcessingWebOrchestrator> logger)
    : ICardProcessingOrchestrator
{
    /// <inheritdoc/>
    public async Task<CardReadResult> OrchestrateCardProcessingAsync(CardReadEvent cardRead)
    {
        logger.LogInformation("Web orchestrator processing card event for reader {ReaderId}, card {CardNumber}", 
            cardRead.ReaderId, cardRead.CardNumber);

        // Call core orchestrator
        var result = await coreOrchestrator.ProcessEventAsync(cardRead);

        // Handle web-specific concerns (notifications)
        await HandleNotificationAsync(cardRead, result);

        logger.LogInformation("Web card processing completed for {CardNumber}: {Success}", 
            cardRead.CardNumber, result.PluginResult.Success);

        return result.PluginResult;
    }

    private async Task HandleNotificationAsync(CardReadEvent cardRead, EventProcessingResult<CardReadResult> result)
    {
        try
        {
            var notification = new CardEventNotification
            {
                ReaderId = cardRead.ReaderId,
                ReaderName = cardRead.ReaderName,
                CardNumber = cardRead.CardNumber,
                BitLength = cardRead.BitLength,
                Timestamp = cardRead.Timestamp,
                Success = result.PluginResult.Success,
                Message = result.PluginResult.Message,
                Feedback = result.Feedback
            };

            await notificationAggregator.BroadcastAsync(notification);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to broadcast card event notification for {CardNumber}", cardRead.CardNumber);
            // Don't fail the process due to notification issues
        }
    }
}