using ApBox.Core.Extensions;
using ApBox.Core.Models;
using ApBox.Core.Services.Security;
using Moq;
using NUnit.Framework;

namespace ApBox.Core.Tests.Extensions;

[TestFixture]
public class ReaderConfigurationExtensionsTests
{
    private Mock<IOsdpSecurityService> _mockSecurityService = null!;

    [SetUp]
    public void SetUp()
    {
        _mockSecurityService = new Mock<IOsdpSecurityService>();
    }

    [Test]
    public void ToOsdpConfiguration_WithBasicProperties_MapsCorrectly()
    {
        // Arrange
        var readerId = Guid.NewGuid();
        var reader = new ReaderConfiguration
        {
            ReaderId = readerId,
            ReaderName = "Test Reader",
            Address = 5,
            SerialPort = "COM3",
            BaudRate = 19200,
            SecurityMode = OsdpSecurityMode.ClearText,
            IsEnabled = true
        };

        _mockSecurityService.Setup(s => s.GetSecurityKey(OsdpSecurityMode.ClearText, null))
            .Returns((byte[]?)null);

        // Act
        var result = reader.ToOsdpConfiguration(_mockSecurityService.Object);

        // Assert
        Assert.That(result.Id, Is.EqualTo(readerId));
        Assert.That(result.Name, Is.EqualTo("Test Reader"));
        Assert.That(result.Address, Is.EqualTo(5));
        Assert.That(result.ConnectionString, Is.EqualTo("COM3"));
        Assert.That(result.BaudRate, Is.EqualTo(19200));
        Assert.That(result.UseSecureChannel, Is.False);
        Assert.That(result.SecureChannelKey, Is.Null);
        Assert.That(result.IsEnabled, Is.True);
    }

    [Test]
    public void ToOsdpConfiguration_WithClearTextSecurity_DisablesSecureChannel()
    {
        // Arrange
        var reader = new ReaderConfiguration
        {
            SecurityMode = OsdpSecurityMode.ClearText
        };

        _mockSecurityService.Setup(s => s.GetSecurityKey(OsdpSecurityMode.ClearText, null))
            .Returns((byte[]?)null);

        // Act
        var result = reader.ToOsdpConfiguration(_mockSecurityService.Object);

        // Assert
        Assert.That(result.UseSecureChannel, Is.False);
        Assert.That(result.SecureChannelKey, Is.Null);
        _mockSecurityService.Verify(s => s.GetSecurityKey(OsdpSecurityMode.ClearText, null), Times.Once);
    }

    [Test]
    public void ToOsdpConfiguration_WithInstallSecurity_EnablesSecureChannelWithDefaultKey()
    {
        // Arrange
        var defaultKey = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
        var reader = new ReaderConfiguration
        {
            SecurityMode = OsdpSecurityMode.Install
        };

        _mockSecurityService.Setup(s => s.GetSecurityKey(OsdpSecurityMode.Install, null))
            .Returns(defaultKey);

        // Act
        var result = reader.ToOsdpConfiguration(_mockSecurityService.Object);

        // Assert
        Assert.That(result.UseSecureChannel, Is.True);
        Assert.That(result.SecureChannelKey, Is.EqualTo(defaultKey));
        _mockSecurityService.Verify(s => s.GetSecurityKey(OsdpSecurityMode.Install, null), Times.Once);
    }

    [Test]
    public void ToOsdpConfiguration_WithSecureMode_EnablesSecureChannelWithStoredKey()
    {
        // Arrange
        var storedKey = new byte[] { 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 };
        var reader = new ReaderConfiguration
        {
            SecurityMode = OsdpSecurityMode.Secure,
            SecureChannelKey = storedKey
        };

        _mockSecurityService.Setup(s => s.GetSecurityKey(OsdpSecurityMode.Secure, storedKey))
            .Returns(storedKey);

        // Act
        var result = reader.ToOsdpConfiguration(_mockSecurityService.Object);

        // Assert
        Assert.That(result.UseSecureChannel, Is.True);
        Assert.That(result.SecureChannelKey, Is.EqualTo(storedKey));
        _mockSecurityService.Verify(s => s.GetSecurityKey(OsdpSecurityMode.Secure, storedKey), Times.Once);
    }

    [Test]
    public void ToOsdpConfiguration_WithNullSerialPort_ReturnsEmptyConnectionString()
    {
        // Arrange
        var reader = new ReaderConfiguration
        {
            SerialPort = null
        };

        _mockSecurityService.Setup(s => s.GetSecurityKey(It.IsAny<OsdpSecurityMode>(), It.IsAny<byte[]>()))
            .Returns((byte[]?)null);

        // Act
        var result = reader.ToOsdpConfiguration(_mockSecurityService.Object);

        // Assert
        Assert.That(result.ConnectionString, Is.EqualTo(string.Empty));
    }

    [Test]
    public void ToOsdpConfiguration_WithLinuxSerialPort_MapsCorrectly()
    {
        // Arrange
        var reader = new ReaderConfiguration
        {
            SerialPort = "/dev/ttyUSB0",
            BaudRate = 115200
        };

        _mockSecurityService.Setup(s => s.GetSecurityKey(It.IsAny<OsdpSecurityMode>(), It.IsAny<byte[]>()))
            .Returns((byte[]?)null);

        // Act
        var result = reader.ToOsdpConfiguration(_mockSecurityService.Object);

        // Assert
        Assert.That(result.ConnectionString, Is.EqualTo("/dev/ttyUSB0"));
        Assert.That(result.BaudRate, Is.EqualTo(115200));
    }

    // Poll interval test removed - using default OSDP bus timing

    [Test]
    public void ToOsdpConfiguration_WithDisabledReader_MapsCorrectly()
    {
        // Arrange
        var reader = new ReaderConfiguration
        {
            IsEnabled = false
        };

        _mockSecurityService.Setup(s => s.GetSecurityKey(It.IsAny<OsdpSecurityMode>(), It.IsAny<byte[]>()))
            .Returns((byte[]?)null);

        // Act
        var result = reader.ToOsdpConfiguration(_mockSecurityService.Object);

        // Assert
        Assert.That(result.IsEnabled, Is.False);
    }
}