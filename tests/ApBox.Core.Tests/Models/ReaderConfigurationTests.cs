using ApBox.Core.Models;
using NUnit.Framework;

namespace ApBox.Core.Tests.Models;

[TestFixture]
public class ReaderConfigurationTests
{
    [Test]
    public void ReaderConfiguration_DefaultValues_AreSetCorrectly()
    {
        // Act
        var config = new ReaderConfiguration();

        // Assert
        Assert.That(config.ReaderId, Is.EqualTo(Guid.Empty)); // Default constructor doesn't auto-generate GUID
        Assert.That(config.ReaderName, Is.EqualTo(string.Empty));
        Assert.That(config.Address, Is.EqualTo(0));
        Assert.That(config.IsEnabled, Is.True);
        Assert.That(config.SerialPort, Is.Null);
        Assert.That(config.BaudRate, Is.EqualTo(9600));
        Assert.That(config.SecurityMode, Is.EqualTo(OsdpSecurityMode.ClearText));
        Assert.That(config.SecureChannelKey, Is.Null);
        Assert.That(config.CreatedAt, Is.LessThanOrEqualTo(DateTime.UtcNow));
        Assert.That(config.UpdatedAt, Is.LessThanOrEqualTo(DateTime.UtcNow));
    }

    [Test]
    public void ReaderConfiguration_WithOsdpProperties_CanBeSet()
    {
        // Arrange
        var readerId = Guid.NewGuid();
        var secureKey = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };

        // Act
        var config = new ReaderConfiguration
        {
            ReaderId = readerId,
            ReaderName = "Test Reader",
            Address = 5,
            SerialPort = "COM3",
            BaudRate = 19200,
            SecurityMode = OsdpSecurityMode.Secure,
            SecureChannelKey = secureKey,
            IsEnabled = false
        };

        // Assert
        Assert.That(config.ReaderId, Is.EqualTo(readerId));
        Assert.That(config.ReaderName, Is.EqualTo("Test Reader"));
        Assert.That(config.Address, Is.EqualTo(5));
        Assert.That(config.SerialPort, Is.EqualTo("COM3"));
        Assert.That(config.BaudRate, Is.EqualTo(19200));
        Assert.That(config.SecurityMode, Is.EqualTo(OsdpSecurityMode.Secure));
        Assert.That(config.SecureChannelKey, Is.EqualTo(secureKey));
        Assert.That(config.IsEnabled, Is.False);
    }

    [Test]
    public void ReaderConfiguration_SecurityModeEnum_HasCorrectValues()
    {
        // Assert
        Assert.That((int)OsdpSecurityMode.ClearText, Is.EqualTo(0));
        Assert.That((int)OsdpSecurityMode.Install, Is.EqualTo(1));
        Assert.That((int)OsdpSecurityMode.Secure, Is.EqualTo(2));
    }

    [Test]
    public void ReaderConfiguration_WithLinuxSerialPort_IsValid()
    {
        // Act
        var config = new ReaderConfiguration
        {
            SerialPort = "/dev/ttyUSB0",
            BaudRate = 115200
        };

        // Assert
        Assert.That(config.SerialPort, Is.EqualTo("/dev/ttyUSB0"));
        Assert.That(config.BaudRate, Is.EqualTo(115200));
    }

    [Test]
    public void ReaderConfiguration_WithNullSerialPort_IsValid()
    {
        // Act
        var config = new ReaderConfiguration
        {
            SerialPort = null
        };

        // Assert
        Assert.That(config.SerialPort, Is.Null);
    }

    // Poll interval test removed - using default OSDP bus timing
}