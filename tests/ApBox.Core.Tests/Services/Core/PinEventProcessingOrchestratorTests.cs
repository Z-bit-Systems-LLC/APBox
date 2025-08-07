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
public class PinEventProcessingOrchestratorTests
{
    private Mock<IPinProcessingService> _mockPinProcessingService;
    private Mock<IPinEventPersistenceService> _mockPersistenceService;
    private Mock<IReaderService> _mockReaderService;
    private Mock<IFeedbackConfigurationService> _mockFeedbackConfigurationService;
    private Mock<ILogger<PinEventProcessingOrchestrator>> _mockLogger;
    private PinEventProcessingOrchestrator _orchestrator;

    [SetUp]
    public void Setup()
    {
        _mockPinProcessingService = new Mock<IPinProcessingService>();
        _mockPersistenceService = new Mock<IPinEventPersistenceService>();
        _mockReaderService = new Mock<IReaderService>();
        _mockFeedbackConfigurationService = new Mock<IFeedbackConfigurationService>();
        _mockLogger = new Mock<ILogger<PinEventProcessingOrchestrator>>();

        _orchestrator = new PinEventProcessingOrchestrator(
            _mockPinProcessingService.Object,
            _mockPersistenceService.Object,
            _mockReaderService.Object,
            _mockFeedbackConfigurationService.Object,
            _mockLogger.Object);
    }

    [Test]
    public async Task ProcessEventAsync_Success_ReturnsCompleteResult()
    {
        // Arrange
        var pinRead = new PinReadEvent
        {
            ReaderId = Guid.NewGuid(),
            ReaderName = "Test Reader",
            Pin = "1234",
            CompletionReason = PinCompletionReason.PoundKey,
            Timestamp = DateTime.UtcNow
        };

        var expectedPluginResult = new PinReadResult
        {
            Success = true,
            Message = "PIN validation successful"
        };

        var expectedFeedback = new ReaderFeedback
        {
            Type = ReaderFeedbackType.Success,
            DisplayMessage = "PIN accepted"
        };

        _mockPinProcessingService
            .Setup(x => x.ProcessPinReadAsync(pinRead))
            .ReturnsAsync(expectedPluginResult);

        _mockFeedbackConfigurationService
            .Setup(x => x.GetSuccessFeedbackAsync())
            .ReturnsAsync(expectedFeedback);

        _mockPersistenceService
            .Setup(x => x.PersistPinEventAsync(pinRead, expectedPluginResult))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _orchestrator.ProcessEventAsync(pinRead);

        // Assert
        Assert.That(result.PluginResult, Is.EqualTo(expectedPluginResult));
        Assert.That(result.Feedback, Is.EqualTo(expectedFeedback));
        Assert.That(result.PersistenceSuccessful, Is.True);
        Assert.That(result.FeedbackDeliverySuccessful, Is.True);

        // Verify all services were called
        _mockPinProcessingService.Verify(x => x.ProcessPinReadAsync(pinRead), Times.Once);
        _mockFeedbackConfigurationService.Verify(x => x.GetSuccessFeedbackAsync(), Times.Once);
        _mockPersistenceService.Verify(x => x.PersistPinEventAsync(pinRead, expectedPluginResult), Times.Once);
        _mockReaderService.Verify(x => x.SendFeedbackAsync(pinRead.ReaderId, expectedFeedback), Times.Once);
    }

    [Test]
    public async Task ProcessEventAsync_PluginProcessingFails_HandlesErrorGracefully()
    {
        // Arrange
        var pinRead = new PinReadEvent
        {
            ReaderId = Guid.NewGuid(),
            ReaderName = "Test Reader",
            Pin = "1234",
            CompletionReason = PinCompletionReason.PoundKey
        };

        _mockPinProcessingService
            .Setup(x => x.ProcessPinReadAsync(pinRead))
            .ThrowsAsync(new Exception("PIN processing failed"));

        // Act
        var result = await _orchestrator.ProcessEventAsync(pinRead);

        // Assert
        Assert.That(result.PluginResult.Success, Is.False);
        Assert.That(result.PluginResult.Message, Is.EqualTo("Plugin processing error occurred"));
        Assert.That(result.Feedback.Type, Is.EqualTo(ReaderFeedbackType.Failure));

        // Verify error was persisted
        _mockPersistenceService.Verify(x => x.PersistPinEventErrorAsync(pinRead, "Plugin processing error occurred"), Times.Once);
    }

    [Test]
    public async Task ProcessEventAsync_PersistenceFails_ContinuesWithFeedback()
    {
        // Arrange
        var pinRead = new PinReadEvent
        {
            ReaderId = Guid.NewGuid(),
            ReaderName = "Test Reader",
            Pin = "1234",
            CompletionReason = PinCompletionReason.PoundKey
        };

        var pluginResult = new PinReadResult { Success = true, Message = "Success" };
        var feedback = new ReaderFeedback { Type = ReaderFeedbackType.Success };

        _mockPinProcessingService
            .Setup(x => x.ProcessPinReadAsync(pinRead))
            .ReturnsAsync(pluginResult);

        _mockFeedbackConfigurationService
            .Setup(x => x.GetSuccessFeedbackAsync())
            .ReturnsAsync(feedback);

        _mockPersistenceService
            .Setup(x => x.PersistPinEventAsync(pinRead, pluginResult))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _orchestrator.ProcessEventAsync(pinRead);

        // Assert
        Assert.That(result.PluginResult.Success, Is.True);
        Assert.That(result.PersistenceSuccessful, Is.False);
        Assert.That(result.FeedbackDeliverySuccessful, Is.True);

        // Verify feedback was still sent
        _mockReaderService.Verify(x => x.SendFeedbackAsync(pinRead.ReaderId, feedback), Times.Once);
    }

    [Test]
    public async Task ProcessEventAsync_FeedbackDeliveryFails_StillReturnsResult()
    {
        // Arrange
        var pinRead = new PinReadEvent
        {
            ReaderId = Guid.NewGuid(),
            ReaderName = "Test Reader",
            Pin = "1234",
            CompletionReason = PinCompletionReason.PoundKey
        };

        var pluginResult = new PinReadResult { Success = true, Message = "Success" };
        var feedback = new ReaderFeedback { Type = ReaderFeedbackType.Success };

        _mockPinProcessingService
            .Setup(x => x.ProcessPinReadAsync(pinRead))
            .ReturnsAsync(pluginResult);

        _mockFeedbackConfigurationService
            .Setup(x => x.GetSuccessFeedbackAsync())
            .ReturnsAsync(feedback);

        _mockPersistenceService
            .Setup(x => x.PersistPinEventAsync(pinRead, pluginResult))
            .Returns(Task.CompletedTask);

        _mockReaderService
            .Setup(x => x.SendFeedbackAsync(pinRead.ReaderId, feedback))
            .ThrowsAsync(new Exception("Reader communication error"));

        // Act
        var result = await _orchestrator.ProcessEventAsync(pinRead);

        // Assert
        Assert.That(result.PluginResult.Success, Is.True);
        Assert.That(result.PersistenceSuccessful, Is.True);
        Assert.That(result.FeedbackDeliverySuccessful, Is.False);
    }

    [Test]
    public async Task ProcessEventAsync_FailedPluginResult_PersistsError()
    {
        // Arrange
        var pinRead = new PinReadEvent
        {
            ReaderId = Guid.NewGuid(),
            ReaderName = "Test Reader",
            Pin = "1234",
            CompletionReason = PinCompletionReason.PoundKey
        };

        var failedResult = new PinReadResult
        {
            Success = false,
            Message = "PIN validation failed"
        };

        var errorFeedback = new ReaderFeedback
        {
            Type = ReaderFeedbackType.Failure,
            DisplayMessage = "PIN rejected"
        };

        _mockPinProcessingService
            .Setup(x => x.ProcessPinReadAsync(pinRead))
            .ReturnsAsync(failedResult);

        _mockFeedbackConfigurationService
            .Setup(x => x.GetFailureFeedbackAsync())
            .ReturnsAsync(errorFeedback);

        // Act
        var result = await _orchestrator.ProcessEventAsync(pinRead);

        // Assert
        Assert.That(result.PluginResult.Success, Is.False);
        Assert.That(result.PersistenceSuccessful, Is.True);

        // Verify error was persisted instead of normal event
        _mockPersistenceService.Verify(x => x.PersistPinEventErrorAsync(pinRead, "PIN validation failed"), Times.Once);
        _mockPersistenceService.Verify(x => x.PersistPinEventAsync(It.IsAny<PinReadEvent>(), It.IsAny<PinReadResult>()), Times.Never);
    }
}