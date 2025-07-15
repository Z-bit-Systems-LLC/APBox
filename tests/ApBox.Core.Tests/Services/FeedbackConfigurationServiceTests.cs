using ApBox.Core.Data.Repositories;
using ApBox.Core.Models;
using ApBox.Core.Services;
using ApBox.Plugins;
using Microsoft.Extensions.Logging;
using Moq;

namespace ApBox.Core.Tests.Services;

[TestFixture]
[Category("Unit")]
public class FeedbackConfigurationServiceTests
{
    private Mock<IFeedbackConfigurationRepository> _mockRepository;
    private Mock<ILogger<FeedbackConfigurationService>> _mockLogger;
    private FeedbackConfigurationService _service;

    [SetUp]
    public void Setup()
    {
        _mockRepository = new Mock<IFeedbackConfigurationRepository>();
        _mockLogger = new Mock<ILogger<FeedbackConfigurationService>>();
        _service = new FeedbackConfigurationService(_mockRepository.Object, _mockLogger.Object);
    }

    #region GetDefaultConfigurationAsync Tests

    [Test]
    public async Task GetDefaultConfigurationAsync_WhenRepositoryReturnsData_ReturnsConfiguration()
    {
        // Arrange
        var expectedConfig = new FeedbackConfiguration
        {
            SuccessFeedback = new ReaderFeedback
            {
                Type = ReaderFeedbackType.Success,
                LedColor = LedColor.Green,
                LedDurationMs = 1000,
                BeepCount = 1,
                DisplayMessage = "ACCESS GRANTED"
            },
            FailureFeedback = new ReaderFeedback
            {
                Type = ReaderFeedbackType.Failure,
                LedColor = LedColor.Red,
                LedDurationMs = 2000,
                BeepCount = 3,
                DisplayMessage = "ACCESS DENIED"
            },
            IdleState = new IdleStateFeedback
            {
                PermanentLedColor = LedColor.Blue,
                HeartbeatFlashColor = LedColor.Green
            }
        };

        _mockRepository.Setup(r => r.GetConfigurationAsync())
            .ReturnsAsync(expectedConfig);

        // Act
        var result = await _service.GetDefaultConfigurationAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.SuccessFeedback.LedColor, Is.EqualTo(LedColor.Green));
        Assert.That(result.FailureFeedback.LedColor, Is.EqualTo(LedColor.Red));
        Assert.That(result.IdleState.PermanentLedColor, Is.EqualTo(LedColor.Blue));
        
        _mockRepository.Verify(r => r.GetConfigurationAsync(), Times.Once);
    }

    [Test]
    public async Task GetDefaultConfigurationAsync_WhenRepositoryThrows_ReturnsDefaults()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetConfigurationAsync())
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _service.GetDefaultConfigurationAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.SuccessFeedback, Is.Not.Null);
        Assert.That(result.FailureFeedback, Is.Not.Null);
        Assert.That(result.IdleState, Is.Not.Null);
        
        // Verify defaults
        Assert.That(result.SuccessFeedback.LedColor, Is.EqualTo(LedColor.Green));
        Assert.That(result.SuccessFeedback.LedDurationMs, Is.EqualTo(1000));
        Assert.That(result.SuccessFeedback.BeepCount, Is.EqualTo(1));
        Assert.That(result.SuccessFeedback.DisplayMessage, Is.EqualTo("ACCESS GRANTED"));
        
        Assert.That(result.FailureFeedback.LedColor, Is.EqualTo(LedColor.Red));
        Assert.That(result.FailureFeedback.LedDurationMs, Is.EqualTo(2000));
        Assert.That(result.FailureFeedback.BeepCount, Is.EqualTo(3));
        Assert.That(result.FailureFeedback.DisplayMessage, Is.EqualTo("ACCESS DENIED"));
        
        Assert.That(result.IdleState.PermanentLedColor, Is.EqualTo(LedColor.Blue));
        Assert.That(result.IdleState.HeartbeatFlashColor, Is.EqualTo(LedColor.Green));
    }

    [Test]
    public async Task GetDefaultConfigurationAsync_WhenRepositoryReturnsNulls_FillsWithDefaults()
    {
        // Arrange
        var configWithNulls = new FeedbackConfiguration
        {
            SuccessFeedback = null,
            FailureFeedback = null,
            IdleState = null
        };

        _mockRepository.Setup(r => r.GetConfigurationAsync())
            .ReturnsAsync(configWithNulls);

        // Act
        var result = await _service.GetDefaultConfigurationAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.SuccessFeedback, Is.Not.Null);
        Assert.That(result.FailureFeedback, Is.Not.Null);
        Assert.That(result.IdleState, Is.Not.Null);
    }

    #endregion

    #region SaveDefaultConfigurationAsync Tests

    [Test]
    public async Task SaveDefaultConfigurationAsync_ValidConfiguration_CallsRepository()
    {
        // Arrange
        var configuration = new FeedbackConfiguration
        {
            SuccessFeedback = new ReaderFeedback
            {
                Type = ReaderFeedbackType.Success,
                LedColor = LedColor.Green,
                LedDurationMs = 1000,
                BeepCount = 1,
                DisplayMessage = "OK"
            },
            FailureFeedback = new ReaderFeedback
            {
                Type = ReaderFeedbackType.Failure,
                LedColor = LedColor.Red,
                LedDurationMs = 2000,
                BeepCount = 3,
                DisplayMessage = "FAIL"
            },
            IdleState = new IdleStateFeedback
            {
                PermanentLedColor = LedColor.Blue,
                HeartbeatFlashColor = LedColor.Green
            }
        };

        _mockRepository.Setup(r => r.SaveConfigurationAsync(It.IsAny<FeedbackConfiguration>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.SaveDefaultConfigurationAsync(configuration);

        // Assert
        _mockRepository.Verify(r => r.SaveConfigurationAsync(configuration), Times.Once);
    }

    [Test]
    public void SaveDefaultConfigurationAsync_NullConfiguration_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () => 
            await _service.SaveDefaultConfigurationAsync(null));
    }

    [Test]
    public void SaveDefaultConfigurationAsync_NullSuccessFeedback_ThrowsArgumentNullException()
    {
        // Arrange
        var configuration = new FeedbackConfiguration
        {
            SuccessFeedback = null,
            FailureFeedback = new ReaderFeedback { Type = ReaderFeedbackType.Failure },
            IdleState = new IdleStateFeedback()
        };

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () => 
            await _service.SaveDefaultConfigurationAsync(configuration));
    }

    [Test]
    public void SaveDefaultConfigurationAsync_InvalidLedDuration_ThrowsArgumentException()
    {
        // Arrange
        var configuration = new FeedbackConfiguration
        {
            SuccessFeedback = new ReaderFeedback
            {
                Type = ReaderFeedbackType.Success,
                LedDurationMs = 50 // Too short (minimum 100ms)
            },
            FailureFeedback = new ReaderFeedback { Type = ReaderFeedbackType.Failure },
            IdleState = new IdleStateFeedback()
        };

        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(async () => 
            await _service.SaveDefaultConfigurationAsync(configuration));
    }

    [Test]
    public void SaveDefaultConfigurationAsync_NegativeBeepCount_ThrowsArgumentException()
    {
        // Arrange
        var configuration = new FeedbackConfiguration
        {
            SuccessFeedback = new ReaderFeedback
            {
                Type = ReaderFeedbackType.Success,
                BeepCount = -1 // Negative not allowed
            },
            FailureFeedback = new ReaderFeedback { Type = ReaderFeedbackType.Failure },
            IdleState = new IdleStateFeedback()
        };

        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(async () => 
            await _service.SaveDefaultConfigurationAsync(configuration));
    }

    [Test]
    public void SaveDefaultConfigurationAsync_TooLongDisplayMessage_ThrowsArgumentException()
    {
        // Arrange
        var configuration = new FeedbackConfiguration
        {
            SuccessFeedback = new ReaderFeedback
            {
                Type = ReaderFeedbackType.Success,
                DisplayMessage = "This message is way too long for a 16 character display"
            },
            FailureFeedback = new ReaderFeedback { Type = ReaderFeedbackType.Failure },
            IdleState = new IdleStateFeedback()
        };

        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(async () => 
            await _service.SaveDefaultConfigurationAsync(configuration));
    }

    #endregion

    #region GetSuccessFeedbackAsync Tests

    [Test]
    public async Task GetSuccessFeedbackAsync_WhenRepositoryReturnsData_ReturnsFeedback()
    {
        // Arrange
        var expectedFeedback = new ReaderFeedback
        {
            Type = ReaderFeedbackType.Success,
            LedColor = LedColor.Green,
            LedDurationMs = 1000,
            BeepCount = 1,
            DisplayMessage = "SUCCESS"
        };

        _mockRepository.Setup(r => r.GetFeedbackByTypeAsync(ReaderFeedbackType.Success))
            .ReturnsAsync(expectedFeedback);

        // Act
        var result = await _service.GetSuccessFeedbackAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.LedColor, Is.EqualTo(LedColor.Green));
        Assert.That(result.DisplayMessage, Is.EqualTo("SUCCESS"));
        
        _mockRepository.Verify(r => r.GetFeedbackByTypeAsync(ReaderFeedbackType.Success), Times.Once);
    }

    [Test]
    public async Task GetSuccessFeedbackAsync_WhenRepositoryReturnsNull_ReturnsDefault()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetFeedbackByTypeAsync(ReaderFeedbackType.Success))
            .ReturnsAsync((ReaderFeedback)null);

        // Act
        var result = await _service.GetSuccessFeedbackAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Type, Is.EqualTo(ReaderFeedbackType.Success));
        Assert.That(result.LedColor, Is.EqualTo(LedColor.Green));
        Assert.That(result.DisplayMessage, Is.EqualTo("ACCESS GRANTED"));
    }

    [Test]
    public async Task GetSuccessFeedbackAsync_WhenRepositoryThrows_ReturnsDefault()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetFeedbackByTypeAsync(ReaderFeedbackType.Success))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _service.GetSuccessFeedbackAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Type, Is.EqualTo(ReaderFeedbackType.Success));
        Assert.That(result.LedColor, Is.EqualTo(LedColor.Green));
    }

    #endregion

    #region GetFailureFeedbackAsync Tests

    [Test]
    public async Task GetFailureFeedbackAsync_WhenRepositoryReturnsData_ReturnsFeedback()
    {
        // Arrange
        var expectedFeedback = new ReaderFeedback
        {
            Type = ReaderFeedbackType.Failure,
            LedColor = LedColor.Red,
            LedDurationMs = 2000,
            BeepCount = 3,
            DisplayMessage = "FAILURE"
        };

        _mockRepository.Setup(r => r.GetFeedbackByTypeAsync(ReaderFeedbackType.Failure))
            .ReturnsAsync(expectedFeedback);

        // Act
        var result = await _service.GetFailureFeedbackAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.LedColor, Is.EqualTo(LedColor.Red));
        Assert.That(result.DisplayMessage, Is.EqualTo("FAILURE"));
        
        _mockRepository.Verify(r => r.GetFeedbackByTypeAsync(ReaderFeedbackType.Failure), Times.Once);
    }

    #endregion

    #region GetIdleStateAsync Tests

    [Test]
    public async Task GetIdleStateAsync_WhenRepositoryReturnsData_ReturnsIdleState()
    {
        // Arrange
        var expectedIdleState = new IdleStateFeedback
        {
            PermanentLedColor = LedColor.Amber,
            HeartbeatFlashColor = LedColor.Red
        };

        _mockRepository.Setup(r => r.GetIdleStateAsync())
            .ReturnsAsync(expectedIdleState);

        // Act
        var result = await _service.GetIdleStateAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.PermanentLedColor, Is.EqualTo(LedColor.Amber));
        Assert.That(result.HeartbeatFlashColor, Is.EqualTo(LedColor.Red));
        
        _mockRepository.Verify(r => r.GetIdleStateAsync(), Times.Once);
    }

    #endregion

    #region SaveSuccessFeedbackAsync Tests

    [Test]
    public async Task SaveSuccessFeedbackAsync_ValidFeedback_CallsRepository()
    {
        // Arrange
        var feedback = new ReaderFeedback
        {
            LedColor = LedColor.Green,
            LedDurationMs = 1000,
            BeepCount = 1,
            DisplayMessage = "OK"
        };

        _mockRepository.Setup(r => r.SaveFeedbackByTypeAsync(It.IsAny<ReaderFeedback>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.SaveSuccessFeedbackAsync(feedback);

        // Assert
        Assert.That(feedback.Type, Is.EqualTo(ReaderFeedbackType.Success)); // Should be set by service
        _mockRepository.Verify(r => r.SaveFeedbackByTypeAsync(feedback), Times.Once);
    }

    #endregion

    #region SaveFailureFeedbackAsync Tests

    [Test]
    public async Task SaveFailureFeedbackAsync_ValidFeedback_CallsRepository()
    {
        // Arrange
        var feedback = new ReaderFeedback
        {
            LedColor = LedColor.Red,
            LedDurationMs = 2000,
            BeepCount = 3,
            DisplayMessage = "FAIL"
        };

        _mockRepository.Setup(r => r.SaveFeedbackByTypeAsync(It.IsAny<ReaderFeedback>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.SaveFailureFeedbackAsync(feedback);

        // Assert
        Assert.That(feedback.Type, Is.EqualTo(ReaderFeedbackType.Failure)); // Should be set by service
        _mockRepository.Verify(r => r.SaveFeedbackByTypeAsync(feedback), Times.Once);
    }

    #endregion

    #region SaveIdleStateAsync Tests

    [Test]
    public async Task SaveIdleStateAsync_ValidIdleState_CallsRepository()
    {
        // Arrange
        var idleState = new IdleStateFeedback
        {
            PermanentLedColor = LedColor.Blue,
            HeartbeatFlashColor = LedColor.Green
        };

        _mockRepository.Setup(r => r.SaveIdleStateAsync(It.IsAny<IdleStateFeedback>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.SaveIdleStateAsync(idleState);

        // Assert
        _mockRepository.Verify(r => r.SaveIdleStateAsync(idleState), Times.Once);
    }

    #endregion
}