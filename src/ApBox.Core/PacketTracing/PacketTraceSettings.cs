namespace ApBox.Core.PacketTracing
{
    public class PacketTraceSettings
    {
        public bool Enabled { get; set; } = false;
        public TraceLimitMode LimitMode { get; set; } = TraceLimitMode.Size;
        public int MaxPacketsPerReader { get; set; } = 500;
        public int MaxPacketsTotal { get; set; } = 5000;
        public int MaxAgeMinutes { get; set; } = 15;
        public bool FilterPollCommands { get; set; } = true;
        public bool FilterAckCommands { get; set; } = false;
        public int MemoryLimitMB { get; set; } = 50;
        public bool AutoStopOnMemoryLimit { get; set; } = true;
        public bool CaptureRawData { get; set; } = true;
        public bool ParseDetails { get; set; } = true;
    }
    
    public enum TraceLimitMode
    {
        Size,      // Limit by number of packets
        Time,      // Limit by age of packets
        Hybrid     // Both size and time limits
    }
}