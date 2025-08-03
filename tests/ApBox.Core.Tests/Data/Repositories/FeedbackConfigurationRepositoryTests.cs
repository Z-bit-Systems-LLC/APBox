using System.Data;
using ApBox.Core.Data;
using ApBox.Core.Data.Migrations;
using ApBox.Core.Data.Repositories;
using ApBox.Core.Models;
using ApBox.Core.Services.Infrastructure;
using ApBox.Plugins;
using Microsoft.Extensions.Logging;
using Moq;

namespace ApBox.Core.Tests.Data.Repositories;

[TestFixture]
[Category("Unit")]
public class FeedbackConfigurationRepositoryTests
{
    private string _testConnectionString;
    private ILogger<ApBoxDbContext> _dbLogger;
    private ILogger<MigrationRunner> _migrationLogger;
    private ILogger<FeedbackConfigurationRepository> _repositoryLogger;
    private IFileSystem _fileSystem;
    private ApBoxDbContext _dbContext;
    private FeedbackConfigurationRepository _repository;
    private IDbConnection _persistConnection;

    [SetUp]
    public async Task Setup()
    {
        // Use an in-memory SQLite database
        _testConnectionString = $"Data Source=file:memdb{Guid.NewGuid():N}?mode=memory&cache=shared";
        
        // Create mock loggers
        _dbLogger = new Mock<ILogger<ApBoxDbContext>>().Object;
        _migrationLogger = new Mock<ILogger<MigrationRunner>>().Object;
        _repositoryLogger = new Mock<ILogger<FeedbackConfigurationRepository>>().Object;
        _fileSystem = new FileSystem();
        
        _dbContext = new ApBoxDbContext(_testConnectionString, _dbLogger);
        
        // Create a shared connection
        _persistConnection = _dbContext.CreateDbConnectionAsync();
        _persistConnection.Open();
        
        // Run migrations to set up the database schema
        var migrationRunner = new MigrationRunner(_dbContext, _fileSystem, _migrationLogger);
        await migrationRunner.RunMigrationsAsync();
        
        _repository = new FeedbackConfigurationRepository(_dbContext, _repositoryLogger);
    }

    [TearDown]
    public void TearDown()
    {
        _persistConnection?.Close();
        _persistConnection?.Dispose();
    }

    #region GetConfigurationAsync Tests

    [Test]
    public async Task GetConfigurationAsync_WhenDatabaseEmpty_ReturnsDefaultConfiguration()
    {
        // Act
        var result = await _repository.GetConfigurationAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.SuccessFeedback, Is.Not.Null);
        Assert.That(result.FailureFeedback, Is.Not.Null);
        Assert.That(result.IdleState, Is.Not.Null);
    }

    [Test]
    public async Task GetConfigurationAsync_WhenDataExists_ReturnsCorrectConfiguration()
    {
        // Arrange - Database should have been seeded by migration
        // Debug: Check what migrations were applied
        var migrationRunner = new MigrationRunner(_dbContext, _fileSystem, _migrationLogger);
        var appliedMigrations = await migrationRunner.GetAppliedMigrationsAsync();
        Console.WriteLine($"Applied migrations: {string.Join(", ", appliedMigrations)}");
        
        // Act
        var result = await _repository.GetConfigurationAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        
        // Debug output
        Console.WriteLine($"SuccessFeedback.Type: {result.SuccessFeedback?.Type}");
        Console.WriteLine($"SuccessFeedback.LedColor: {result.SuccessFeedback?.LedColor}");
        
        // Check success feedback (from migration seed data)
        Assert.That(result.SuccessFeedback, Is.Not.Null, "SuccessFeedback should not be null");
        Assert.That(result.SuccessFeedback.Type, Is.EqualTo(ReaderFeedbackType.Success));
        Assert.That(result.SuccessFeedback.LedColor, Is.EqualTo(LedColor.Green));
        Assert.That(result.SuccessFeedback.LedDuration, Is.EqualTo(1)); // 1 second * 1000
        Assert.That(result.SuccessFeedback.BeepCount, Is.EqualTo(1));
        Assert.That(result.SuccessFeedback.DisplayMessage, Is.EqualTo("ACCESS GRANTED"));
        
        // Check failure feedback (from migration seed data)
        Assert.That(result.FailureFeedback.Type, Is.EqualTo(ReaderFeedbackType.Failure));
        Assert.That(result.FailureFeedback.LedColor, Is.EqualTo(LedColor.Red));
        Assert.That(result.FailureFeedback.LedDuration, Is.EqualTo(2)); // 2 seconds * 1000
        Assert.That(result.FailureFeedback.BeepCount, Is.EqualTo(3));
        Assert.That(result.FailureFeedback.DisplayMessage, Is.EqualTo("ACCESS DENIED"));
        
        // Check idle state (from migration seed data)
        Assert.That(result.IdleState.PermanentLedColor, Is.EqualTo(LedColor.Blue));
        Assert.That(result.IdleState.HeartbeatFlashColor, Is.EqualTo(LedColor.Green));
    }

    #endregion

    #region SaveConfigurationAsync Tests

    [Test]
    public async Task SaveConfigurationAsync_SavesAllConfigurations()
    {
        // Arrange
        var configuration = new FeedbackConfiguration
        {
            SuccessFeedback = new ReaderFeedback
            {
                Type = ReaderFeedbackType.Success,
                LedColor = LedColor.Amber,
                LedDuration = 5000,
                BeepCount = 2,
                DisplayMessage = "WELCOME"
            },
            FailureFeedback = new ReaderFeedback
            {
                Type = ReaderFeedbackType.Failure,
                LedColor = LedColor.Blue,
                LedDuration = 3000,
                BeepCount = 5,
                DisplayMessage = "NO ACCESS"
            },
            IdleState = new IdleStateFeedback
            {
                PermanentLedColor = LedColor.Amber,
                HeartbeatFlashColor = LedColor.Red
            }
        };

        // Act
        await _repository.SaveConfigurationAsync(configuration);

        // Assert - Verify by retrieving the configuration
        var saved = await _repository.GetConfigurationAsync();
        
        Assert.That(saved.SuccessFeedback.LedColor, Is.EqualTo(LedColor.Amber));
        Assert.That(saved.SuccessFeedback.LedDuration, Is.EqualTo(5000));
        Assert.That(saved.SuccessFeedback.BeepCount, Is.EqualTo(2));
        Assert.That(saved.SuccessFeedback.DisplayMessage, Is.EqualTo("WELCOME"));
        
        Assert.That(saved.FailureFeedback.LedColor, Is.EqualTo(LedColor.Blue));
        Assert.That(saved.FailureFeedback.LedDuration, Is.EqualTo(3000));
        Assert.That(saved.FailureFeedback.BeepCount, Is.EqualTo(5));
        Assert.That(saved.FailureFeedback.DisplayMessage, Is.EqualTo("NO ACCESS"));
        
        Assert.That(saved.IdleState.PermanentLedColor, Is.EqualTo(LedColor.Amber));
        Assert.That(saved.IdleState.HeartbeatFlashColor, Is.EqualTo(LedColor.Red));
    }

    [Test]
    public async Task SaveConfigurationAsync_UpdatesExistingConfiguration()
    {
        // Arrange - First save a configuration
        var initialConfig = new FeedbackConfiguration
        {
            SuccessFeedback = new ReaderFeedback
            {
                Type = ReaderFeedbackType.Success,
                LedColor = LedColor.Green,
                LedDuration = 1000,
                BeepCount = 1,
                DisplayMessage = "OK"
            },
            FailureFeedback = new ReaderFeedback
            {
                Type = ReaderFeedbackType.Failure,
                LedColor = LedColor.Red,
                LedDuration = 2000,
                BeepCount = 3,
                DisplayMessage = "FAIL"
            },
            IdleState = new IdleStateFeedback
            {
                PermanentLedColor = LedColor.Blue,
                HeartbeatFlashColor = LedColor.Green
            }
        };
        await _repository.SaveConfigurationAsync(initialConfig);
        
        // Arrange - Update the configuration
        var updatedConfig = new FeedbackConfiguration
        {
            SuccessFeedback = new ReaderFeedback
            {
                Type = ReaderFeedbackType.Success,
                LedColor = LedColor.Amber,
                LedDuration = 2500,
                BeepCount = 4,
                DisplayMessage = "UPDATED"
            },
            FailureFeedback = new ReaderFeedback
            {
                Type = ReaderFeedbackType.Failure,
                LedColor = LedColor.Blue,
                LedDuration = 1500,
                BeepCount = 2,
                DisplayMessage = "CHANGED"
            },
            IdleState = new IdleStateFeedback
            {
                PermanentLedColor = LedColor.Red,
                HeartbeatFlashColor = LedColor.Amber
            }
        };

        // Act
        await _repository.SaveConfigurationAsync(updatedConfig);

        // Assert
        var saved = await _repository.GetConfigurationAsync();
        
        Assert.That(saved.SuccessFeedback.LedColor, Is.EqualTo(LedColor.Amber));
        Assert.That(saved.SuccessFeedback.DisplayMessage, Is.EqualTo("UPDATED"));
        Assert.That(saved.FailureFeedback.LedColor, Is.EqualTo(LedColor.Blue));
        Assert.That(saved.FailureFeedback.DisplayMessage, Is.EqualTo("CHANGED"));
        Assert.That(saved.IdleState.PermanentLedColor, Is.EqualTo(LedColor.Red));
        Assert.That(saved.IdleState.HeartbeatFlashColor, Is.EqualTo(LedColor.Amber));
    }

    #endregion

    #region GetFeedbackByTypeAsync Tests

    [Test]
    public async Task GetFeedbackByTypeAsync_Success_ReturnsCorrectFeedback()
    {
        // Act
        var result = await _repository.GetFeedbackByTypeAsync(ReaderFeedbackType.Success);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Type, Is.EqualTo(ReaderFeedbackType.Success));
        Assert.That(result.LedColor, Is.EqualTo(LedColor.Green));
        Assert.That(result.DisplayMessage, Is.EqualTo("ACCESS GRANTED"));
    }

    [Test]
    public async Task GetFeedbackByTypeAsync_Failure_ReturnsCorrectFeedback()
    {
        // Act
        var result = await _repository.GetFeedbackByTypeAsync(ReaderFeedbackType.Failure);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Type, Is.EqualTo(ReaderFeedbackType.Failure));
        Assert.That(result.LedColor, Is.EqualTo(LedColor.Red));
        Assert.That(result.DisplayMessage, Is.EqualTo("ACCESS DENIED"));
    }

    [Test]
    public void GetFeedbackByTypeAsync_InvalidType_ThrowsException()
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(async () => 
            await _repository.GetFeedbackByTypeAsync(ReaderFeedbackType.None));
    }

    #endregion

    #region GetIdleStateAsync Tests

    [Test]
    public async Task GetIdleStateAsync_ReturnsCorrectIdleState()
    {
        // Act
        var result = await _repository.GetIdleStateAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.PermanentLedColor, Is.EqualTo(LedColor.Blue));
        Assert.That(result.HeartbeatFlashColor, Is.EqualTo(LedColor.Green));
    }

    #endregion

    #region SaveFeedbackByTypeAsync Tests

    [Test]
    public async Task SaveFeedbackByTypeAsync_SavesSuccessFeedback()
    {
        // Arrange
        var feedback = new ReaderFeedback
        {
            Type = ReaderFeedbackType.Success,
            LedColor = LedColor.Amber,
            LedDuration = 5000,
            BeepCount = 2,
            DisplayMessage = "CUSTOM SUCCESS"
        };

        // Act
        await _repository.SaveFeedbackByTypeAsync(feedback);

        // Assert
        var saved = await _repository.GetFeedbackByTypeAsync(ReaderFeedbackType.Success);
        Assert.That(saved, Is.Not.Null);
        Assert.That(saved.LedColor, Is.EqualTo(LedColor.Amber));
        Assert.That(saved.LedDuration, Is.EqualTo(5000));
        Assert.That(saved.BeepCount, Is.EqualTo(2));
        Assert.That(saved.DisplayMessage, Is.EqualTo("CUSTOM SUCCESS"));
    }

    [Test]
    public async Task SaveFeedbackByTypeAsync_SavesFailureFeedback()
    {
        // Arrange
        var feedback = new ReaderFeedback
        {
            Type = ReaderFeedbackType.Failure,
            LedColor = LedColor.Blue,
            LedDuration = 3000,
            BeepCount = 4,
            DisplayMessage = "CUSTOM FAILURE"
        };

        // Act
        await _repository.SaveFeedbackByTypeAsync(feedback);

        // Assert
        var saved = await _repository.GetFeedbackByTypeAsync(ReaderFeedbackType.Failure);
        Assert.That(saved, Is.Not.Null);
        Assert.That(saved.LedColor, Is.EqualTo(LedColor.Blue));
        Assert.That(saved.LedDuration, Is.EqualTo(3000));
        Assert.That(saved.BeepCount, Is.EqualTo(4));
        Assert.That(saved.DisplayMessage, Is.EqualTo("CUSTOM FAILURE"));
    }

    #endregion

    #region SaveIdleStateAsync Tests

    [Test]
    public async Task SaveIdleStateAsync_SavesIdleState()
    {
        // Arrange
        var idleState = new IdleStateFeedback
        {
            PermanentLedColor = LedColor.Amber,
            HeartbeatFlashColor = LedColor.Red
        };

        // Act
        await _repository.SaveIdleStateAsync(idleState);

        // Assert
        var saved = await _repository.GetIdleStateAsync();
        Assert.That(saved, Is.Not.Null);
        Assert.That(saved.PermanentLedColor, Is.EqualTo(LedColor.Amber));
        Assert.That(saved.HeartbeatFlashColor, Is.EqualTo(LedColor.Red));
    }

    [Test]
    public async Task SaveIdleStateAsync_AllowsNullColors()
    {
        // Arrange
        var idleState = new IdleStateFeedback
        {
            PermanentLedColor = null,
            HeartbeatFlashColor = null
        };

        // Act
        await _repository.SaveIdleStateAsync(idleState);

        // Assert
        var saved = await _repository.GetIdleStateAsync();
        Assert.That(saved, Is.Not.Null);
        Assert.That(saved.PermanentLedColor, Is.Null);
        Assert.That(saved.HeartbeatFlashColor, Is.Null);
    }

    #endregion
}