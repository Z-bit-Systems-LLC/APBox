using NUnit.Framework;
using Moq;
using Microsoft.Extensions.Logging;
using ApBox.Core.Services.Core;
using ApBox.Core.Services.Reader;
using ApBox.Core.Services.Persistence;
using ApBox.Web.Services;
using ApBox.Plugins;
using ApBox.Core.Services.Plugins;
using ApBox.Core.Models;

namespace ApBox.Web.Tests.Services;

[TestFixture]
public class CardProcessingOrchestratorTests
{
    private Mock<ICardProcessingService> _mockCardProcessingService;
    private Mock<ICardEventPersistenceService> _mockPersistenceService;
    private Mock<IReaderService> _mockReaderService;
    private Mock<ICardEventNotificationService> _mockNotificationService;
    private Mock<ILogger<CardProcessingOrchestrator>> _mockLogger;
    private CardProcessingOrchestrator _orchestrator;

    [SetUp]
    public void Setup()
    {
        _mockCardProcessingService = new Mock<ICardProcessingService>();
        _mockPersistenceService = new Mock<ICardEventPersistenceService>();
        _mockReaderService = new Mock<IReaderService>();
        _mockNotificationService = new Mock<ICardEventNotificationService>();
        _mockLogger = new Mock<ILogger<CardProcessingOrchestrator>>();

        _orchestrator = new CardProcessingOrchestrator(
            _mockCardProcessingService.Object,
            _mockPersistenceService.Object,
            _mockReaderService.Object,
            _mockNotificationService.Object,
            _mockLogger.Object);
    }

    [Test]
    public async Task OrchestrateCardProcessingAsync_Success_CallsAllServices()
    {
        // Arrange
        var cardRead = new CardReadEvent
        {
            ReaderId = Guid.NewGuid(),
            CardNumber = "1234567890",
            ReaderName = "Test Reader"
        };

        var expectedResult = new CardReadResult
        {
            Success = true,
            Message = "Success"
        };

        var expectedFeedback = new ReaderFeedback
        {
            Type = ReaderFeedbackType.Success
        };

        _mockCardProcessingService
            .Setup(x => x.ProcessCardReadAsync(It.IsAny<CardReadEvent>()))
            .ReturnsAsync(expectedResult);

        _mockCardProcessingService
            .Setup(x => x.GetFeedbackAsync(It.IsAny<Guid>(), It.IsAny<CardReadResult>()))
            .ReturnsAsync(expectedFeedback);

        _mockPersistenceService
            .Setup(x => x.PersistCardEventAsync(It.IsAny<CardReadEvent>(), It.IsAny<CardReadResult>()))
            .ReturnsAsync(true);

        // Act
        var result = await _orchestrator.OrchestrateCardProcessingAsync(cardRead);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Message, Is.EqualTo("Success"));

        // Verify all services were called
        _mockCardProcessingService.Verify(x => x.ProcessCardReadAsync(cardRead), Times.Once);
        _mockCardProcessingService.Verify(x => x.GetFeedbackAsync(cardRead.ReaderId, expectedResult), Times.Once);
        _mockPersistenceService.Verify(x => x.PersistCardEventAsync(cardRead, expectedResult), Times.Once);
        _mockReaderService.Verify(x => x.SendFeedbackAsync(cardRead.ReaderId, expectedFeedback), Times.Once);
        _mockNotificationService.Verify(x => x.BroadcastCardEventAsync(cardRead, expectedResult, expectedFeedback), Times.Once);
    }

    [Test]
    public async Task OrchestrateCardProcessingAsync_ProcessingFails_ReturnsFailureResult()
    {
        // Arrange
        var cardRead = new CardReadEvent
        {
            ReaderId = Guid.NewGuid(),
            CardNumber = "1234567890",
            ReaderName = "Test Reader"
        };

        _mockCardProcessingService
            .Setup(x => x.ProcessCardReadAsync(It.IsAny<CardReadEvent>()))
            .ThrowsAsync(new Exception("Processing failed"));

        // Act
        var result = await _orchestrator.OrchestrateCardProcessingAsync(cardRead);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Is.EqualTo("Processing error occurred"));

        // Verify error was persisted
        _mockPersistenceService.Verify(x => x.PersistCardEventErrorAsync(cardRead, "Processing failed"), Times.Once);
    }

    [Test]
    public async Task OrchestrateCardProcessingAsync_NotificationFails_StillReturnsSuccess()
    {
        // Arrange
        var cardRead = new CardReadEvent
        {
            ReaderId = Guid.NewGuid(),
            CardNumber = "1234567890",
            ReaderName = "Test Reader"
        };

        var expectedResult = new CardReadResult
        {
            Success = true,
            Message = "Success"
        };

        var expectedFeedback = new ReaderFeedback
        {
            Type = ReaderFeedbackType.Success
        };

        _mockCardProcessingService
            .Setup(x => x.ProcessCardReadAsync(It.IsAny<CardReadEvent>()))
            .ReturnsAsync(expectedResult);

        _mockCardProcessingService
            .Setup(x => x.GetFeedbackAsync(It.IsAny<Guid>(), It.IsAny<CardReadResult>()))
            .ReturnsAsync(expectedFeedback);

        _mockNotificationService
            .Setup(x => x.BroadcastCardEventAsync(It.IsAny<CardReadEvent>(), It.IsAny<CardReadResult>(), It.IsAny<ReaderFeedback>()))
            .ThrowsAsync(new Exception("Notification failed"));

        // Act
        var result = await _orchestrator.OrchestrateCardProcessingAsync(cardRead);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Message, Is.EqualTo("Success"));
    }
}