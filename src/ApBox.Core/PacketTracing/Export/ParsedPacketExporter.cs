using System.Text;
using ApBox.Core.PacketTracing.Models;
using OSDP.Net.Tracing;

namespace ApBox.Core.PacketTracing.Export;

/// <summary>
/// Exports packet traces to a human-readable text format using OSDP.Net's OSDPPacketTextFormatter.
/// </summary>
public class ParsedPacketExporter : IPacketExporter
{
    private readonly OSDPPacketTextFormatter _formatter = new();

    public string FileExtension => ".txt";
    public string ContentType => "text/plain";
    public string DisplayName => "Parsed Packets (.txt)";

    public Task<byte[]> ExportAsync(IEnumerable<PacketTraceEntry> packets)
    {
        var packetList = packets.OrderBy(p => p.Timestamp).ToList();
        if (packetList.Count == 0)
        {
            return Task.FromResult(Array.Empty<byte>());
        }

        var stringBuilder = new StringBuilder();

        foreach (var packet in packetList)
        {
            var formattedPacket = _formatter.FormatPacket(
                packet.Packet,
                packet.Timestamp,
                packet.Interval.TotalMilliseconds > 0 ? packet.Interval : null);

            stringBuilder.AppendLine(formattedPacket);
        }

        return Task.FromResult(Encoding.UTF8.GetBytes(stringBuilder.ToString()));
    }
}
