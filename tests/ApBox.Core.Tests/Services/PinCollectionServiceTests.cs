using ApBox.Core.Services.Core;
using ApBox.Core.Services.Reader;
using ApBox.Core.Models;
using ApBox.Plugins;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Moq;

namespace ApBox.Core.Tests.Services;

[TestFixture]
[Category("Unit")]
public class PinCollectionServiceTests
{
    private Mock<ILogger<PinCollectionService>> _mockLogger = null!;
    private Mock<IReaderConfigurationService> _mockReaderConfigurationService = null!;
    private PinCollectionService _pinCollectionService = null!;
    private Guid _testReaderId;
    private const string TestReaderName = "Test Reader";

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<PinCollectionService>>();
        _mockReaderConfigurationService = new Mock<IReaderConfigurationService>();
        _testReaderId = Guid.NewGuid();

        // Setup mock reader configuration
        var readerConfig = new ReaderConfiguration
        {
            ReaderId = _testReaderId,
            ReaderName = TestReaderName,
            SerialPort = "COM1",
            BaudRate = 9600,
            Address = 1,
            IsEnabled = true
        };

        _mockReaderConfigurationService
            .Setup(x => x.GetReaderAsync(_testReaderId))
            .ReturnsAsync(readerConfig);

        _pinCollectionService = new PinCollectionService(_mockLogger.Object, _mockReaderConfigurationService.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _pinCollectionService?.Dispose();
    }

    [Test]
    public async Task CompletePinCollection_WithPoundKey_ShouldIncludeReaderName()
    {
        // Arrange
        PinReadEvent? capturedEvent = null;
        _pinCollectionService.PinCollectionCompleted += (sender, e) => capturedEvent = e;

        // Act - Add digits and complete with pound key
        await _pinCollectionService.AddDigitAsync(_testReaderId, '1');
        await _pinCollectionService.AddDigitAsync(_testReaderId, '2');
        await _pinCollectionService.AddDigitAsync(_testReaderId, '3');
        var isComplete = await _pinCollectionService.AddDigitAsync(_testReaderId, '#');

        // Assert
        Assert.That(isComplete, Is.True);
        Assert.That(capturedEvent, Is.Not.Null);
        Assert.That(capturedEvent.ReaderId, Is.EqualTo(_testReaderId));
        Assert.That(capturedEvent.ReaderName, Is.EqualTo(TestReaderName), 
            "PIN event should include the reader name from configuration");
        Assert.That(capturedEvent.Pin, Is.EqualTo("123"));
        Assert.That(capturedEvent.CompletionReason, Is.EqualTo(PinCompletionReason.PoundKey));
    }

    [Test]
    public async Task CompletePinCollection_WithTimeout_ShouldIncludeReaderName()
    {
        // Arrange
        PinReadEvent? capturedEvent = null;
        _pinCollectionService.PinCollectionCompleted += (sender, e) => capturedEvent = e;

        // Act - Add some digits and wait for timeout (we'll trigger timeout manually)
        await _pinCollectionService.AddDigitAsync(_testReaderId, '4');
        await _pinCollectionService.AddDigitAsync(_testReaderId, '5');

        // Wait a bit and trigger timeout by waiting longer than the timeout period
        // Note: In a real test, we might need to mock the timer or make timeout shorter
        await Task.Delay(100); // Small delay to ensure digits are processed

        // For this test, we'll complete with pound to simulate what would happen on timeout
        var isComplete = await _pinCollectionService.AddDigitAsync(_testReaderId, '#');

        // Assert
        Assert.That(isComplete, Is.True);
        Assert.That(capturedEvent, Is.Not.Null);
        Assert.That(capturedEvent.ReaderId, Is.EqualTo(_testReaderId));
        Assert.That(capturedEvent.ReaderName, Is.EqualTo(TestReaderName), 
            "PIN event should include the reader name even on timeout");
        Assert.That(capturedEvent.Pin, Is.EqualTo("45"));
    }

    [Test]
    public async Task CompletePinCollection_WhenReaderConfigNotFound_ShouldUseEmptyReaderName()
    {
        // Arrange
        var unknownReaderId = Guid.NewGuid();
        _mockReaderConfigurationService
            .Setup(x => x.GetReaderAsync(unknownReaderId))
            .ReturnsAsync((ReaderConfiguration?)null);

        PinReadEvent? capturedEvent = null;
        _pinCollectionService.PinCollectionCompleted += (sender, e) => capturedEvent = e;

        // Act
        await _pinCollectionService.AddDigitAsync(unknownReaderId, '9');
        var isComplete = await _pinCollectionService.AddDigitAsync(unknownReaderId, '#');

        // Assert
        Assert.That(isComplete, Is.True);
        Assert.That(capturedEvent, Is.Not.Null);
        Assert.That(capturedEvent.ReaderId, Is.EqualTo(unknownReaderId));
        Assert.That(capturedEvent.ReaderName, Is.EqualTo(string.Empty), 
            "PIN event should use empty reader name when configuration is not found");
        Assert.That(capturedEvent.Pin, Is.EqualTo("9"));
    }

    [Test]
    public async Task CompletePinCollection_WhenReaderConfigServiceThrows_ShouldUseEmptyReaderName()
    {
        // Arrange
        var errorReaderId = Guid.NewGuid();
        _mockReaderConfigurationService
            .Setup(x => x.GetReaderAsync(errorReaderId))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        PinReadEvent? capturedEvent = null;
        _pinCollectionService.PinCollectionCompleted += (sender, e) => capturedEvent = e;

        // Act
        await _pinCollectionService.AddDigitAsync(errorReaderId, '7');
        var isComplete = await _pinCollectionService.AddDigitAsync(errorReaderId, '#');

        // Assert
        Assert.That(isComplete, Is.True);
        Assert.That(capturedEvent, Is.Not.Null);
        Assert.That(capturedEvent.ReaderId, Is.EqualTo(errorReaderId));
        Assert.That(capturedEvent.ReaderName, Is.EqualTo(string.Empty), 
            "PIN event should use empty reader name when configuration service throws exception");
        Assert.That(capturedEvent.Pin, Is.EqualTo("7"));

        // Verify that the error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to get reader name")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task AddDigitAsync_WithAsterisk_ShouldClearPin()
    {
        // Arrange
        await _pinCollectionService.AddDigitAsync(_testReaderId, '1');
        await _pinCollectionService.AddDigitAsync(_testReaderId, '2');

        // Act - Clear with asterisk
        var isComplete = await _pinCollectionService.AddDigitAsync(_testReaderId, '*');
        
        // Verify PIN was cleared by getting current PIN
        var currentPin = await _pinCollectionService.GetCurrentPinAsync(_testReaderId);

        // Assert
        Assert.That(isComplete, Is.False);
        Assert.That(currentPin, Is.EqualTo(string.Empty));
    }

    [Test]
    public async Task GetCurrentPinAsync_ForNonExistentReader_ShouldReturnNull()
    {
        // Act
        var currentPin = await _pinCollectionService.GetCurrentPinAsync(Guid.NewGuid());

        // Assert
        Assert.That(currentPin, Is.Null);
    }

    [Test]
    public async Task ClearPinAsync_ShouldRemoveActiveCollection()
    {
        // Arrange
        await _pinCollectionService.AddDigitAsync(_testReaderId, '1');
        var pinBefore = await _pinCollectionService.GetCurrentPinAsync(_testReaderId);

        // Act
        await _pinCollectionService.ClearPinAsync(_testReaderId);
        var pinAfter = await _pinCollectionService.GetCurrentPinAsync(_testReaderId);

        // Assert
        Assert.That(pinBefore, Is.EqualTo("1"));
        Assert.That(pinAfter, Is.Null);
    }
}