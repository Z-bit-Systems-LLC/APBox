using ApBox.Core.OSDP;
using ApBox.Core.Models;
using ApBox.Core.Services;
using ApBox.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ApBox.Web.Services;

/// <summary>
/// Hosted service that bridges OSDP card read events to the card processing pipeline
/// </summary>
public class CardProcessingBridgeService : BackgroundService
{
    private readonly ILogger<CardProcessingBridgeService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private IOsdpCommunicationManager? _osdpManager;

    public CardProcessingBridgeService(
        ILogger<CardProcessingBridgeService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Card Processing Bridge Service starting");

        // Wait for OSDP Communication Manager to be available
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _osdpManager = _serviceProvider.GetService<IOsdpCommunicationManager>();
                if (_osdpManager != null)
                {
                    break;
                }
                
                _logger.LogDebug("Waiting for OSDP Communication Manager to be available...");
                await Task.Delay(1000, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting OSDP Communication Manager, retrying...");
                await Task.Delay(5000, stoppingToken);
            }
        }

        if (_osdpManager != null && !stoppingToken.IsCancellationRequested)
        {
            // Subscribe to card read events
            _osdpManager.CardRead += OnCardRead;
            _logger.LogInformation("Card Processing Bridge Service started and subscribed to card read events");

            // Keep the service running until cancellation is requested
            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Expected when cancellation is requested
            }
        }
    }

    private async void OnCardRead(object? sender, CardReadEvent e)
    {
        try
        {
            _logger.LogInformation("Processing card read from device {ReaderName}: Card {CardNumber}", 
                e.ReaderName, e.CardNumber);

            using var scope = _serviceProvider.CreateScope();
            var cardProcessingService = scope.ServiceProvider.GetRequiredService<IEnhancedCardProcessingService>();
            
            await cardProcessingService.ProcessCardReadAsync(e);
            
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