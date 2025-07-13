using ApBox.Web.Controllers;
using ApBox.Core.OSDP;
using ApBox.Core.Services;
using ApBox.Plugins;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace ApBox.Web.Tests.Controllers;

[TestFixture]
[Category("Unit")]
public class ReadersControllerTests
{
    private ReadersController _controller;
    private Mock<IReaderService> _mockReaderService;
    private Mock<IOsdpCommunicationManager> _mockOsdpManager;
    private Mock<ILogger<ReadersController>> _mockLogger;
    
    [SetUp]
    public void Setup()
    {
        _mockReaderService = new Mock<IReaderService>();
        _mockOsdpManager = new Mock<IOsdpCommunicationManager>();
        _mockLogger = new Mock<ILogger<ReadersController>>();
        
        _controller = new ReadersController(
            _mockReaderService.Object,
            _mockOsdpManager.Object,
            _mockLogger.Object);
    }
    
    [Test]
    public async Task GetReaders_ValidRequest_ReturnsOkResult()
    {
        // Arrange
        var readerId = Guid.NewGuid();
        var readerConfigs = new List<ReaderConfiguration>
        {
            new ReaderConfiguration
            {
                ReaderId = readerId,
                ReaderName = "Test Reader"
            }
        };
        
        var osdpDevices = new List<IOsdpDevice>
        {
            CreateMockOsdpDevice(readerId, "Test Reader", true)
        };
        
        _mockReaderService.Setup(x => x.GetReadersAsync())
            .ReturnsAsync(readerConfigs);
        _mockOsdpManager.Setup(x => x.GetDevicesAsync())
            .ReturnsAsync(osdpDevices);
        
        // Act
        var result = await _controller.GetReaders();
        
        // Assert
        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
        var okResult = result.Result as OkObjectResult;
        var readers = okResult?.Value as IEnumerable<ReaderDto>;
        
        Assert.That(readers, Is.Not.Null);
        Assert.That(readers.Count(), Is.EqualTo(1));
        Assert.That(readers.First().Name, Is.EqualTo("Test Reader"));
        Assert.That(readers.First().IsOnline, Is.True);
    }
    
    [Test]
    public async Task GetReader_ExistingReader_ReturnsOkResult()
    {
        // Arrange
        var readerId = Guid.NewGuid();
        var readerConfig = new ReaderConfiguration
        {
            ReaderId = readerId,
            ReaderName = "Test Reader"
        };
        
        var osdpDevice = CreateMockOsdpDevice(readerId, "Test Reader", true);
        
        _mockReaderService.Setup(x => x.GetReaderAsync(readerId))
            .ReturnsAsync(readerConfig);
        _mockOsdpManager.Setup(x => x.GetDeviceAsync(readerId))
            .ReturnsAsync(osdpDevice);
        
        // Act
        var result = await _controller.GetReader(readerId);
        
        // Assert
        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
        var okResult = result.Result as OkObjectResult;
        var reader = okResult?.Value as ReaderDto;
        
        Assert.That(reader, Is.Not.Null);
        Assert.That(reader.Id, Is.EqualTo(readerId));
        Assert.That(reader.Name, Is.EqualTo("Test Reader"));
    }
    
    [Test]
    public async Task GetReader_NonExistentReader_ReturnsNotFound()
    {
        // Arrange
        var readerId = Guid.NewGuid();
        
        _mockReaderService.Setup(x => x.GetReaderAsync(readerId))
            .ReturnsAsync((ReaderConfiguration?)null);
        
        // Act
        var result = await _controller.GetReader(readerId);
        
        // Assert
        Assert.That(result.Result, Is.TypeOf<NotFoundObjectResult>());
    }
    
    [Test]
    public async Task SendFeedback_ValidRequest_ReturnsOk()
    {
        // Arrange
        var readerId = Guid.NewGuid();
        var feedback = new ReaderFeedback
        {
            Type = ReaderFeedbackType.Success,
            LedColor = LedColor.Green
        };
        
        _mockReaderService.Setup(x => x.SendFeedbackAsync(readerId, feedback))
            .ReturnsAsync(true);
        
        // Act
        var result = await _controller.SendFeedback(readerId, feedback);
        
        // Assert
        Assert.That(result, Is.TypeOf<OkResult>());
    }
    
    [Test]
    public async Task SendFeedback_FailedToSend_ReturnsBadRequest()
    {
        // Arrange
        var readerId = Guid.NewGuid();
        var feedback = new ReaderFeedback
        {
            Type = ReaderFeedbackType.Success,
            LedColor = LedColor.Green
        };
        
        _mockReaderService.Setup(x => x.SendFeedbackAsync(readerId, feedback))
            .ReturnsAsync(false);
        
        // Act
        var result = await _controller.SendFeedback(readerId, feedback);
        
        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
    }
    
    private IOsdpDevice CreateMockOsdpDevice(Guid id, string name, bool isOnline)
    {
        var mockDevice = new Mock<IOsdpDevice>();
        mockDevice.Setup(x => x.Id).Returns(id);
        mockDevice.Setup(x => x.Name).Returns(name);
        mockDevice.Setup(x => x.IsOnline).Returns(isOnline);
        mockDevice.Setup(x => x.LastActivity).Returns(DateTime.UtcNow);
        mockDevice.Setup(x => x.Address).Returns(1);
        return mockDevice.Object;
    }
}