using ApBox.Core.Data.Repositories;
using ApBox.Core.Models;

namespace ApBox.Core.Services.Configuration;

/// <summary>
/// Service implementation for feedback configuration management
/// </summary>
public class FeedbackConfigurationService : IFeedbackConfigurationService
{
    private readonly IFeedbackConfigurationRepository _repository;
    private readonly ILogger<FeedbackConfigurationService> _logger;

    public FeedbackConfigurationService(
        IFeedbackConfigurationRepository repository,
        ILogger<FeedbackConfigurationService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<FeedbackConfiguration> GetDefaultConfigurationAsync()
    {
        try
        {
            var configuration = await _repository.GetConfigurationAsync();
            
            // Ensure we have valid configurations with defaults if nothing exists
            configuration.SuccessFeedback ??= GetDefaultSuccessFeedback();
            configuration.FailureFeedback ??= GetDefaultFailureFeedback();
            configuration.IdleState ??= GetDefaultIdleState();

            return configuration;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving default feedback configuration");
            
            // Return sensible defaults if database read fails
            return new FeedbackConfiguration
            {
                SuccessFeedback = GetDefaultSuccessFeedback(),
                FailureFeedback = GetDefaultFailureFeedback(),
                IdleState = GetDefaultIdleState()
            };
        }
    }

    public async Task SaveDefaultConfigurationAsync(FeedbackConfiguration configuration)
    {
        try
        {
            // Validate configuration
            ValidateConfiguration(configuration);
            
            await _repository.SaveConfigurationAsync(configuration);
            
            _logger.LogInformation("Successfully saved default feedback configuration");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving default feedback configuration");
            throw;
        }
    }

    public async Task<ReaderFeedback> GetSuccessFeedbackAsync()
    {
        try
        {
            var feedback = await _repository.GetFeedbackByTypeAsync(ReaderFeedbackType.Success);
            return feedback ?? GetDefaultSuccessFeedback();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving success feedback configuration");
            return GetDefaultSuccessFeedback();
        }
    }

    public async Task<ReaderFeedback> GetFailureFeedbackAsync()
    {
        try
        {
            var feedback = await _repository.GetFeedbackByTypeAsync(ReaderFeedbackType.Failure);
            return feedback ?? GetDefaultFailureFeedback();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving failure feedback configuration");
            return GetDefaultFailureFeedback();
        }
    }

    public async Task<IdleStateFeedback> GetIdleStateAsync()
    {
        try
        {
            var idleState = await _repository.GetIdleStateAsync();
            return idleState ?? GetDefaultIdleState();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving idle state configuration");
            return GetDefaultIdleState();
        }
    }

    public async Task SaveSuccessFeedbackAsync(ReaderFeedback feedback)
    {
        try
        {
            feedback.Type = ReaderFeedbackType.Success;
            ValidateReaderFeedback(feedback);
            
            await _repository.SaveFeedbackByTypeAsync(feedback);
            
            _logger.LogInformation("Successfully saved success feedback configuration");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving success feedback configuration");
            throw;
        }
    }

    public async Task SaveFailureFeedbackAsync(ReaderFeedback feedback)
    {
        try
        {
            feedback.Type = ReaderFeedbackType.Failure;
            ValidateReaderFeedback(feedback);
            
            await _repository.SaveFeedbackByTypeAsync(feedback);
            
            _logger.LogInformation("Successfully saved failure feedback configuration");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving failure feedback configuration");
            throw;
        }
    }

    public async Task SaveIdleStateAsync(IdleStateFeedback idleState)
    {
        try
        {
            await _repository.SaveIdleStateAsync(idleState);
            
            _logger.LogInformation("Successfully saved idle state configuration");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving idle state configuration");
            throw;
        }
    }

    private static ReaderFeedback GetDefaultSuccessFeedback()
    {
        return new ReaderFeedback
        {
            Type = ReaderFeedbackType.Success,
            LedColor = LedColor.Green,
            LedDuration = 1, // 1 second
            BeepCount = 1,
            DisplayMessage = "ACCESS GRANTED"
        };
    }

    private static ReaderFeedback GetDefaultFailureFeedback()
    {
        return new ReaderFeedback
        {
            Type = ReaderFeedbackType.Failure,
            LedColor = LedColor.Red,
            LedDuration = 2, // 2 seconds
            BeepCount = 3,
            DisplayMessage = "ACCESS DENIED"
        };
    }

    private static IdleStateFeedback GetDefaultIdleState()
    {
        return new IdleStateFeedback
        {
            PermanentLedColor = LedColor.Blue,
            HeartbeatFlashColor = LedColor.Green
        };
    }

    private static void ValidateConfiguration(FeedbackConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(configuration.SuccessFeedback);
        ArgumentNullException.ThrowIfNull(configuration.FailureFeedback);
        ArgumentNullException.ThrowIfNull(configuration.IdleState);

        ValidateReaderFeedback(configuration.SuccessFeedback);
        ValidateReaderFeedback(configuration.FailureFeedback);
    }

    private static void ValidateReaderFeedback(ReaderFeedback feedback)
    {
        ArgumentNullException.ThrowIfNull(feedback);

        if (feedback.LedDuration < 0)
            throw new ArgumentException("LED duration cannot be negative");

        if (feedback.BeepCount < 0)
            throw new ArgumentException("Beep count cannot be negative");

        if (!string.IsNullOrEmpty(feedback.DisplayMessage) && feedback.DisplayMessage.Length > 16)
            throw new ArgumentException("Display message cannot exceed 16 characters");
    }
}