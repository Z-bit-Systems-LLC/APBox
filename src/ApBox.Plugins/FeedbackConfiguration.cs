namespace ApBox.Plugins;

/// <summary>
/// Complete feedback configuration containing success, failure, and idle state settings
/// </summary>
public class FeedbackConfiguration
{
    public ReaderFeedback SuccessFeedback { get; set; } = new();
    public ReaderFeedback FailureFeedback { get; set; } = new();
    public IdleStateFeedback IdleState { get; set; } = new();
}