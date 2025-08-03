using ApBox.Plugins;

namespace ApBox.Core.Models;

/// <summary>
/// Complete feedback configuration containing success, failure, and idle state settings
/// </summary>
public class FeedbackConfiguration
{
    /// <summary>
    /// Feedback configuration for successful card reads
    /// </summary>
    public ReaderFeedback SuccessFeedback { get; set; } = new();
    
    /// <summary>
    /// Feedback configuration for failed card reads
    /// </summary>
    public ReaderFeedback FailureFeedback { get; set; } = new();
    
    /// <summary>
    /// Idle state configuration when no activity is occurring
    /// </summary>
    public IdleStateFeedback IdleState { get; set; } = new();
}