using System.Text;
using System.Text.Json;
using ApBox.Core.PacketTracing.Models;
using OSDP.Net.Tracing;

namespace ApBox.Core.PacketTracing.Export
{
    public class OsdpCapExporter
    {
        public async Task<byte[]> ExportToOsdpCapAsync(
            IEnumerable<PacketTraceEntry> packets,
            OsdpCapMetadata metadata)
        {
            var stringBuilder = new StringBuilder();
            
            foreach (var packet in packets)
            {
                var unixTime = packet.Timestamp.Subtract(new DateTime(1970, 1, 1));
                long timeNano = (unixTime.Ticks - (long)Math.Floor(unixTime.TotalSeconds) * TimeSpan.TicksPerSecond) * 100L;
                
                var traceEntry = new 
                {
                    timeSec = Math.Floor(unixTime.TotalSeconds).ToString("F0"),
                    timeNano = timeNano.ToString("000000000"),
                    io = packet.Direction == TraceDirection.Input ? "input" : "output",
                    data = GetPacketDataAsHex(packet),
                    osdpTraceVersion = "1",
                    osdpSource = "APBox"
                };
                
                var line = JsonSerializer.Serialize(traceEntry);
                stringBuilder.AppendLine(line);
            }
            
            await Task.CompletedTask; // Keep async for consistency
            return Encoding.UTF8.GetBytes(stringBuilder.ToString());
        }
        
        private static string GetPacketDataAsHex(PacketTraceEntry packet)
        {
            // Use the raw packet data from OSDP.Net Packet
            return BitConverter.ToString(packet.Packet.RawData.ToArray());
        }
    }
    
    public class OsdpCapMetadata
    {
        public string DeviceName { get; set; } = string.Empty;
        public DateTime CaptureStartTime { get; set; }
        public DateTime CaptureEndTime { get; set; }
        public string Version { get; set; } = "1.0";
        public Dictionary<string, string> CustomFields { get; set; } = new();
    }
}