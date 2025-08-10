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
        
        public void Add(PacketTraceEntry entry)
        {
            _buffer.Add(entry);
        }
        
        public void Clear()
        {
            _buffer.Clear();
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
    }
}