using System;
using System.Text;
using System.Text.RegularExpressions;

namespace ApBox.Core.PacketTracing.Models
{
    public class PacketTraceEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public DateTime LocalTimestamp => Timestamp.ToLocalTime();
        public TimeSpan? Interval { get; set; }
        
        public PacketDirection Direction { get; set; }
        public string ReaderId { get; set; } = string.Empty;
        public string ReaderName { get; set; } = string.Empty;
        public byte Address { get; set; }
        
        public byte[]? RawData { get; set; }
        public int Length => RawData?.Length ?? 0;
        
        public string? Type { get; set; }
        public string? Command { get; set; }
        public string? Reply { get; set; }
        
        public bool IsSecure { get; set; }
        public int? Sequence { get; set; }
        public bool IsValid { get; set; }
        public string? ValidationError { get; set; }
        
        public string? Details { get; set; }
        public string? SessionId { get; set; }
        
        // Factory method pattern
        public static PacketTraceEntry Create(
            byte[] rawData,
            PacketDirection direction,
            string readerId,
            string readerName,
            byte address,
            PacketTraceEntry? previousEntry = null)
        {
            var entry = new PacketTraceEntry
            {
                RawData = rawData,
                Direction = direction,
                ReaderId = readerId,
                ReaderName = readerName,
                Address = address,
                Timestamp = DateTime.UtcNow
            };
            
            if (previousEntry != null)
            {
                entry.Interval = entry.Timestamp - previousEntry.Timestamp;
            }
            
            return entry;
        }
        
        public string GetHexDisplay()
        {
            if (RawData == null || RawData.Length == 0) return string.Empty;
            
            var sb = new StringBuilder();
            for (int i = 0; i < RawData.Length; i++)
            {
                if (i > 0 && i % 16 == 0) sb.AppendLine();
                if (i > 0 && i % 8 == 0 && i % 16 != 0) sb.Append("  ");
                sb.AppendFormat("{0:X2} ", RawData[i]);
            }
            return sb.ToString().TrimEnd();
        }
        
        public string GetAsciiDisplay()
        {
            if (RawData == null) return string.Empty;
            
            var sb = new StringBuilder();
            foreach (var b in RawData)
            {
                sb.Append(b >= 32 && b <= 126 ? (char)b : '.');
            }
            return sb.ToString();
        }
        
        // Format enum names with spaces for readability
        private static string ToSpacedString(Enum enumValue)
        {
            return Regex.Replace(enumValue.ToString(), "(?<!^)([A-Z](?=[a-z]))", " $1");
        }
    }
    
    public enum PacketDirection
    {
        Outgoing,  // Control Panel to Reader
        Incoming   // Reader to Control Panel
    }
}