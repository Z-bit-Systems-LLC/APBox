using ApBox.Core.Models;
using ApBox.Core.Services.Configuration;
using ApBox.Core.Services.Core;
using ApBox.Core.Services.Persistence;
using ApBox.Core.Services.Reader;
using ApBox.Plugins;
using Microsoft.Extensions.Logging;
using Moq;

namespace ApBox.Core.Tests.Services.Core;

[TestFixture]
public class CardEventProcessingOrchestratorTests
{
    private Mock<ICardProcessingService> _mockCardProcessingService;
    private Mock<ICardEventPersistenceService> _mockPersistenceService;
    private Mock<IReaderService> _mockReaderService;
    private Mock<IFeedbackConfigurationService> _mockFeedbackConfigurationService;
    private Mock<ILogger<CardEventProcessingOrchestrator>> _mockLogger;
    private CardEventProcessingOrchestrator _orchestrator;

    [SetUp]
    public void Setup()
    {
        _mockCardProcessingService = new Mock<ICardProcessingService>();
        _mockPersistenceService = new Mock<ICardEventPersistenceService>();
        _mockReaderService = new Mock<IReaderService>();
        _mockFeedbackConfigurationService = new Mock<IFeedbackConfigurationService>();
        _mockLogger = new Mock<ILogger<CardEventProcessingOrchestrator>>();

        _orchestrator = new CardEventProcessingOrchestrator(
            _mockCardProcessingService.Object,
            _mockPersistenceService.Object,
            _mockReaderService.Object,
            _mockFeedbackConfigurationService.Object,
            _mockLogger.Object);
    }

    [Test]
    public async Task ProcessEventAsync_Success_ReturnsCompleteResult()
    {
        // Arrange
        var cardRead = new CardReadEvent
        {
            ReaderId = Guid.NewGuid(),
            CardNumber = "1234567890",
            ReaderName = "Test Reader",
            Timestamp = DateTime.UtcNow
        };

        var expectedPluginResult = new CardReadResult
        {
            Success = true,
            Message = "Plugin processing successful"
        };

        var expectedFeedback = new ReaderFeedback
        {
            Type = ReaderFeedbackType.Success,
            DisplayMessage = "Access granted"
        };

        _mockCardProcessingService
            .Setup(x => x.ProcessCardReadAsync(cardRead))
            .ReturnsAsync(expectedPluginResult);

        _mockFeedbackConfigurationService
            .Setup(x => x.GetSuccessFeedbackAsync())
            .ReturnsAsync(expectedFeedback);

        _mockPersistenceService
            .Setup(x => x.PersistCardEventAsync(cardRead, expectedPluginResult))
            .ReturnsAsync(true);

        // Act
        var result = await _orchestrator.ProcessEventAsync(cardRead);

        // Assert
        Assert.That(result.PluginResult, Is.EqualTo(expectedPluginResult));
        Assert.That(result.Feedback, Is.EqualTo(expectedFeedback));
        Assert.That(result.PersistenceSuccessful, Is.True);
        Assert.That(result.FeedbackDeliverySuccessful, Is.True);

        // Verify all services were called
        _mockCardProcessingService.Verify(x => x.ProcessCardReadAsync(cardRead), Times.Once);
        _mockFeedbackConfigurationService.Verify(x => x.GetSuccessFeedbackAsync(), Times.Once);
        _mockPersistenceService.Verify(x => x.PersistCardEventAsync(cardRead, expectedPluginResult), Times.Once);
        _mockReaderService.Verify(x => x.SendFeedbackAsync(cardRead.ReaderId, expectedFeedback), Times.Once);
    }

    [Test]
    public async Task ProcessEventAsync_PluginProcessingFails_HandlesErrorGracefully()
    {
        // Arrange
        var cardRead = new CardReadEvent
        {
            ReaderId = Guid.NewGuid(),
            CardNumber = "1234567890",
            ReaderName = "Test Reader"
        };

        _mockCardProcessingService
            .Setup(x => x.ProcessCardReadAsync(cardRead))
            .ThrowsAsync(new Exception("Plugin processing failed"));

        // Act
        var result = await _orchestrator.ProcessEventAsync(cardRead);

        // Assert
        Assert.That(result.PluginResult.Success, Is.False);
        Assert.That(result.PluginResult.Message, Is.EqualTo("Plugin processing error occurred"));
        Assert.That(result.Feedback.Type, Is.EqualTo(ReaderFeedbackType.Failure));

        // Verify error was persisted
        _mockPersistenceService.Verify(x => x.PersistCardEventErrorAsync(cardRead, "Plugin processing error occurred"), Times.Once);
    }

    [Test]
    public async Task ProcessEventAsync_PersistenceFails_ContinuesWithFeedback()
    {
        // Arrange
        var cardRead = new CardReadEvent
        {
            ReaderId = Guid.NewGuid(),
            CardNumber = "1234567890",
            ReaderName = "Test Reader"
        };

        var pluginResult = new CardReadResult { Success = true, Message = "Success" };
        var feedback = new ReaderFeedback { Type = ReaderFeedbackType.Success };

        _mockCardProcessingService
            .Setup(x => x.ProcessCardReadAsync(cardRead))
            .ReturnsAsync(pluginResult);

        _mockFeedbackConfigurationService
            .Setup(x => x.GetSuccessFeedbackAsync())
            .ReturnsAsync(feedback);

        _mockPersistenceService
            .Setup(x => x.PersistCardEventAsync(cardRead, pluginResult))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _orchestrator.ProcessEventAsync(cardRead);

        // Assert
        Assert.That(result.PluginResult.Success, Is.True);
        Assert.That(result.PersistenceSuccessful, Is.False);
        Assert.That(result.FeedbackDeliverySuccessful, Is.True);

        // Verify feedback was still sent
        _mockReaderService.Verify(x => x.SendFeedbackAsync(cardRead.ReaderId, feedback), Times.Once);
    }

    [Test]
    public async Task ProcessEventAsync_FeedbackDeliveryFails_StillReturnsResult()
    {
        // Arrange
        var cardRead = new CardReadEvent
        {
            ReaderId = Guid.NewGuid(),
            CardNumber = "1234567890",
            ReaderName = "Test Reader"
        };

        var pluginResult = new CardReadResult { Success = true, Message = "Success" };
        var feedback = new ReaderFeedback { Type = ReaderFeedbackType.Success };

        _mockCardProcessingService
            .Setup(x => x.ProcessCardReadAsync(cardRead))
            .ReturnsAsync(pluginResult);

        _mockFeedbackConfigurationService
            .Setup(x => x.GetSuccessFeedbackAsync())
            .ReturnsAsync(feedback);

        _mockPersistenceService
            .Setup(x => x.PersistCardEventAsync(cardRead, pluginResult))
            .ReturnsAsync(true);

        _mockReaderService
            .Setup(x => x.SendFeedbackAsync(cardRead.ReaderId, feedback))
            .ThrowsAsync(new Exception("Reader communication error"));

        // Act
        var result = await _orchestrator.ProcessEventAsync(cardRead);

        // Assert
        Assert.That(result.PluginResult.Success, Is.True);
        Assert.That(result.PersistenceSuccessful, Is.True);
        Assert.That(result.FeedbackDeliverySuccessful, Is.False);
    }

    [Test]
    public async Task ProcessEventAsync_FailedPluginResult_PersistsError()
    {
        // Arrange
        var cardRead = new CardReadEvent
        {
            ReaderId = Guid.NewGuid(),
            CardNumber = "1234567890",
            ReaderName = "Test Reader"
        };

        var failedResult = new CardReadResult
        {
            Success = false,
            Message = "Plugin validation failed"
        };

        var errorFeedback = new ReaderFeedback
        {
            Type = ReaderFeedbackType.Failure,
            DisplayMessage = "Access denied"
        };

        _mockCardProcessingService
            .Setup(x => x.ProcessCardReadAsync(cardRead))
            .ReturnsAsync(failedResult);

        _mockFeedbackConfigurationService
            .Setup(x => x.GetFailureFeedbackAsync())
            .ReturnsAsync(errorFeedback);

        // Act
        var result = await _orchestrator.ProcessEventAsync(cardRead);

        // Assert
        Assert.That(result.PluginResult.Success, Is.False);
        Assert.That(result.PersistenceSuccessful, Is.True);

        // Verify error was persisted instead of normal event
        _mockPersistenceService.Verify(x => x.PersistCardEventErrorAsync(cardRead, "Plugin validation failed"), Times.Once);
        _mockPersistenceService.Verify(x => x.PersistCardEventAsync(It.IsAny<CardReadEvent>(), It.IsAny<CardReadResult>()), Times.Never);
    }
}