using ApBox.Core.OSDP;
using ApBox.Core.Services.Core;
using ApBox.Plugins;
using ApBox.Core.Services.Plugins;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ApBox.Web.Services;

/// <summary>
/// Hosted service that bridges OSDP PIN digit events to the PIN collection and processing pipeline
/// </summary>
public class PinProcessingBridgeService : BackgroundService
{
    private readonly ILogger<PinProcessingBridgeService> _logger;
    private readonly IOsdpCommunicationManager _osdpManager;
    private readonly IPinCollectionService _pinCollectionService;
    private readonly IPinProcessingOrchestrator _orchestrator;

    public PinProcessingBridgeService(
        ILogger<PinProcessingBridgeService> logger,
        IOsdpCommunicationManager osdpManager,
        IPinCollectionService pinCollectionService,
        IPinProcessingOrchestrator orchestrator)
    {
        _logger = logger;
        _osdpManager = osdpManager;
        _pinCollectionService = pinCollectionService;
        _orchestrator = orchestrator;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PIN Processing Bridge Service starting");

        try
        {
            // Subscribe to PIN digit events from OSDP devices
            _osdpManager.PinDigitReceived += OnPinDigitReceived;
            
            // Subscribe to completed PIN collections
            _pinCollectionService.PinCollectionCompleted += OnPinCollectionCompleted;
            
            _logger.LogInformation("PIN Processing Bridge Service started and subscribed to PIN events");

            // Keep the service running until cancellation is requested
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            // Expected when cancellation is requested
            _logger.LogInformation("PIN Processing Bridge Service stopping");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in PIN Processing Bridge Service");
        }
        finally
        {
            // Unsubscribe from events
            _osdpManager.PinDigitReceived -= OnPinDigitReceived;
            _pinCollectionService.PinCollectionCompleted -= OnPinCollectionCompleted;
        }
    }

    private void OnPinDigitReceived(object? sender, PinDigitEvent e)
    {
        // Fire and forget with proper error handling
        _ = Task.Run(async () => await ProcessPinDigitAsync(e));
    }
    
    private async Task ProcessPinDigitAsync(PinDigitEvent e)
    {
        try
        {
            _logger.LogDebug("Processing PIN digit from device {ReaderName}: {Digit} (sequence {SequenceNumber})", 
                e.ReaderName, e.Digit, e.SequenceNumber);

            var isComplete = await _pinCollectionService.AddDigitAsync(e.ReaderId, e.Digit);
            
            if (isComplete)
            {
                _logger.LogDebug("PIN collection completed after adding digit from device {ReaderName}", e.ReaderName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PIN digit {Digit} from device {ReaderName}", 
                e.Digit, e.ReaderName);
        }
    }
    
    private void OnPinCollectionCompleted(object? sender, PinReadEvent e)
    {
        // Fire and forget with proper error handling
        _ = Task.Run(async () => await ProcessPinReadAsync(e));
    }
    
    private async Task ProcessPinReadAsync(PinReadEvent e)
    {
        try
        {
            _logger.LogInformation("Processing completed PIN read from device {ReaderName}: {PinLength} digits, reason: {CompletionReason}", 
                e.ReaderName, e.Pin.Length, e.CompletionReason);

            // Orchestrate the complete PIN processing workflow
            await _orchestrator.OrchestratePinProcessingAsync(e);
            
            _logger.LogDebug("PIN read processing completed for device {ReaderName}", e.ReaderName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing completed PIN read from device {ReaderName}", e.ReaderName);
        }
    }
}