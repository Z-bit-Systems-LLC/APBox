using ApBox.Core.Models;
using ApBox.Core.OSDP;
using ApBox.Core.Services;
using ApBox.Core.Tests.Mocks;
using ApBox.Plugins;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace ApBox.Core.Tests.OSDP;

[TestFixture]
[Category("Unit")]
public class OsdpCommunicationTests
{
    private OsdpCommunicationManager _communicationManager;
    private ILogger<OsdpCommunicationManager> _logger;
    private Mock<IServiceProvider> _mockServiceProvider;
    private Mock<IServiceScope> _mockServiceScope;
    private Mock<IServiceScopeFactory> _mockServiceScopeFactory;
    private MockSerialPortService _mockSerialPortService;
    
    [SetUp]
    public void Setup()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<OsdpCommunicationManager>();
        
        // Create mocks for dependency injection
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockServiceScope = new Mock<IServiceScope>();
        _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
        
        // Setup service provider to return mocked services
        _mockServiceScope.Setup(s => s.ServiceProvider).Returns(_mockServiceProvider.Object);
        _mockServiceScopeFactory.Setup(f => f.CreateScope()).Returns(_mockServiceScope.Object);
        _mockServiceProvider.Setup(p => p.GetService(typeof(IServiceScopeFactory))).Returns(_mockServiceScopeFactory.Object);
        
        // Mock the SecurityModeUpdateService
        var mockSecurityModeUpdateService = new Mock<ISecurityModeUpdateService>();
        mockSecurityModeUpdateService.Setup(s => s.UpdateSecurityModeAsync(It.IsAny<Guid>(), It.IsAny<OsdpSecurityMode>(), It.IsAny<byte[]>()))
                                     .ReturnsAsync(true);
        _mockServiceProvider.Setup(p => p.GetService(typeof(ISecurityModeUpdateService))).Returns(mockSecurityModeUpdateService.Object);
        
        // Create mock serial port service
        _mockSerialPortService = new MockSerialPortService();
        
        _communicationManager = new OsdpCommunicationManager(_logger, _mockServiceProvider.Object, _mockSerialPortService);
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