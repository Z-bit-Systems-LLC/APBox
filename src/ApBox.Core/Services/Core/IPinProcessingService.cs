using ApBox.Core.Models;
using ApBox.Plugins;

namespace ApBox.Core.Services.Core;

/// <summary>
/// Service for processing PIN reads through the plugin system
/// </summary>
public interface IPinProcessingService
{
    /// <summary>
    /// Process a PIN read through configured plugins
    /// </summary>
    /// <param name="pinRead">The PIN read event</param>
    /// <returns>Processing result</returns>
    Task<PinReadResult> ProcessPinReadAsync(PinReadEvent pinRead);
    
}

/// <summary>
/// Result of PIN processing
/// </summary>
public class PinReadResult : IProcessingResult
{
    /// <summary>
    /// Whether PIN processing was successful
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Processing message
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Additional data from processing
    /// </summary>
    public Dictionary<string, object> AdditionalData { get; set; } = new();
    
    /// <summary>
    /// Detailed plugin results
    /// </summary>
    public Dictionary<string, PluginResult> PluginResults { get; set; } = new();
}