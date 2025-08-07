using ApBox.Core.Data.Models;
using ApBox.Core.Models;

namespace ApBox.Core.Tests.Data.Models;

[TestFixture]
public class ReaderConfigurationEntityTests
{
    [Test]
    public void ToReaderConfiguration_WithValidData_MapsCorrectly()
    {
        // Arrange
        var readerId = Guid.NewGuid();
        var secureKey = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 });
        var createdAt = DateTime.UtcNow.AddDays(-1);
        var updatedAt = DateTime.UtcNow;

        var entity = new ReaderConfigurationEntity
        {
            ReaderId = readerId.ToString(),
            ReaderName = "Test Reader",
            Address = 5,
            SerialPort = "COM3",
            BaudRate = 19200,
            SecurityMode = 2, // Secure
            SecureChannelKey = secureKey,
            IsEnabled = false,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        // Act
        var result = entity.ToReaderConfiguration();

        // Assert
        Assert.That(result.ReaderId, Is.EqualTo(readerId));
        Assert.That(result.ReaderName, Is.EqualTo("Test Reader"));
        Assert.That(result.Address, Is.EqualTo(5));
        Assert.That(result.SerialPort, Is.EqualTo("COM3"));
        Assert.That(result.BaudRate, Is.EqualTo(19200));
        Assert.That(result.SecurityMode, Is.EqualTo(OsdpSecurityMode.Secure));
        Assert.That(result.SecureChannelKey, Is.Not.Null);
        Assert.That(result.SecureChannelKey!.Length, Is.EqualTo(16));
        Assert.That(result.IsEnabled, Is.False);
        Assert.That(result.CreatedAt, Is.EqualTo(createdAt));
        Assert.That(result.UpdatedAt, Is.EqualTo(updatedAt));
    }

    [Test]
    public void ToReaderConfiguration_WithNullSecureKey_ReturnsNullKey()
    {
        // Arrange
        var entity = new ReaderConfigurationEntity
        {
            ReaderId = Guid.NewGuid().ToString(),
            ReaderName = "Test Reader",
            SecureChannelKey = null
        };

        // Act
        var result = entity.ToReaderConfiguration();

        // Assert
        Assert.That(result.SecureChannelKey, Is.Null);
    }

    [Test]
    public void ToReaderConfiguration_WithEmptySecureKey_ReturnsNullKey()
    {
        // Arrange
        var entity = new ReaderConfigurationEntity
        {
            ReaderId = Guid.NewGuid().ToString(),
            ReaderName = "Test Reader",
            SecureChannelKey = string.Empty
        };

        // Act
        var result = entity.ToReaderConfiguration();

        // Assert
        Assert.That(result.SecureChannelKey, Is.Null);
    }

    [Test]
    public void ToReaderConfiguration_WithInvalidGuid_ThrowsException()
    {
        // Arrange
        var entity = new ReaderConfigurationEntity
        {
            ReaderId = "invalid-guid",
            ReaderName = "Test Reader"
        };

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => entity.ToReaderConfiguration());
        Assert.That(ex.Message, Does.Contain("Invalid GUID format"));
    }

    [Test]
    public void FromReaderConfiguration_WithValidData_MapsCorrectly()
    {
        // Arrange
        var readerId = Guid.NewGuid();
        var secureKey = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
        var createdAt = DateTime.UtcNow.AddDays(-1);
        var updatedAt = DateTime.UtcNow;

        var config = new ReaderConfiguration
        {
            ReaderId = readerId,
            ReaderName = "Test Reader",
            Address = 5,
            SerialPort = "COM3",
            BaudRate = 19200,
            SecurityMode = OsdpSecurityMode.Secure,
            SecureChannelKey = secureKey,
            IsEnabled = false,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        // Act
        var result = ReaderConfigurationEntity.FromReaderConfiguration(config);

        // Assert
        Assert.That(result.ReaderId, Is.EqualTo(readerId.ToString()));
        Assert.That(result.ReaderName, Is.EqualTo("Test Reader"));
        Assert.That(result.Address, Is.EqualTo(5));
        Assert.That(result.SerialPort, Is.EqualTo("COM3"));
        Assert.That(result.BaudRate, Is.EqualTo(19200));
        Assert.That(result.SecurityMode, Is.EqualTo(2));
        Assert.That(result.SecureChannelKey, Is.Not.Null);
        Assert.That(result.IsEnabled, Is.False);
        Assert.That(result.CreatedAt, Is.EqualTo(createdAt));
        Assert.That(result.UpdatedAt, Is.EqualTo(updatedAt));
    }

    [Test]
    public void FromReaderConfiguration_WithNullSecureKey_ReturnsNullKey()
    {
        // Arrange
        var config = new ReaderConfiguration
        {
            ReaderId = Guid.NewGuid(),
            ReaderName = "Test Reader",
            SecureChannelKey = null
        };

        // Act
        var result = ReaderConfigurationEntity.FromReaderConfiguration(config);

        // Assert
        Assert.That(result.SecureChannelKey, Is.Null);
    }

    [Test]
    public void RoundTrip_PreservesAllData()
    {
        // Arrange
        var originalConfig = new ReaderConfiguration
        {
            ReaderId = Guid.NewGuid(),
            ReaderName = "Test Reader",
            Address = 5,
            SerialPort = "/dev/ttyUSB0",
            BaudRate = 115200,
            SecurityMode = OsdpSecurityMode.Install,
            SecureChannelKey = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 },
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var entity = ReaderConfigurationEntity.FromReaderConfiguration(originalConfig);
        var roundTripConfig = entity.ToReaderConfiguration();

        // Assert
        Assert.That(roundTripConfig.ReaderId, Is.EqualTo(originalConfig.ReaderId));
        Assert.That(roundTripConfig.ReaderName, Is.EqualTo(originalConfig.ReaderName));
        Assert.That(roundTripConfig.Address, Is.EqualTo(originalConfig.Address));
        Assert.That(roundTripConfig.SerialPort, Is.EqualTo(originalConfig.SerialPort));
        Assert.That(roundTripConfig.BaudRate, Is.EqualTo(originalConfig.BaudRate));
        Assert.That(roundTripConfig.SecurityMode, Is.EqualTo(originalConfig.SecurityMode));
        Assert.That(roundTripConfig.SecureChannelKey, Is.EqualTo(originalConfig.SecureChannelKey));
        Assert.That(roundTripConfig.IsEnabled, Is.EqualTo(originalConfig.IsEnabled));
        Assert.That(roundTripConfig.CreatedAt, Is.EqualTo(originalConfig.CreatedAt));
        Assert.That(roundTripConfig.UpdatedAt, Is.EqualTo(originalConfig.UpdatedAt));
    }

    [Test]
    public void SecurityMode_Mapping_WorksForAllValues()
    {
        // Test ClearText (0)
        var clearTextConfig = new ReaderConfiguration { SecurityMode = OsdpSecurityMode.ClearText };
        var clearTextEntity = ReaderConfigurationEntity.FromReaderConfiguration(clearTextConfig);
        Assert.That(clearTextEntity.SecurityMode, Is.EqualTo(0));
        Assert.That(clearTextEntity.ToReaderConfiguration().SecurityMode, Is.EqualTo(OsdpSecurityMode.ClearText));

        // Test Install (1)
        var installConfig = new ReaderConfiguration { SecurityMode = OsdpSecurityMode.Install };
        var installEntity = ReaderConfigurationEntity.FromReaderConfiguration(installConfig);
        Assert.That(installEntity.SecurityMode, Is.EqualTo(1));
        Assert.That(installEntity.ToReaderConfiguration().SecurityMode, Is.EqualTo(OsdpSecurityMode.Install));

        // Test Secure (2)
        var secureConfig = new ReaderConfiguration { SecurityMode = OsdpSecurityMode.Secure };
        var secureEntity = ReaderConfigurationEntity.FromReaderConfiguration(secureConfig);
        Assert.That(secureEntity.SecurityMode, Is.EqualTo(2));
        Assert.That(secureEntity.ToReaderConfiguration().SecurityMode, Is.EqualTo(OsdpSecurityMode.Secure));
    }
}