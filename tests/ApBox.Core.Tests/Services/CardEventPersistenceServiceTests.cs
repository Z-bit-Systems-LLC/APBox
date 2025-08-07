using Moq;
using Microsoft.Extensions.Logging;
using ApBox.Core.Services.Persistence;
using ApBox.Core.Data.Repositories;
using ApBox.Core.Data.Models;
using ApBox.Plugins;

namespace ApBox.Core.Tests.Services;

[TestFixture]
public class CardEventPersistenceServiceTests
{
    private Mock<ICardEventRepository> _mockRepository;
    private Mock<ILogger<CardEventPersistenceService>> _mockLogger;
    private CardEventPersistenceService _service;

    [SetUp]
    public void Setup()
    {
        _mockRepository = new Mock<ICardEventRepository>();
        _mockLogger = new Mock<ILogger<CardEventPersistenceService>>();
        _service = new CardEventPersistenceService(_mockRepository.Object, _mockLogger.Object);
    }

    [Test]
    public async Task PersistCardEventAsync_Success_ReturnsTrue()
    {
        // Arrange
        var cardRead = new CardReadEvent
        {
            ReaderId = Guid.NewGuid(),
            CardNumber = "1234567890",
            ReaderName = "Test Reader"
        };

        var result = new CardReadResult
        {
            Success = true,
            Message = "Success"
        };

        _mockRepository
            .Setup(x => x.CreateAsync(It.IsAny<CardReadEvent>(), It.IsAny<CardReadResult>()))
            .ReturnsAsync(new CardEventEntity());

        // Act
        var success = await _service.PersistCardEventAsync(cardRead, result);

        // Assert
        Assert.That(success, Is.True);
        _mockRepository.Verify(x => x.CreateAsync(cardRead, result), Times.Once);
    }

    [Test]
    public async Task PersistCardEventAsync_RepositoryThrows_ReturnsFalse()
    {
        // Arrange
        var cardRead = new CardReadEvent
        {
            ReaderId = Guid.NewGuid(),
            CardNumber = "1234567890",
            ReaderName = "Test Reader"
        };

        var result = new CardReadResult
        {
            Success = true,
            Message = "Success"
        };

        _mockRepository
            .Setup(x => x.CreateAsync(It.IsAny<CardReadEvent>(), It.IsAny<CardReadResult>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var success = await _service.PersistCardEventAsync(cardRead, result);

        // Assert
        Assert.That(success, Is.False);
    }

    [Test]
    public async Task PersistCardEventErrorAsync_Success_ReturnsTrue()
    {
        // Arrange
        var cardRead = new CardReadEvent
        {
            ReaderId = Guid.NewGuid(),
            CardNumber = "1234567890",
            ReaderName = "Test Reader"
        };

        var errorMessage = "Processing failed";

        _mockRepository
            .Setup(x => x.CreateAsync(It.IsAny<CardReadEvent>(), It.IsAny<CardReadResult>()))
            .ReturnsAsync(new CardEventEntity());

        // Act
        var success = await _service.PersistCardEventErrorAsync(cardRead, errorMessage);

        // Assert
        Assert.That(success, Is.True);
        _mockRepository.Verify(x => x.CreateAsync(cardRead, It.Is<CardReadResult>(r => 
            r.Success == false && r.Message == errorMessage)), Times.Once);
    }
}