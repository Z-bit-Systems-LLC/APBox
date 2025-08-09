namespace ApBox.Core.PacketTracing
{
    public class TracingStatistics
    {
        public int TotalPackets { get; set; }
        public int FilteredPackets { get; set; }
        public long MemoryUsageBytes { get; set; }
        public string FormattedMemoryUsage => FormatBytes(MemoryUsageBytes);
        public Dictionary<string, int> PacketsPerReader { get; set; } = new();
        public DateTime? TracingStartedAt { get; set; }
        public TimeSpan? TracingDuration => 
            TracingStartedAt.HasValue ? DateTime.UtcNow - TracingStartedAt.Value : null;
        
        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}