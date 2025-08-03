using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ApBox.Core.Services;
using ApBox.Core.Data.Repositories;
using ApBox.Core.Models;
using ApBox.Plugins;
using ApBox.Web.Hubs;
using ApBox.Web.Services;

namespace ApBox.Web.ViewModels;

/// <summary>
/// ViewModel for the dashboard page using MVVM pattern
/// </summary>
public partial class DashboardViewModel(
    IReaderService readerService,
    IPluginLoader pluginLoader,
    ICardEventRepository cardEventRepository,
    ICardEventNotificationService cardEventNotificationService,
    IPinEventNotificationService pinEventNotificationService)
    : ObservableObject, IDisposable
{
    private readonly ICardEventNotificationService _cardEventNotificationService = cardEventNotificationService;
    private readonly IPinEventNotificationService _pinEventNotificationService = pinEventNotificationService;

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
    private ObservableCollection<object> _recentEvents = new(); // Mixed collection of CardReadEvent and PinEventDisplay

    [ObservableProperty]
    private List<ReaderConfiguration> _readers = new();

    [ObservableProperty]
    private Dictionary<Guid, bool> _readerStatuses = new();

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>
    /// Initializes the dashboard data and SignalR event handlers
    /// </summary>
    [RelayCommand]
    public async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            await LoadDashboardDataAsync();
            InitializeSignalRHandlers(); 
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
            var todayLocal = DateTime.Now.Date;
            var tomorrowLocal = todayLocal.AddDays(1);
            var todayUtc = todayLocal.ToUniversalTime();
            var tomorrowUtc = tomorrowLocal.ToUniversalTime();
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
            var todayLocal = DateTime.Now.Date;
            var tomorrowLocal = todayLocal.AddDays(1);
            var todayUtc = todayLocal.ToUniversalTime();
            var tomorrowUtc = tomorrowLocal.ToUniversalTime();
            var todaysEvents = await cardEventRepository.GetByDateRangeAsync(todayUtc, tomorrowUtc);
            return todaysEvents.Count();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to get today's event count: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Initializes SignalR event handlers for real-time updates
    /// </summary>
    private void InitializeSignalRHandlers()
    {
        // Register event handlers
        _cardEventNotificationService.OnCardEventProcessed += OnCardEventProcessed;
        _pinEventNotificationService.OnPinEventProcessed += OnPinEventProcessed;
        _cardEventNotificationService.OnReaderStatusChanged += OnReaderStatusChanged;
    }

    /// <summary>
    /// Handles incoming card events from SignalR
    /// </summary>
    private void OnCardEventProcessed(CardEventNotification notification)
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

            // Update total events count only if event is from today (local timezone)
            if (cardEvent.Timestamp.ToLocalTime().Date == DateTime.Now.Date)
            {
                TotalEvents++;
            }

            // Notify UI to update
            InvokeAsync(() => { StateHasChanged(); return Task.CompletedTask; });
        }
        catch (Exception ex)
        {
            // Log error but don't throw to avoid breaking SignalR
            Console.WriteLine($"Error handling card event: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles incoming PIN events from SignalR
    /// </summary>
    private void OnPinEventProcessed(PinEventNotification notification)
    {
        try
        {
            // Create PinEventDisplay from notification
            var pinEvent = new PinEventDisplay
            {
                ReaderId = notification.ReaderId,
                ReaderName = notification.ReaderName,
                PinLength = notification.PinLength,
                CompletionReason = notification.CompletionReason,
                Timestamp = notification.Timestamp,
                Success = notification.Success,
                Message = notification.Message
            };
            
            // Add to recent events at the beginning
            RecentEvents.Insert(0, pinEvent);
            
            // Keep only the most recent 25 events
            while (RecentEvents.Count > 25)
            {
                RecentEvents.RemoveAt(RecentEvents.Count - 1);
            }

            // Update total events count only if event is from today (local timezone)
            if (pinEvent.Timestamp.ToLocalTime().Date == DateTime.Now.Date)
            {
                TotalEvents++;
            }

            // Notify UI to update
            InvokeAsync(() => { StateHasChanged(); return Task.CompletedTask; });
        }
        catch (Exception ex)
        {
            // Log error but don't throw to avoid breaking SignalR
            Console.WriteLine($"Error handling PIN event: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles reader status changes from SignalR
    /// </summary>
    private void OnReaderStatusChanged(ReaderStatusNotification notification)
    {
        try
        {
            // Update reader status
            ReaderStatuses[notification.ReaderId] = notification.IsOnline;

            // Recalculate online readers
            OnlineReaders = ReaderStatuses.Count(kvp => kvp.Value);

            // Notify UI to update
            InvokeAsync(() => { StateHasChanged(); return Task.CompletedTask; });
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
    /// Disposes resources and unsubscribes from SignalR events
    /// </summary>
    public void Dispose()
    {
        // Unsubscribe from events
        _cardEventNotificationService.OnCardEventProcessed -= OnCardEventProcessed;
        _pinEventNotificationService.OnPinEventProcessed -= OnPinEventProcessed;
        _cardEventNotificationService.OnReaderStatusChanged -= OnReaderStatusChanged;
    }
}