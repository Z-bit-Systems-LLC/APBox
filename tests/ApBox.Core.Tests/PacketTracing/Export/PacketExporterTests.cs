using ApBox.Core.PacketTracing.Export;
using ApBox.Core.PacketTracing.Models;
using NUnit.Framework;
using OSDP.Net.Model;
using OSDP.Net.Tracing;
using System.Reflection;
using System.Text;

namespace ApBox.Core.Tests.PacketTracing.Export;

[TestFixture]
[Category("Unit")]
public class OsdpCaptureExporterTests
{
    private OsdpCaptureExporter _exporter;

    [SetUp]
    public void Setup()
    {
        _exporter = new OsdpCaptureExporter();
    }

    [Test]
    public void FileExtension_ReturnsOsdpcap()
    {
        Assert.That(_exporter.FileExtension, Is.EqualTo(".osdpcap"));
    }

    [Test]
    public void ContentType_ReturnsOctetStream()
    {
        Assert.That(_exporter.ContentType, Is.EqualTo("application/octet-stream"));
    }

    [Test]
    public void DisplayName_ReturnsExpectedValue()
    {
        Assert.That(_exporter.DisplayName, Is.EqualTo("OSDP Capture (.osdpcap)"));
    }

    [Test]
    public async Task ExportAsync_WithEmptyPacketList_ReturnsEmptyArray()
    {
        var packets = Enumerable.Empty<PacketTraceEntry>();
        var result = await _exporter.ExportAsync(packets);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task ExportAsync_WithPackets_ReturnsNonEmptyData()
    {
        var packets = new List<PacketTraceEntry>
        {
            CreateTestPacketEntry(TraceDirection.Output, DateTime.UtcNow)
        };

        var result = await _exporter.ExportAsync(packets);
        Assert.That(result, Is.Not.Empty);
    }

    [Test]
    public async Task ExportAsync_WithMultiplePackets_ExportsAll()
    {
        var baseTime = DateTime.UtcNow;
        var packets = new List<PacketTraceEntry>
        {
            CreateTestPacketEntry(TraceDirection.Output, baseTime),
            CreateTestPacketEntry(TraceDirection.Input, baseTime.AddMilliseconds(10)),
            CreateTestPacketEntry(TraceDirection.Output, baseTime.AddMilliseconds(20))
        };

        var result = await _exporter.ExportAsync(packets);
        Assert.That(result, Is.Not.Empty);
        var content = Encoding.UTF8.GetString(result);
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.That(lines.Length, Is.EqualTo(3));
    }

    private static PacketTraceEntry CreateTestPacketEntry(TraceDirection direction, DateTime timestamp)
    {
        var rawData = new byte[] { 0x53, 0x00, 0x08, 0x00, 0x04, 0x60, 0x3F, 0x00 };
        var packet = CreatePacketFromRawData(rawData);

        var createMethod = typeof(PacketTraceEntry).GetMethod("Create",
            BindingFlags.NonPublic | BindingFlags.Static);

        return (PacketTraceEntry)createMethod!.Invoke(null, new object[]
        {
            direction,
            timestamp,
            TimeSpan.FromMilliseconds(10),
            packet
        })!;
    }

    private static Packet CreatePacketFromRawData(byte[] rawData)
    {
        var messageSpy = new MessageSpy();
        if (messageSpy.TryParsePacket(rawData, out var packet) && packet != null)
        {
            return packet;
        }
        throw new InvalidOperationException("Failed to parse test packet");
    }
}

[TestFixture]
[Category("Unit")]
public class ParsedPacketExporterTests
{
    private ParsedPacketExporter _exporter;

    [SetUp]
    public void Setup()
    {
        _exporter = new ParsedPacketExporter();
    }

    [Test]
    public void FileExtension_ReturnsTxt()
    {
        Assert.That(_exporter.FileExtension, Is.EqualTo(".txt"));
    }

    [Test]
    public void ContentType_ReturnsTextPlain()
    {
        Assert.That(_exporter.ContentType, Is.EqualTo("text/plain"));
    }

    [Test]
    public void DisplayName_ReturnsExpectedValue()
    {
        Assert.That(_exporter.DisplayName, Is.EqualTo("Parsed Packets (.txt)"));
    }

    [Test]
    public async Task ExportAsync_WithEmptyPacketList_ReturnsEmptyArray()
    {
        var packets = Enumerable.Empty<PacketTraceEntry>();
        var result = await _exporter.ExportAsync(packets);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task ExportAsync_WithPackets_ReturnsFormattedText()
    {
        var packets = new List<PacketTraceEntry>
        {
            CreateTestPacketEntry(TraceDirection.Output, DateTime.UtcNow)
        };

        var result = await _exporter.ExportAsync(packets);
        Assert.That(result, Is.Not.Empty);

        var content = Encoding.UTF8.GetString(result);
        Assert.That(content, Does.Contain("osdp_POLL"));
    }

    [Test]
    public async Task ExportAsync_WithMultiplePackets_FormatsAll()
    {
        var baseTime = DateTime.UtcNow;
        var packets = new List<PacketTraceEntry>
        {
            CreateTestPacketEntry(TraceDirection.Output, baseTime),
            CreateTestPacketEntry(TraceDirection.Input, baseTime.AddMilliseconds(10))
        };

        var result = await _exporter.ExportAsync(packets);
        var content = Encoding.UTF8.GetString(result);
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.That(lines.Length, Is.GreaterThan(1));
    }

    private static PacketTraceEntry CreateTestPacketEntry(TraceDirection direction, DateTime timestamp)
    {
        var rawData = new byte[] { 0x53, 0x00, 0x08, 0x00, 0x04, 0x60, 0x3F, 0x00 };
        var packet = CreatePacketFromRawData(rawData);

        var createMethod = typeof(PacketTraceEntry).GetMethod("Create",
            BindingFlags.NonPublic | BindingFlags.Static);

        return (PacketTraceEntry)createMethod!.Invoke(null, new object[]
        {
            direction,
            timestamp,
            TimeSpan.FromMilliseconds(10),
            packet
        })!;
    }

    private static Packet CreatePacketFromRawData(byte[] rawData)
    {
        var messageSpy = new MessageSpy();
        if (messageSpy.TryParsePacket(rawData, out var packet) && packet != null)
        {
            return packet;
        }
        throw new InvalidOperationException("Failed to parse test packet");
    }
}

[TestFixture]
[Category("Unit")]
public class IPacketExporterInterfaceTests
{
    [Test]
    public void OsdpCaptureExporter_ImplementsInterface()
    {
        var exporter = new OsdpCaptureExporter();
        Assert.That(exporter, Is.InstanceOf<IPacketExporter>());
    }

    [Test]
    public void ParsedPacketExporter_ImplementsInterface()
    {
        var exporter = new ParsedPacketExporter();
        Assert.That(exporter, Is.InstanceOf<IPacketExporter>());
    }
}
