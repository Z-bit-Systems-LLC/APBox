using ApBox.Core.OSDP;
using ApBox.Core.Services.Core;
using ApBox.Plugins;
using ApBox.Core.Services.Plugins;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ApBox.Web.Services;

/// <summary>
/// Hosted service that bridges OSDP card read events to the card processing pipeline
/// </summary>
public class CardProcessingBridgeService : BackgroundService
{
    private readonly ILogger<CardProcessingBridgeService> _logger;
    private readonly IOsdpCommunicationManager _osdpManager;
    private readonly IEnhancedCardProcessingService _cardProcessingService;

    public CardProcessingBridgeService(
        ILogger<CardProcessingBridgeService> logger,
        IOsdpCommunicationManager osdpManager,
        IEnhancedCardProcessingService cardProcessingService)
    {
        _logger = logger;
        _osdpManager = osdpManager;
        _cardProcessingService = cardProcessingService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Card Processing Bridge Service starting");

        try
        {
            // Subscribe to card read events
            _osdpManager.CardRead += OnCardRead;
            _logger.LogInformation("Card Processing Bridge Service started and subscribed to card read events");

            // Keep the service running until cancellation is requested
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            // Expected when cancellation is requested
            _logger.LogInformation("Card Processing Bridge Service stopping");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Card Processing Bridge Service");
        }
        finally
        {
            // Unsubscribe from events
            _osdpManager.CardRead -= OnCardRead;
        }
    }

    private void OnCardRead(object? sender, CardReadEvent e)
    {
        // Fire and forget with proper error handling
        _ = Task.Run(async () => await ProcessCardReadAsync(e));
    }
    
    private async Task ProcessCardReadAsync(CardReadEvent e)
    {
        try
        {
            _logger.LogInformation("Processing card read from device {ReaderName}: Card {CardNumber}", 
                e.ReaderName, e.CardNumber);

            await _cardProcessingService.ProcessCardReadWithNotificationAsync(e);
            
            _logger.LogDebug("Card read processed successfully for card {CardNumber}", e.CardNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing card read for card {CardNumber} from device {ReaderName}", 
                e.CardNumber, e.ReaderName);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Card Processing Bridge Service stopping");

        if (_osdpManager != null)
        {
            _osdpManager.CardRead -= OnCardRead;
            _logger.LogInformation("Unsubscribed from card read events");
        }

        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        if (_osdpManager != null)
        {
            _osdpManager.CardRead -= OnCardRead;
        }
        base.Dispose();
    }
}