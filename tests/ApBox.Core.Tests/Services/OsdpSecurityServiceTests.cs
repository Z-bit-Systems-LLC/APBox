using ApBox.Core.Models;
using ApBox.Core.Services;
using NUnit.Framework;

namespace ApBox.Core.Tests.Services;

[TestFixture]
public class OsdpSecurityServiceTests
{
    private OsdpSecurityService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _service = new OsdpSecurityService();
    }

    [Test]
    public void GetSecurityKey_ClearTextMode_ReturnsNull()
    {
        // Act
        var result = _service.GetSecurityKey(OsdpSecurityMode.ClearText, null);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetSecurityKey_ClearTextMode_WithStoredKey_ReturnsNull()
    {
        // Arrange
        var storedKey = new byte[] { 1, 2, 3, 4 };

        // Act
        var result = _service.GetSecurityKey(OsdpSecurityMode.ClearText, storedKey);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetSecurityKey_InstallMode_ReturnsDefaultKey()
    {
        // Act
        var result = _service.GetSecurityKey(OsdpSecurityMode.Install, null);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Length, Is.EqualTo(16));
        
        // Verify it's the expected default OSDP key
        var expectedKey = new byte[] 
        { 
            0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37,
            0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F
        };
        Assert.That(result, Is.EqualTo(expectedKey));
    }

    [Test]
    public void GetSecurityKey_SecureMode_WithStoredKey_ReturnsStoredKey()
    {
        // Arrange
        var storedKey = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };

        // Act
        var result = _service.GetSecurityKey(OsdpSecurityMode.Secure, storedKey);

        // Assert
        Assert.That(result, Is.EqualTo(storedKey));
    }

    [Test]
    public void GetSecurityKey_SecureMode_WithoutStoredKey_ThrowsException()
    {
        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => 
            _service.GetSecurityKey(OsdpSecurityMode.Secure, null));
        
        Assert.That(ex.Message, Does.Contain("No secure key stored"));
    }

    [Test]
    public void GenerateRandomKey_ReturnsValidKey()
    {
        // Act
        var result = _service.GenerateRandomKey();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Length, Is.EqualTo(16));
    }

    [Test]
    public void GenerateRandomKey_GeneratesTwoDifferentKeys()
    {
        // Act
        var key1 = _service.GenerateRandomKey();
        var key2 = _service.GenerateRandomKey();

        // Assert
        Assert.That(key1, Is.Not.EqualTo(key2), "Generated keys should be different");
    }

    [Test]
    public void GetDefaultInstallationKey_ReturnsExpectedKey()
    {
        // Act
        var result = _service.GetDefaultInstallationKey();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Length, Is.EqualTo(16));
        
        var expectedKey = new byte[] 
        { 
            0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37,
            0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F
        };
        Assert.That(result, Is.EqualTo(expectedKey));
    }

    [Test]
    public void GetDefaultInstallationKey_ReturnsCopy()
    {
        // Act
        var key1 = _service.GetDefaultInstallationKey();
        var key2 = _service.GetDefaultInstallationKey();
        
        // Modify one key
        key1[0] = 0xFF;

        // Assert
        Assert.That(key2[0], Is.Not.EqualTo(0xFF), "Should return a copy, not the same array");
    }
}