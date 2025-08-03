using NUnit.Framework;
using Microsoft.Extensions.Logging;
using Moq;
using ApBox.Core.Services.Security;

namespace ApBox.Core.Tests.Services;

[TestFixture]
public class DataEncryptionServiceTests
{
    private IDataEncryptionService _encryptionService;
    private Mock<ILogger<DataEncryptionService>> _mockLogger;
    private Mock<IEncryptionKeyService> _mockKeyService;

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<DataEncryptionService>>();
        _mockKeyService = new Mock<IEncryptionKeyService>();
        
        // Setup mock key service to return a consistent key
        var testKey = new byte[32]; // All zeros for consistent testing
        _mockKeyService.Setup(x => x.GetEncryptionKeyAsync()).ReturnsAsync(testKey);
        
        _encryptionService = new DataEncryptionService(_mockKeyService.Object, _mockLogger.Object);
    }

    [Test]
    public void EncryptData_WithValidInput_ReturnsEncryptedString()
    {
        // Arrange
        var plainText = "1234";

        // Act
        var encrypted = _encryptionService.EncryptData(plainText);

        // Assert
        Assert.That(encrypted, Is.Not.Null);
        Assert.That(encrypted, Is.Not.Empty);
        Assert.That(encrypted, Is.Not.EqualTo(plainText));
        Assert.That(encrypted.Length, Is.GreaterThan(plainText.Length)); // Encrypted should be longer
    }

    [Test]
    public void DecryptData_WithValidEncryptedData_ReturnsOriginalString()
    {
        // Arrange
        var originalText = "5678";
        var encrypted = _encryptionService.EncryptData(originalText);

        // Act
        var decrypted = _encryptionService.DecryptData(encrypted);

        // Assert
        Assert.That(decrypted, Is.EqualTo(originalText));
    }

    [Test]
    public void EncryptDecrypt_RoundTrip_PreservesOriginalData()
    {
        // Arrange
        var testPins = new[] { "1234", "567890", "12", "999999999" };

        foreach (var pin in testPins)
        {
            // Act
            var encrypted = _encryptionService.EncryptData(pin);
            var decrypted = _encryptionService.DecryptData(encrypted);

            // Assert
            Assert.That(decrypted, Is.EqualTo(pin), $"Failed for PIN: {pin}");
        }
    }

    [Test]
    public void EncryptData_WithEmptyString_ReturnsEmptyString()
    {
        // Act
        var result = _encryptionService.EncryptData(string.Empty);

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void DecryptData_WithEmptyString_ReturnsEmptyString()
    {
        // Act
        var result = _encryptionService.DecryptData(string.Empty);

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void EncryptData_MultipleCallsSameInput_ProducesDifferentEncryption()
    {
        // Arrange
        var plainText = "1234";

        // Act
        var encrypted1 = _encryptionService.EncryptData(plainText);
        var encrypted2 = _encryptionService.EncryptData(plainText);

        // Assert
        Assert.That(encrypted1, Is.Not.EqualTo(encrypted2)); // Should be different due to random IV
        
        // But both should decrypt to the same value
        var decrypted1 = _encryptionService.DecryptData(encrypted1);
        var decrypted2 = _encryptionService.DecryptData(encrypted2);
        Assert.That(decrypted1, Is.EqualTo(plainText));
        Assert.That(decrypted2, Is.EqualTo(plainText));
    }
}