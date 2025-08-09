namespace ApBox.Core.PacketTracing
{
    public class PacketTraceSettings
    {
        public bool Enabled { get; set; } = false;
        public int MemoryLimitMB { get; set; } = 10;
        public bool AutoStopOnMemoryLimit { get; set; } = true;
        public bool FilterPollCommands { get; set; } = true;
        public bool FilterAckCommands { get; set; } = false;
    }
}