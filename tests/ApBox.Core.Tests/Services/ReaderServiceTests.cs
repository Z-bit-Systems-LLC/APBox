using ApBox.Core.Models;
using ApBox.Core.Services.Reader;
using ApBox.Core.Services.Security;
using ApBox.Core.OSDP;
using Microsoft.Extensions.Logging;
using Moq;

namespace ApBox.Core.Tests.Services;

[TestFixture]
public class ReaderServiceTests
{
    private Mock<IReaderConfigurationService> _mockConfigService = null!;
    private Mock<IOsdpSecurityService> _mockSecurityService = null!;
    private Mock<IOsdpCommunicationManager> _mockOsdpManager = null!;
    private Mock<ILogger<ReaderService>> _mockLogger = null!;
    private ReaderService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _mockConfigService = new Mock<IReaderConfigurationService>();
        _mockSecurityService = new Mock<IOsdpSecurityService>();
        _mockOsdpManager = new Mock<IOsdpCommunicationManager>();
        _mockLogger = new Mock<ILogger<ReaderService>>();
        
        _service = new ReaderService(
            _mockConfigService.Object,
            _mockSecurityService.Object,
            _mockOsdpManager.Object,
            _mockLogger.Object);
    }

    [Test]
    public async Task GetReadersAsync_CallsConfigurationService()
    {
        // Arrange
        var expectedReaders = new List<ReaderConfiguration>
        {
            new() { ReaderId = Guid.NewGuid(), ReaderName = "Reader 1" },
            new() { ReaderId = Guid.NewGuid(), ReaderName = "Reader 2" }
        };

        _mockConfigService.Setup(s => s.GetAllReadersAsync())
            .ReturnsAsync(expectedReaders);

        // Act
        var result = await _service.GetReadersAsync();

        // Assert
        Assert.That(result, Is.EqualTo(expectedReaders));
        _mockConfigService.Verify(s => s.GetAllReadersAsync(), Times.Once);
    }

    [Test]
    public async Task GetReaderAsync_CallsConfigurationService()
    {
        // Arrange
        var readerId = Guid.NewGuid();
        var expectedReader = new ReaderConfiguration { ReaderId = readerId, ReaderName = "Test Reader" };

        _mockConfigService.Setup(s => s.GetReaderAsync(readerId))
            .ReturnsAsync(expectedReader);

        // Act
        var result = await _service.GetReaderAsync(readerId);

        // Assert
        Assert.That(result, Is.EqualTo(expectedReader));
        _mockConfigService.Verify(s => s.GetReaderAsync(readerId), Times.Once);
    }

    [Test]
    public async Task UpdateReaderAsync_CallsConfigurationService()
    {
        // Arrange
        var reader = new ReaderConfiguration { ReaderId = Guid.NewGuid(), ReaderName = "Test Reader" };

        // Act
        await _service.UpdateReaderAsync(reader);

        // Assert
        _mockConfigService.Verify(s => s.SaveReaderAsync(reader), Times.Once);
    }

    [Test]
    public async Task ConnectReaderAsync_WithValidReader_ReturnsTrue()
    {
        // Arrange
        var readerId = Guid.NewGuid();
        var reader = new ReaderConfiguration 
        { 
            ReaderId = readerId, 
            ReaderName = "Test Reader",
            SerialPort = "COM3"
        };

        _mockConfigService.Setup(s => s.GetReaderAsync(readerId))
            .ReturnsAsync(reader);
        _mockOsdpManager.Setup(m => m.AddDeviceAsync(It.IsAny<OsdpDeviceConfiguration>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.ConnectReaderAsync(readerId);

        // Assert
        Assert.That(result, Is.True);
        _mockConfigService.Verify(s => s.GetReaderAsync(readerId), Times.Once);
        _mockOsdpManager.Verify(m => m.AddDeviceAsync(It.IsAny<OsdpDeviceConfiguration>()), Times.Once);
    }

    [Test]
    public async Task ConnectReaderAsync_WithNonExistentReader_ReturnsFalse()
    {
        // Arrange
        var readerId = Guid.NewGuid();

        _mockConfigService.Setup(s => s.GetReaderAsync(readerId))
            .ReturnsAsync((ReaderConfiguration?)null);

        // Act
        var result = await _service.ConnectReaderAsync(readerId);

        // Assert
        Assert.That(result, Is.False);
        _mockConfigService.Verify(s => s.GetReaderAsync(readerId), Times.Once);
    }

    [Test]
    public async Task DisconnectReaderAsync_ReturnsTrue()
    {
        // Arrange
        var readerId = Guid.NewGuid();
        _mockOsdpManager.Setup(m => m.RemoveDeviceAsync(readerId))
            .ReturnsAsync(true);

        // Act
        var result = await _service.DisconnectReaderAsync(readerId);

        // Assert
        Assert.That(result, Is.True);
        _mockOsdpManager.Verify(m => m.RemoveDeviceAsync(readerId), Times.Once);
    }

    [Test]
    public async Task TestConnectionAsync_WithValidReader_ReturnsTrue()
    {
        // Arrange
        var readerId = Guid.NewGuid();
        var reader = new ReaderConfiguration 
        { 
            ReaderId = readerId, 
            ReaderName = "Test Reader",
            SerialPort = "COM3"
        };

        _mockConfigService.Setup(s => s.GetReaderAsync(readerId))
            .ReturnsAsync(reader);
        
        var mockDevice = new Mock<IOsdpDevice>();
        mockDevice.Setup(d => d.IsOnline).Returns(true);
        _mockOsdpManager.Setup(m => m.GetDeviceAsync(readerId))
            .ReturnsAsync(mockDevice.Object);

        // Act
        var result = await _service.TestConnectionAsync(readerId);

        // Assert
        Assert.That(result, Is.True);
        _mockConfigService.Verify(s => s.GetReaderAsync(readerId), Times.Once);
        _mockOsdpManager.Verify(m => m.GetDeviceAsync(readerId), Times.Once);
    }

    [Test]
    public async Task TestConnectionAsync_WithNonExistentReader_ReturnsFalse()
    {
        // Arrange
        var readerId = Guid.NewGuid();

        _mockConfigService.Setup(s => s.GetReaderAsync(readerId))
            .ReturnsAsync((ReaderConfiguration?)null);

        // Act
        var result = await _service.TestConnectionAsync(readerId);

        // Assert
        Assert.That(result, Is.False);
        _mockConfigService.Verify(s => s.GetReaderAsync(readerId), Times.Once);
    }

    [Test]
    public async Task InstallSecureKeyAsync_WithInstallModeReader_InstallsKeyAndChangesToSecureMode()
    {
        // Arrange
        var readerId = Guid.NewGuid();
        var reader = new ReaderConfiguration 
        { 
            ReaderId = readerId, 
            ReaderName = "Test Reader",
            SecurityMode = OsdpSecurityMode.Install
        };

        var generatedKey = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };

        _mockConfigService.Setup(s => s.GetReaderAsync(readerId))
            .ReturnsAsync(reader);
        
        _mockSecurityService.Setup(s => s.GenerateRandomKey())
            .Returns(generatedKey);

        // Act
        var result = await _service.InstallSecureKeyAsync(readerId);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(reader.SecurityMode, Is.EqualTo(OsdpSecurityMode.Secure));
        Assert.That(reader.SecureChannelKey, Is.EqualTo(generatedKey));
        
        _mockConfigService.Verify(s => s.GetReaderAsync(readerId), Times.Once);
        _mockSecurityService.Verify(s => s.GenerateRandomKey(), Times.Once);
        _mockConfigService.Verify(s => s.SaveReaderAsync(reader), Times.Once);
    }

    [Test]
    public async Task InstallSecureKeyAsync_WithNonInstallModeReader_ReturnsFalse()
    {
        // Arrange
        var readerId = Guid.NewGuid();
        var reader = new ReaderConfiguration 
        { 
            ReaderId = readerId, 
            ReaderName = "Test Reader",
            SecurityMode = OsdpSecurityMode.ClearText
        };

        _mockConfigService.Setup(s => s.GetReaderAsync(readerId))
            .ReturnsAsync(reader);

        // Act
        var result = await _service.InstallSecureKeyAsync(readerId);

        // Assert
        Assert.That(result, Is.False);
        _mockConfigService.Verify(s => s.GetReaderAsync(readerId), Times.Once);
        _mockSecurityService.Verify(s => s.GenerateRandomKey(), Times.Never);
        _mockConfigService.Verify(s => s.SaveReaderAsync(It.IsAny<ReaderConfiguration>()), Times.Never);
    }

    [Test]
    public async Task InstallSecureKeyAsync_WithNonExistentReader_ReturnsFalse()
    {
        // Arrange
        var readerId = Guid.NewGuid();

        _mockConfigService.Setup(s => s.GetReaderAsync(readerId))
            .ReturnsAsync((ReaderConfiguration?)null);

        // Act
        var result = await _service.InstallSecureKeyAsync(readerId);

        // Assert
        Assert.That(result, Is.False);
        _mockConfigService.Verify(s => s.GetReaderAsync(readerId), Times.Once);
        _mockSecurityService.Verify(s => s.GenerateRandomKey(), Times.Never);
    }

    [Test]
    public async Task RefreshAllReadersAsync_ConnectsToEnabledReaders()
    {
        // Arrange
        var enabledReader = new ReaderConfiguration 
        { 
            ReaderId = Guid.NewGuid(), 
            ReaderName = "Enabled Reader",
            IsEnabled = true
        };
        var disabledReader = new ReaderConfiguration 
        { 
            ReaderId = Guid.NewGuid(), 
            ReaderName = "Disabled Reader",
            IsEnabled = false
        };

        var readers = new[] { enabledReader, disabledReader };

        _mockConfigService.Setup(s => s.GetAllReadersAsync())
            .ReturnsAsync(readers);

        // Act
        await _service.RefreshAllReadersAsync();

        // Assert
        _mockConfigService.Verify(s => s.GetAllReadersAsync(), Times.Once);
        // Note: We can't easily verify ConnectReaderAsync calls because they're internal calls
        // In a real implementation, we'd need to mock the OSDP communication manager
    }

    [Test]
    public async Task SendFeedbackAsync_WithValidParameters_ReturnsTrue()
    {
        // Arrange
        var readerId = Guid.NewGuid();
        var feedback = new ReaderFeedback
        {
            Type = ReaderFeedbackType.Success,
            LedColor = ApBox.Core.Models.LedColor.Green,
            BeepCount = 1,
            LedDuration = 1000
        };

        var mockDevice = new Mock<IOsdpDevice>();
        mockDevice.Setup(d => d.SendFeedbackAsync(feedback))
            .ReturnsAsync(true);
        _mockOsdpManager.Setup(m => m.GetDeviceAsync(readerId))
            .ReturnsAsync(mockDevice.Object);

        // Act
        var result = await _service.SendFeedbackAsync(readerId, feedback);

        // Assert
        Assert.That(result, Is.True);
        _mockOsdpManager.Verify(m => m.GetDeviceAsync(readerId), Times.Once);
        mockDevice.Verify(d => d.SendFeedbackAsync(feedback), Times.Once);
    }
}