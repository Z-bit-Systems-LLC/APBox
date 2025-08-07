using ApBox.Core.Models;
using ApBox.Core.Services.Configuration;
using ApBox.Core.Services.Core;
using ApBox.Core.Services.Persistence;
using ApBox.Core.Services.Reader;
using ApBox.Plugins;
using ApBox.Web.Models.Notifications;
using ApBox.Web.Services;
using ApBox.Web.Services.Notifications;
using ApBox.Web.Tests.Services;
using Microsoft.Extensions.Logging;

namespace ApBox.Web.Tests.Services;

[TestFixture]
public class PinProcessingWebOrchestratorTests
{
    private Mock<PinEventProcessingOrchestrator> _mockCoreOrchestrator;
    private MockNotificationAggregator _mockNotificationAggregator;
    private Mock<ILogger<PinProcessingWebOrchestrator>> _mockLogger;
    private PinProcessingWebOrchestrator _orchestrator;

    [SetUp]
    public void Setup()
    {
        _mockCoreOrchestrator = new Mock<PinEventProcessingOrchestrator>(
            Mock.Of<IPinProcessingService>(),
            Mock.Of<IPinEventPersistenceService>(),
            Mock.Of<IReaderService>(),
            Mock.Of<IFeedbackConfigurationService>(),
            Mock.Of<ILogger<PinEventProcessingOrchestrator>>());
        _mockNotificationAggregator = new MockNotificationAggregator();
        _mockLogger = new Mock<ILogger<PinProcessingWebOrchestrator>>();

        _orchestrator = new PinProcessingWebOrchestrator(
            _mockCoreOrchestrator.Object,
            _mockNotificationAggregator,
            _mockLogger.Object);
    }

    [Test]
    public async Task OrchestratePinProcessingAsync_Success_CallsCoreAndBroadcastsNotification()
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

        var pluginResult = new PinReadResult
        {
            Success = true,
            Message = "PIN validated successfully"
        };

        var feedback = new ReaderFeedback
        {
            Type = ReaderFeedbackType.Success,
            DisplayMessage = "PIN accepted"
        };

        var coreResult = new EventProcessingResult<PinReadResult>
        {
            PluginResult = pluginResult,
            Feedback = feedback,
            PersistenceSuccessful = true,
            FeedbackDeliverySuccessful = true
        };

        _mockCoreOrchestrator
            .Setup(x => x.ProcessEventAsync(pinRead))
            .ReturnsAsync(coreResult);

        // Act
        var result = await _orchestrator.OrchestratePinProcessingAsync(pinRead);

        // Assert
        Assert.That(result, Is.EqualTo(pluginResult));

        // Verify core orchestrator was called
        _mockCoreOrchestrator.Verify(x => x.ProcessEventAsync(pinRead), Times.Once);

        // Verify notification was broadcast
        var notifications = _mockNotificationAggregator.GetNotifications<PinEventNotification>();
        Assert.That(notifications, Has.Count.EqualTo(1));
        
        var notification = notifications[0];
        Assert.That(notification.ReaderId, Is.EqualTo(pinRead.ReaderId));
        Assert.That(notification.ReaderName, Is.EqualTo(pinRead.ReaderName));
        Assert.That(notification.PinLength, Is.EqualTo(pinRead.Pin.Length));
        Assert.That(notification.CompletionReason, Is.EqualTo(pinRead.CompletionReason));
        Assert.That(notification.Timestamp, Is.EqualTo(pinRead.Timestamp));
        Assert.That(notification.Success, Is.True);
        Assert.That(notification.Message, Is.EqualTo("PIN validated successfully"));
        Assert.That(notification.Feedback, Is.EqualTo(feedback));
    }

    [Test]
    public async Task OrchestratePinProcessingAsync_CoreFails_ReturnsFailureResult()
    {
        // Arrange
        var pinRead = new PinReadEvent
        {
            ReaderId = Guid.NewGuid(),
            ReaderName = "Test Reader",
            Pin = "1234",
            CompletionReason = PinCompletionReason.PoundKey
        };

        var pluginResult = new PinReadResult
        {
            Success = false,
            Message = "PIN validation failed"
        };

        var feedback = new ReaderFeedback
        {
            Type = ReaderFeedbackType.Failure,
            DisplayMessage = "PIN rejected"
        };

        var coreResult = new EventProcessingResult<PinReadResult>
        {
            PluginResult = pluginResult,
            Feedback = feedback,
            PersistenceSuccessful = false,
            FeedbackDeliverySuccessful = true
        };

        _mockCoreOrchestrator
            .Setup(x => x.ProcessEventAsync(pinRead))
            .ReturnsAsync(coreResult);

        // Act
        var result = await _orchestrator.OrchestratePinProcessingAsync(pinRead);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Is.EqualTo("PIN validation failed"));

        // Verify notification was still broadcast
        var notifications = _mockNotificationAggregator.GetNotifications<PinEventNotification>();
        Assert.That(notifications, Has.Count.EqualTo(1));
        Assert.That(notifications[0].Success, Is.False);
    }

    [Test]
    public async Task OrchestratePinProcessingAsync_NotificationFails_StillReturnsResult()
    {
        // Arrange
        var pinRead = new PinReadEvent
        {
            ReaderId = Guid.NewGuid(),
            ReaderName = "Test Reader",
            Pin = "1234",
            CompletionReason = PinCompletionReason.PoundKey
        };

        var pluginResult = new PinReadResult
        {
            Success = true,
            Message = "Success"
        };

        var coreResult = new EventProcessingResult<PinReadResult>
        {
            PluginResult = pluginResult,
            Feedback = new ReaderFeedback { Type = ReaderFeedbackType.Success },
            PersistenceSuccessful = true,
            FeedbackDeliverySuccessful = true
        };

        _mockCoreOrchestrator
            .Setup(x => x.ProcessEventAsync(pinRead))
            .ReturnsAsync(coreResult);

        // Create a notification aggregator that throws
        var throwingAggregator = new Mock<INotificationAggregator>();
        throwingAggregator
            .Setup(x => x.BroadcastAsync(It.IsAny<PinEventNotification>()))
            .ThrowsAsync(new Exception("Notification failed"));

        var orchestrator = new PinProcessingWebOrchestrator(
            _mockCoreOrchestrator.Object,
            throwingAggregator.Object,
            _mockLogger.Object);

        // Act
        var result = await orchestrator.OrchestratePinProcessingAsync(pinRead);

        // Assert - should still return the result even if notification failed
        Assert.That(result.Success, Is.True);
        Assert.That(result.Message, Is.EqualTo("Success"));
    }

    [Test]
    public async Task OrchestratePinProcessingAsync_CoreThrows_PropagatesException()
    {
        // Arrange
        var pinRead = new PinReadEvent
        {
            ReaderId = Guid.NewGuid(),
            ReaderName = "Test Reader",
            Pin = "1234",
            CompletionReason = PinCompletionReason.PoundKey
        };

        _mockCoreOrchestrator
            .Setup(x => x.ProcessEventAsync(pinRead))
            .ThrowsAsync(new Exception("Core processing failed"));

        // Act & Assert
        Assert.ThrowsAsync<Exception>(async () => await _orchestrator.OrchestratePinProcessingAsync(pinRead));
    }
}