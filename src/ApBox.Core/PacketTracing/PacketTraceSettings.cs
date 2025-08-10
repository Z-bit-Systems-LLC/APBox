namespace ApBox.Core.PacketTracing
{
    public class PacketTraceSettings
    {
        public bool Enabled { get; init; }

        public int MemoryLimitMB { get; init; } = 10;
        
        public bool FilterPollCommands { get; init; } = true;
        
        public bool FilterAckCommands { get; init; }
    }
}