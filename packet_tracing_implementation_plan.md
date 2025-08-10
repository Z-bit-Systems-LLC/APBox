# OSDP Packet Tracing Implementation Plan for ApBox

## Overview
Implement a comprehensive packet tracing system for ApBox based on OSDP-Bench's approach, featuring in-memory storage, UI-configurable settings, and OSDPCAP export capability.

## Implementation Tasks

### 1. Core Models (`src/ApBox.Core/PacketTracing/Models/`)

#### PacketTraceEntry.cs
```csharp
using System;
using System.Text;
using System.Text.RegularExpressions;

namespace ApBox.Core.PacketTracing.Models
{
    public class PacketTraceEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public DateTime LocalTimestamp => Timestamp.ToLocalTime();
        public TimeSpan? Interval { get; set; }
        
        public PacketDirection Direction { get; set; }
        public string ReaderId { get; set; } = string.Empty;
        public string ReaderName { get; set; } = string.Empty;
        public byte Address { get; set; }
        
        public byte[]? RawData { get; set; }
        public int Length => RawData?.Length ?? 0;
        
        public string? Type { get; set; }
        public string? Command { get; set; }
        public string? Reply { get; set; }
        
        public bool IsSecure { get; set; }
        public int? Sequence { get; set; }
        public bool IsValid { get; set; }
        public string? ValidationError { get; set; }
        
        public string? Details { get; set; }
        public string? SessionId { get; set; }
        
        // Factory method pattern
        public static PacketTraceEntry Create(
            byte[] rawData,
            PacketDirection direction,
            string readerId,
            string readerName,
            byte address,
            PacketTraceEntry? previousEntry = null)
        {
            var entry = new PacketTraceEntry
            {
                RawData = rawData,
                Direction = direction,
                ReaderId = readerId,
                ReaderName = readerName,
                Address = address,
                Timestamp = DateTime.UtcNow
            };
            
            if (previousEntry != null)
            {
                entry.Interval = entry.Timestamp - previousEntry.Timestamp;
            }
            
            return entry;
        }
        
        public string GetHexDisplay()
        {
            if (RawData == null || RawData.Length == 0) return string.Empty;
            
            var sb = new StringBuilder();
            for (int i = 0; i < RawData.Length; i++)
            {
                if (i > 0 && i % 16 == 0) sb.AppendLine();
                if (i > 0 && i % 8 == 0 && i % 16 != 0) sb.Append("  ");
                sb.AppendFormat("{0:X2} ", RawData[i]);
            }
            return sb.ToString().TrimEnd();
        }
        
        public string GetAsciiDisplay()
        {
            if (RawData == null) return string.Empty;
            
            var sb = new StringBuilder();
            foreach (var b in RawData)
            {
                sb.Append(b >= 32 && b <= 126 ? (char)b : '.');
            }
            return sb.ToString();
        }
        
        // Format enum names with spaces for readability
        private static string ToSpacedString(Enum enumValue)
        {
            return Regex.Replace(enumValue.ToString(), "(?<!^)([A-Z](?=[a-z]))", " $1");
        }
    }
    
    public enum PacketDirection
    {
        Outgoing,  // Control Panel to Reader
        Incoming   // Reader to Control Panel
    }
}
```

#### PacketTraceEntryBuilder.cs
```csharp
namespace ApBox.Core.PacketTracing.Models
{
    public class PacketTraceEntryBuilder
    {
        private byte[]? _rawData;
        private PacketDirection _direction;
        private string _readerId = string.Empty;
        private string _readerName = string.Empty;
        private byte _address;
        private PacketTraceEntry? _previousEntry;
        
        public PacketTraceEntryBuilder FromRawData(byte[] rawData)
        {
            _rawData = rawData;
            return this;
        }
        
        public PacketTraceEntryBuilder WithDirection(PacketDirection direction)
        {
            _direction = direction;
            return this;
        }
        
        public PacketTraceEntryBuilder WithReader(string readerId, string readerName, byte address)
        {
            _readerId = readerId;
            _readerName = readerName;
            _address = address;
            return this;
        }
        
        public PacketTraceEntryBuilder WithPreviousEntry(PacketTraceEntry? previousEntry)
        {
            _previousEntry = previousEntry;
            return this;
        }
        
        public PacketTraceEntry Build()
        {
            if (_rawData == null)
                throw new InvalidOperationException("Raw data is required");
                
            var entry = PacketTraceEntry.Create(
                _rawData, 
                _direction, 
                _readerId, 
                _readerName, 
                _address, 
                _previousEntry);
            
            // Parse OSDP packet details here using OSDP.Net
            // entry.Type = ParsePacketType(_rawData);
            // entry.Details = ParsePacketDetails(_rawData);
            
            return entry;
        }
    }
}
```

### 2. In-Memory Packet Store (`src/ApBox.Core/PacketTracing/`)

#### CircularBuffer.cs
```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace ApBox.Core.PacketTracing
{
    public class CircularBuffer<T> : IEnumerable<T>
    {
        private readonly T[] _buffer;
        private readonly ReaderWriterLockSlim _lock = new();
        private int _head;
        private int _tail;
        private int _count;
        
        public CircularBuffer(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentException("Capacity must be positive", nameof(capacity));
            
            _buffer = new T[capacity];
        }
        
        public int Capacity => _buffer.Length;
        public int Count
        {
            get
            {
                _lock.EnterReadLock();
                try { return _count; }
                finally { _lock.ExitReadLock(); }
            }
        }
        
        public void Add(T item)
        {
            _lock.EnterWriteLock();
            try
            {
                _buffer[_tail] = item;
                _tail = (_tail + 1) % _buffer.Length;
                
                if (_count < _buffer.Length)
                {
                    _count++;
                }
                else
                {
                    _head = (_head + 1) % _buffer.Length;
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
        
        public void Clear()
        {
            _lock.EnterWriteLock();
            try
            {
                _head = 0;
                _tail = 0;
                _count = 0;
                Array.Clear(_buffer, 0, _buffer.Length);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
        
        public IEnumerator<T> GetEnumerator()
        {
            _lock.EnterReadLock();
            try
            {
                var items = new List<T>(_count);
                var index = _head;
                for (int i = 0; i < _count; i++)
                {
                    items.Add(_buffer[index]);
                    index = (index + 1) % _buffer.Length;
                }
                return items.GetEnumerator();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
        
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
```

#### PacketTraceBuffer.cs
```csharp
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
```

### 3. Packet Capture Service (`src/ApBox.Core/PacketTracing/Services/`)

#### IPacketTraceService.cs
```csharp
namespace ApBox.Core.PacketTracing.Services
{
    public interface IPacketTraceService
    {
        bool IsTracing { get; }
        bool IsTracingReader(string readerId);
        
        void StartTracing(string readerId);
        void StopTracing(string readerId);
        void StartTracingAll();
        void StopTracingAll();
        void ClearTraces(string? readerId = null);
        
        IEnumerable<PacketTraceEntry> GetTraces(string? readerId = null, int? limit = null);
        
        void UpdateSettings(PacketTraceSettings settings);
        PacketTraceSettings GetCurrentSettings();
        TracingStatistics GetStatistics();
        
        event EventHandler<PacketTraceEntry>? PacketCaptured;
    }
}
```

#### PacketTraceSettings.cs
```csharp
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
```

#### TracingStatistics.cs
```csharp
namespace ApBox.Core.PacketTracing
{
    public class TracingStatistics
    {
        public int TotalPackets { get; set; }
        public int FilteredPackets { get; set; }
        public long MemoryUsageBytes { get; set; }
        public string FormattedMemoryUsage => FormatBytes(MemoryUsageBytes);
        public Dictionary<string, int> PacketsPerReader { get; set; } = new();
        public DateTime? TracingStartedAt { get; set; }
        public TimeSpan? TracingDuration => 
            TracingStartedAt.HasValue ? DateTime.UtcNow - TracingStartedAt.Value : null;
        
        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
```

#### PacketTraceService.cs
```csharp
namespace ApBox.Core.PacketTracing.Services
{
    public class PacketTraceService : IPacketTraceService
    {
        private readonly Dictionary<string, PacketTraceBuffer> _readerBuffers = new();
        private readonly Dictionary<string, PacketTraceEntry?> _lastEntries = new();
        private readonly HashSet<string> _activeReaders = new();
        private PacketTraceSettings _settings = new();
        private DateTime? _tracingStarted;
        
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
        
        public void StartTracingAll()
        {
            // Get all configured readers and start tracing
            // This would integrate with the reader management service
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
            if (readerId != null)
            {
                return _readerBuffers.ContainsKey(readerId) 
                    ? _readerBuffers[readerId].GetEntries(limit) 
                    : Enumerable.Empty<PacketTraceEntry>();
            }
            
            // Return all traces from all readers
            var allTraces = _readerBuffers.Values
                .SelectMany(b => b.GetEntries())
                .OrderByDescending(e => e.Timestamp);
            
            return limit.HasValue ? allTraces.Take(limit.Value) : allTraces;
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
                PacketsPerReader = new Dictionary<string, int>()
            };
            
            foreach (var kvp in _readerBuffers)
            {
                var count = kvp.Value.CurrentSize;
                stats.PacketsPerReader[kvp.Key] = count;
                stats.TotalPackets += count;
                stats.MemoryUsageBytes += kvp.Value.MemoryUsageBytes;
            }
            
            return stats;
        }
        
        // This method would be called by the OSDP communication layer
        public void CapturePacket(byte[] rawData, PacketDirection direction, 
            string readerId, string readerName, byte address)
        {
            if (!IsTracingReader(readerId)) return;
            
            // Apply filters
            if (_settings.FilterPollCommands && IsPollCommand(rawData))
            {
                GetStatistics().FilteredPackets++;
                return;
            }
            
            if (_settings.FilterAckCommands && IsAckReply(rawData))
            {
                GetStatistics().FilteredPackets++;
                return;
            }
            
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
            // This would use OSDP.Net packet parsing
            return false; // Placeholder
        }
        
        private bool IsAckReply(byte[] data)
        {
            // Check if packet is OSDP ACK reply (0x40)
            // This would use OSDP.Net packet parsing
            return false; // Placeholder
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
```

### 4. UI Components (`src/ApBox.Web/Pages/PacketTrace/`)

#### PacketTraceViewModel.cs
```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Blazorise.Localization;

namespace ApBox.Web.Pages.PacketTrace
{
    [ObservableObject]
    public partial class PacketTraceViewModel
    {
        private readonly IPacketTraceService _traceService;
        private readonly ILocalStorageService _localStorage;
        private readonly ISnackbar _snackbar;
        private const string SETTINGS_KEY = "packetTraceSettings";
        
        [ObservableProperty]
        private ObservableCollection<PacketTraceEntry> _packets = new();
        
        // Live Settings
        [ObservableProperty]
        private bool _tracingEnabled;
        
        [ObservableProperty]
        private int _maxPacketsPerReader = 500;
        
        [ObservableProperty]
        private int _maxAgeMinutes = 15;
        
        [ObservableProperty]
        private bool _filterPollCommands = true;
        
        [ObservableProperty]
        private bool _filterAckCommands = false;
        
        [ObservableProperty]
        private TraceLimitMode _limitMode = TraceLimitMode.Size;
        
        // Statistics
        [ObservableProperty]
        private string _memoryUsage = "0 KB";
        
        [ObservableProperty]
        private int _totalPackets;
        
        [ObservableProperty]
        private int _filteredPackets;
        
        [ObservableProperty]
        private string _tracingDuration = "00:00:00";
        
        public PacketTraceViewModel(
            IPacketTraceService traceService,
            ILocalStorageService localStorage,
            ISnackbar snackbar)
        {
            _traceService = traceService;
            _localStorage = localStorage;
            _snackbar = snackbar;
            
            // Subscribe to packet capture events
            _traceService.PacketCaptured += OnPacketCaptured;
        }
        
        public async Task InitializeAsync()
        {
            // Load settings from LocalStorage
            if (await _localStorage.ContainKeyAsync(SETTINGS_KEY))
            {
                var settings = await _localStorage.GetItemAsync<PacketTraceSettings>(SETTINGS_KEY);
                if (settings != null)
                {
                    ApplySettingsToViewModel(settings);
                }
            }
            
            // Load existing traces
            RefreshPacketList();
            UpdateStatistics();
        }
        
        [RelayCommand]
        private async Task ApplySettingsAsync()
        {
            var settings = new PacketTraceSettings
            {
                Enabled = TracingEnabled,
                MaxPacketsPerReader = MaxPacketsPerReader,
                MaxAgeMinutes = MaxAgeMinutes,
                FilterPollCommands = FilterPollCommands,
                FilterAckCommands = FilterAckCommands,
                LimitMode = LimitMode
            };
            
            // Save to LocalStorage
            await _localStorage.SetItemAsync(SETTINGS_KEY, settings);
            
            // Apply to service
            _traceService.UpdateSettings(settings);
            
            _snackbar.Add("Settings applied successfully", Snackbar.Color.Success);
        }
        
        [RelayCommand]
        private async Task StartTracingAsync()
        {
            _traceService.StartTracingAll();
            TracingEnabled = true;
            _snackbar.Add("Packet tracing started", Snackbar.Color.Success);
        }
        
        [RelayCommand]
        private async Task StopTracingAsync()
        {
            _traceService.StopTracingAll();
            TracingEnabled = false;
            _snackbar.Add("Packet tracing stopped", Snackbar.Color.Info);
        }
        
        [RelayCommand]
        private void ClearAllTraces()
        {
            _traceService.ClearTraces();
            Packets.Clear();
            UpdateStatistics();
            _snackbar.Add("All traces cleared", Snackbar.Color.Warning);
        }
        
        [RelayCommand]
        private async Task ExportToOsdpCapAsync()
        {
            try
            {
                var exporter = new OsdpCapExporter();
                var metadata = new OsdpCapMetadata
                {
                    DeviceName = "ApBox",
                    CaptureStartTime = _traceService.GetStatistics().TracingStartedAt ?? DateTime.UtcNow,
                    CaptureEndTime = DateTime.UtcNow,
                    Version = "1.0"
                };
                
                var data = await exporter.ExportToOsdpCapAsync(Packets, metadata);
                
                // Trigger file download
                await DownloadFileAsync("capture.osdpcap", data);
                
                _snackbar.Add("Export completed", Snackbar.Color.Success);
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Export failed: {ex.Message}", Snackbar.Color.Danger);
            }
        }
        
        private void OnPacketCaptured(object? sender, PacketTraceEntry e)
        {
            // Add to UI collection (limit to recent 100 for performance)
            Application.Current?.Dispatcher.Dispatch(() =>
            {
                Packets.Insert(0, e);
                if (Packets.Count > 100)
                {
                    Packets.RemoveAt(Packets.Count - 1);
                }
                UpdateStatistics();
            });
        }
        
        private void RefreshPacketList()
        {
            var traces = _traceService.GetTraces(limit: 100);
            Packets.Clear();
            foreach (var trace in traces)
            {
                Packets.Add(trace);
            }
        }
        
        private void UpdateStatistics()
        {
            var stats = _traceService.GetStatistics();
            TotalPackets = stats.TotalPackets;
            FilteredPackets = stats.FilteredPackets;
            MemoryUsage = stats.FormattedMemoryUsage;
            
            if (stats.TracingDuration.HasValue)
            {
                TracingDuration = stats.TracingDuration.Value.ToString(@"hh\:mm\:ss");
            }
        }
        
        private void ApplySettingsToViewModel(PacketTraceSettings settings)
        {
            TracingEnabled = settings.Enabled;
            MaxPacketsPerReader = settings.MaxPacketsPerReader;
            MaxAgeMinutes = settings.MaxAgeMinutes;
            FilterPollCommands = settings.FilterPollCommands;
            FilterAckCommands = settings.FilterAckCommands;
            LimitMode = settings.LimitMode;
        }
        
        private async Task DownloadFileAsync(string filename, byte[] data)
        {
            // Implementation would use JavaScript interop to trigger browser download
            // await _jsRuntime.InvokeVoidAsync("downloadFile", filename, data);
        }
    }
}
```

#### PacketTrace.razor
```razor
@page "/packet-trace"
@using ApBox.Core.PacketTracing
@using ApBox.Core.PacketTracing.Models
@inject ILocalStorageService LocalStorage
@inject PacketTraceViewModel ViewModel

<PageTitle>Packet Trace</PageTitle>

<Container Fluid>
    <Row>
        <Column ColumnSize="ColumnSize.Is3.OnDesktop.Is12.OnMobile">
            <!-- Settings Panel -->
            <Card>
                <CardHeader>
                    <CardTitle>
                        <Icon Name="IconName.Settings" /> Trace Settings
                    </CardTitle>
                </CardHeader>
                <CardBody>
                    <!-- Enable/Disable Toggle -->
                    <Field>
                        <Switch @bind-Checked="@ViewModel.TracingEnabled" Disabled="@ViewModel.IsTracing">
                            Enable Tracing
                        </Switch>
                    </Field>
                    
                    <Divider />
                    
                    <!-- Limit Mode -->
                    <Field>
                        <FieldLabel>Limit Mode</FieldLabel>
                        <RadioGroup @bind-CheckedValue="@ViewModel.LimitMode" Orientation="Orientation.Vertical">
                            <Radio Value="TraceLimitMode.Size">By Count</Radio>
                            <Radio Value="TraceLimitMode.Time">By Age</Radio>
                            <Radio Value="TraceLimitMode.Hybrid">Both</Radio>
                        </RadioGroup>
                    </Field>
                    
                    <!-- Max Packets -->
                    <Field>
                        <FieldLabel>Max Packets/Reader</FieldLabel>
                        <NumericEdit @bind-Value="@ViewModel.MaxPacketsPerReader" 
                                     Min="100" Max="10000" Step="100" />
                        <FieldHelp>Maximum packets to store per reader</FieldHelp>
                    </Field>
                    
                    <!-- Max Age -->
                    <Field>
                        <FieldLabel>Max Age (minutes)</FieldLabel>
                        <NumericEdit @bind-Value="@ViewModel.MaxAgeMinutes" 
                                     Min="1" Max="60" Step="5"
                                     Disabled="@(ViewModel.LimitMode == TraceLimitMode.Size)" />
                        <FieldHelp>Remove packets older than this</FieldHelp>
                    </Field>
                    
                    <Divider />
                    
                    <!-- Filters -->
                    <Field>
                        <FieldLabel>Packet Filters</FieldLabel>
                        <Check @bind-Checked="@ViewModel.FilterPollCommands">
                            Hide Poll Commands
                        </Check>
                        <Check @bind-Checked="@ViewModel.FilterAckCommands">
                            Hide ACK Replies
                        </Check>
                    </Field>
                    
                    <!-- Apply Button -->
                    <Button Color="Color.Primary" 
                            Clicked="@ViewModel.ApplySettingsAsync" 
                            Block>
                        <Icon Name="IconName.Save" /> Apply Settings
                    </Button>
                </CardBody>
            </Card>
            
            <!-- Statistics Card -->
            <Card Margin="Margin.Is3.FromTop">
                <CardHeader>
                    <CardTitle>
                        <Icon Name="IconName.ChartLine" /> Statistics
                    </CardTitle>
                </CardHeader>
                <CardBody>
                    <Field>
                        <FieldLabel>Memory Usage</FieldLabel>
                        <Text>@ViewModel.MemoryUsage</Text>
                        <Progress Value="@GetMemoryPercentage()" Color="@GetMemoryProgressColor()" />
                    </Field>
                    
                    <Field>
                        <FieldLabel>Total Packets</FieldLabel>
                        <Badge Color="Color.Primary">@ViewModel.TotalPackets</Badge>
                    </Field>
                    
                    <Field>
                        <FieldLabel>Filtered</FieldLabel>
                        <Badge Color="Color.Warning">@ViewModel.FilteredPackets</Badge>
                    </Field>
                    
                    <Field>
                        <FieldLabel>Duration</FieldLabel>
                        <Text>@ViewModel.TracingDuration</Text>
                    </Field>
                </CardBody>
            </Card>
        </Column>
        
        <Column ColumnSize="ColumnSize.Is9.OnDesktop.Is12.OnMobile">
            <!-- Packet Trace Grid -->
            <Card>
                <CardHeader>
                    <CardTitle>
                        <Icon Name="IconName.List" /> Packet Traces
                    </CardTitle>
                    <CardActions>
                        <ButtonGroup>
                            <Button Color="Color.Success" 
                                    Clicked="@ViewModel.StartTracingAsync" 
                                    Disabled="@ViewModel.TracingEnabled"
                                    Size="Size.Small">
                                <Icon Name="IconName.Play" /> Start
                            </Button>
                            <Button Color="Color.Danger" 
                                    Clicked="@ViewModel.StopTracingAsync"
                                    Disabled="@(!ViewModel.TracingEnabled)"
                                    Size="Size.Small">
                                <Icon Name="IconName.Stop" /> Stop
                            </Button>
                            <Button Color="Color.Warning" 
                                    Clicked="@ViewModel.ClearAllTraces"
                                    Size="Size.Small">
                                <Icon Name="IconName.Clear" /> Clear
                            </Button>
                            <Button Color="Color.Info" 
                                    Clicked="@ViewModel.ExportToOsdpCapAsync"
                                    Size="Size.Small"
                                    Disabled="@(ViewModel.TotalPackets == 0)">
                                <Icon Name="IconName.Download" /> Export
                            </Button>
                        </ButtonGroup>
                    </CardActions>
                </CardHeader>
                <CardBody>
                    @if (ViewModel.Packets.Any())
                    {
                        <DataGrid TItem="PacketTraceEntry" 
                                  Data="@ViewModel.Packets" 
                                  Striped 
                                  Bordered 
                                  Hoverable 
                                  Responsive
                                  ShowPager 
                                  PageSize="50"
                                  Virtualize
                                  VirtualizeOptions="@(new() { ItemHeight = 45 })">
                            
                            <DataGridColumn Field="@nameof(PacketTraceEntry.LocalTimestamp)" 
                                            Caption="Time" 
                                            Width="150px">
                                <DisplayTemplate>
                                    <Small>@context.LocalTimestamp.ToString("HH:mm:ss.fff")</Small>
                                </DisplayTemplate>
                            </DataGridColumn>
                            
                            <DataGridColumn Field="@nameof(PacketTraceEntry.Interval)" 
                                            Caption="Δt" 
                                            Width="80px">
                                <DisplayTemplate>
                                    @if (context.Interval.HasValue)
                                    {
                                        <Small Class="text-muted">
                                            +@context.Interval.Value.TotalMilliseconds.ToString("F0")ms
                                        </Small>
                                    }
                                </DisplayTemplate>
                            </DataGridColumn>
                            
                            <DataGridColumn Field="@nameof(PacketTraceEntry.Direction)" 
                                            Caption="Direction" 
                                            Width="100px">
                                <DisplayTemplate>
                                    <Badge Color="@(context.Direction == PacketDirection.Outgoing ? 
                                                  Color.Primary : Color.Success)">
                                        @if (context.Direction == PacketDirection.Outgoing)
                                        {
                                            <Icon Name="IconName.ArrowRight" /> OUT
                                        }
                                        else
                                        {
                                            <Icon Name="IconName.ArrowLeft" /> IN
                                        }
                                    </Badge>
                                </DisplayTemplate>
                            </DataGridColumn>
                            
                            <DataGridColumn Field="@nameof(PacketTraceEntry.ReaderName)" 
                                            Caption="Reader" 
                                            Width="120px">
                                <DisplayTemplate>
                                    <Text>@context.ReaderName</Text>
                                    <Small Class="text-muted">[0x@context.Address.ToString("X2")]</Small>
                                </DisplayTemplate>
                            </DataGridColumn>
                            
                            <DataGridColumn Field="@nameof(PacketTraceEntry.Type)" 
                                            Caption="Type" 
                                            Width="150px">
                                <DisplayTemplate>
                                    <Text>@context.Type</Text>
                                    @if (context.IsSecure)
                                    {
                                        <Icon Name="IconName.Lock" TextColor="TextColor.Warning" />
                                    }
                                </DisplayTemplate>
                            </DataGridColumn>
                            
                            <DataGridColumn Field="@nameof(PacketTraceEntry.Length)" 
                                            Caption="Len" 
                                            Width="60px">
                                <DisplayTemplate>
                                    <Small>@context.Length</Small>
                                </DisplayTemplate>
                            </DataGridColumn>
                            
                            <DataGridColumn Field="@nameof(PacketTraceEntry.Details)" 
                                            Caption="Details">
                                <DisplayTemplate>
                                    <Text Overflow="Overflow.Wrap">@context.Details</Text>
                                </DisplayTemplate>
                            </DataGridColumn>
                            
                            <DataGridColumn Width="50px">
                                <DisplayTemplate>
                                    <Button Size="Size.Small" 
                                            Color="Color.Light"
                                            Clicked="@(() => ShowPacketDetails(context))">
                                        <Icon Name="IconName.Eye" />
                                    </Button>
                                </DisplayTemplate>
                            </DataGridColumn>
                        </DataGrid>
                    }
                    else
                    {
                        <Alert Color="Color.Info">
                            <AlertMessage>No packets captured yet</AlertMessage>
                            <AlertDescription>
                                Start tracing to capture OSDP packets from configured readers.
                            </AlertDescription>
                        </Alert>
                    }
                </CardBody>
            </Card>
        </Column>
    </Row>
</Container>

<!-- Packet Details Modal -->
<Modal @ref="detailsModal">
    <ModalContent Size="ModalSize.Large">
        <ModalHeader>
            <ModalTitle>Packet Details</ModalTitle>
            <CloseButton />
        </ModalHeader>
        <ModalBody>
            @if (selectedPacket != null)
            {
                <Tabs SelectedTab="@selectedTab">
                    <Items>
                        <Tab Name="info">Info</Tab>
                        <Tab Name="hex">Hex</Tab>
                        <Tab Name="ascii">ASCII</Tab>
                    </Items>
                    <Content>
                        <TabPanel Name="info">
                            <Fields>
                                <Field>
                                    <FieldLabel>Timestamp</FieldLabel>
                                    <TextEdit Value="@selectedPacket.LocalTimestamp.ToString("yyyy-MM-dd HH:mm:ss.fff")" 
                                              ReadOnly />
                                </Field>
                                <Field>
                                    <FieldLabel>Direction</FieldLabel>
                                    <TextEdit Value="@selectedPacket.Direction.ToString()" ReadOnly />
                                </Field>
                                <Field>
                                    <FieldLabel>Type</FieldLabel>
                                    <TextEdit Value="@selectedPacket.Type" ReadOnly />
                                </Field>
                                <Field>
                                    <FieldLabel>Length</FieldLabel>
                                    <TextEdit Value="@selectedPacket.Length.ToString()" ReadOnly />
                                </Field>
                                @if (!string.IsNullOrEmpty(selectedPacket.Details))
                                {
                                    <Field>
                                        <FieldLabel>Details</FieldLabel>
                                        <MemoEdit Value="@selectedPacket.Details" ReadOnly Rows="3" />
                                    </Field>
                                }
                            </Fields>
                        </TabPanel>
                        <TabPanel Name="hex">
                            <Pre>@selectedPacket.GetHexDisplay()</Pre>
                        </TabPanel>
                        <TabPanel Name="ascii">
                            <Pre>@selectedPacket.GetAsciiDisplay()</Pre>
                        </TabPanel>
                    </Content>
                </Tabs>
            }
        </ModalBody>
    </ModalContent>
</Modal>

@code {
    private Modal? detailsModal;
    private PacketTraceEntry? selectedPacket;
    private string selectedTab = "info";
    
    protected override async Task OnInitializedAsync()
    {
        await ViewModel.InitializeAsync();
    }
    
    private int GetMemoryPercentage()
    {
        // Parse memory usage string and calculate percentage
        // This is a simplified implementation
        return 25;
    }
    
    private Color GetMemoryProgressColor()
    {
        var percentage = GetMemoryPercentage();
        return percentage switch
        {
            < 50 => Color.Success,
            < 80 => Color.Warning,
            _ => Color.Danger
        };
    }
    
    private async Task ShowPacketDetails(PacketTraceEntry packet)
    {
        selectedPacket = packet;
        await detailsModal!.Show();
    }
}
```

### 5. Export Functionality (`src/ApBox.Core/PacketTracing/Export/`)

#### OsdpCapExporter.cs
```csharp
namespace ApBox.Core.PacketTracing.Export
{
    public class OsdpCapExporter
    {
        // OSDPCAP format implementation
        // Will be implemented based on the specification provided
        
        public async Task<byte[]> ExportToOsdpCapAsync(
            IEnumerable<PacketTraceEntry> packets,
            OsdpCapMetadata metadata)
        {
            // Placeholder for OSDPCAP format implementation
            // The actual implementation will depend on the OSDPCAP specification
            
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            
            // Write OSDPCAP header
            // Write metadata
            // Write packet records
            
            await Task.CompletedTask; // Async placeholder
            
            return stream.ToArray();
        }
    }
    
    public class OsdpCapMetadata
    {
        public string DeviceName { get; set; } = string.Empty;
        public DateTime CaptureStartTime { get; set; }
        public DateTime CaptureEndTime { get; set; }
        public string Version { get; set; } = "1.0";
        public Dictionary<string, string> CustomFields { get; set; } = new();
    }
}
```

### 6. Program.cs Configuration

```csharp
// Add Blazorise LocalStorage
builder.Services.AddBlazorise(options =>
{
    options.Immediate = true;
})
.AddBootstrapProviders()
.AddFontAwesomeIcons()
.AddBlazoriseLocalStorage(); // Add LocalStorage support

// Register packet tracing services
builder.Services.AddSingleton<IPacketTraceService, PacketTraceService>();
builder.Services.AddScoped<PacketTraceViewModel>();
builder.Services.AddScoped<ISnackbar, SnackbarService>();
```

### 7. SignalR Hub (`src/ApBox.Web/Hubs/`)

#### PacketTraceHub.cs
```csharp
using Microsoft.AspNetCore.SignalR;

namespace ApBox.Web.Hubs
{
    public class PacketTraceHub : Hub
    {
        private readonly IPacketTraceService _traceService;
        
        public PacketTraceHub(IPacketTraceService traceService)
        {
            _traceService = traceService;
        }
        
        public async Task StartTracing(string readerId)
        {
            _traceService.StartTracing(readerId);
            await Clients.All.SendAsync("TracingStarted", readerId);
        }
        
        public async Task StopTracing(string readerId)
        {
            _traceService.StopTracing(readerId);
            await Clients.All.SendAsync("TracingStopped", readerId);
        }
        
        public async Task SendPacketTrace(PacketTraceEntry entry)
        {
            await Clients.All.SendAsync("PacketReceived", entry);
        }
        
        public async Task SendStatistics(TracingStatistics stats)
        {
            await Clients.All.SendAsync("StatisticsUpdated", stats);
        }
        
        public override async Task OnConnectedAsync()
        {
            // Send current state to new client
            var stats = _traceService.GetStatistics();
            await Clients.Caller.SendAsync("StatisticsUpdated", stats);
            
            await base.OnConnectedAsync();
        }
    }
}
```

## Testing Requirements

### Unit Tests (`tests/ApBox.Core.Tests/PacketTracing/`)

1. **CircularBufferTests.cs**
   - Test capacity limits
   - Test thread safety
   - Test overflow behavior
   - Test enumeration

2. **PacketTraceServiceTests.cs**
   - Test packet capture
   - Test filtering
   - Test memory limits
   - Test settings updates

3. **PacketTraceEntryBuilderTests.cs**
   - Test builder pattern
   - Test interval calculation
   - Test packet parsing

### Integration Tests (`tests/ApBox.Web.Tests/PacketTracing/`)

1. **PacketTraceViewModelTests.cs**
   - Test settings persistence
   - Test command execution
   - Test statistics updates

2. **PacketTraceComponentTests.cs** (using bUnit)
   - Test UI rendering
   - Test user interactions
   - Test real-time updates

## Implementation Notes

1. **Memory Management**
   - Use circular buffer to limit memory usage
   - Monitor memory consumption in real-time
   - Auto-stop on memory threshold

2. **Performance Considerations**
   - Virtualize DataGrid for large datasets
   - Limit UI updates to recent packets
   - Use background threads for cleanup

3. **Integration Points**
   - Hook into OSDP.Net packet events
   - Use existing SignalR infrastructure
   - Leverage Blazorise components

4. **User Experience**
   - Persist settings in browser LocalStorage
   - Real-time packet display
   - Clear visual indicators for packet types
   - Export capability for offline analysis

## Next Steps

1. Implement OSDPCAP export format (pending specification)
2. Integrate with OSDP.Net library for packet parsing
3. Add advanced filtering options
4. Implement packet search functionality
5. Add packet replay capability

## Dependencies

- OSDP.Net library (for packet parsing)
- Blazorise.LocalStorage (for settings persistence)
- CommunityToolkit.Mvvm (for MVVM pattern)
- System.Threading.Channels (for async packet processing)

## References

- OSDP-Bench implementation: https://github.com/Z-bit-Systems-LLC/OSDP-Bench
- OSDPCAP format specification: [To be provided]
- OSDP protocol specification: SIA OSDP v2.2