using ApBox.Core.PacketTracing.Services;
using ApBox.Core.PacketTracing.Models;
using ApBox.Core.PacketTracing;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace ApBox.Core.Tests.PacketTracing;

[TestFixture]
[Category("Integration")]
public class PacketTraceServiceIntegrationTests
{
    private PacketTraceService _service;
    private ILogger<PacketTraceService> _logger;

    [SetUp]
    public void Setup()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<PacketTraceService>();
        _service = new PacketTraceService(null); // No reader service needed for tests
    }

    [Test]
    public void CapturePacket_WithOsdpPollCommand_ParsesCorrectly()
    {
        // Arrange
        var readerId = Guid.NewGuid().ToString();
        var readerName = "Test Reader";
        byte address = 0x01;
        
        // Configure settings to NOT filter POLL commands for this test
        var settings = new PacketTraceSettings
        {
            FilterPollCommands = false,
            FilterAckCommands = false
        };
        _service.UpdateSettings(settings);
        
        // Start tracing for the reader
        _service.StartTracing(readerId);
        
        // Simulate OSDP POLL command (0x60)
        var osdpPollPacket = new byte[] { 0x53, 0x01, 0x08, 0x00, 0x60, 0x00, 0x1C, 0x9B };
        
        // Act
        _service.CapturePacket(osdpPollPacket, PacketDirection.Outgoing, readerId, readerName, address);
        
        // Assert
        var traces = _service.GetTraces(readerId).ToList();
        Assert.That(traces, Has.Count.EqualTo(1));
        
        var trace = traces.First();
        Assert.That(trace.Type, Is.EqualTo("POLL"));
        Assert.That(trace.Command, Is.EqualTo("0x60"));
        Assert.That(trace.Direction, Is.EqualTo(PacketDirection.Outgoing));
        Assert.That(trace.ReaderId, Is.EqualTo(readerId));
        Assert.That(trace.ReaderName, Is.EqualTo(readerName));
        Assert.That(trace.Address, Is.EqualTo(address));
        Assert.That(trace.IsValid, Is.True);
        Assert.That(trace.Details, Does.Contain("OSDP POLL"));
    }

    [Test]
    public void CapturePacket_WithOsdpAckReply_ParsesCorrectly()
    {
        // Arrange
        var readerId = Guid.NewGuid().ToString();
        var readerName = "Test Reader";
        byte address = 0x01;
        
        // Start tracing for the reader
        _service.StartTracing(readerId);
        
        // Simulate OSDP ACK reply (0x40)
        var osdpAckPacket = new byte[] { 0x53, 0x01, 0x08, 0x00, 0x40, 0x00, 0x1C, 0x7B };
        
        // Act
        _service.CapturePacket(osdpAckPacket, PacketDirection.Incoming, readerId, readerName, address);
        
        // Assert
        var traces = _service.GetTraces(readerId).ToList();
        Assert.That(traces, Has.Count.EqualTo(1));
        
        var trace = traces.First();
        Assert.That(trace.Type, Is.EqualTo("ACK"));
        Assert.That(trace.Command, Is.EqualTo("0x40"));
        Assert.That(trace.Direction, Is.EqualTo(PacketDirection.Incoming));
        Assert.That(trace.IsValid, Is.True);
        Assert.That(trace.Details, Does.Contain("OSDP ACK"));
    }

    [Test]
    public void CapturePacket_WithRawCardData_ParsesCorrectly()
    {
        // Arrange
        var readerId = Guid.NewGuid().ToString();
        var readerName = "Test Reader";
        byte address = 0x01;
        
        // Start tracing for the reader
        _service.StartTracing(readerId);
        
        // Simulate raw card data (not OSDP packet format)
        var rawCardData = new byte[] { 0x12, 0x34, 0x56, 0x78 };
        
        // Act
        _service.CapturePacket(rawCardData, PacketDirection.Incoming, readerId, readerName, address);
        
        // Assert
        var traces = _service.GetTraces(readerId).ToList();
        Assert.That(traces, Has.Count.EqualTo(1));
        
        var trace = traces.First();
        Assert.That(trace.Type, Is.EqualTo("Raw Data"));
        Assert.That(trace.Direction, Is.EqualTo(PacketDirection.Incoming));
        Assert.That(trace.IsValid, Is.True);
        Assert.That(trace.Details, Does.Contain("Raw data packet"));
    }

    [Test]
    public void CapturePacket_WithFilteringEnabled_FiltersCorrectly()
    {
        // Arrange
        var readerId = Guid.NewGuid().ToString();
        var readerName = "Test Reader";
        byte address = 0x01;
        
        // Configure filtering
        var settings = new PacketTraceSettings
        {
            FilterPollCommands = true,
            FilterAckCommands = false
        };
        _service.UpdateSettings(settings);
        _service.StartTracing(readerId);
        
        // Simulate OSDP POLL command (should be filtered)
        var osdpPollPacket = new byte[] { 0x53, 0x01, 0x08, 0x00, 0x60, 0x00, 0x1C, 0x9B };
        
        // Simulate OSDP ACK reply (should not be filtered)
        var osdpAckPacket = new byte[] { 0x53, 0x01, 0x08, 0x00, 0x40, 0x00, 0x1C, 0x7B };
        
        // Act
        _service.CapturePacket(osdpPollPacket, PacketDirection.Outgoing, readerId, readerName, address);
        _service.CapturePacket(osdpAckPacket, PacketDirection.Incoming, readerId, readerName, address);
        
        // Assert - All packets should be stored, filtering only affects display
        var allTraces = _service.GetTraces(readerId).ToList();
        Assert.That(allTraces, Has.Count.EqualTo(2)); // Both packets stored
        
        // When retrieving with filters applied, only non-poll commands should be returned
        var filteredTraces = _service.GetTraces(readerId, limit: null, filterPollCommands: true, filterAckCommands: false).ToList();
        Assert.That(filteredTraces, Has.Count.EqualTo(1)); // Only ACK packet shown
        Assert.That(filteredTraces.First().Type, Is.EqualTo("ACK"));
        
        var stats = _service.GetStatistics();
        Assert.That(stats.TotalPackets, Is.EqualTo(2)); // All packets counted
        Assert.That(stats.FilteredPackets, Is.EqualTo(1)); // Poll command counted as filtered
    }

    [Test]
    public void CapturePacket_MultipleReaders_TracesCorrectly()
    {
        // Arrange
        var reader1Id = Guid.NewGuid().ToString();
        var reader2Id = Guid.NewGuid().ToString();
        var reader1Name = "Reader 1";
        var reader2Name = "Reader 2";
        
        // Configure settings to NOT filter commands for this test
        var settings = new PacketTraceSettings
        {
            FilterPollCommands = false,
            FilterAckCommands = false
        };
        _service.UpdateSettings(settings);
        
        // Start tracing for both readers
        _service.StartTracing(reader1Id);
        _service.StartTracing(reader2Id);
        
        var packet1 = new byte[] { 0x53, 0x01, 0x08, 0x00, 0x60, 0x00, 0x1C, 0x9B };
        var packet2 = new byte[] { 0x53, 0x02, 0x08, 0x00, 0x40, 0x00, 0x1C, 0x7A };
        
        // Act
        _service.CapturePacket(packet1, PacketDirection.Outgoing, reader1Id, reader1Name, 0x01);
        _service.CapturePacket(packet2, PacketDirection.Incoming, reader2Id, reader2Name, 0x02);
        
        // Assert
        var reader1Traces = _service.GetTraces(reader1Id).ToList();
        var reader2Traces = _service.GetTraces(reader2Id).ToList();
        var allTraces = _service.GetTraces().ToList();
        
        Assert.That(reader1Traces, Has.Count.EqualTo(1));
        Assert.That(reader2Traces, Has.Count.EqualTo(1));
        Assert.That(allTraces, Has.Count.EqualTo(2));
        
        Assert.That(reader1Traces.First().ReaderId, Is.EqualTo(reader1Id));
        Assert.That(reader2Traces.First().ReaderId, Is.EqualTo(reader2Id));
    }

    [TearDown]
    public void TearDown()
    {
        _service.StopTracingAll();
        _service.ClearTraces();
    }
}