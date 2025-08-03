using ApBox.Plugins;

namespace ApBox.Core.Services;

/// <summary>
/// Service for persisting PIN events to storage
/// </summary>
public interface IPinEventPersistenceService
{
    /// <summary>
    /// Persist a PIN event with its processing result
    /// </summary>
    /// <param name="pinRead">The PIN read event</param>
    /// <param name="result">The processing result</param>
    Task PersistPinEventAsync(PinReadEvent pinRead, PinReadResult result);
    
    /// <summary>
    /// Persist a PIN event error
    /// </summary>
    /// <param name="pinRead">The PIN read event</param>
    /// <param name="errorMessage">Error message</param>
    Task PersistPinEventErrorAsync(PinReadEvent pinRead, string errorMessage);
}