using ApBox.Plugins;

namespace ApBox.Core.Data.Models;

/// <summary>
/// Database entity for storing feedback configuration
/// </summary>
public class FeedbackConfigurationEntity
{
    public int Id { get; set; }
    public string ConfigurationType { get; set; } = string.Empty; // "success", "failure", "idle"
    public string? LedColor { get; set; }
    public int? LedDurationSeconds { get; set; }
    public int? BeepCount { get; set; }
    public string? DisplayMessage { get; set; }
    public string? PermanentLedColor { get; set; }
    public string? HeartbeatFlashColor { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Converts entity to ReaderFeedback domain model
    /// </summary>
    public ReaderFeedback ToReaderFeedback()
    {
        return new ReaderFeedback
        {
            Type = ConfigurationType switch
            {
                "success" => ReaderFeedbackType.Success,
                "failure" => ReaderFeedbackType.Failure,
                _ => ReaderFeedbackType.None
            },
            LedColor = ParseLedColor(LedColor),
            LedDurationMs = (LedDurationSeconds ?? 0) * 1000, // Convert seconds to milliseconds, default to 0 if null
            BeepCount = BeepCount ?? 0,
            DisplayMessage = DisplayMessage
        };
    }

    /// <summary>
    /// Converts entity to IdleStateFeedback domain model
    /// </summary>
    public IdleStateFeedback ToIdleStateFeedback()
    {
        return new IdleStateFeedback
        {
            PermanentLedColor = ParseLedColor(PermanentLedColor),
            HeartbeatFlashColor = ParseLedColor(HeartbeatFlashColor)
        };
    }

    /// <summary>
    /// Creates entity from ReaderFeedback domain model
    /// </summary>
    public static FeedbackConfigurationEntity FromReaderFeedback(ReaderFeedback feedback)
    {
        var configurationType = feedback.Type switch
        {
            ReaderFeedbackType.Success => "success",
            ReaderFeedbackType.Failure => "failure",
            _ => throw new ArgumentException($"Unsupported feedback type: {feedback.Type}")
        };

        return new FeedbackConfigurationEntity
        {
            ConfigurationType = configurationType,
            LedColor = feedback.LedColor?.ToString(),
            LedDurationSeconds = feedback.LedDurationMs / 1000, // Convert milliseconds to seconds
            BeepCount = feedback.BeepCount,
            DisplayMessage = feedback.DisplayMessage,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates entity from IdleStateFeedback domain model
    /// </summary>
    public static FeedbackConfigurationEntity FromIdleStateFeedback(IdleStateFeedback idleState)
    {
        return new FeedbackConfigurationEntity
        {
            ConfigurationType = "idle",
            PermanentLedColor = idleState.PermanentLedColor?.ToString(),
            HeartbeatFlashColor = idleState.HeartbeatFlashColor?.ToString(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static LedColor? ParseLedColor(string? colorString)
    {
        if (string.IsNullOrEmpty(colorString))
            return null;

        return Enum.TryParse<LedColor>(colorString, true, out var color) ? color : null;
    }
}