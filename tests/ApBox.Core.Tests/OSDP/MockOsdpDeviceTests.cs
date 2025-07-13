using ApBox.Core.OSDP;
using ApBox.Plugins;
using Microsoft.Extensions.Logging;

namespace ApBox.Core.Tests.OSDP;

[TestFixture]
[Category("Unit")]
public class MockOsdpDeviceTests
{
    private MockOsdpDevice _device;
    private ILogger _logger;
    
    [SetUp]
    public void Setup()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<MockOsdpDevice>();
        
        var config = new OsdpDeviceConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "Test Reader",
            Address = 1,
            ConnectionString = "COM1",
            IsEnabled = true
        };
        
        _device = new MockOsdpDevice(config, _logger);
    }
    
    [TearDown]
    public async Task TearDown()
    {
        await _device.DisconnectAsync();
        _device.Dispose();
    }
    
    [Test]
    public void Constructor_ValidConfiguration_SetsProperties()
    {
        Assert.That(_device.Name, Is.EqualTo("Test Reader"));
        Assert.That(_device.Address, Is.EqualTo(1));
        Assert.That(_device.IsEnabled, Is.True);
        Assert.That(_device.IsOnline, Is.False);
    }
    
    [Test]
    public async Task Connect_EnabledDevice_ReturnsTrue()
    {
        var result = await _device.ConnectAsync();
        
        Assert.That(result, Is.True);
        Assert.That(_device.IsOnline, Is.True);
    }
    
    [Test]
    public async Task Disconnect_ConnectedDevice_SetsOffline()
    {
        await _device.ConnectAsync();
        await _device.DisconnectAsync();
        
        Assert.That(_device.IsOnline, Is.False);
    }
    
    [Test]
    public async Task SendCommand_OnlineDevice_ReturnsTrue()
    {
        await _device.ConnectAsync();
        
        var command = new LedCommand
        {
            Color = LedColor.Green,
            Count = 1
        };
        
        var result = await _device.SendCommandAsync(command);
        
        Assert.That(result, Is.True);
    }
    
    [Test]
    public async Task SendCommand_OfflineDevice_ReturnsFalse()
    {
        var command = new LedCommand
        {
            Color = LedColor.Green,
            Count = 1
        };
        
        var result = await _device.SendCommandAsync(command);
        
        Assert.That(result, Is.False);
    }
    
    [Test]
    public async Task SendFeedback_ValidFeedback_ReturnsTrue()
    {
        await _device.ConnectAsync();
        
        var feedback = new ReaderFeedback
        {
            Type = ReaderFeedbackType.Success,
            LedColor = LedColor.Green,
            BeepCount = 2,
            LedDurationMs = 1000
        };
        
        var result = await _device.SendFeedbackAsync(feedback);
        
        Assert.That(result, Is.True);
    }
    
    [Test]
    public async Task StatusChanged_OnConnect_RaisesEvent()
    {
        bool eventRaised = false;
        OsdpStatusChangedEventArgs? eventArgs = null;
        
        _device.StatusChanged += (sender, args) =>
        {
            eventRaised = true;
            eventArgs = args;
        };
        
        await _device.ConnectAsync();
        
        Assert.That(eventRaised, Is.True);
        Assert.That(eventArgs, Is.Not.Null);
        Assert.That(eventArgs.IsOnline, Is.True);
    }
}