namespace ApBox.Core.Services;

/// <summary>
/// Service for managing system restart operations
/// </summary>
public interface ISystemRestartService
{
    /// <summary>
    /// Check if the system can be restarted safely
    /// </summary>
    Task<bool> CanRestartAsync();
    
    /// <summary>
    /// Prepare the system for restart (graceful shutdown)
    /// </summary>
    Task PrepareRestartAsync();
    
    /// <summary>
    /// Restart the application
    /// </summary>
    Task RestartApplicationAsync();
    
    /// <summary>
    /// Get estimated restart time
    /// </summary>
    Task<TimeSpan> GetEstimatedRestartTimeAsync();
    
    /// <summary>
    /// Check if a restart is currently in progress
    /// </summary>
    bool IsRestartInProgress { get; }
}