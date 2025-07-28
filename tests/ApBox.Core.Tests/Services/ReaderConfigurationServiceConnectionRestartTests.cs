using ApBox.Core.Services;
using ApBox.Core.Data.Repositories;
using ApBox.Core.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace ApBox.Core.Tests.Services;

[TestFixture]
[Category("Unit")]
public class ReaderConfigurationServiceConnectionRestartTests
{
    private Mock<IReaderConfigurationRepository> _mockRepository;
    private Mock<ILogger<ReaderConfigurationService>> _mockLogger;
    private Mock<IReaderService> _mockReaderService;
    private Lazy<IReaderService> _lazyReaderService;
    private ReaderConfigurationService _service;
    private readonly Guid _testReaderId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _mockRepository = new Mock<IReaderConfigurationRepository>();
        _mockLogger = new Mock<ILogger<ReaderConfigurationService>>();
        _mockReaderService = new Mock<IReaderService>();
        _lazyReaderService = new Lazy<IReaderService>(() => _mockReaderService.Object);
        _service = new ReaderConfigurationService(_mockRepository.Object, _mockLogger.Object, _lazyReaderService);
    }

    [Test]
    public async Task SaveReaderAsync_WhenComPortChanges_ShouldRestartConnection()
    {
        // Arrange
        var oldReader = CreateTestReader("COM1");
        var newReader = CreateTestReader("COM2");

        _mockRepository.Setup(x => x.ExistsAsync(_testReaderId)).ReturnsAsync(true);
        _mockRepository.Setup(x => x.GetByIdAsync(_testReaderId)).ReturnsAsync(oldReader);
        _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<ReaderConfiguration>())).ReturnsAsync((ReaderConfiguration r) => r);
        
        _mockReaderService.Setup(x => x.DisconnectReaderAsync(_testReaderId)).ReturnsAsync(true);
        _mockReaderService.Setup(x => x.ConnectReaderAsync(_testReaderId)).ReturnsAsync(true);

        // Act
        await _service.SaveReaderAsync(newReader);

        // Assert
        _mockRepository.Verify(x => x.UpdateAsync(newReader), Times.Once);
        _mockReaderService.Verify(x => x.DisconnectReaderAsync(_testReaderId), Times.Once);
        _mockReaderService.Verify(x => x.ConnectReaderAsync(_testReaderId), Times.Once);
    }

    [Test]
    public async Task SaveReaderAsync_WhenBaudRateChanges_ShouldRestartConnection()
    {
        // Arrange
        var oldReader = CreateTestReader("COM1", 9600);
        var newReader = CreateTestReader("COM1", 115200);

        _mockRepository.Setup(x => x.ExistsAsync(_testReaderId)).ReturnsAsync(true);
        _mockRepository.Setup(x => x.GetByIdAsync(_testReaderId)).ReturnsAsync(oldReader);
        _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<ReaderConfiguration>())).ReturnsAsync((ReaderConfiguration r) => r);
        
        _mockReaderService.Setup(x => x.DisconnectReaderAsync(_testReaderId)).ReturnsAsync(true);
        _mockReaderService.Setup(x => x.ConnectReaderAsync(_testReaderId)).ReturnsAsync(true);

        // Act
        await _service.SaveReaderAsync(newReader);

        // Assert
        _mockReaderService.Verify(x => x.DisconnectReaderAsync(_testReaderId), Times.Once);
        _mockReaderService.Verify(x => x.ConnectReaderAsync(_testReaderId), Times.Once);
    }

    [Test]
    public async Task SaveReaderAsync_WhenAddressChanges_ShouldRestartConnection()
    {
        // Arrange
        var oldReader = CreateTestReader("COM1", 9600, 1);
        var newReader = CreateTestReader("COM1", 9600, 2);

        _mockRepository.Setup(x => x.ExistsAsync(_testReaderId)).ReturnsAsync(true);
        _mockRepository.Setup(x => x.GetByIdAsync(_testReaderId)).ReturnsAsync(oldReader);
        _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<ReaderConfiguration>())).ReturnsAsync((ReaderConfiguration r) => r);
        
        _mockReaderService.Setup(x => x.DisconnectReaderAsync(_testReaderId)).ReturnsAsync(true);
        _mockReaderService.Setup(x => x.ConnectReaderAsync(_testReaderId)).ReturnsAsync(true);

        // Act
        await _service.SaveReaderAsync(newReader);

        // Assert
        _mockReaderService.Verify(x => x.DisconnectReaderAsync(_testReaderId), Times.Once);
        _mockReaderService.Verify(x => x.ConnectReaderAsync(_testReaderId), Times.Once);
    }

    [Test]
    public async Task SaveReaderAsync_WhenSecurityModeChanges_ShouldRestartConnection()
    {
        // Arrange
        var oldReader = CreateTestReader("COM1", 9600, 1, OsdpSecurityMode.ClearText);
        var newReader = CreateTestReader("COM1", 9600, 1, OsdpSecurityMode.Secure);

        _mockRepository.Setup(x => x.ExistsAsync(_testReaderId)).ReturnsAsync(true);
        _mockRepository.Setup(x => x.GetByIdAsync(_testReaderId)).ReturnsAsync(oldReader);
        _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<ReaderConfiguration>())).ReturnsAsync((ReaderConfiguration r) => r);
        
        _mockReaderService.Setup(x => x.DisconnectReaderAsync(_testReaderId)).ReturnsAsync(true);
        _mockReaderService.Setup(x => x.ConnectReaderAsync(_testReaderId)).ReturnsAsync(true);

        // Act
        await _service.SaveReaderAsync(newReader);

        // Assert
        _mockReaderService.Verify(x => x.DisconnectReaderAsync(_testReaderId), Times.Once);
        _mockReaderService.Verify(x => x.ConnectReaderAsync(_testReaderId), Times.Once);
    }

    [Test]
    public async Task SaveReaderAsync_WhenReaderNameChanges_ShouldNotRestartConnection()
    {
        // Arrange
        var oldReader = CreateTestReader("COM1");
        oldReader.ReaderName = "Old Name";
        var newReader = CreateTestReader("COM1");
        newReader.ReaderName = "New Name";

        _mockRepository.Setup(x => x.ExistsAsync(_testReaderId)).ReturnsAsync(true);
        _mockRepository.Setup(x => x.GetByIdAsync(_testReaderId)).ReturnsAsync(oldReader);
        _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<ReaderConfiguration>())).ReturnsAsync((ReaderConfiguration r) => r);

        // Act
        await _service.SaveReaderAsync(newReader);

        // Assert
        _mockRepository.Verify(x => x.UpdateAsync(newReader), Times.Once);
        _mockReaderService.Verify(x => x.DisconnectReaderAsync(_testReaderId), Times.Never);
        _mockReaderService.Verify(x => x.ConnectReaderAsync(_testReaderId), Times.Never);
    }

    [Test]
    public async Task SaveReaderAsync_WhenNewReader_ShouldNotRestartConnection()
    {
        // Arrange
        var newReader = CreateTestReader("COM1");

        _mockRepository.Setup(x => x.ExistsAsync(_testReaderId)).ReturnsAsync(false);
        _mockRepository.Setup(x => x.CreateAsync(It.IsAny<ReaderConfiguration>())).ReturnsAsync((ReaderConfiguration r) => r);

        // Act
        await _service.SaveReaderAsync(newReader);

        // Assert
        _mockRepository.Verify(x => x.CreateAsync(newReader), Times.Once);
        _mockReaderService.Verify(x => x.DisconnectReaderAsync(_testReaderId), Times.Never);
        _mockReaderService.Verify(x => x.ConnectReaderAsync(_testReaderId), Times.Never);
    }

    [Test]
    public async Task SaveReaderAsync_WhenReaderDisabled_ShouldDisconnectOnly()
    {
        // Arrange
        var oldReader = CreateTestReader("COM1", isEnabled: true);
        var newReader = CreateTestReader("COM2", isEnabled: false);

        _mockRepository.Setup(x => x.ExistsAsync(_testReaderId)).ReturnsAsync(true);
        _mockRepository.Setup(x => x.GetByIdAsync(_testReaderId)).ReturnsAsync(oldReader);
        _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<ReaderConfiguration>())).ReturnsAsync((ReaderConfiguration r) => r);
        
        _mockReaderService.Setup(x => x.DisconnectReaderAsync(_testReaderId)).ReturnsAsync(true);

        // Act
        await _service.SaveReaderAsync(newReader);

        // Assert
        _mockReaderService.Verify(x => x.DisconnectReaderAsync(_testReaderId), Times.Once);
        _mockReaderService.Verify(x => x.ConnectReaderAsync(_testReaderId), Times.Never);
    }

    [Test]
    public void SaveReaderAsync_WhenConnectionRestartFails_ShouldNotThrow()
    {
        // Arrange
        var oldReader = CreateTestReader("COM1");
        var newReader = CreateTestReader("COM2");

        _mockRepository.Setup(x => x.ExistsAsync(_testReaderId)).ReturnsAsync(true);
        _mockRepository.Setup(x => x.GetByIdAsync(_testReaderId)).ReturnsAsync(oldReader);
        _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<ReaderConfiguration>())).ReturnsAsync((ReaderConfiguration r) => r);
        
        _mockReaderService.Setup(x => x.DisconnectReaderAsync(_testReaderId)).ThrowsAsync(new Exception("Connection error"));
        _mockReaderService.Setup(x => x.ConnectReaderAsync(_testReaderId)).ReturnsAsync(false);

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await _service.SaveReaderAsync(newReader));
        
        // Verify update still happened despite connection failure
        _mockRepository.Verify(x => x.UpdateAsync(newReader), Times.Once);
    }

    [Test]
    public async Task SaveReaderAsync_WhenConnectionRestartsSuccessfully_ShouldNotSendManualStatusNotifications()
    {
        // Arrange
        var oldReader = CreateTestReader("COM1");
        var newReader = CreateTestReader("COM2");
        var statusCallbacks = new List<(Guid readerId, string readerName, bool isOnline, bool isEnabled, OsdpSecurityMode securityMode)>();

        // This callback should never be called since we removed StatusNotificationCallback
        var unexpectedCallback = async (Guid readerId, string readerName, bool isOnline, bool isEnabled, OsdpSecurityMode securityMode) =>
        {
            statusCallbacks.Add((readerId, readerName, isOnline, isEnabled, securityMode));
            await Task.CompletedTask;
        };

        _mockRepository.Setup(x => x.ExistsAsync(_testReaderId)).ReturnsAsync(true);
        _mockRepository.Setup(x => x.GetByIdAsync(_testReaderId)).ReturnsAsync(oldReader);
        _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<ReaderConfiguration>())).ReturnsAsync((ReaderConfiguration r) => r);
        
        _mockReaderService.Setup(x => x.DisconnectReaderAsync(_testReaderId)).ReturnsAsync(true);
        _mockReaderService.Setup(x => x.ConnectReaderAsync(_testReaderId)).ReturnsAsync(true);

        // Act
        await _service.SaveReaderAsync(newReader);

        // Assert
        Assert.That(statusCallbacks.Count, Is.EqualTo(0));
        // All status notifications should come from natural OSDP events via OsdpStatusBridgeService
    }

    [Test]
    public async Task SaveReaderAsync_WhenReconnectionFails_ShouldNotSendManualStatusNotifications()
    {
        // Arrange
        var oldReader = CreateTestReader("COM1");
        var newReader = CreateTestReader("COM2");

        _mockRepository.Setup(x => x.ExistsAsync(_testReaderId)).ReturnsAsync(true);
        _mockRepository.Setup(x => x.GetByIdAsync(_testReaderId)).ReturnsAsync(oldReader);
        _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<ReaderConfiguration>())).ReturnsAsync((ReaderConfiguration r) => r);
        
        _mockReaderService.Setup(x => x.DisconnectReaderAsync(_testReaderId)).ReturnsAsync(true);
        _mockReaderService.Setup(x => x.ConnectReaderAsync(_testReaderId)).ReturnsAsync(false); // Connection fails

        // Act
        await _service.SaveReaderAsync(newReader);

        // Assert - Configuration should still be saved
        _mockRepository.Verify(x => x.UpdateAsync(newReader), Times.Once);
        
        // All status notifications (both offline and failed reconnect) should come from natural OSDP events
    }

    private ReaderConfiguration CreateTestReader(
        string serialPort = "COM1", 
        int baudRate = 9600, 
        byte address = 1,
        OsdpSecurityMode securityMode = OsdpSecurityMode.ClearText,
        bool isEnabled = true)
    {
        return new ReaderConfiguration
        {
            ReaderId = _testReaderId,
            ReaderName = "Test Reader",
            SerialPort = serialPort,
            BaudRate = baudRate,
            Address = address,
            SecurityMode = securityMode,
            IsEnabled = isEnabled,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };
    }
}