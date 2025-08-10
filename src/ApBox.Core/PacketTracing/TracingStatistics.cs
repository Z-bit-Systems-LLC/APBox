namespace ApBox.Core.PacketTracing
{
    public class TracingStatistics
    {
        // Reply tracking statistics
        public int TotalOutgoingPackets { get; set; }
        public int PacketsWithReplies { get; set; }
        public double ReplyPercentage => TotalOutgoingPackets > 0 
            ? (double)PacketsWithReplies / TotalOutgoingPackets * 100 
            : 0;
        
        // Response time statistics
        public double AverageResponseTimeMs { get; set; }
        public int ResponseTimeCount { get; set; }
    }
}