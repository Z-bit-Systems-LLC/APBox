using Microsoft.Extensions.Logging;
using ApBox.SamplePlugins;
using Moq;

namespace ApBox.Plugins.Tests;

[TestFixture]
[Category("Unit")]
public class PinValidationPluginTests
{
    private PinValidationPlugin _plugin = null!;
    private Mock<ILogger<PinValidationPlugin>> _mockLogger = null!;

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<PinValidationPlugin>>();
        _plugin = new PinValidationPlugin(_mockLogger.Object);
    }

    [Test]
    public void Constructor_InitializesWithDefaultAuthorizedPins()
    {
        // Arrange & Act
        var plugin = new PinValidationPlugin();

        // Assert
        Assert.That(plugin.GetAuthorizedPinCount(), Is.EqualTo(6));
    }

    [Test]
    public void PluginProperties_ReturnExpectedValues()
    {
        // Assert
        Assert.That(_plugin.Id, Is.EqualTo(new Guid("B2C3D4E5-6789-ABCD-EF01-234567890123")));
        Assert.That(_plugin.Name, Is.EqualTo("PIN Validation Plugin"));
        Assert.That(_plugin.Version, Is.EqualTo("1.0.0"));
        Assert.That(_plugin.Description, Contains.Substring("6-digit PIN codes"));
    }

    [Test]
    public async Task InitializeAsync_LogsInitializationMessage()
    {
        // Act
        await _plugin.InitializeAsync();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("initialized with")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task ShutdownAsync_LogsShutdownMessage()
    {
        // Act
        await _plugin.ShutdownAsync();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("shutting down")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task ProcessCardReadAsync_AlwaysReturnsTrue()
    {
        // Arrange
        var cardRead = new CardReadEvent
        {
            CardNumber = "12345678",
            ReaderName = "Test Reader",
            Timestamp = DateTime.UtcNow
        };

        // Act
        var result = await _plugin.ProcessCardReadAsync(cardRead);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task ProcessPinReadAsync_WithValidAuthorizedPin_ReturnsTrue()
    {
        // Arrange
        var pinRead = new PinReadEvent
        {
            Pin = "123456", // This is one of the default authorized PINs
            ReaderName = "Test Reader",
            CompletionReason = PinCompletionReason.PoundKey,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var result = await _plugin.ProcessPinReadAsync(pinRead);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task ProcessPinReadAsync_WithUnauthorizedPin_ReturnsFalse()
    {
        // Arrange
        var pinRead = new PinReadEvent
        {
            Pin = "999999", // This is not in the default authorized PINs
            ReaderName = "Test Reader",
            CompletionReason = PinCompletionReason.PoundKey,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var result = await _plugin.ProcessPinReadAsync(pinRead);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    [TestCase("12345", "too short")]
    [TestCase("1234567", "too long")]
    [TestCase("", "empty")]
    public async Task ProcessPinReadAsync_WithInvalidLength_ReturnsFalse(string pin, string description)
    {
        // Arrange
        var pinRead = new PinReadEvent
        {
            Pin = pin,
            ReaderName = "Test Reader",
            CompletionReason = PinCompletionReason.PoundKey,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var result = await _plugin.ProcessPinReadAsync(pinRead);

        // Assert
        Assert.That(result, Is.False, $"PIN should be rejected when {description}");
    }

    [Test]
    [TestCase("12345a")]
    [TestCase("abcdef")]
    [TestCase("123-45")]
    [TestCase("123 45")]
    public async Task ProcessPinReadAsync_WithNonDigitCharacters_ReturnsFalse(string pin)
    {
        // Arrange
        var pinRead = new PinReadEvent
        {
            Pin = pin,
            ReaderName = "Test Reader",
            CompletionReason = PinCompletionReason.PoundKey,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var result = await _plugin.ProcessPinReadAsync(pinRead);

        // Assert
        Assert.That(result, Is.False, $"PIN '{pin}' should be rejected for containing non-digit characters");
    }

    [Test]
    public void AddAuthorizedPin_WithValidPin_ReturnsTrue()
    {
        // Arrange
        const string newPin = "777777";

        // Act
        var result = _plugin.AddAuthorizedPin(newPin);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(_plugin.IsAuthorizedPin(newPin), Is.True);
        Assert.That(_plugin.GetAuthorizedPinCount(), Is.EqualTo(7)); // 6 default + 1 new
    }

    [Test]
    public void AddAuthorizedPin_WithDuplicatePin_ReturnsFalse()
    {
        // Arrange
        const string existingPin = "123456"; // This is a default authorized PIN

        // Act
        var result = _plugin.AddAuthorizedPin(existingPin);

        // Assert
        Assert.That(result, Is.False);
        Assert.That(_plugin.GetAuthorizedPinCount(), Is.EqualTo(6)); // Should remain unchanged
    }

    [Test]
    [TestCase("12345", "too short")]
    [TestCase("1234567", "too long")]
    [TestCase("12345a", "contains letters")]
    [TestCase("", "empty")]
    [TestCase("   ", "whitespace")]
    [TestCase(null, "null")]
    public void AddAuthorizedPin_WithInvalidPin_ReturnsFalse(string? pin, string description)
    {
        // Act
        var result = _plugin.AddAuthorizedPin(pin!);

        // Assert
        Assert.That(result, Is.False, $"Should reject PIN when {description}");
        Assert.That(_plugin.GetAuthorizedPinCount(), Is.EqualTo(6)); // Should remain unchanged
    }

    [Test]
    public void RemoveAuthorizedPin_WithExistingPin_ReturnsTrue()
    {
        // Arrange
        const string existingPin = "123456"; // This is a default authorized PIN

        // Act
        var result = _plugin.RemoveAuthorizedPin(existingPin);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(_plugin.IsAuthorizedPin(existingPin), Is.False);
        Assert.That(_plugin.GetAuthorizedPinCount(), Is.EqualTo(5)); // 6 default - 1 removed
    }

    [Test]
    public void RemoveAuthorizedPin_WithNonExistentPin_ReturnsFalse()
    {
        // Arrange
        const string nonExistentPin = "999999";

        // Act
        var result = _plugin.RemoveAuthorizedPin(nonExistentPin);

        // Assert
        Assert.That(result, Is.False);
        Assert.That(_plugin.GetAuthorizedPinCount(), Is.EqualTo(6)); // Should remain unchanged
    }

    [Test]
    public void IsAuthorizedPin_WithAuthorizedPin_ReturnsTrue()
    {
        // Arrange
        const string authorizedPin = "123456"; // Default authorized PIN

        // Act & Assert
        Assert.That(_plugin.IsAuthorizedPin(authorizedPin), Is.True);
    }

    [Test]
    public void IsAuthorizedPin_WithUnauthorizedPin_ReturnsFalse()
    {
        // Arrange
        const string unauthorizedPin = "999999";

        // Act & Assert
        Assert.That(_plugin.IsAuthorizedPin(unauthorizedPin), Is.False);
    }

    [Test]
    public async Task ProcessPinReadAsync_LogsCorrectInformation()
    {
        // Arrange
        var pinRead = new PinReadEvent
        {
            Pin = "123456",
            ReaderName = "Test Reader",
            CompletionReason = PinCompletionReason.PoundKey,
            Timestamp = DateTime.UtcNow
        };

        // Act
        await _plugin.ProcessPinReadAsync(pinRead);

        // Assert - Verify processing log
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("processing PIN from reader")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Assert - Verify authorization log
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("is authorized - granting access")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}