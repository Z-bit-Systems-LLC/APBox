using ApBox.Core.PacketTracing.Models;

namespace ApBox.Core.PacketTracing
{
    public class PacketTraceBuffer
    {
        private readonly CircularBuffer<PacketTraceEntry> _buffer;
        private readonly Timer? _cleanupTimer;
        private TimeSpan _maxAge;
        private int _maxSize;
        
        public PacketTraceBuffer(int maxSize = 1000, TimeSpan? maxAge = null)
        {
            _maxSize = maxSize;
            _maxAge = maxAge ?? TimeSpan.Zero;
            _buffer = new CircularBuffer<PacketTraceEntry>(maxSize);
            
            if (maxAge.HasValue)
            {
                _cleanupTimer = new Timer(
                    CleanupOldEntries, 
                    null, 
                    TimeSpan.FromMinutes(1), 
                    TimeSpan.FromMinutes(1));
            }
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
        
        public void UpdateLimits(int? maxSize, TimeSpan? maxAge)
        {
            if (maxSize.HasValue)
                _maxSize = maxSize.Value;
            if (maxAge.HasValue)
                _maxAge = maxAge.Value;
        }
        
        private void CleanupOldEntries(object? state)
        {
            if (_maxAge == TimeSpan.Zero) return;
            
            var cutoff = DateTime.UtcNow - _maxAge;
            // Note: This requires a more sophisticated buffer implementation
            // that supports removal of old entries
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