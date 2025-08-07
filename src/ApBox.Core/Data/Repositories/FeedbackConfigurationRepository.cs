using ApBox.Core.Data.Models;
using ApBox.Core.Models;
using Dapper;

namespace ApBox.Core.Data.Repositories;

/// <summary>
/// Repository implementation for feedback configuration data access
/// </summary>
public class FeedbackConfigurationRepository(IApBoxDbContext dbContext, ILogger<FeedbackConfigurationRepository> logger)
    : IFeedbackConfigurationRepository
{
    public async Task<FeedbackConfiguration> GetConfigurationAsync()
    {
        using var connection = dbContext.CreateDbConnectionAsync();
        connection.Open();

        var sql = @"
            SELECT * FROM feedback_configurations 
            ORDER BY configuration_type";
        
        var entities = await connection.QueryAsync<FeedbackConfigurationEntity>(sql);
        
        var config = new FeedbackConfiguration();
        
        foreach (var entity in entities)
        {
            switch (entity.ConfigurationType)
            {
                case "success":
                    config.SuccessFeedback = entity.ToReaderFeedback();
                    break;
                case "failure":
                    config.FailureFeedback = entity.ToReaderFeedback();
                    break;
                case "idle":
                    config.IdleState = entity.ToIdleStateFeedback();
                    break;
            }
        }

        return config;
    }

    public async Task SaveConfigurationAsync(FeedbackConfiguration configuration)
    {
        using var connection = dbContext.CreateDbConnectionAsync();
        connection.Open();

        using var transaction = connection.BeginTransaction();
        
        try
        {
            // Save success feedback
            await SaveFeedbackByTypeAsync(configuration.SuccessFeedback, connection, transaction);
            
            // Save failure feedback
            await SaveFeedbackByTypeAsync(configuration.FailureFeedback, connection, transaction);
            
            // Save idle state
            await SaveIdleStateAsync(configuration.IdleState, connection, transaction);
            
            transaction.Commit();
            
            logger.LogInformation("Saved complete feedback configuration");
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            logger.LogError(ex, "Error saving feedback configuration - transaction rolled back");
            throw;
        }
    }

    public async Task<ReaderFeedback?> GetFeedbackByTypeAsync(ReaderFeedbackType type)
    {
        using var connection = dbContext.CreateDbConnectionAsync();
        connection.Open();

        var configType = type switch
        {
            ReaderFeedbackType.Success => "success",
            ReaderFeedbackType.Failure => "failure",
            _ => throw new ArgumentException($"Unsupported feedback type: {type}")
        };

        var sql = @"
            SELECT * FROM feedback_configurations 
            WHERE configuration_type = @ConfigurationType";
        
        var entity = await connection.QueryFirstOrDefaultAsync<FeedbackConfigurationEntity>(
            sql, new { ConfigurationType = configType });
        
        return entity?.ToReaderFeedback();
    }

    public async Task<IdleStateFeedback?> GetIdleStateAsync()
    {
        using var connection = dbContext.CreateDbConnectionAsync();
        connection.Open();

        var sql = @"
            SELECT * FROM feedback_configurations 
            WHERE configuration_type = @ConfigurationType";
        
        var entity = await connection.QueryFirstOrDefaultAsync<FeedbackConfigurationEntity>(
            sql, new { ConfigurationType = "idle" });
        
        return entity?.ToIdleStateFeedback();
    }

    public async Task SaveFeedbackByTypeAsync(ReaderFeedback feedback)
    {
        using var connection = dbContext.CreateDbConnectionAsync();
        connection.Open();

        using var transaction = connection.BeginTransaction();
        
        try
        {
            await SaveFeedbackByTypeAsync(feedback, connection, transaction);
            transaction.Commit();
            
            logger.LogInformation("Saved {FeedbackType} feedback configuration", feedback.Type);
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            logger.LogError(ex, "Error saving {FeedbackType} feedback configuration", feedback.Type);
            throw;
        }
    }

    public async Task SaveIdleStateAsync(IdleStateFeedback idleState)
    {
        using var connection = dbContext.CreateDbConnectionAsync();
        connection.Open();

        using var transaction = connection.BeginTransaction();
        
        try
        {
            await SaveIdleStateAsync(idleState, connection, transaction);
            transaction.Commit();
            
            logger.LogInformation("Saved idle state configuration");
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            logger.LogError(ex, "Error saving idle state configuration");
            throw;
        }
    }

    private async Task SaveFeedbackByTypeAsync(ReaderFeedback feedback, System.Data.IDbConnection connection, System.Data.IDbTransaction transaction)
    {
        var entity = FeedbackConfigurationEntity.FromReaderFeedback(feedback);
        entity.UpdatedAt = DateTime.UtcNow;

        try
        {
            // Use a simpler approach: delete and insert
            var deleteSql = "DELETE FROM feedback_configurations WHERE configuration_type = @ConfigurationType";
            await connection.ExecuteAsync(deleteSql, new { entity.ConfigurationType }, transaction);

            var insertSql = @"
                INSERT INTO feedback_configurations 
                (configuration_type, led_color, led_duration_seconds, beep_count, display_message, created_at, updated_at)
                VALUES (@ConfigurationType, @LedColor, @LedDurationSeconds, @BeepCount, @DisplayMessage, @CreatedAt, @UpdatedAt)";

            await connection.ExecuteAsync(insertSql, entity, transaction);
            
            logger.LogInformation("Saved {FeedbackType} feedback: LedColor={LedColor}, Duration={Duration}s", 
                entity.ConfigurationType, entity.LedColor, entity.LedDurationSeconds);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save {FeedbackType} feedback", entity.ConfigurationType);
            throw;
        }
    }

    private async Task SaveIdleStateAsync(IdleStateFeedback idleState, System.Data.IDbConnection connection, System.Data.IDbTransaction transaction)
    {
        var entity = FeedbackConfigurationEntity.FromIdleStateFeedback(idleState);
        entity.UpdatedAt = DateTime.UtcNow;

        try
        {
            // Use a simpler approach: delete and insert
            var deleteSql = "DELETE FROM feedback_configurations WHERE configuration_type = @ConfigurationType";
            await connection.ExecuteAsync(deleteSql, new { entity.ConfigurationType }, transaction);

            var insertSql = @"
                INSERT INTO feedback_configurations 
                (configuration_type, permanent_led_color, heartbeat_flash_color, created_at, updated_at)
                VALUES (@ConfigurationType, @PermanentLedColor, @HeartbeatFlashColor, @CreatedAt, @UpdatedAt)";

            await connection.ExecuteAsync(insertSql, entity, transaction);
            
            logger.LogInformation("Saved idle state: PermanentLed={PermanentLed}, HeartbeatLed={HeartbeatLed}", 
                entity.PermanentLedColor, entity.HeartbeatFlashColor);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save idle state feedback");
            throw;
        }
    }
}