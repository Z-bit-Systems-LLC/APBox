using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ApBox.Core.Services.Reader;
using ApBox.Core.Data.Repositories;
using ApBox.Core.Models;
using ApBox.Web.Hubs;
using ApBox.Web.Services;

namespace ApBox.Web.ViewModels;

/// <summary>
/// ViewModel for the card events page with real-time updates
/// </summary>
public partial class CardEventsViewModel(
    IReaderService readerService,
    ICardEventRepository cardEventRepository,
    ICardEventNotificationService cardEventNotificationService)
    : ObservableObject, IDisposable
{
    private readonly ICardEventNotificationService _cardEventNotificationService = cardEventNotificationService;

    [ObservableProperty]
    private ObservableCollection<CardEventDisplay> _allEvents = new();

    [ObservableProperty]
    private List<ReaderConfiguration> _readers = new();

    [ObservableProperty]
    private string _searchTerm = "";

    [ObservableProperty]
    private string _selectedReader = "";

    [ObservableProperty]
    private int _pageSize = 20;

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private CardEventDisplay? _selectedEvent;

    /// <summary>
    /// Gets filtered events based on search criteria
    /// </summary>
    public IEnumerable<CardEventDisplay> FilteredEvents => FilterEvents();

    /// <summary>
    /// Initializes the view model and SignalR connection
    /// </summary>
    [RelayCommand]
    public async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            await LoadDataAsync();
            InitializeSignalRHandlers();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to initialize card events: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }


    /// <summary>
    /// Loads more events (pagination)
    /// </summary>
    [RelayCommand]
    public void LoadMore()
    {
        PageSize += 20;
    }

    /// <summary>
    /// Updates search term and triggers filtering
    /// </summary>
    public void UpdateSearchTerm(string searchTerm)
    {
        SearchTerm = searchTerm;
        OnPropertyChanged(nameof(FilteredEvents));
    }

    /// <summary>
    /// Updates reader filter and triggers filtering
    /// </summary>
    public void UpdateReaderFilter(string selectedReader)
    {
        SelectedReader = selectedReader;
        OnPropertyChanged(nameof(FilteredEvents));
    }

    /// <summary>
    /// Sets the selected event for detail viewing
    /// </summary>
    public void SelectEvent(CardEventDisplay cardEvent)
    {
        SelectedEvent = cardEvent;
    }

    /// <summary>
    /// Loads all necessary data
    /// </summary>
    private async Task LoadDataAsync()
    {
        try
        {
            // Load readers
            var readerConfigs = await readerService.GetReadersAsync();
            Readers = readerConfigs.ToList();

            // Load card events from database
            await LoadRecentEventsAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load data: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Loads recent events from the database
    /// </summary>
    private async Task LoadRecentEventsAsync()
    {
        try
        {
            var eventEntities = await cardEventRepository.GetRecentAsync(500); // Load more for filtering
            var events = eventEntities.Select(CardEventDisplay.FromEntity).ToList();
            
            AllEvents.Clear();
            foreach (var evt in events)
            {
                AllEvents.Add(evt);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load recent events: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Filters events based on search and reader criteria
    /// </summary>
    private IEnumerable<CardEventDisplay> FilterEvents()
    {
        var filtered = AllEvents.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            filtered = filtered.Where(e => 
                e.CardNumber.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                e.ReaderName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(SelectedReader))
        {
            filtered = filtered.Where(e => e.ReaderName == SelectedReader);
        }

        return filtered;
    }

    /// <summary>
    /// Initializes SignalR event handlers for real-time updates
    /// </summary>
    private void InitializeSignalRHandlers()
    {
        // Register event handler
        _cardEventNotificationService.OnCardEventProcessed += OnCardEventProcessed;
    }

    /// <summary>
    /// Handles incoming card events from SignalR
    /// </summary>
    private void OnCardEventProcessed(CardEventNotification notification)
    {
        try
        {
            // Create CardEventDisplay from notification
            var cardEvent = new CardEventDisplay
            {
                ReaderId = notification.ReaderId,
                ReaderName = notification.ReaderName,
                CardNumber = notification.CardNumber,
                BitLength = notification.BitLength,
                Timestamp = notification.Timestamp,
                Success = notification.Success,
                Message = notification.Message
            };
            
            // Add to events at the beginning (most recent first)
            AllEvents.Insert(0, cardEvent);
            
            // Notify UI to update filtered events
            OnPropertyChanged(nameof(FilteredEvents));

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
    }
}