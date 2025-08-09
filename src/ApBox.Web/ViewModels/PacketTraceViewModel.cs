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
            INotificationAggregator notificationAggregator)
        {
            _traceService = traceService;
            _localStorage = localStorage;
            _notificationAggregator = notificationAggregator;
            
            // Subscribe to packet capture events
            _traceService.PacketCaptured += OnPacketCaptured;
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
                MaxPacketsPerReader = MaxPacketsPerReader,
                MaxAgeMinutes = MaxAgeMinutes,
                FilterPollCommands = FilterPollCommands,
                FilterAckCommands = FilterAckCommands,
                LimitMode = LimitMode
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
        private async Task ExportToOsdpCapAsync()
        {
            // TODO: Implement OSDPCAP export when specification is provided
            await Task.Delay(100); // Placeholder
            
            // For now, just show a message
            throw new NotImplementedException("OSDPCAP export will be implemented when specification is provided");
        }
        
        private void OnPacketCaptured(object? sender, PacketTraceEntry e)
        {
            // Add to UI collection (limit to recent 100 for performance)
            Packets.Insert(0, e);
            if (Packets.Count > 100)
            {
                Packets.RemoveAt(Packets.Count - 1);
            }
            UpdateStatistics();
            
            // Notify that the collection has changed
            OnPropertyChanged(nameof(Packets));
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
    }
}