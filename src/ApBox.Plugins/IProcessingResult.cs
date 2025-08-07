namespace ApBox.Plugins;

/// <summary>
/// Interface for processing result objects to ensure they have Success and Message properties
/// </summary>
public interface IProcessingResult
{
    /// <summary>
    /// Whether the processing was successful
    /// </summary>
    bool Success { get; set; }
    
    /// <summary>
    /// Processing message
    /// </summary>
    string Message { get; set; }
}