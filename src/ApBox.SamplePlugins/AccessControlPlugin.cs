using ApBox.Plugins;
using Microsoft.Extensions.Logging;

namespace ApBox.SamplePlugins;

/// <summary>
/// Sample access control plugin that demonstrates basic allow/deny functionality
/// based on a predefined list of authorized card numbers
/// </summary>
public class AccessControlPlugin : IApBoxPlugin
{
    private readonly HashSet<string> _authorizedCards;
    private readonly ILogger<AccessControlPlugin>? _logger;

    public AccessControlPlugin()
    {
        // Initialize with some sample authorized card numbers
        _authorizedCards = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "12345678",    // Sample authorized cards
            "87654321",
            "11111111",
            "22222222",
            "12345123",
            "98765987"
        };
    }

    public AccessControlPlugin(ILogger<AccessControlPlugin> logger) : this()
    {
        _logger = logger;
    }

    public Guid Id => new Guid("A1B2C3D4-5678-9ABC-DEF0-123456789001");
    public string Name => "Access Control Plugin";
    public string Version => "1.0.0";
    public string Description => "Provides basic access control functionality by maintaining a list of authorized card numbers";

    public async Task<bool> ProcessCardReadAsync(CardReadEvent cardRead)
    {
        await Task.CompletedTask; // Async signature for future extensibility
        
        _logger?.LogInformation("Access Control Plugin processing card {CardNumber} from reader {ReaderName}", 
            cardRead.CardNumber, cardRead.ReaderName);

        // Check if the card is in our authorized list
        bool isAuthorized = _authorizedCards.Contains(cardRead.CardNumber);

        if (isAuthorized)
        {
            _logger?.LogInformation("Card {CardNumber} is authorized - granting access", cardRead.CardNumber);
        }
        else
        {
            _logger?.LogWarning("Card {CardNumber} is not authorized - denying access", cardRead.CardNumber);
        }

        return isAuthorized;
    }

    public Task InitializeAsync()
    {
        _logger?.LogInformation("Access Control Plugin initialized with {Count} authorized cards", 
            _authorizedCards.Count);
        return Task.CompletedTask;
    }

    public Task ShutdownAsync()
    {
        _logger?.LogInformation("Access Control Plugin shutting down");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Add a card to the authorized list (for demonstration purposes)
    /// </summary>
    public void AddAuthorizedCard(string cardNumber)
    {
        if (_authorizedCards.Add(cardNumber))
        {
            _logger?.LogInformation("Added card {CardNumber} to authorized list", cardNumber);
        }
    }

    /// <summary>
    /// Remove a card from the authorized list (for demonstration purposes)
    /// </summary>
    public void RemoveAuthorizedCard(string cardNumber)
    {
        if (_authorizedCards.Remove(cardNumber))
        {
            _logger?.LogInformation("Removed card {CardNumber} from authorized list", cardNumber);
        }
    }

    /// <summary>
    /// Get the current list of authorized cards
    /// </summary>
    public IReadOnlySet<string> GetAuthorizedCards() => _authorizedCards;
}