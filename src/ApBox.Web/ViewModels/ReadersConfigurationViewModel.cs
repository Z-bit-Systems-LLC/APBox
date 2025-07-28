using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ApBox.Core.Services;
using ApBox.Core.Models;
using ApBox.Web.Services;
using ApBox.Plugins;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace ApBox.Web.ViewModels;

/// <summary>
/// ViewModel for the Readers Configuration page using MVVM pattern
/// </summary>
public partial class ReadersConfigurationViewModel : ObservableValidator, IAsyncDisposable
{
    private readonly IReaderConfigurationService _readerConfigurationService;
    private readonly IReaderService _readerService;
    private readonly ISerialPortService _serialPortService;
    private readonly IPluginLoader _pluginLoader;
    private readonly IReaderPluginMappingService _readerPluginMappingService;
    private readonly ILogger<ReadersConfigurationViewModel> _logger;
    private readonly IHubConnectionWrapper? _hubConnection;
    private bool _disposed = false;
    private readonly List<IDisposable> _signalRSubscriptions = new();

    public ReadersConfigurationViewModel(
        IReaderConfigurationService readerConfigurationService,
        IReaderService readerService,
        ISerialPortService serialPortService,
        IPluginLoader pluginLoader,
        IReaderPluginMappingService readerPluginMappingService,
        ILogger<ReadersConfigurationViewModel> logger,
        IHubConnectionWrapper? hubConnectionWrapper = null)
    {
        _readerConfigurationService = readerConfigurationService;
        _readerService = readerService;
        _serialPortService = serialPortService;
        _pluginLoader = pluginLoader;
        _readerPluginMappingService = readerPluginMappingService;
        _logger = logger;
        _hubConnection = hubConnectionWrapper;
    }

    [ObservableProperty]
    private ObservableCollection<ReaderConfiguration> _readers = new();

    [ObservableProperty]
    private Dictionary<Guid, bool> _readerStatuses = new();

    [ObservableProperty]
    private List<string> _availablePorts = new();

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private bool _isDeleting;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _successMessage;

    [ObservableProperty]
    private ReaderConfiguration? _editingReader;

    [ObservableProperty]
    private ReaderConfiguration? _readerToDelete;

    // Plugin Selection Properties
    [ObservableProperty]
    private ObservableCollection<IApBoxPlugin> _availablePlugins = new();

    [ObservableProperty]
    private ObservableCollection<Guid> _selectedPluginIds = new();

    [ObservableProperty]
    private bool _loadingPlugins;

    // Form data
    [ObservableProperty]
    [Required(ErrorMessage = "Reader name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Reader name must be between 2 and 100 characters")]
    private string _readerName = string.Empty;

    [ObservableProperty]
    [Range(0, 127, ErrorMessage = "Address must be between 0 and 127")]
    private byte _address = 1;

    [ObservableProperty]
    private string? _serialPort;

    [ObservableProperty]
    private int _baudRate = 9600;

    [ObservableProperty]
    private OsdpSecurityMode _securityMode = OsdpSecurityMode.ClearText;

    [ObservableProperty]
    private byte[]? _secureChannelKey;

    [ObservableProperty]
    private bool _isEnabled = true;

    // Modal visibility
    [ObservableProperty]
    private bool _showReaderModal;

    [ObservableProperty]
    private bool _showDeleteModal;

    // Component callbacks
    public Action? StateHasChanged { get; set; }
    public Func<Func<Task>, Task>? InvokeAsync { get; set; }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            // Load readers
            var readers = await _readerConfigurationService.GetAllReadersAsync();
            Readers = new ObservableCollection<ReaderConfiguration>(readers);

            // Load reader statuses
            ReaderStatuses = await _readerService.GetAllReaderStatusesAsync() ?? new Dictionary<Guid, bool>();

            // Load available serial ports
            AvailablePorts = _serialPortService.GetAvailablePortNames().ToList();

            // Load available plugins
            await LoadPluginsAsync();

            // Initialize SignalR
            await InitializeSignalRAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing readers configuration");
            ErrorMessage = $"Error loading configuration: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await InitializeAsync();
    }

    [RelayCommand]
    private void ShowAddReader()
    {
        EditingReader = null;
        ResetForm();
        ShowReaderModal = true;
    }

    [RelayCommand]
    private async Task EditReader(ReaderConfiguration reader)
    {
        try
        {
            // Load reader details into form
            EditingReader = reader;
            ReaderName = reader.ReaderName;
            Address = reader.Address;
            SerialPort = reader.SerialPort;
            BaudRate = reader.BaudRate;
            SecurityMode = reader.SecurityMode;
            SecureChannelKey = reader.SecureChannelKey;
            IsEnabled = reader.IsEnabled;

            // Load existing plugin mappings
            var pluginMappings = await _readerPluginMappingService.GetPluginsForReaderAsync(reader.ReaderId);
            SelectedPluginIds.Clear();
            foreach (var pluginIdString in pluginMappings)
            {
                if (Guid.TryParse(pluginIdString, out var pluginId))
                {
                    SelectedPluginIds.Add(pluginId);
                }
            }

            ShowReaderModal = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading reader for editing");
            ErrorMessage = $"Error loading reader: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SaveReaderAsync()
    {
        try
        {
            IsSaving = true;
            ErrorMessage = null;

            // Validate all properties before saving
            ValidateAllProperties();
            if (HasErrors)
            {
                ErrorMessage = "Please fix validation errors before saving.";
                return;
            }

            var reader = new ReaderConfiguration
            {
                ReaderId = EditingReader?.ReaderId ?? Guid.NewGuid(),
                ReaderName = ReaderName,
                Address = Address,
                SerialPort = SerialPort,
                BaudRate = BaudRate,
                SecurityMode = SecurityMode,
                SecureChannelKey = SecureChannelKey,
                IsEnabled = IsEnabled,
                CreatedAt = EditingReader?.CreatedAt ?? DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Save reader (handles both add and update)
            await _readerConfigurationService.SaveReaderAsync(reader);
            
            // Save plugin mappings
            var selectedPluginStrings = SelectedPluginIds.Select(id => id.ToString()).ToList();
            await _readerPluginMappingService.SetPluginsForReaderAsync(reader.ReaderId, selectedPluginStrings);
            
            // Refresh data from service to ensure consistency
            var readers = await _readerConfigurationService.GetAllReadersAsync();
            Readers = new ObservableCollection<ReaderConfiguration>(readers);
            
            // Re-register SignalR handlers to ensure real-time updates continue working
            // after potential connection restart from configuration changes
            ReregisterSignalRHandlers();
            
            // Also refresh reader statuses to get current state after any connection changes
            ReaderStatuses = await _readerService.GetAllReaderStatusesAsync() ?? new Dictionary<Guid, bool>();
            
            if (EditingReader == null)
            {
                SuccessMessage = "Reader added successfully";
            }
            else
            {
                SuccessMessage = "Reader updated successfully";
            }

            ShowReaderModal = false;
            ResetForm();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving reader");
            ErrorMessage = $"Error saving reader: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private void ConfirmDeleteReader(ReaderConfiguration reader)
    {
        ReaderToDelete = reader;
        ShowDeleteModal = true;
    }

    [RelayCommand]
    private async Task DeleteReaderAsync()
    {
        if (ReaderToDelete == null) return;

        try
        {
            IsDeleting = true;
            ErrorMessage = null;

            await _readerConfigurationService.DeleteReaderAsync(ReaderToDelete.ReaderId);
            
            // Refresh data from service to ensure consistency
            var readers = await _readerConfigurationService.GetAllReadersAsync();
            Readers = new ObservableCollection<ReaderConfiguration>(readers);
            
            SuccessMessage = "Reader deleted successfully";
            ShowDeleteModal = false;
            ReaderToDelete = null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting reader");
            ErrorMessage = $"Error deleting reader: {ex.Message}";
        }
        finally
        {
            IsDeleting = false;
        }
    }

    [RelayCommand]
    private void AddReader()
    {
        ResetForm();
        EditingReader = null;
        ShowReaderModal = true;
    }

    [RelayCommand]
    private void CancelDelete()
    {
        ShowDeleteModal = false;
        ReaderToDelete = null;
    }

    [RelayCommand]
    private void CloseReaderModal()
    {
        ShowReaderModal = false;
        ResetForm();
    }

    [RelayCommand]
    private void TogglePluginSelection(Guid pluginId)
    {
        if (SelectedPluginIds.Contains(pluginId))
        {
            SelectedPluginIds.Remove(pluginId);
        }
        else
        {
            SelectedPluginIds.Add(pluginId);
        }
    }

    public bool IsPluginSelected(Guid pluginId)
    {
        return SelectedPluginIds.Contains(pluginId);
    }

    public bool HasNoPluginsSelected => !SelectedPluginIds.Any();

    private void ResetForm()
    {
        ReaderName = string.Empty;
        Address = 1;
        SerialPort = null;
        BaudRate = 9600;
        SecurityMode = OsdpSecurityMode.ClearText;
        SecureChannelKey = null;
        IsEnabled = true;
        EditingReader = null;
        SelectedPluginIds.Clear();
    }

    private async Task LoadPluginsAsync()
    {
        try
        {
            LoadingPlugins = true;
            var plugins = await _pluginLoader.LoadPluginsAsync();
            AvailablePlugins = new ObservableCollection<IApBoxPlugin>(plugins);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading plugins");
            ErrorMessage = $"Error loading plugins: {ex.Message}";
        }
        finally
        {
            LoadingPlugins = false;
        }
    }

    private async Task InitializeSignalRAsync()
    {
        if (_hubConnection == null) return;

        try
        {
            // Check if connection is in a valid state before setting up handlers
            if (_hubConnection.State == HubConnectionState.Disconnected)
            {
                SetupSignalRHandlers();
                
                // Start the connection
                await _hubConnection.StartAsync();
                _logger.LogInformation("SignalR connection established for readers configuration");
            }
        }
        catch (ObjectDisposedException)
        {
            _logger.LogWarning("SignalR connection was disposed before initialization could complete");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing SignalR connection");
        }
    }

    private void SetupSignalRHandlers()
    {
        if (_hubConnection == null) return;

        // Set up event handlers and store disposables for cleanup
        var readerStatusSubscription = _hubConnection.On<ReaderStatusNotification>("ReaderStatusChanged", async (notification) =>
        {
            if (InvokeAsync != null)
            {
                await InvokeAsync(async () =>
                {
                    _logger.LogDebug("Received reader status change for {ReaderName} ({ReaderId}): {Status}", 
                        notification.ReaderName, notification.ReaderId, notification.IsOnline ? "Online" : "Offline");
                    
                    ReaderStatuses[notification.ReaderId] = notification.IsOnline;
                    StateHasChanged?.Invoke();
                });
            }
        });

        var configSubscription = _hubConnection.On<ReaderConfigurationNotification>("ReaderConfigurationChanged", async (notification) =>
        {
            if (InvokeAsync != null)
            {
                await InvokeAsync(async () =>
                {
                    await HandleReaderConfigurationChangeAsync(notification);
                    StateHasChanged?.Invoke();
                });
            }
        });

        var notificationSubscription = _hubConnection.On<string>("ReceiveNotification", async (message) =>
        {
            if (InvokeAsync != null)
            {
                await InvokeAsync(async () =>
                {
                    _logger.LogInformation("Received notification: {Message}", message);
                    StateHasChanged?.Invoke();
                });
            }
        });

        _signalRSubscriptions.Add(readerStatusSubscription);
        _signalRSubscriptions.Add(configSubscription);
        _signalRSubscriptions.Add(notificationSubscription);
    }

    private void ReregisterSignalRHandlers()
    {
        if (_hubConnection == null) return;

        try
        {
            _logger.LogDebug("Re-registering SignalR handlers after configuration change");
            
            // Dispose existing handlers
            foreach (var subscription in _signalRSubscriptions)
            {
                subscription?.Dispose();
            }
            _signalRSubscriptions.Clear();
            
            // Re-setup handlers
            SetupSignalRHandlers();
            
            _logger.LogDebug("SignalR handlers re-registered successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error re-registering SignalR handlers");
        }
    }

    private async Task HandleReaderConfigurationChangeAsync(ReaderConfigurationNotification notification)
    {
        try
        {
            switch (notification.ChangeType)
            {
                case "Created":
                    // Reload all readers to get the new one
                    var allReaders = await _readerConfigurationService.GetAllReadersAsync();
                    Readers = new ObservableCollection<ReaderConfiguration>(allReaders);
                    break;
                
                case "Updated":
                    // Find and update the existing reader
                    var existingReader = Readers.FirstOrDefault(r => r.ReaderId == notification.ReaderId);
                    if (existingReader != null)
                    {
                        var updatedReader = await _readerConfigurationService.GetReaderAsync(notification.ReaderId);
                        if (updatedReader != null)
                        {
                            var index = Readers.IndexOf(existingReader);
                            Readers[index] = updatedReader;
                        }
                    }
                    break;
                
                case "Deleted":
                    // Remove the reader from the collection
                    var readerToRemove = Readers.FirstOrDefault(r => r.ReaderId == notification.ReaderId);
                    if (readerToRemove != null)
                    {
                        Readers.Remove(readerToRemove);
                    }
                    break;
            }
            
            _logger.LogDebug("Handled reader configuration change: {ChangeType} for reader {ReaderName}", 
                notification.ChangeType, notification.ReaderName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling reader configuration change for reader {ReaderId}", notification.ReaderId);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        // Dispose SignalR subscriptions
        foreach (var subscription in _signalRSubscriptions)
        {
            subscription?.Dispose();
        }
        _signalRSubscriptions.Clear();

        if (_hubConnection != null)
        {
            try
            {
                // Stop the connection before disposing
                if (_hubConnection.State != HubConnectionState.Disconnected)
                {
                    await _hubConnection.StopAsync();
                }
                
                // Dispose the connection
                await _hubConnection.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error stopping SignalR connection during disposal");
            }
        }
    }
}