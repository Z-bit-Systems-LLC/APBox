using ApBox.Core.Data.Repositories;
using ApBox.Core.Models;
using ApBox.Core.Services.Events;
using ApBox.Core.Services.Infrastructure;
using ApBox.Core.Services.Security;
using Microsoft.Extensions.Logging;
using Moq;

namespace ApBox.Core.Tests.Services;

[TestFixture]
public class SecurityModeUpdateServiceTests
{
    private Mock<IReaderConfigurationRepository> _mockReaderRepository;
    private Mock<IOsdpSecurityService> _mockSecurityService;
    private Mock<IEventPublisher> _mockEventPublisher;
    private Mock<ILogger<SecurityModeUpdateService>> _mockLogger;
    private SecurityModeUpdateService _service;

    [SetUp]
    public void SetUp()
    {
        _mockReaderRepository = new Mock<IReaderConfigurationRepository>();
        _mockSecurityService = new Mock<IOsdpSecurityService>();
        _mockEventPublisher = new Mock<IEventPublisher>();
        _mockLogger = new Mock<ILogger<SecurityModeUpdateService>>();

        _service = new SecurityModeUpdateService(
            _mockReaderRepository.Object,
            _mockSecurityService.Object,
            _mockEventPublisher.Object,
            _mockLogger.Object);
    }

    [Test]
    public async Task UpdateSecurityModeAsync_WithValidReader_UpdatesDatabaseAndFiresEvent()
    {
        // Arrange
        var readerId = Guid.NewGuid();
        var secureChannelKey = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
        var reader = new ReaderConfiguration
        {
            ReaderId = readerId,
            ReaderName = "Test Reader",
            SerialPort = "COM1",
            BaudRate = 9600,
            Address = 1,
            SecurityMode = OsdpSecurityMode.Install,
            IsEnabled = true
        };

        _mockReaderRepository
            .Setup(x => x.GetByIdAsync(readerId))
            .ReturnsAsync(reader);

        _mockReaderRepository
            .Setup(x => x.UpdateAsync(It.IsAny<ReaderConfiguration>()))
            .ReturnsAsync((ReaderConfiguration r) => r);

        // Act
        var result = await _service.UpdateSecurityModeAsync(readerId, OsdpSecurityMode.Secure, secureChannelKey);

        // Assert
        Assert.That(result, Is.True);

        // Verify database update
        _mockReaderRepository.Verify(x => x.UpdateAsync(It.Is<ReaderConfiguration>(r =>
            r.ReaderId == readerId &&
            r.SecurityMode == OsdpSecurityMode.Secure &&
            r.SecureChannelKey == secureChannelKey)), Times.Once);

        // Verify event is published
        _mockEventPublisher.Verify(x => x.PublishAsync(It.Is<ReaderStatusChangedEvent>(e =>
            e.ReaderId == readerId &&
            e.ReaderName == "Test Reader" &&
            e.SecurityMode == OsdpSecurityMode.Secure &&
            e.IsOnline == true &&
            e.IsEnabled == true &&
            e.Status == "Security mode updated")), Times.Once);
    }

    [Test]
    public async Task UpdateSecurityModeAsync_WithNonExistentReader_ReturnsFalse()
    {
        // Arrange
        var readerId = Guid.NewGuid();

        _mockReaderRepository
            .Setup(x => x.GetByIdAsync(readerId))
            .ReturnsAsync((ReaderConfiguration?)null);

        // Act
        var result = await _service.UpdateSecurityModeAsync(readerId, OsdpSecurityMode.Secure, null);

        // Assert
        Assert.That(result, Is.False);

        // Verify no database update or event publishing
        _mockReaderRepository.Verify(x => x.UpdateAsync(It.IsAny<ReaderConfiguration>()), Times.Never);
        _mockEventPublisher.Verify(x => x.PublishAsync(It.IsAny<ReaderStatusChangedEvent>()), Times.Never);
    }

    [Test]
    public async Task UpdateSecurityModeAsync_WithDatabaseError_ReturnsFalseAndNoEvent()
    {
        // Arrange
        var readerId = Guid.NewGuid();
        var reader = new ReaderConfiguration
        {
            ReaderId = readerId,
            ReaderName = "Test Reader",
            SecurityMode = OsdpSecurityMode.Install,
            IsEnabled = true
        };

        _mockReaderRepository
            .Setup(x => x.GetByIdAsync(readerId))
            .ReturnsAsync(reader);

        _mockReaderRepository
            .Setup(x => x.UpdateAsync(It.IsAny<ReaderConfiguration>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _service.UpdateSecurityModeAsync(readerId, OsdpSecurityMode.Secure, null);

        // Assert
        Assert.That(result, Is.False);

        // Verify no event is published when database update fails
        _mockEventPublisher.Verify(x => x.PublishAsync(It.IsAny<ReaderStatusChangedEvent>()), Times.Never);
    }

    [Test]
    public async Task UpdateSecurityModeAsync_TransitionFromInstallToSecure_ReproducesIssue5Fix()
    {
        // This test validates the fix for GitHub Issue #5:
        // "Last PD added shows orange status not green status"
        //
        // SCENARIO: Multiple PDs configured with Install mode, last one gets security mode updated
        // EXPECTED: After security mode update, event is fired so UI gets refreshed with new status

        // Arrange - Simulate the last added PD that had the bug
        var lastAddedReaderId = Guid.NewGuid();
        var secureChannelKey = new byte[16]; // Generated secure channel key
        var lastAddedReader = new ReaderConfiguration
        {
            ReaderId = lastAddedReaderId,
            ReaderName = "PD3 (Last Added)",
            SerialPort = "COM3",
            BaudRate = 9600,
            Address = 3,
            SecurityMode = OsdpSecurityMode.Install, // Currently in Install mode (shows Orange)
            IsEnabled = true
        };

        _mockReaderRepository
            .Setup(x => x.GetByIdAsync(lastAddedReaderId))
            .ReturnsAsync(lastAddedReader);

        // Act - Update security mode from Install to Secure (should trigger UI update)
        var result = await _service.UpdateSecurityModeAsync(
            lastAddedReaderId, 
            OsdpSecurityMode.Secure, 
            secureChannelKey);

        // Assert - Verify the fix works
        Assert.That(result, Is.True, "Security mode update should succeed");

        // Verify database is updated with new security mode
        _mockReaderRepository.Verify(x => x.UpdateAsync(It.Is<ReaderConfiguration>(r =>
            r.ReaderId == lastAddedReaderId &&
            r.SecurityMode == OsdpSecurityMode.Secure)), Times.Once, 
            "Database should be updated with Secure mode");

        // Verify event is published to trigger UI refresh (this is the key fix)
        _mockEventPublisher.Verify(x => x.PublishAsync(It.Is<ReaderStatusChangedEvent>(e =>
            e.ReaderId == lastAddedReaderId &&
            e.SecurityMode == OsdpSecurityMode.Secure &&
            e.Status == "Security mode updated")), Times.Once,
            "ReaderStatusChangedEvent should be published to refresh UI with new security mode");
    }

    [Test]
    public async Task UpdateSecurityModeAsync_WithoutSecureChannelKey_UpdatesSecurityModeOnly()
    {
        // Arrange
        var readerId = Guid.NewGuid();
        var reader = new ReaderConfiguration
        {
            ReaderId = readerId,
            ReaderName = "Test Reader",
            SecurityMode = OsdpSecurityMode.ClearText,
            SecureChannelKey = null,
            IsEnabled = true
        };

        _mockReaderRepository
            .Setup(x => x.GetByIdAsync(readerId))
            .ReturnsAsync(reader);

        // Act
        var result = await _service.UpdateSecurityModeAsync(readerId, OsdpSecurityMode.Install, null);

        // Assert
        Assert.That(result, Is.True);

        // Verify security mode is updated but key remains null
        _mockReaderRepository.Verify(x => x.UpdateAsync(It.Is<ReaderConfiguration>(r =>
            r.ReaderId == readerId &&
            r.SecurityMode == OsdpSecurityMode.Install &&
            r.SecureChannelKey == null)), Times.Once);

        // Verify event is still published
        _mockEventPublisher.Verify(x => x.PublishAsync(It.IsAny<ReaderStatusChangedEvent>()), Times.Once);
    }
}