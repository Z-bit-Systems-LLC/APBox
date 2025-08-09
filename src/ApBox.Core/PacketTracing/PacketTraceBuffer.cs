using ApBox.Core.PacketTracing.Models;

namespace ApBox.Core.PacketTracing
{
    public class PacketTraceBuffer
    {
        private readonly CircularBuffer<PacketTraceEntry> _buffer;
        
        public PacketTraceBuffer()
        {
            _buffer = new CircularBuffer<PacketTraceEntry>(10000); // Large buffer, memory limit will control size
        }
        
        public int CurrentSize => _buffer.Count;
        public long MemoryUsageBytes { get; private set; }
        
        public void Add(PacketTraceEntry entry)
        {
            _buffer.Add(entry);
            UpdateMemoryUsage();
        }
        
        public void Clear()
        {
            _buffer.Clear();
            MemoryUsageBytes = 0;
        }
        
        public IEnumerable<PacketTraceEntry> GetEntries(int? limit = null)
        {
            var entries = _buffer.AsEnumerable();
            if (limit.HasValue)
            {
                entries = entries.Take(limit.Value);
            }
            return entries.OrderByDescending(e => e.Timestamp);
        }
        
        
        private void UpdateMemoryUsage()
        {
            // Estimate memory usage
            MemoryUsageBytes = _buffer.Count * 
                (sizeof(long) + // Timestamp
                 sizeof(int) +  // Direction
                 100);          // Estimated average packet size
        }
    }
}