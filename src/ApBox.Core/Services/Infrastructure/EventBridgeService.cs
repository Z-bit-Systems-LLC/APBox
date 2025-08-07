using ApBox.Core.OSDP;
using ApBox.Core.Services.Events;
using ApBox.Plugins;

namespace ApBox.Core.Services.Infrastructure;

/// <summary>
/// Bridges OSDP hardware events to the domain event system
/// </summary>
public class EventBridgeService : BackgroundService
{
    private readonly ILogger<EventBridgeService> _logger;
    private readonly IOsdpCommunicationManager _osdpManager;
    private readonly IEventProcessingPipeline _processingPipeline;

    public EventBridgeService(
        ILogger<EventBridgeService> logger,
        IOsdpCommunicationManager osdpManager,
        IEventProcessingPipeline processingPipeline)
    {
        _logger = logger;
        _osdpManager = osdpManager;
        _processingPipeline = processingPipeline;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Event Bridge Service starting");

        try
        {
            // Subscribe to OSDP hardware events
            _osdpManager.CardRead += OnCardRead;
            _osdpManager.PinRead += OnPinRead;
            _osdpManager.ReaderStatusChanged += OnReaderStatusChanged;
            
            _logger.LogInformation("Event Bridge Service started and subscribed to OSDP events");

            // Keep the service running until cancellation is requested
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("Event Bridge Service stopping");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Event Bridge Service");
        }
        finally
        {
            // Unsubscribe from events
            _osdpManager.CardRead -= OnCardRead;
            _osdpManager.PinRead -= OnPinRead;
            _osdpManager.ReaderStatusChanged -= OnReaderStatusChanged;
        }
    }

    private void OnCardRead(object? sender, CardReadEvent e)
    {
        // Fire and forget with proper error handling
        _ = Task.Run(async () => await ProcessCardReadAsync(e));
    }

    private void OnPinRead(object? sender, PinReadEvent e)
    {
        // Fire and forget with proper error handling  
        _ = Task.Run(async () => await ProcessPinReadAsync(e));
    }

    private void OnReaderStatusChanged(object? sender, ReaderStatusChangedEventArgs e)
    {
        // Fire and forget with proper error handling
        _ = Task.Run(async () => await ProcessReaderStatusChangedAsync(e));
    }

    private async Task ProcessCardReadAsync(CardReadEvent cardRead)
    {
        try
        {
            _logger.LogInformation("Processing card read from device {ReaderName}: Card {CardNumber}", 
                cardRead.ReaderName, cardRead.CardNumber);

            await _processingPipeline.ProcessCardEventAsync(cardRead);
            
            _logger.LogDebug("Card read processed successfully for card {CardNumber}", cardRead.CardNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing card read for card {CardNumber} from device {ReaderName}", 
                cardRead.CardNumber, cardRead.ReaderName);
        }
    }

    private async Task ProcessPinReadAsync(PinReadEvent pinRead)
    {
        try
        {
            _logger.LogInformation("Processing pin read from device {ReaderName}", pinRead.ReaderName);

            await _processingPipeline.ProcessPinEventAsync(pinRead);
            
            _logger.LogDebug("Pin read processed successfully for reader {ReaderName}", pinRead.ReaderName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing pin read from device {ReaderName}", pinRead.ReaderName);
        }
    }

    private async Task ProcessReaderStatusChangedAsync(ReaderStatusChangedEventArgs statusArgs)
    {
        try
        {
            var statusEvent = new ReaderStatusChangedEvent
            {
                ReaderId = statusArgs.ReaderId,
                ReaderName = statusArgs.ReaderName,
                IsOnline = statusArgs.IsOnline,
                ErrorMessage = statusArgs.ErrorMessage
            };

            await _processingPipeline.ProcessReaderStatusChangedAsync(statusEvent);
            
            _logger.LogDebug("Reader status change processed for {ReaderName}: {IsOnline}", 
                statusArgs.ReaderName, statusArgs.IsOnline);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing reader status change for {ReaderName}", 
                statusArgs.ReaderName);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Event Bridge Service stopping");

        if (_osdpManager != null)
        {
            _osdpManager.CardRead -= OnCardRead;
            _osdpManager.PinRead -= OnPinRead;
            _osdpManager.ReaderStatusChanged -= OnReaderStatusChanged;
            _logger.LogInformation("Unsubscribed from OSDP events");
        }

        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        if (_osdpManager != null)
        {
            _osdpManager.CardRead -= OnCardRead;
            _osdpManager.PinRead -= OnPinRead;
            _osdpManager.ReaderStatusChanged -= OnReaderStatusChanged;
        }
        base.Dispose();
    }
}