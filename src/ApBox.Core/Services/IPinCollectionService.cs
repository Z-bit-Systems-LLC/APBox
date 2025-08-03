using ApBox.Plugins;

namespace ApBox.Core.Services;

public interface IPinCollectionService
{
    /// <summary>
    /// Adds a digit to the PIN collection for the specified reader
    /// </summary>
    /// <param name="readerId">The reader ID</param>
    /// <param name="digit">The digit pressed</param>
    /// <returns>True if the PIN is complete, false if collecting more digits</returns>
    Task<bool> AddDigitAsync(Guid readerId, char digit);
    
    /// <summary>
    /// Gets the current PIN collection status for a reader
    /// </summary>
    /// <param name="readerId">The reader ID</param>
    /// <returns>The current PIN or null if no collection in progress</returns>
    Task<string?> GetCurrentPinAsync(Guid readerId);
    
    /// <summary>
    /// Clears the PIN collection for a reader
    /// </summary>
    /// <param name="readerId">The reader ID</param>
    Task ClearPinAsync(Guid readerId);
    
    /// <summary>
    /// Event fired when a PIN collection is completed
    /// </summary>
    event EventHandler<PinReadEvent>? PinCollectionCompleted;
    
    /// <summary>
    /// Event fired when a digit is added to a PIN collection
    /// </summary>
    event EventHandler<PinDigitEvent>? PinDigitReceived;
}