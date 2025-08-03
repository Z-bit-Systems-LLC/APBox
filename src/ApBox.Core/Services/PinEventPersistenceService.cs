using ApBox.Plugins;

namespace ApBox.Core.Services;

/// <summary>
/// Service that persists PIN events - currently logs them since we don't store PIN data for security
/// </summary>
public class PinEventPersistenceService : IPinEventPersistenceService
{
    private readonly ILogger<PinEventPersistenceService> _logger;

    public PinEventPersistenceService(ILogger<PinEventPersistenceService> logger)
    {
        _logger = logger;
    }

    public Task PersistPinEventAsync(PinReadEvent pinRead, PinReadResult result)
    {
        // For security, we don't persist actual PIN data to the database
        // Instead, we log the event with metadata only
        _logger.LogInformation("PIN event processed: Reader={ReaderName} ({ReaderId}), PinLength={PinLength}, CompletionReason={CompletionReason}, Success={Success}, Time={Timestamp:yyyy-MM-dd HH:mm:ss.fff}",
            pinRead.ReaderName,
            pinRead.ReaderId,
            pinRead.Pin.Length,
            pinRead.CompletionReason,
            result.Success,
            pinRead.Timestamp);

        // Log plugin results without PIN data
        if (result.PluginResults.Any())
        {
            foreach (var pluginResult in result.PluginResults)
            {
                _logger.LogInformation("PIN plugin result: Plugin={PluginName}, Success={Success}, Reader={ReaderName}",
                    pluginResult.Value.PluginName,
                    pluginResult.Value.Success,
                    pinRead.ReaderName);
            }
        }

        return Task.CompletedTask;
    }

    public Task PersistPinEventErrorAsync(PinReadEvent pinRead, string errorMessage)
    {
        _logger.LogError("PIN event error: Reader={ReaderName} ({ReaderId}), PinLength={PinLength}, Error={ErrorMessage}, Time={Timestamp:yyyy-MM-dd HH:mm:ss.fff}",
            pinRead.ReaderName,
            pinRead.ReaderId,
            pinRead.Pin.Length,
            errorMessage,
            pinRead.Timestamp);

        return Task.CompletedTask;
    }
}