using ApBox.Core.PacketTracing.Models;
using OSDP.Net.Tracing;

namespace ApBox.Core.PacketTracing.Export;

/// <summary>
/// Exports packet traces to the .osdpcap format using OSDP.Net's OSDPCaptureFileWriter.
/// This format is compatible with OSDP trace analysis tools.
/// </summary>
public class OsdpCaptureExporter : IPacketExporter
{
    private const string DefaultSource = "ApBox";

    public string FileExtension => ".osdpcap";
    public string ContentType => "application/octet-stream";
    public string DisplayName => "OSDP Capture (.osdpcap)";

    public Task<byte[]> ExportAsync(IEnumerable<PacketTraceEntry> packets)
    {
        var packetList = packets.ToList();
        if (packetList.Count == 0)
        {
            return Task.FromResult(Array.Empty<byte>());
        }

        var tempFile = Path.GetTempFileName();
        try
        {
            using (var writer = new OSDPCaptureFileWriter(tempFile, DefaultSource, append: false))
            {
                foreach (var packet in packetList.OrderBy(p => p.Timestamp))
                {
                    writer.WritePacket(
                        packet.Packet.RawData.ToArray(),
                        packet.Direction,
                        packet.Timestamp);
                }
            }

            var bytes = File.ReadAllBytes(tempFile);
            return Task.FromResult(bytes);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
