using ApBox.Core.Models;
using ApBox.Plugins;

namespace ApBox.Core.Services.Configuration;

/// <summary>
/// Service interface for feedback configuration management
/// </summary>
public interface IFeedbackConfigurationService
{
    /// <summary>
    /// Gets the complete default feedback configuration
    /// </summary>
    Task<FeedbackConfiguration> GetDefaultConfigurationAsync();

    /// <summary>
    /// Saves the complete default feedback configuration
    /// </summary>
    Task SaveDefaultConfigurationAsync(FeedbackConfiguration configuration);

    /// <summary>
    /// Gets the success feedback configuration
    /// </summary>
    Task<ReaderFeedback> GetSuccessFeedbackAsync();

    /// <summary>
    /// Gets the failure feedback configuration
    /// </summary>
    Task<ReaderFeedback> GetFailureFeedbackAsync();

    /// <summary>
    /// Gets the idle state configuration
    /// </summary>
    Task<IdleStateFeedback> GetIdleStateAsync();

    /// <summary>
    /// Saves success feedback configuration
    /// </summary>
    Task SaveSuccessFeedbackAsync(ReaderFeedback feedback);

    /// <summary>
    /// Saves failure feedback configuration
    /// </summary>
    Task SaveFailureFeedbackAsync(ReaderFeedback feedback);

    /// <summary>
    /// Saves idle state configuration
    /// </summary>
    Task SaveIdleStateAsync(IdleStateFeedback idleState);
}