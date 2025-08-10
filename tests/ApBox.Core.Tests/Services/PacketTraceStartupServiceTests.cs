using ApBox.Core.PacketTracing.Services;
using ApBox.Core.Services;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Moq;

namespace ApBox.Core.Tests.Services;

[TestFixture]
public class PacketTraceStartupServiceTests
{
    private Mock<IPacketTraceService> _packetTraceService = null!;
    private Mock<ILogger<PacketTraceStartupService>> _logger = null!;
    private PacketTraceStartupService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _packetTraceService = new Mock<IPacketTraceService>();
        _logger = new Mock<ILogger<PacketTraceStartupService>>();
        _service = new PacketTraceStartupService(_packetTraceService.Object, _logger.Object);
    }

    [Test]
    public async Task StartAsync_CallsStartTracingAll()
    {
        // Act
        await _service.StartAsync(CancellationToken.None);

        // Assert
        _packetTraceService.Verify(x => x.StartTracingAll(), Times.Once);
    }

    [Test]
    public void StartAsync_WhenPacketTraceThrows_LogsErrorButDoesNotThrow()
    {
        // Arrange
        var exception = new Exception("Test exception");
        _packetTraceService.Setup(x => x.StartTracingAll()).Throws(exception);

        // Act & Assert
        Assert.DoesNotThrowAsync(() => _service.StartAsync(CancellationToken.None));
    }

    [Test]
    public async Task StopAsync_CallsStopTracingAll()
    {
        // Act
        await _service.StopAsync(CancellationToken.None);

        // Assert
        _packetTraceService.Verify(x => x.StopTracingAll(), Times.Once);
    }

    [Test]
    public void StopAsync_WhenPacketTraceThrows_DoesNotThrow()
    {
        // Arrange
        var exception = new Exception("Test exception");
        _packetTraceService.Setup(x => x.StopTracingAll()).Throws(exception);

        // Act & Assert
        Assert.DoesNotThrowAsync(() => _service.StopAsync(CancellationToken.None));
    }
}