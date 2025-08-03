using ApBox.Plugins;
using ApBox.Core.Data.Repositories;
using ApBox.Core.Data.Models;

namespace ApBox.Core.Services;

/// <summary>
/// Service that persists PIN events to the database with encrypted PIN data
/// </summary>
public class PinEventPersistenceService : IPinEventPersistenceService
{
    private readonly ILogger<PinEventPersistenceService> _logger;
    private readonly IPinEventRepository _pinEventRepository;
    private readonly IDataEncryptionService _encryptionService;

    public PinEventPersistenceService(
        ILogger<PinEventPersistenceService> logger,
        IPinEventRepository pinEventRepository,
        IDataEncryptionService encryptionService)
    {
        _logger = logger;
        _pinEventRepository = pinEventRepository;
        _encryptionService = encryptionService;
    }

    public async Task PersistPinEventAsync(PinReadEvent pinRead, PinReadResult result)
    {
        try
        {
            // Encrypt the PIN data before storing
            var encryptedPin = _encryptionService.EncryptData(pinRead.Pin);
            
            // Determine the processing result message and plugin info
            var message = result.Success ? "PIN accepted" : "PIN rejected";
            var processedByPlugin = result.PluginResults.Any() 
                ? string.Join(", ", result.PluginResults.Select(p => p.Value.PluginName))
                : null;

            // Create and persist the PIN event
            var pinEventEntity = PinEventEntity.FromPinReadEvent(
                pinRead, 
                encryptedPin, 
                result.Success, 
                message, 
                processedByPlugin);

            await _pinEventRepository.CreatePinEventAsync(pinEventEntity);

            _logger.LogInformation("PIN event persisted: Reader={ReaderName} ({ReaderId}), PinLength={PinLength}, CompletionReason={CompletionReason}, Success={Success}, Time={Timestamp:yyyy-MM-dd HH:mm:ss.fff}",
                pinRead.ReaderName,
                pinRead.ReaderId,
                pinRead.Pin.Length,
                pinRead.CompletionReason,
                result.Success,
                pinRead.Timestamp);

            // Log plugin results
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist PIN event for reader {ReaderName} ({ReaderId})", 
                pinRead.ReaderName, pinRead.ReaderId);
            throw;
        }
    }

    public async Task PersistPinEventErrorAsync(PinReadEvent pinRead, string errorMessage)
    {
        try
        {
            // Encrypt the PIN data even for error cases
            var encryptedPin = _encryptionService.EncryptData(pinRead.Pin);
            
            // Create and persist the PIN event with error
            var pinEventEntity = PinEventEntity.FromPinReadEvent(
                pinRead, 
                encryptedPin, 
                success: false, 
                message: errorMessage, 
                processedByPlugin: null);

            await _pinEventRepository.CreatePinEventAsync(pinEventEntity);

            _logger.LogError("PIN event error persisted: Reader={ReaderName} ({ReaderId}), PinLength={PinLength}, Error={ErrorMessage}, Time={Timestamp:yyyy-MM-dd HH:mm:ss.fff}",
                pinRead.ReaderName,
                pinRead.ReaderId,
                pinRead.Pin.Length,
                errorMessage,
                pinRead.Timestamp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist PIN event error for reader {ReaderName} ({ReaderId}): {ErrorMessage}", 
                pinRead.ReaderName, pinRead.ReaderId, errorMessage);
            throw;
        }
    }
}