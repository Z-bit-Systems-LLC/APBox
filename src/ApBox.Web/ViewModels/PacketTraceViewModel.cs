using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using ApBox.Core.PacketTracing.Models;
using ApBox.Core.PacketTracing.Services;
using ApBox.Core.PacketTracing;
using ApBox.Core.PacketTracing.Export;
using ApBox.Web.Models.Notifications;
using ApBox.Web.Services.Notifications;
using Blazored.LocalStorage;
using Microsoft.JSInterop;

namespace ApBox.Web.ViewModels
{
    public partial class PacketTraceViewModel(
        IPacketTraceService traceService,
        INotificationAggregator notificationAggregator,
        ILocalStorageService localStorage,
        OsdpCapExporter exporter,
        IJSRuntime jsRuntime)
        : ObservableObject, IDisposable
    {
        private const string SettingsKey = "packetTraceSettings";
        private IDisposable? _packetTraceSubscription;
        private IDisposable? _statisticsSubscription;

        [ObservableProperty]
        private ObservableCollection<PacketTraceEntry> _packets = [];
        
        // Live Settings
        [ObservableProperty]
        private bool _tracingEnabled;
        
        [ObservableProperty]
        private bool _filterPollCommands;
        
        [ObservableProperty]
        private bool _filterAckCommands;
        
        // Statistics
        [ObservableProperty]
        private double _replyPercentage;
        
        [ObservableProperty]
        private double _averageResponseTimeMs;
        
        [ObservableProperty]
        private bool _isLoading;
        
        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [RelayCommand]
        private async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;
                
                // Synchronize UI state with actual service state
                TracingEnabled = traceService.IsTracing;
                
                await Task.Run(async () =>
                {
                    if (await localStorage.ContainKeyAsync(SettingsKey))
                    {
                        var settings = await localStorage.GetItemAsync<PacketTraceSettings>(SettingsKey);
                        if (settings != null)
                        {
                            ApplySettingsToViewModel(settings);
                        }
                    }
                    
                    RefreshPacketList();

                    // Subscribe and store the disposable tokens
                    _packetTraceSubscription = notificationAggregator.Subscribe<PacketTraceNotification>(OnPacketTraceNotification);
                    _statisticsSubscription = notificationAggregator.Subscribe<TracingStatisticsNotification>(OnTracingStatisticsNotification);
                });
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to initialize packet trace: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
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
                await localStorage.SetItemAsync(SettingsKey, settings);
            }
            catch (Exception)
            {
                // If LocalStorage fails during server-side rendering, ignore the error
                // Settings will still be applied to the service for the current session
            }
            
            // Apply to service
            traceService.UpdateSettings(settings);
        }
        
        [RelayCommand]
        private void StartTracing()
        {
            // Clear the UI packet collection as well
            Packets.Clear();
            traceService.ClearTraces();
            traceService.StartTracingAll();
            TracingEnabled = true;
        }
        
        [RelayCommand]
        private void StopTracing()
        {
            traceService.StopTracingAll();
            TracingEnabled = false;
        }
        
        
        [RelayCommand]
        private void RefreshDisplay()
        {
            RefreshPacketList();
            // Statistics are updated automatically via notifications, no need to manually refresh
        }
        
        
        [RelayCommand]
        private async Task ExportToOsdpCapAsync()
        {
            try
            {
                var packets = traceService.GetTraces().OrderBy(p => p.Timestamp).ToList();
                
                if (!packets.Any())
                {
                    ErrorMessage = "No packet data available for export";
                    return;
                }

                var metadata = new OsdpCapMetadata
                {
                    DeviceName = "ApBox Gateway",
                    CaptureStartTime = packets.First().Timestamp,
                    CaptureEndTime = packets.Last().Timestamp,
                    Version = "1.0"
                };

                var exportData = await exporter.ExportToOsdpCapAsync(packets, metadata);
                var fileName = $"apbox-trace-{DateTime.Now:yyyyMMdd-HHmmss}.osdpcap";
                
                // Trigger immediate download via JavaScript
                var base64Data = Convert.ToBase64String(exportData);
                var dataUri = $"data:application/octet-stream;base64,{base64Data}";
                
                await jsRuntime.InvokeVoidAsync("downloadFile", dataUri, fileName);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Export failed: {ex.Message}";
            }
        }
        
        private void OnPacketTraceNotification(PacketTraceNotification notification)
        {
            var entry = notification.TraceEntry;
            
            // Check if packet should be displayed based on current filters
            bool shouldDisplay = true;
            if (entry.Packet != null)
            {
                if (FilterPollCommands && IsPollCommand(entry.Type))
                    shouldDisplay = false;
                if (FilterAckCommands && IsAckReply(entry.Type))
                    shouldDisplay = false;
            }
            
            if (shouldDisplay)
            {
                // Add to UI collection (limit to recent 100 for performance)
                Packets.Insert(0, entry);
                if (Packets.Count > 100)
                {
                    Packets.RemoveAt(Packets.Count - 1);
                }
                
                // Notify that the collection has changed
                OnPropertyChanged(nameof(Packets));
            }
            
            // Update UI on the main thread
            InvokeAsync?.Invoke(() => { StateHasChanged(); return Task.CompletedTask; });
        }
        
        private void OnTracingStatisticsNotification(TracingStatisticsNotification notification)
        {
            var stats = notification.Statistics;
            
            ReplyPercentage = stats.ReplyPercentage;
            AverageResponseTimeMs = stats.AverageResponseTimeMs;
            
            // Update UI on the main thread
            InvokeAsync?.Invoke(() => { StateHasChanged(); return Task.CompletedTask; });
        }
        
        private void RefreshPacketList()
        {
            // Get all traces from service (no filtering at service level)
            var allTraces = traceService.GetTraces(readerId: null, limit: 200);
            
            // Apply filters at ViewModel level
            var filteredTraces = allTraces.Where(trace => 
            {
                if (FilterPollCommands && IsPollCommand(trace.Type)) return false;
                if (FilterAckCommands && IsAckReply(trace.Type)) return false;
                return true;
            }).Take(100); // Limit to 100 for UI performance
            
            Packets.Clear();
            foreach (var trace in filteredTraces)
            {
                Packets.Add(trace);
            }
        }
        
        /// <summary>
        /// Manual statistics update - only used for initial load or explicit refresh
        /// Real-time updates come through TracingStatisticsNotification
        /// </summary>
        private void UpdateStatistics()
        {
            var stats = traceService.GetStatistics();

            ReplyPercentage = stats.ReplyPercentage;
            AverageResponseTimeMs = stats.AverageResponseTimeMs;
        }
        
        private void ApplySettingsToViewModel(PacketTraceSettings settings)
        {
            FilterPollCommands = settings.FilterPollCommands;
            FilterAckCommands = settings.FilterAckCommands;
        }
        
        private static bool IsPollCommand(string packetType)
        {
            // Check if the packet type indicates a Poll command
            return packetType.Contains("Poll", StringComparison.OrdinalIgnoreCase) == true;
        }
        
        private static bool IsAckReply(string packetType)
        {
            // Check if the packet type indicates an ACK reply
            return packetType.Contains("Ack", StringComparison.OrdinalIgnoreCase) == true;
        }
        
        /// <summary>
        /// Placeholder for StateHasChanged - will be set by the component
        /// </summary>
        public Action StateHasChanged { get; set; } = () => { };
        
        /// <summary>
        /// Placeholder for InvokeAsync - will be set by the component
        /// </summary>
        public Func<Func<Task>, Task>? InvokeAsync { get; set; }
        
        #region IDisposable Implementation
        
        private bool _disposed = false;
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose subscription tokens to automatically unsubscribe
                    _packetTraceSubscription?.Dispose();
                    _packetTraceSubscription = null;

                    _statisticsSubscription?.Dispose();
                    _statisticsSubscription = null;
                }

                _disposed = true;
            }
        }
        
        #endregion
    }
}