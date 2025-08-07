using ApBox.Core.Models;
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
public class CardProcessingWebOrchestratorTests
{
    private Mock<CardEventProcessingOrchestrator> _mockCoreOrchestrator;
    private MockNotificationAggregator _mockNotificationAggregator;
    private Mock<ILogger<CardProcessingWebOrchestrator>> _mockLogger;
    private CardProcessingWebOrchestrator _orchestrator;

    [SetUp]
    public void Setup()
    {
        _mockCoreOrchestrator = new Mock<CardEventProcessingOrchestrator>(
            Mock.Of<ICardProcessingService>(),
            Mock.Of<ICardEventPersistenceService>(),
            Mock.Of<IReaderService>(),
            Mock.Of<ILogger<CardEventProcessingOrchestrator>>());
        _mockNotificationAggregator = new MockNotificationAggregator();
        _mockLogger = new Mock<ILogger<CardProcessingWebOrchestrator>>();

        _orchestrator = new CardProcessingWebOrchestrator(
            _mockCoreOrchestrator.Object,
            _mockNotificationAggregator,
            _mockLogger.Object);
    }

    [Test]
    public async Task OrchestrateCardProcessingAsync_Success_CallsCoreAndBroadcastsNotification()
    {
        // Arrange
        var cardRead = new CardReadEvent
        {
            ReaderId = Guid.NewGuid(),
            CardNumber = "1234567890",
            ReaderName = "Test Reader",
            BitLength = 26,
            Timestamp = DateTime.UtcNow
        };

        var pluginResult = new CardReadResult
        {
            Success = true,
            Message = "Success"
        };

        var feedback = new ReaderFeedback
        {
            Type = ReaderFeedbackType.Success,
            DisplayMessage = "Access granted"
        };

        var coreResult = new EventProcessingResult<CardReadResult>
        {
            PluginResult = pluginResult,
            Feedback = feedback,
            PersistenceSuccessful = true,
            FeedbackDeliverySuccessful = true
        };

        _mockCoreOrchestrator
            .Setup(x => x.ProcessEventAsync(cardRead))
            .ReturnsAsync(coreResult);

        // Act
        var result = await _orchestrator.OrchestrateCardProcessingAsync(cardRead);

        // Assert
        Assert.That(result, Is.EqualTo(pluginResult));

        // Verify core orchestrator was called
        _mockCoreOrchestrator.Verify(x => x.ProcessEventAsync(cardRead), Times.Once);

        // Verify notification was broadcast
        var notifications = _mockNotificationAggregator.GetNotifications<CardEventNotification>();
        Assert.That(notifications, Has.Count.EqualTo(1));
        
        var notification = notifications[0];
        Assert.That(notification.ReaderId, Is.EqualTo(cardRead.ReaderId));
        Assert.That(notification.ReaderName, Is.EqualTo(cardRead.ReaderName));
        Assert.That(notification.CardNumber, Is.EqualTo(cardRead.CardNumber));
        Assert.That(notification.BitLength, Is.EqualTo(cardRead.BitLength));
        Assert.That(notification.Timestamp, Is.EqualTo(cardRead.Timestamp));
        Assert.That(notification.Success, Is.True);
        Assert.That(notification.Message, Is.EqualTo("Success"));
        Assert.That(notification.Feedback, Is.EqualTo(feedback));
    }

    [Test]
    public async Task OrchestrateCardProcessingAsync_CoreFails_ReturnsFailureResult()
    {
        // Arrange
        var cardRead = new CardReadEvent
        {
            ReaderId = Guid.NewGuid(),
            CardNumber = "1234567890",
            ReaderName = "Test Reader"
        };

        var pluginResult = new CardReadResult
        {
            Success = false,
            Message = "Processing failed"
        };

        var feedback = new ReaderFeedback
        {
            Type = ReaderFeedbackType.Failure,
            DisplayMessage = "Access denied"
        };

        var coreResult = new EventProcessingResult<CardReadResult>
        {
            PluginResult = pluginResult,
            Feedback = feedback,
            PersistenceSuccessful = false,
            FeedbackDeliverySuccessful = true
        };

        _mockCoreOrchestrator
            .Setup(x => x.ProcessEventAsync(cardRead))
            .ReturnsAsync(coreResult);

        // Act
        var result = await _orchestrator.OrchestrateCardProcessingAsync(cardRead);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Is.EqualTo("Processing failed"));

        // Verify notification was still broadcast
        var notifications = _mockNotificationAggregator.GetNotifications<CardEventNotification>();
        Assert.That(notifications, Has.Count.EqualTo(1));
        Assert.That(notifications[0].Success, Is.False);
    }

    [Test]
    public async Task OrchestrateCardProcessingAsync_NotificationFails_StillReturnsResult()
    {
        // Arrange
        var cardRead = new CardReadEvent
        {
            ReaderId = Guid.NewGuid(),
            CardNumber = "1234567890",
            ReaderName = "Test Reader"
        };

        var pluginResult = new CardReadResult
        {
            Success = true,
            Message = "Success"
        };

        var coreResult = new EventProcessingResult<CardReadResult>
        {
            PluginResult = pluginResult,
            Feedback = new ReaderFeedback { Type = ReaderFeedbackType.Success },
            PersistenceSuccessful = true,
            FeedbackDeliverySuccessful = true
        };

        _mockCoreOrchestrator
            .Setup(x => x.ProcessEventAsync(cardRead))
            .ReturnsAsync(coreResult);

        // Create a notification aggregator that throws
        var throwingAggregator = new Mock<INotificationAggregator>();
        throwingAggregator
            .Setup(x => x.BroadcastAsync(It.IsAny<CardEventNotification>()))
            .ThrowsAsync(new Exception("Notification failed"));

        var orchestrator = new CardProcessingWebOrchestrator(
            _mockCoreOrchestrator.Object,
            throwingAggregator.Object,
            _mockLogger.Object);

        // Act
        var result = await orchestrator.OrchestrateCardProcessingAsync(cardRead);

        // Assert - should still return the result even if notification failed
        Assert.That(result.Success, Is.True);
        Assert.That(result.Message, Is.EqualTo("Success"));
    }

    [Test]
    public async Task OrchestrateCardProcessingAsync_CoreThrows_PropagatesException()
    {
        // Arrange
        var cardRead = new CardReadEvent
        {
            ReaderId = Guid.NewGuid(),
            CardNumber = "1234567890",
            ReaderName = "Test Reader"
        };

        _mockCoreOrchestrator
            .Setup(x => x.ProcessEventAsync(cardRead))
            .ThrowsAsync(new Exception("Core processing failed"));

        // Act & Assert
        Assert.ThrowsAsync<Exception>(async () => await _orchestrator.OrchestrateCardProcessingAsync(cardRead));
    }
}