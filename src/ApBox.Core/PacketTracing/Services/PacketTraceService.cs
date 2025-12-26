using ApBox.Core.PacketTracing.Models;
using ApBox.Core.Services.Reader;
using OSDP.Net.Tracing;

namespace ApBox.Core.PacketTracing.Services
{
    public class PacketTraceService : IPacketTraceService
    {
        private readonly Dictionary<string, PacketTraceBuffer> _readerBuffers = new();
        private readonly Dictionary<string, PacketTraceEntry?> _lastEntries = new();
        private readonly HashSet<string> _activeReaders = new();
        private PacketTraceSettings _settings = new();
        private readonly IReaderConfigurationService? _readerConfigurationService;

        public PacketTraceService(IReaderConfigurationService? readerConfigurationService = null)
        {
            _readerConfigurationService = readerConfigurationService;
        }
        
        public bool IsTracing => _activeReaders.Count > 0;
        public bool IsTracingReader(string readerId) => _activeReaders.Contains(readerId);
        
        public event EventHandler<PacketTraceEntry>? PacketCaptured;
        
        public void StartTracing(string readerId)
        {
            if (!_readerBuffers.ContainsKey(readerId))
            {
                var buffer = new PacketTraceBuffer();
                _readerBuffers[readerId] = buffer;
            }
            
            _activeReaders.Add(readerId);
        }
        
        public void StopTracing(string readerId)
        {
            _activeReaders.Remove(readerId);
        }
        
        public async void StartTracingAll()
        {
            if (_readerConfigurationService == null)
            {
                return; // No reader service available
            }

            try
            {
                var readers = await _readerConfigurationService.GetAllReadersAsync();
                foreach (var reader in readers.Where(r => r.IsEnabled))
                {
                    StartTracing(reader.ReaderId.ToString());
                }
            }
            catch (Exception)
            {
                // If we can't get readers, just continue silently
                // This could happen during testing or if database is not available
            }
        }
        
        public void StopTracingAll()
        {
            _activeReaders.Clear();
        }
        
        public void ClearTraces(string? readerId = null)
        {
            if (readerId != null)
            {
                if (_readerBuffers.ContainsKey(readerId))
                {
                    _readerBuffers[readerId].Clear();
                    _lastEntries.Remove(readerId);
                }
            }
            else
            {
                foreach (var buffer in _readerBuffers.Values)
                {
                    buffer.Clear();
                }
                _lastEntries.Clear();
            }
        }
        
        public IEnumerable<PacketTraceEntry> GetTraces(string? readerId = null, int? limit = null)
        {
            IEnumerable<PacketTraceEntry> traces;
            
            if (readerId != null)
            {
                traces = _readerBuffers.ContainsKey(readerId) 
                    ? _readerBuffers[readerId].GetEntries() 
                    : Enumerable.Empty<PacketTraceEntry>();
            }
            else
            {
                // Return all traces from all readers
                traces = _readerBuffers.Values
                    .SelectMany(b => b.GetEntries())
                    .OrderByDescending(e => e.Timestamp);
            }
            
            return limit.HasValue ? traces.Take(limit.Value) : traces;
        }
        
        public void UpdateSettings(PacketTraceSettings settings)
        {
            _settings = settings;
        }
        
        public PacketTraceSettings GetCurrentSettings() => _settings;
        
        public TracingStatistics GetStatistics()
        {
            var stats = new TracingStatistics();
            
            // Calculate reply percentages across all readers
            CalculateReplyStatistics(stats);
            
            return stats;
        }
        
        private void CalculateReplyStatistics(TracingStatistics stats)
        {
            int totalOutgoing = 0;
            int packetsWithReplies = 0;
            var responseTimes = new List<double>();
            
            // Analyze packets from all readers
            foreach (var buffer in _readerBuffers.Values)
            {
                var packets = buffer.GetEntries().OrderBy(p => p.Timestamp).ToList();
                
                // Find the index of the last outgoing packet to exclude it
                int lastOutgoingIndex = -1;
                for (int i = packets.Count - 1; i >= 0; i--)
                {
                    if (packets[i].Direction == TraceDirection.Output)
                    {
                        lastOutgoingIndex = i;
                        break;
                    }
                }
                
                for (int i = 0; i < packets.Count; i++)
                {
                    var currentPacket = packets[i];
                    
                    // Check if current packet is outgoing
                    if (currentPacket.Direction == TraceDirection.Output)
                    {
                        // Exclude the most recent outgoing packet from statistics
                        if (i == lastOutgoingIndex)
                            continue;
                            
                        totalOutgoing++;
                        
                        // Look for a reply (incoming packet) before the next outgoing packet
                        bool hasReply = false;
                        DateTime? replyTime = null;
                        
                        for (int j = i + 1; j < packets.Count; j++)
                        {
                            var nextPacket = packets[j];
                            
                            // If we find another outgoing packet, stop looking
                            if (nextPacket.Direction == TraceDirection.Output)
                                break;
                            
                            // If we find an incoming packet, this outgoing packet has a reply
                            if (nextPacket.Direction == TraceDirection.Input)
                            {
                                hasReply = true;
                                replyTime = nextPacket.Timestamp;
                                break;
                            }
                        }
                        
                        if (hasReply)
                        {
                            packetsWithReplies++;
                            
                            // Calculate response time if we have both timestamps
                            if (replyTime.HasValue)
                            {
                                var responseTimeMs = (replyTime.Value - currentPacket.Timestamp).TotalMilliseconds;
                                if (responseTimeMs >= 0) // Ensure valid response time
                                {
                                    responseTimes.Add(responseTimeMs);
                                }
                            }
                        }
                    }
                }
            }
            
            stats.TotalOutgoingPackets = totalOutgoing;
            stats.PacketsWithReplies = packetsWithReplies;
            
            // Calculate average response time
            if (responseTimes.Count > 0)
            {
                stats.AverageResponseTimeMs = responseTimes.Average();
                stats.ResponseTimeCount = responseTimes.Count;
            }
            else
            {
                stats.AverageResponseTimeMs = 0;
                stats.ResponseTimeCount = 0;
            }
        }
        
        // Primary method to capture packet from OSDP.Net TraceEntry
        public void CapturePacket(TraceEntry traceEntry, string readerId, string readerName)
        {
            if (!IsTracingReader(readerId)) return;

            // Capture timestamp immediately when packet is received
            var receptionTimestamp = DateTime.UtcNow;

            // Build packet entry using MessageSpy for exception-free parsing
            var builder = new PacketTraceEntryBuilder();
            var entry = builder
                .FromTraceEntry(traceEntry, _lastEntries.GetValueOrDefault(readerId), receptionTimestamp)
                .Build();

            // Skip packets that couldn't be parsed
            if (entry == null) return;

            // Store in buffer
            if (_readerBuffers.TryGetValue(readerId, out var buffer))
            {
                buffer.Add(entry);
                _lastEntries[readerId] = entry;

                // Raise event for real-time UI updates
                PacketCaptured?.Invoke(this, entry);
            }
        }
    }
}