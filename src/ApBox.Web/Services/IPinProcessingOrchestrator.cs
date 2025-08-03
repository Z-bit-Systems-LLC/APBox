using ApBox.Core.Services;
using ApBox.Plugins;

namespace ApBox.Web.Services;

/// <summary>
/// Orchestrates the complete PIN processing workflow
/// </summary>
public interface IPinProcessingOrchestrator
{
    /// <summary>
    /// Orchestrate the complete PIN processing workflow including plugin processing,
    /// feedback, persistence, and notifications
    /// </summary>
    /// <param name="pinRead">PIN read event</param>
    /// <returns>Processing result</returns>
    Task<PinReadResult> OrchestratePinProcessingAsync(PinReadEvent pinRead);
}