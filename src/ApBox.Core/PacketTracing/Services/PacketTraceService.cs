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
        private DateTime? _tracingStarted;
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
                var buffer = new PacketTraceBuffer(
                    _settings.MaxPacketsPerReader,
                    _settings.LimitMode != TraceLimitMode.Size ? 
                        TimeSpan.FromMinutes(_settings.MaxAgeMinutes) : null);
                _readerBuffers[readerId] = buffer;
            }
            
            _activeReaders.Add(readerId);
            
            if (_tracingStarted == null)
                _tracingStarted = DateTime.UtcNow;
        }
        
        public void StopTracing(string readerId)
        {
            _activeReaders.Remove(readerId);
            
            if (_activeReaders.Count == 0)
                _tracingStarted = null;
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
            _tracingStarted = null;
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
            
            // Update buffer limits
            foreach (var buffer in _readerBuffers.Values)
            {
                buffer.UpdateLimits(
                    settings.MaxPacketsPerReader,
                    settings.LimitMode != TraceLimitMode.Size ? 
                        TimeSpan.FromMinutes(settings.MaxAgeMinutes) : null);
            }
        }
        
        public PacketTraceSettings GetCurrentSettings() => _settings;
        
        public TracingStatistics GetStatistics()
        {
            var stats = new TracingStatistics
            {
                TracingStartedAt = _tracingStarted,
                PacketsPerReader = new Dictionary<string, int>(),
                FilteredPackets = 0 // Will be calculated based on current filter settings
            };
            
            foreach (var kvp in _readerBuffers)
            {
                var count = kvp.Value.CurrentSize;
                stats.PacketsPerReader[kvp.Key] = count;
                stats.TotalPackets += count;
                stats.MemoryUsageBytes += kvp.Value.MemoryUsageBytes;
            }
            
            // FilteredPackets will be calculated by the ViewModel based on display filters
            
            return stats;
        }
        
        // Primary method to capture packet from OSDP.Net TraceEntry
        public void CapturePacket(TraceEntry traceEntry, string readerId, string readerName)
        {
            if (!IsTracingReader(readerId)) return;
            
            // Build packet entry using OSDP-Bench pattern
            var builder = new PacketTraceEntryBuilder();
            PacketTraceEntry entry;
            try 
            {
                entry = builder.FromTraceEntry(traceEntry, _lastEntries.ContainsKey(readerId) ? _lastEntries[readerId] : null).Build();
            }
            catch (Exception)
            {
                return; // Skip entries that can't be parsed
            }
            
            // Store in buffer
            if (_readerBuffers.ContainsKey(readerId))
            {
                _readerBuffers[readerId].Add(entry);
                _lastEntries[readerId] = entry;
                
                // Raise event for real-time UI updates
                PacketCaptured?.Invoke(this, entry);
            }
            
            // Check memory limits
            CheckMemoryLimits();
        }
        
        // Legacy method for backward compatibility
        public void CapturePacket(byte[] rawData, TraceDirection direction, 
            string readerId, string readerName, byte address)
        {
            // For legacy support, we would need to create a TraceEntry
            // But since OSDP.Net provides TraceEntry objects, this method may not be needed
            throw new NotSupportedException("Use CapturePacket(TraceEntry, string, string) instead - OSDP.Net provides TraceEntry objects directly");
        }
        
        
        private void CheckMemoryLimits()
        {
            var stats = GetStatistics();
            var limitBytes = _settings.MemoryLimitMB * 1024 * 1024;
            
            if (stats.MemoryUsageBytes > limitBytes && _settings.AutoStopOnMemoryLimit)
            {
                StopTracingAll();
                // Raise memory limit event
            }
        }
        
    }
}