using ApBox.Core.PacketTracing.Models;
using ApBox.Core.Services.Reader;

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
        
        public IEnumerable<PacketTraceEntry> GetTraces(string? readerId = null, int? limit = null, bool filterPollCommands = false, bool filterAckCommands = false)
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
            
            // Apply filters if specified
            if (filterPollCommands || filterAckCommands)
            {
                traces = traces.Where(entry => 
                {
                    if (entry.RawData == null) return true;
                    if (filterPollCommands && IsPollCommand(entry.RawData))
                        return false;
                    if (filterAckCommands && IsAckReply(entry.RawData))
                        return false;
                    return true;
                });
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
            
            // Calculate filtered packets based on current filter settings
            if (_settings.FilterPollCommands || _settings.FilterAckCommands)
            {
                var allPackets = _readerBuffers.Values.SelectMany(b => b.GetEntries());
                foreach (var packet in allPackets)
                {
                    if (packet.RawData != null && 
                        ((_settings.FilterPollCommands && IsPollCommand(packet.RawData)) ||
                         (_settings.FilterAckCommands && IsAckReply(packet.RawData))))
                    {
                        stats.FilteredPackets++;
                    }
                }
            }
            
            return stats;
        }
        
        // This method would be called by the OSDP communication layer
        public void CapturePacket(byte[] rawData, PacketDirection direction, 
            string readerId, string readerName, byte address)
        {
            if (!IsTracingReader(readerId)) return;
            
            // Build packet entry
            var builder = new PacketTraceEntryBuilder()
                .FromRawData(rawData)
                .WithDirection(direction)
                .WithReader(readerId, readerName, address);
            
            if (_lastEntries.ContainsKey(readerId))
            {
                builder.WithPreviousEntry(_lastEntries[readerId]);
            }
            
            var entry = builder.Build();
            
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
        
        private bool IsPollCommand(byte[] data)
        {
            // Check if packet is OSDP Poll command (0x60)
            // OSDP packets typically have: SOM (0x53), Address, LEN_LSB, LEN_MSB, Control, Data..., Checksum
            if (data.Length < 6) return false;
            
            // Look for OSDP SOM (Start of Message)
            if (data[0] != 0x53) return false;
            
            // Check for Poll command (0x60) in the data portion
            // The command is typically at position 4 after SOM, Address, LEN_LSB, LEN_MSB
            return data.Length > 4 && data[4] == 0x60;
        }
        
        private bool IsAckReply(byte[] data)
        {
            // Check if packet is OSDP ACK reply (0x40)
            if (data.Length < 6) return false;
            
            // Look for OSDP SOM (Start of Message)
            if (data[0] != 0x53) return false;
            
            // Check for ACK reply (0x40) in the data portion
            return data.Length > 4 && data[4] == 0x40;
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