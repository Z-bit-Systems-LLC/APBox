using ApBox.Core.Services.Reader;
using ApBox.Core.Services.Infrastructure;
using ApBox.Core.Data.Repositories;
using ApBox.Core.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace ApBox.Core.Tests.Services;

[TestFixture]
[Category("Unit")]
public class ReaderConfigurationServiceNotificationTests
{
    private Mock<IReaderConfigurationRepository> _mockRepository;
    private Mock<ILogger<ReaderConfigurationService>> _mockLogger;
    private Mock<IReaderService> _mockReaderService;
    private Mock<INotificationService> _mockNotificationService;
    private Lazy<IReaderService> _lazyReaderService;
    private ReaderConfigurationService _service;
    private readonly Guid _testReaderId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _mockRepository = new Mock<IReaderConfigurationRepository>();
        _mockLogger = new Mock<ILogger<ReaderConfigurationService>>();
        _mockReaderService = new Mock<IReaderService>();
        _mockNotificationService = new Mock<INotificationService>();
        _lazyReaderService = new Lazy<IReaderService>(() => _mockReaderService.Object);
        _service = new ReaderConfigurationService(_mockRepository.Object, _mockLogger.Object, _lazyReaderService, _mockNotificationService.Object);
    }

    [Test]
    public async Task SaveReaderAsync_WhenCreatingNewReader_ShouldBroadcastCreatedNotification()
    {
        // Arrange
        var newReader = CreateTestReader();
        _mockRepository.Setup(x => x.ExistsAsync(_testReaderId)).ReturnsAsync(false);
        _mockRepository.Setup(x => x.CreateAsync(It.IsAny<ReaderConfiguration>())).ReturnsAsync((ReaderConfiguration r) => r);

        // Act
        await _service.SaveReaderAsync(newReader);

        // Assert
        _mockNotificationService.Verify(x => x.BroadcastReaderConfigurationAsync(
            It.Is<ReaderConfiguration>(r => r.ReaderId == _testReaderId), 
            "Created"), Times.Once);
    }

    [Test]
    public async Task SaveReaderAsync_WhenUpdatingExistingReader_ShouldBroadcastUpdatedNotification()
    {
        // Arrange
        var existingReader = CreateTestReader();
        var updatedReader = CreateTestReader();
        updatedReader.ReaderName = "Updated Reader";

        _mockRepository.Setup(x => x.ExistsAsync(_testReaderId)).ReturnsAsync(true);
        _mockRepository.Setup(x => x.GetByIdAsync(_testReaderId)).ReturnsAsync(existingReader);
        _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<ReaderConfiguration>())).ReturnsAsync((ReaderConfiguration r) => r);

        // Act
        await _service.SaveReaderAsync(updatedReader);

        // Assert
        _mockNotificationService.Verify(x => x.BroadcastReaderConfigurationAsync(
            It.Is<ReaderConfiguration>(r => r.ReaderId == _testReaderId && r.ReaderName == "Updated Reader"), 
            "Updated"), Times.Once);
    }

    [Test]
    public async Task DeleteReaderAsync_WhenReaderExists_ShouldDisconnectFromOsdpAndBroadcastDeletedNotification()
    {
        // Arrange
        var readerToDelete = CreateTestReader();
        _mockRepository.Setup(x => x.GetByIdAsync(_testReaderId)).ReturnsAsync(readerToDelete);
        _mockRepository.Setup(x => x.DeleteAsync(_testReaderId)).ReturnsAsync(true);
        _mockReaderService.Setup(x => x.DisconnectReaderAsync(_testReaderId)).ReturnsAsync(true);

        // Act
        await _service.DeleteReaderAsync(_testReaderId);

        // Assert
        _mockReaderService.Verify(x => x.DisconnectReaderAsync(_testReaderId), Times.Once);
        _mockNotificationService.Verify(x => x.BroadcastReaderConfigurationAsync(
            It.Is<ReaderConfiguration>(r => r.ReaderId == _testReaderId), 
            "Deleted"), Times.Once);
    }

    [Test]
    public async Task DeleteReaderAsync_WhenReaderDoesNotExist_ShouldStillAttemptDisconnectAndNotBroadcastNotification()
    {
        // Arrange
        _mockRepository.Setup(x => x.GetByIdAsync(_testReaderId)).ReturnsAsync((ReaderConfiguration?)null);
        _mockRepository.Setup(x => x.DeleteAsync(_testReaderId)).ReturnsAsync(false);
        _mockReaderService.Setup(x => x.DisconnectReaderAsync(_testReaderId)).ReturnsAsync(false);

        // Act
        await _service.DeleteReaderAsync(_testReaderId);

        // Assert
        _mockReaderService.Verify(x => x.DisconnectReaderAsync(_testReaderId), Times.Once);
        _mockNotificationService.Verify(x => x.BroadcastReaderConfigurationAsync(
            It.IsAny<ReaderConfiguration>(), 
            It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task DeleteReaderAsync_WhenDisconnectFails_ShouldStillDeleteFromDatabase()
    {
        // Arrange
        var readerToDelete = CreateTestReader();
        _mockRepository.Setup(x => x.GetByIdAsync(_testReaderId)).ReturnsAsync(readerToDelete);
        _mockRepository.Setup(x => x.DeleteAsync(_testReaderId)).ReturnsAsync(true);
        _mockReaderService.Setup(x => x.DisconnectReaderAsync(_testReaderId)).ThrowsAsync(new Exception("OSDP disconnect failed"));

        // Act
        await _service.DeleteReaderAsync(_testReaderId);

        // Assert
        _mockReaderService.Verify(x => x.DisconnectReaderAsync(_testReaderId), Times.Once);
        _mockRepository.Verify(x => x.DeleteAsync(_testReaderId), Times.Once);
        _mockNotificationService.Verify(x => x.BroadcastReaderConfigurationAsync(
            It.Is<ReaderConfiguration>(r => r.ReaderId == _testReaderId), 
            "Deleted"), Times.Once);
    }

    [Test]
    public void SaveReaderAsync_WhenNoNotificationServiceProvided_ShouldNotThrow()
    {
        // Arrange
        var newReader = CreateTestReader();
        var serviceWithoutNotifications = new ReaderConfigurationService(_mockRepository.Object, _mockLogger.Object, _lazyReaderService, null);
        _mockRepository.Setup(x => x.ExistsAsync(_testReaderId)).ReturnsAsync(false);
        _mockRepository.Setup(x => x.CreateAsync(It.IsAny<ReaderConfiguration>())).ReturnsAsync((ReaderConfiguration r) => r);

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await serviceWithoutNotifications.SaveReaderAsync(newReader));
    }

    private ReaderConfiguration CreateTestReader()
    {
        return new ReaderConfiguration
        {
            ReaderId = _testReaderId,
            ReaderName = "Test Reader",
            SerialPort = "COM1",
            BaudRate = 9600,
            Address = 1,
            SecurityMode = OsdpSecurityMode.ClearText,
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };
    }
}