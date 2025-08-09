using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using ApBox.Core.PacketTracing.Models;
using ApBox.Core.PacketTracing.Services;
using ApBox.Core.PacketTracing;
using ApBox.Web.Services.Notifications;
using Blazored.LocalStorage;

namespace ApBox.Web.ViewModels
{
    public partial class PacketTraceViewModel : ObservableObject
    {
        private readonly IPacketTraceService _traceService;
        private readonly ILocalStorageService _localStorage;
        private readonly INotificationAggregator _notificationAggregator;
        private const string SETTINGS_KEY = "packetTraceSettings";
        
        [ObservableProperty]
        private ObservableCollection<PacketTraceEntry> _packets = new();
        
        // Live Settings
        [ObservableProperty]
        private bool _tracingEnabled;
        
        [ObservableProperty]
        private bool _filterPollCommands = true;
        
        [ObservableProperty]
        private bool _filterAckCommands = false;
        
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
            INotificationAggregator notificationAggregator)
        {
            _traceService = traceService;
            _localStorage = localStorage;
            _notificationAggregator = notificationAggregator;
            
            // Subscribe to packet capture events
            _traceService.PacketCaptured += OnPacketCaptured;
        }
        
        // Auto-refresh display when filter properties change
        partial void OnFilterPollCommandsChanged(bool value)
        {
            // Only refresh if we have a service and are not in initialization mode
            if (_traceService != null && Packets != null)
            {
                RefreshPacketList();
                UpdateStatistics();
            }
        }
        
        partial void OnFilterAckCommandsChanged(bool value)
        {
            // Only refresh if we have a service and are not in initialization mode
            if (_traceService != null && Packets != null)
            {
                RefreshPacketList();
                UpdateStatistics();
            }
        }
        
        
        public Task InitializeAsync()
        {
            // Load existing traces without JavaScript interop
            RefreshPacketList();
            UpdateStatistics();
            return Task.CompletedTask;
        }

        public async Task InitializeWithJavaScriptAsync()
        {
            // Load settings from LocalStorage - only call after component is rendered
            try
            {
                if (await _localStorage.ContainKeyAsync(SETTINGS_KEY))
                {
                    var settings = await _localStorage.GetItemAsync<PacketTraceSettings>(SETTINGS_KEY);
                    if (settings != null)
                    {
                        ApplySettingsToViewModel(settings);
                    }
                }
            }
            catch (Exception)
            {
                // If LocalStorage fails, just use default settings
            }
        }
        
        [RelayCommand]
        private async Task ApplySettingsAsync()
        {
            var settings = new PacketTraceSettings
            {
                Enabled = TracingEnabled,
                FilterPollCommands = FilterPollCommands,
                FilterAckCommands = FilterAckCommands
            };
            
            // Save to LocalStorage with error handling
            try
            {
                await _localStorage.SetItemAsync(SETTINGS_KEY, settings);
            }
            catch (Exception)
            {
                // If LocalStorage fails during server-side rendering, ignore the error
                // Settings will still be applied to the service for the current session
            }
            
            // Apply to service
            _traceService.UpdateSettings(settings);
        }
        
        [RelayCommand]
        private void StartTracing()
        {
            _traceService.StartTracingAll();
            TracingEnabled = true;
        }
        
        [RelayCommand]
        private void StopTracing()
        {
            _traceService.StopTracingAll();
            TracingEnabled = false;
        }
        
        [RelayCommand]
        private void ClearAllTraces()
        {
            _traceService.ClearTraces();
            Packets.Clear();
            UpdateStatistics();
        }
        
        [RelayCommand]
        private void RefreshDisplay()
        {
            RefreshPacketList();
            UpdateStatistics();
        }
        
        
        [RelayCommand]
        private async Task ExportToOsdpCapAsync()
        {
            // TODO: Implement OSDPCAP export when specification is provided
            await Task.Delay(100); // Placeholder
            
            // For now, just show a message
            throw new NotImplementedException("OSDPCAP export will be implemented when specification is provided");
        }
        
        private void OnPacketCaptured(object? sender, PacketTraceEntry e)
        {
            // Check if packet should be displayed based on current filters
            bool shouldDisplay = true;
            if (e.Packet != null)
            {
                if (FilterPollCommands && IsPollCommand(e.Type))
                    shouldDisplay = false;
                if (FilterAckCommands && IsAckReply(e.Type))
                    shouldDisplay = false;
            }
            
            if (shouldDisplay)
            {
                // Add to UI collection (limit to recent 100 for performance)
                Packets.Insert(0, e);
                if (Packets.Count > 100)
                {
                    Packets.RemoveAt(Packets.Count - 1);
                }
                
                // Notify that the collection has changed
                OnPropertyChanged(nameof(Packets));
            }
            
            // Always update statistics (they show total and filtered counts)
            UpdateStatistics();
            
            // Update UI on the main thread
            InvokeAsync?.Invoke(() => { StateHasChanged(); return Task.CompletedTask; });
        }
        
        private void RefreshPacketList()
        {
            if (_traceService == null) return; // Avoid null reference during initialization or testing
            
            // Get all traces from service (no filtering at service level)
            var allTraces = _traceService.GetTraces(readerId: null, limit: 200);
            
            // Apply filters at ViewModel level
            var filteredTraces = allTraces.Where(trace => 
            {
                if (trace.Packet == null) return true;
                if (FilterPollCommands && IsPollCommand(trace.Type))
                    return false;
                if (FilterAckCommands && IsAckReply(trace.Type))
                    return false;
                return true;
            }).Take(100); // Limit to 100 for UI performance
            
            Packets.Clear();
            foreach (var trace in filteredTraces)
            {
                Packets.Add(trace);
            }
        }
        
        private void UpdateStatistics()
        {
            if (_traceService == null) return; // Avoid null reference during initialization or testing
            
            var stats = _traceService.GetStatistics();
            if (stats == null) return; // Avoid null reference if service returns null stats
            
            TotalPackets = stats.TotalPackets;
            MemoryUsage = stats.FormattedMemoryUsage;
            
            // Calculate filtered packets at ViewModel level
            if (FilterPollCommands || FilterAckCommands)
            {
                var allTraces = _traceService.GetTraces(readerId: null, limit: null);
                FilteredPackets = allTraces.Count(trace =>
                {
                    if (trace.Packet == null) return false;
                    return (FilterPollCommands && IsPollCommand(trace.Type)) ||
                           (FilterAckCommands && IsAckReply(trace.Type));
                });
            }
            else
            {
                FilteredPackets = 0;
            }
            
            if (stats.TracingDuration.HasValue)
            {
                TracingDuration = stats.TracingDuration.Value.ToString(@"hh\:mm\:ss");
            }
        }
        
        private void ApplySettingsToViewModel(PacketTraceSettings settings)
        {
            TracingEnabled = settings.Enabled;
            FilterPollCommands = settings.FilterPollCommands;
            FilterAckCommands = settings.FilterAckCommands;
        }
        
        private bool IsPollCommand(string packetType)
        {
            // Check if packet type indicates a Poll command
            return packetType?.Contains("Poll", StringComparison.OrdinalIgnoreCase) == true;
        }
        
        private bool IsAckReply(string packetType)
        {
            // Check if packet type indicates an ACK reply
            return packetType?.Contains("Ack", StringComparison.OrdinalIgnoreCase) == true;
        }
        
        /// <summary>
        /// Placeholder for StateHasChanged - will be set by the component
        /// </summary>
        public Action StateHasChanged { get; set; } = () => { };
        
        /// <summary>
        /// Placeholder for InvokeAsync - will be set by the component
        /// </summary>
        public Func<Func<Task>, Task>? InvokeAsync { get; set; }
        
    }
}