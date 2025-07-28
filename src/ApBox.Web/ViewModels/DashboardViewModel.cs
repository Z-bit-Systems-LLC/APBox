using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ApBox.Core.Services;
using ApBox.Core.Data.Repositories;
using ApBox.Core.Models;
using ApBox.Plugins;
using ApBox.Web.Hubs;
using ApBox.Web.Services;
using Microsoft.AspNetCore.SignalR.Client;

namespace ApBox.Web.ViewModels;

/// <summary>
/// ViewModel for the dashboard page using MVVM pattern
/// </summary>
public partial class DashboardViewModel(
    IReaderService readerService,
    IPluginLoader pluginLoader,
    ICardEventRepository cardEventRepository,
    IHubConnectionWrapper? hubConnectionWrapper = null)
    : ObservableObject, IAsyncDisposable
{
    private readonly IHubConnectionWrapper? _hubConnection = hubConnectionWrapper;

    [ObservableProperty]
    private int _configuredReaders;

    [ObservableProperty]
    private int _onlineReaders;

    [ObservableProperty]
    private int _loadedPlugins;

    [ObservableProperty]
    private int _totalEvents;

    [ObservableProperty]
    private string _systemStatus = "Online";

    [ObservableProperty]
    private ObservableCollection<CardReadEvent> _recentEvents = new();

    [ObservableProperty]
    private List<ReaderConfiguration> _readers = new();

    [ObservableProperty]
    private Dictionary<Guid, bool> _readerStatuses = new();

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>
    /// Initializes the dashboard data and SignalR connection
    /// </summary>
    [RelayCommand]
    public async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            await LoadDashboardDataAsync();
            await InitializeSignalRAsync(); 
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to initialize dashboard: {ex.Message}";
            SystemStatus = "Error";
        }
        finally
        {
            IsLoading = false;
        }
    }


    /// <summary>
    /// Loads all dashboard data from services
    /// </summary>
    private async Task LoadDashboardDataAsync()
    {
        try
        {
            // Load readers
            var readerConfigs = await readerService.GetReadersAsync();
            Readers = readerConfigs.ToList();
            
            // Load reader statuses
            ReaderStatuses = await readerService.GetAllReaderStatusesAsync();
            
            // Calculate reader counts
            ConfiguredReaders = Readers.Count;
            OnlineReaders = ReaderStatuses.Count(kvp => kvp.Value);

            // Load plugins
            var plugins = await pluginLoader.LoadPluginsAsync();
            LoadedPlugins = plugins.Count();

            // Load recent card events from database
            await LoadRecentEventsAsync();
            
            // Get today's event count
            TotalEvents = await GetTodaysEventCountAsync();

            SystemStatus = "Online";
        }
        catch (Exception ex)
        {
            SystemStatus = "Error";
            throw new InvalidOperationException($"Dashboard data loading failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Loads today's recent card events from the database
    /// </summary>
    private async Task LoadRecentEventsAsync()
    {
        try
        {
            var todayUtc = DateTime.UtcNow.Date;
            var tomorrowUtc = todayUtc.AddDays(1);
            var eventEntities = await cardEventRepository.GetByDateRangeAsync(todayUtc, tomorrowUtc, 25);
            var events = eventEntities.Select(e => e.ToCardReadEvent()).ToList();
            
            RecentEvents.Clear();
            foreach (var evt in events)
            {
                RecentEvents.Add(evt);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load today's recent events: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets today's total event count
    /// </summary>
    private async Task<int> GetTodaysEventCountAsync()
    {
        try
        {
            var todayUtc = DateTime.UtcNow.Date;
            var tomorrowUtc = todayUtc.AddDays(1);
            var todaysEvents = await cardEventRepository.GetByDateRangeAsync(todayUtc, tomorrowUtc);
            return todaysEvents.Count();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to get today's event count: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Initializes SignalR connection for real-time updates
    /// </summary>
    private async Task InitializeSignalRAsync()
    {
        try
        {
            // Skip SignalR initialization if no hub connection is provided (e.g., in tests)
            if (_hubConnection == null)
            {
                return;
            }

            // Always register handlers regardless of connection state
            _hubConnection.On<CardEventNotification>("CardEventProcessed", OnCardEventProcessed);
            _hubConnection.On<ReaderStatusNotification>("ReaderStatusChanged", OnReaderStatusChanged);

            // Only start connection if it's disconnected
            if (_hubConnection.State == HubConnectionState.Disconnected)
            {
                await _hubConnection.StartAsync();
            }
        }
        catch (ObjectDisposedException)
        {
            // Connection was disposed, ignore
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"SignalR initialization failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Handles incoming card events from SignalR
    /// </summary>
    private async Task OnCardEventProcessed(CardEventNotification notification)
    {
        try
        {
            // Create CardReadEvent from notification
            var cardEvent = new CardReadEvent
            {
                ReaderId = notification.ReaderId,
                ReaderName = notification.ReaderName,
                CardNumber = notification.CardNumber,
                BitLength = notification.BitLength,
                Timestamp = notification.Timestamp
            };
            
            // Add to recent events at the beginning
            RecentEvents.Insert(0, cardEvent);
            
            // Keep only the most recent 25 events
            while (RecentEvents.Count > 25)
            {
                RecentEvents.RemoveAt(RecentEvents.Count - 1);
            }

            // Update total events count only if event is from today (UTC)
            if (cardEvent.Timestamp.Date == DateTime.UtcNow.Date)
            {
                TotalEvents++;
            }

            // Notify UI to update
            await InvokeAsync(() => { StateHasChanged(); return Task.CompletedTask; });
        }
        catch (Exception ex)
        {
            // Log error but don't throw to avoid breaking SignalR
            Console.WriteLine($"Error handling card event: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles reader status changes from SignalR
    /// </summary>
    private async Task OnReaderStatusChanged(ReaderStatusNotification notification)
    {
        try
        {
            // Update reader status
            ReaderStatuses[notification.ReaderId] = notification.IsOnline;

            // Recalculate online readers
            OnlineReaders = ReaderStatuses.Count(kvp => kvp.Value);

            // Notify UI to update
            await InvokeAsync(() => { StateHasChanged(); return Task.CompletedTask; });
        }
        catch (Exception ex)
        {
            // Log error but don't throw to avoid breaking SignalR
            Console.WriteLine($"Error handling reader status: {ex.Message}");
        }
    }

    /// <summary>
    /// Placeholder for StateHasChanged - will be set by the component
    /// </summary>
    public Action StateHasChanged { get; set; } = () => { };

    /// <summary>
    /// Placeholder for InvokeAsync - will be set by the component
    /// </summary>
    public Func<Func<Task>, Task> InvokeAsync { get; set; } = func => func();

    /// <summary>
    /// Disposes resources
    /// </summary>
    private bool _disposed;

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        // Don't dispose the hub connection - let it be reused by other ViewModels
        // The connection will be disposed when the application shuts down
        await Task.CompletedTask;
    }
}