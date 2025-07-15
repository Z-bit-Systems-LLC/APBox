using ApBox.Core.Models;
using ApBox.Plugins;

namespace ApBox.Core.Data.Repositories;

/// <summary>
/// Repository interface for feedback configuration data access
/// </summary>
public interface IFeedbackConfigurationRepository
{
    /// <summary>
    /// Gets the complete feedback configuration (success, failure, and idle state)
    /// </summary>
    Task<FeedbackConfiguration> GetConfigurationAsync();

    /// <summary>
    /// Saves the complete feedback configuration
    /// </summary>
    Task SaveConfigurationAsync(FeedbackConfiguration configuration);

    /// <summary>
    /// Gets feedback configuration for a specific type
    /// </summary>
    Task<ReaderFeedback?> GetFeedbackByTypeAsync(ReaderFeedbackType type);

    /// <summary>
    /// Gets the idle state configuration
    /// </summary>
    Task<IdleStateFeedback?> GetIdleStateAsync();

    /// <summary>
    /// Saves feedback configuration for a specific type
    /// </summary>
    Task SaveFeedbackByTypeAsync(ReaderFeedback feedback);

    /// <summary>
    /// Saves the idle state configuration
    /// </summary>
    Task SaveIdleStateAsync(IdleStateFeedback idleState);
}