using ApBox.Core.PacketTracing.Services;
using ApBox.Core.PacketTracing.Models;
using ApBox.Core.PacketTracing;
using ApBox.Core.Services.Reader;
using ApBox.Core.Models;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ApBox.Core.Tests.PacketTracing;

[TestFixture]
[Category("Unit")]
public class PacketTraceServiceReaderIntegrationTests
{
    private PacketTraceService _service;
    private Mock<IReaderConfigurationService> _mockReaderService;
    private ILogger<PacketTraceService> _logger;

    [SetUp]
    public void Setup()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<PacketTraceService>();
        
        _mockReaderService = new Mock<IReaderConfigurationService>();
        _service = new PacketTraceService(_mockReaderService.Object);
    }

    [Test]
    public async Task StartTracingAll_WithEnabledReaders_StartsTracingForAllEnabledReaders()
    {
        // Arrange
        var reader1Id = Guid.NewGuid();
        var reader2Id = Guid.NewGuid();
        var reader3Id = Guid.NewGuid();
        
        var readers = new List<ReaderConfiguration>
        {
            new ReaderConfiguration { ReaderId = reader1Id, ReaderName = "Reader 1", IsEnabled = true },
            new ReaderConfiguration { ReaderId = reader2Id, ReaderName = "Reader 2", IsEnabled = true },
            new ReaderConfiguration { ReaderId = reader3Id, ReaderName = "Reader 3", IsEnabled = false } // Disabled
        };
        
        _mockReaderService.Setup(s => s.GetAllReadersAsync())
                         .ReturnsAsync(readers);

        // Act
        _service.StartTracingAll();
        
        // Allow async method to complete
        await Task.Delay(100);

        // Assert
        Assert.That(_service.IsTracingReader(reader1Id.ToString()), Is.True);
        Assert.That(_service.IsTracingReader(reader2Id.ToString()), Is.True);
        Assert.That(_service.IsTracingReader(reader3Id.ToString()), Is.False); // Should not be tracing disabled reader
        Assert.That(_service.IsTracing, Is.True);
    }

    [Test]
    public async Task StartTracingAll_WithNoEnabledReaders_DoesNotStartTracing()
    {
        // Arrange
        var readers = new List<ReaderConfiguration>
        {
            new ReaderConfiguration { ReaderId = Guid.NewGuid(), ReaderName = "Reader 1", IsEnabled = false },
            new ReaderConfiguration { ReaderId = Guid.NewGuid(), ReaderName = "Reader 2", IsEnabled = false }
        };
        
        _mockReaderService.Setup(s => s.GetAllReadersAsync())
                         .ReturnsAsync(readers);

        // Act
        _service.StartTracingAll();
        
        // Allow async method to complete
        await Task.Delay(100);

        // Assert
        Assert.That(_service.IsTracing, Is.False);
    }

    [Test]
    public async Task StartTracingAll_WithNoReaders_DoesNotStartTracing()
    {
        // Arrange
        _mockReaderService.Setup(s => s.GetAllReadersAsync())
                         .ReturnsAsync(new List<ReaderConfiguration>());

        // Act
        _service.StartTracingAll();
        
        // Allow async method to complete
        await Task.Delay(100);

        // Assert
        Assert.That(_service.IsTracing, Is.False);
    }

    [Test]
    public async Task StartTracingAll_WhenReaderServiceThrows_DoesNotThrow()
    {
        // Arrange
        _mockReaderService.Setup(s => s.GetAllReadersAsync())
                         .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            _service.StartTracingAll();
            await Task.Delay(100);
        });
        
        Assert.That(_service.IsTracing, Is.False);
    }

    [Test]
    public void StartTracingAll_WithNullReaderService_DoesNotThrow()
    {
        // Arrange
        var serviceWithNullDependency = new PacketTraceService(null);

        // Act & Assert
        Assert.DoesNotThrow(() => serviceWithNullDependency.StartTracingAll());
        Assert.That(serviceWithNullDependency.IsTracing, Is.False);
    }

    [Test]
    public async Task CapturePacket_AfterStartTracingAll_CapturesPacketsForTrackedReaders()
    {
        // Arrange
        var reader1Id = Guid.NewGuid();
        var reader2Id = Guid.NewGuid();
        
        var readers = new List<ReaderConfiguration>
        {
            new ReaderConfiguration { ReaderId = reader1Id, ReaderName = "Reader 1", IsEnabled = true },
            new ReaderConfiguration { ReaderId = reader2Id, ReaderName = "Reader 2", IsEnabled = false }
        };
        
        _mockReaderService.Setup(s => s.GetAllReadersAsync())
                         .ReturnsAsync(readers);

        // Start tracing all
        _service.StartTracingAll();
        await Task.Delay(100);

        var osdpPacket = new byte[] { 0x53, 0x01, 0x08, 0x00, 0x40, 0x00, 0x1C, 0x7B };

        // Act
        _service.CapturePacket(osdpPacket, PacketDirection.Incoming, reader1Id.ToString(), "Reader 1", 0x01);
        _service.CapturePacket(osdpPacket, PacketDirection.Incoming, reader2Id.ToString(), "Reader 2", 0x02);

        // Assert
        var reader1Traces = _service.GetTraces(reader1Id.ToString()).ToList();
        var reader2Traces = _service.GetTraces(reader2Id.ToString()).ToList();

        Assert.That(reader1Traces, Has.Count.EqualTo(1)); // Should capture for enabled reader
        Assert.That(reader2Traces, Has.Count.EqualTo(0)); // Should not capture for disabled reader
    }

    [Test]
    public async Task StopTracingAll_AfterStartTracingAll_StopsAllTracing()
    {
        // Arrange
        var reader1Id = Guid.NewGuid();
        var reader2Id = Guid.NewGuid();
        
        var readers = new List<ReaderConfiguration>
        {
            new ReaderConfiguration { ReaderId = reader1Id, ReaderName = "Reader 1", IsEnabled = true },
            new ReaderConfiguration { ReaderId = reader2Id, ReaderName = "Reader 2", IsEnabled = true }
        };
        
        _mockReaderService.Setup(s => s.GetAllReadersAsync())
                         .ReturnsAsync(readers);

        // Start tracing
        _service.StartTracingAll();
        await Task.Delay(100);
        
        Assert.That(_service.IsTracing, Is.True);

        // Act
        _service.StopTracingAll();

        // Assert
        Assert.That(_service.IsTracing, Is.False);
        Assert.That(_service.IsTracingReader(reader1Id.ToString()), Is.False);
        Assert.That(_service.IsTracingReader(reader2Id.ToString()), Is.False);
    }

    [Test]
    public async Task PacketCapturedEvent_IsRaisedWhenPacketCaptured()
    {
        // Arrange
        var reader1Id = Guid.NewGuid();
        var readers = new List<ReaderConfiguration>
        {
            new ReaderConfiguration { ReaderId = reader1Id, ReaderName = "Reader 1", IsEnabled = true }
        };
        
        _mockReaderService.Setup(s => s.GetAllReadersAsync())
                         .ReturnsAsync(readers);

        PacketTraceEntry? capturedPacket = null;
        _service.PacketCaptured += (sender, packet) => capturedPacket = packet;

        // Start tracing
        _service.StartTracingAll();
        await Task.Delay(100);

        var osdpPacket = new byte[] { 0x53, 0x01, 0x08, 0x00, 0x40, 0x00, 0x1C, 0x7B };

        // Act
        _service.CapturePacket(osdpPacket, PacketDirection.Incoming, reader1Id.ToString(), "Reader 1", 0x01);

        // Assert
        Assert.That(capturedPacket, Is.Not.Null);
        Assert.That(capturedPacket.Type, Is.EqualTo("ACK"));
        Assert.That(capturedPacket.ReaderId, Is.EqualTo(reader1Id.ToString()));
        Assert.That(capturedPacket.ReaderName, Is.EqualTo("Reader 1"));
    }

    [Test]
    public async Task GetStatistics_AfterStartTracingAll_ReflectsTracingState()
    {
        // Arrange
        var reader1Id = Guid.NewGuid();
        var readers = new List<ReaderConfiguration>
        {
            new ReaderConfiguration { ReaderId = reader1Id, ReaderName = "Reader 1", IsEnabled = true }
        };
        
        _mockReaderService.Setup(s => s.GetAllReadersAsync())
                         .ReturnsAsync(readers);

        // Act
        _service.StartTracingAll();
        await Task.Delay(100);

        var stats = _service.GetStatistics();

        // Assert
        Assert.That(stats.TracingStartedAt, Is.Not.Null);
        Assert.That(stats.TracingDuration, Is.Not.Null);
    }

    [TearDown]
    public void TearDown()
    {
        _service.StopTracingAll();
        _service.ClearTraces();
    }
}