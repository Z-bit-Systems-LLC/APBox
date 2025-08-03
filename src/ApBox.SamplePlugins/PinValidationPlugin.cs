using ApBox.Plugins;
using Microsoft.Extensions.Logging;

namespace ApBox.SamplePlugins;

/// <summary>
/// Sample PIN validation plugin that validates 6-digit PIN codes for access control.
/// Maintains a list of authorized PIN codes and grants access only to valid PINs.
/// </summary>
public class PinValidationPlugin : IApBoxPlugin
{
    private readonly HashSet<string> _authorizedPins;
    private readonly ILogger? _logger;

    public PinValidationPlugin()
    {
        // Initialize with some sample authorized 6-digit PIN codes
        _authorizedPins = new HashSet<string>(StringComparer.Ordinal)
        {
            "123456",    // Sample authorized PINs
            "654321",
            "111111",
            "222222",
            "987654",
            "456789"
        };
    }

    public PinValidationPlugin(ILogger<PinValidationPlugin> logger) : this()
    {
        _logger = logger;
    }

    // Constructor for non-generic ILogger (used by plugin loader)
    public PinValidationPlugin(ILogger logger) : this()
    {
        _logger = logger;
    }

    public Guid Id => new Guid("B2C3D4E5-6789-ABCD-EF01-234567890123");
    public string Name => "PIN Validation Plugin";
    public string Version => "1.0.0";
    public string Description => "Validates 6-digit PIN codes for access control by maintaining a list of authorized PIN codes";

    public async Task<bool> ProcessCardReadAsync(CardReadEvent cardRead)
    {
        await Task.CompletedTask; // This plugin doesn't process card reads
        
        _logger?.LogDebug("PIN Validation Plugin does not process card reads - skipping card {CardNumber} from reader {ReaderName}", 
            cardRead.CardNumber, cardRead.ReaderName);

        // Return true to indicate successful processing (no-op for card reads)
        return true;
    }

    public async Task<bool> ProcessPinReadAsync(PinReadEvent pinRead)
    {
        await Task.CompletedTask; // Async signature for future extensibility
        
        _logger?.LogInformation("PIN Validation Plugin processing PIN from reader {ReaderName}: {PinLength} digits, reason: {CompletionReason}", 
            pinRead.ReaderName, pinRead.Pin.Length, pinRead.CompletionReason);

        // Validate PIN format: exactly 6 digits
        if (pinRead.Pin.Length != 6)
        {
            _logger?.LogWarning("PIN from reader {ReaderName} rejected - invalid length: {PinLength} (expected 6 digits)", 
                pinRead.ReaderName, pinRead.Pin.Length);
            return false;
        }

        // Ensure all characters are digits
        if (!pinRead.Pin.All(char.IsDigit))
        {
            _logger?.LogWarning("PIN from reader {ReaderName} rejected - contains non-digit characters", 
                pinRead.ReaderName);
            return false;
        }

        // Check if the PIN is in our authorized list
        bool isAuthorized = _authorizedPins.Contains(pinRead.Pin);

        if (isAuthorized)
        {
            _logger?.LogInformation("PIN from reader {ReaderName} is authorized - granting access", pinRead.ReaderName);
        }
        else
        {
            _logger?.LogWarning("PIN from reader {ReaderName} is not authorized - denying access", pinRead.ReaderName);
        }

        return isAuthorized;
    }

    public Task InitializeAsync()
    {
        _logger?.LogInformation("PIN Validation Plugin initialized with {Count} authorized 6-digit PIN codes", 
            _authorizedPins.Count);
        return Task.CompletedTask;
    }

    public Task ShutdownAsync()
    {
        _logger?.LogInformation("PIN Validation Plugin shutting down");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Add a PIN to the authorized list (for demonstration purposes)
    /// </summary>
    /// <param name="pin">The 6-digit PIN to add</param>
    /// <returns>True if the PIN was added, false if it was invalid or already exists</returns>
    public bool AddAuthorizedPin(string pin)
    {
        if (string.IsNullOrWhiteSpace(pin) || pin.Length != 6 || !pin.All(char.IsDigit))
        {
            _logger?.LogWarning("Cannot add invalid PIN: must be exactly 6 digits");
            return false;
        }

        if (_authorizedPins.Add(pin))
        {
            _logger?.LogInformation("Added PIN to authorized list");
            return true;
        }

        _logger?.LogDebug("PIN already exists in authorized list");
        return false;
    }

    /// <summary>
    /// Remove a PIN from the authorized list (for demonstration purposes)
    /// </summary>
    /// <param name="pin">The PIN to remove</param>
    /// <returns>True if the PIN was removed, false if it didn't exist</returns>
    public bool RemoveAuthorizedPin(string pin)
    {
        if (_authorizedPins.Remove(pin))
        {
            _logger?.LogInformation("Removed PIN from authorized list");
            return true;
        }

        _logger?.LogDebug("PIN not found in authorized list");
        return false;
    }

    /// <summary>
    /// Get the count of authorized PINs (for security, don't expose the actual PINs)
    /// </summary>
    public int GetAuthorizedPinCount() => _authorizedPins.Count;

    /// <summary>
    /// Check if a PIN is authorized without logging sensitive information
    /// </summary>
    /// <param name="pin">The PIN to check</param>
    /// <returns>True if the PIN is authorized</returns>
    public bool IsAuthorizedPin(string pin) => _authorizedPins.Contains(pin);
}