using ApBox.Core.Models;
using ApBox.Core.OSDP;
using ApBox.Core.Services.Configuration;
using ApBox.Core.Services.Core;
using ApBox.Core.Services.Infrastructure;
using ApBox.Core.Services.Security;
using Microsoft.Extensions.Logging;
using Moq;
using OSDP.Net.Connections;

namespace ApBox.Core.Tests.OSDP;

[TestFixture]
[Category("Unit")]
public class OsdpCommunicationTests
{
    private OsdpCommunicationManager _communicationManager;
    private ILogger<OsdpCommunicationManager> _logger;
    private Mock<ISerialPortService> _mockSerialPortService;
    
    [SetUp]
    public void Setup()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<OsdpCommunicationManager>();
        
        // Mock the SecurityModeUpdateService
        var mockSecurityModeUpdateService = new Mock<ISecurityModeUpdateService>();
        mockSecurityModeUpdateService.Setup(s => s.UpdateSecurityModeAsync(It.IsAny<Guid>(), It.IsAny<OsdpSecurityMode>(), It.IsAny<byte[]>()))
                                     .ReturnsAsync(true);
        
        // Mock the FeedbackConfigurationService
        var mockFeedbackConfigurationService = new Mock<IFeedbackConfigurationService>();
        mockFeedbackConfigurationService.Setup(s => s.GetIdleStateAsync())
                                        .ReturnsAsync(new IdleStateFeedback { PermanentLedColor = LedColor.Blue, HeartbeatFlashColor = LedColor.Green });
        
        // Create mock serial port service
        _mockSerialPortService = new Mock<ISerialPortService>();
        
        // Setup mock serial port service with default behavior
        _mockSerialPortService.Setup(s => s.GetAvailablePortNames())
                             .Returns(new[] { "COM1", "COM2", "COM3", "/dev/ttyUSB0", "/dev/ttyUSB1" });
        
        _mockSerialPortService.Setup(s => s.PortExists(It.IsAny<string>()))
                             .Returns<string>(port => !string.IsNullOrEmpty(port) && 
                                                     new[] { "COM1", "COM2", "COM3", "/dev/ttyUSB0", "/dev/ttyUSB1" }.Contains(port));
        
        _mockSerialPortService.Setup(s => s.CreateConnection(It.IsAny<string>(), It.IsAny<int>()))
                             .Returns<string, int>((port, baud) => {
                                 // Return a mock connection - in real tests this would be mocked further
                                 try 
                                 {
                                     return new SerialPortOsdpConnection("COM1", 9600);
                                 }
                                 catch
                                 {
                                     // If we can't create a real connection, throw for proper test behavior
                                     throw new InvalidOperationException($"Cannot create connection to {port}");
                                 }
                             });
        
        var mockPinCollectionService = new Mock<IPinCollectionService>();
        _communicationManager = new OsdpCommunicationManager(_mockSerialPortService.Object, mockSecurityModeUpdateService.Object, mockFeedbackConfigurationService.Object, mockPinCollectionService.Object, _logger);
    }
    
    [TearDown]
    public async Task TearDown()
    {
        await _communicationManager.StopAsync();
    }
    
    [Test]
    public async Task AddDevice_ValidConfiguration_ReturnsTrue()
    {
        var config = new OsdpDeviceConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "Test Reader",
            Address = 1,
            ConnectionString = "COM1",
            IsEnabled = true
        };
        
        var result = await _communicationManager.AddDeviceAsync(config);
        
        Assert.That(result, Is.True);
    }
    
    [Test]
    public async Task GetDevices_AfterAddingDevice_ReturnsDevice()
    {
        var config = new OsdpDeviceConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "Test Reader",
            Address = 1,
            ConnectionString = "COM1",
            IsEnabled = true
        };
        
        await _communicationManager.AddDeviceAsync(config);
        var devices = await _communicationManager.GetDevicesAsync();
        
        Assert.That(devices, Is.Not.Empty);
        Assert.That(devices.First().Name, Is.EqualTo("Test Reader"));
        Assert.That(devices.First().Address, Is.EqualTo(1));
    }
    
    [Test]
    public async Task GetDevice_ExistingDevice_ReturnsDevice()
    {
        var config = new OsdpDeviceConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "Test Reader",
            Address = 1,
            ConnectionString = "COM1",
            IsEnabled = true
        };
        
        await _communicationManager.AddDeviceAsync(config);
        var device = await _communicationManager.GetDeviceAsync(config.Id);
        
        Assert.That(device, Is.Not.Null);
        Assert.That(device.Id, Is.EqualTo(config.Id));
    }
    
    [Test]
    public async Task RemoveDevice_ExistingDevice_ReturnsTrue()
    {
        var config = new OsdpDeviceConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "Test Reader",
            Address = 1,
            ConnectionString = "COM1",
            IsEnabled = true
        };
        
        await _communicationManager.AddDeviceAsync(config);
        var result = await _communicationManager.RemoveDeviceAsync(config.Id);
        
        Assert.That(result, Is.True);
        
        var device = await _communicationManager.GetDeviceAsync(config.Id);
        Assert.That(device, Is.Null);
    }
    
    [Test]
    public async Task StartStop_CommunicationManager_ExecutesWithoutError()
    {
        var config = new OsdpDeviceConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "Test Reader",
            Address = 1,
            ConnectionString = "COM1",
            IsEnabled = true
        };
        
        await _communicationManager.AddDeviceAsync(config);
        
        Assert.DoesNotThrowAsync(async () => await _communicationManager.StartAsync());
        Assert.DoesNotThrowAsync(async () => await _communicationManager.StopAsync());
    }
    
}