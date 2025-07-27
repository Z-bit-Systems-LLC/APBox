using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ApBox.Core.Services;
using ApBox.Core.Models;
using ApBox.Web.Services;
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
    private readonly ILogger<ReadersConfigurationViewModel> _logger;
    private readonly IHubConnectionWrapper? _hubConnection;
    private bool _disposed = false;

    public ReadersConfigurationViewModel(
        IReaderConfigurationService readerConfigurationService,
        IReaderService readerService,
        ISerialPortService serialPortService,
        ILogger<ReadersConfigurationViewModel> logger,
        IHubConnectionWrapper? hubConnectionWrapper = null)
    {
        _readerConfigurationService = readerConfigurationService;
        _readerService = readerService;
        _serialPortService = serialPortService;
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

    // Form data
    [ObservableProperty]
    [Required(ErrorMessage = "Reader name is required")]
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
    private void EditReader(ReaderConfiguration reader)
    {
        EditingReader = reader;
        
        // Populate form with existing values
        ReaderName = reader.ReaderName;
        Address = reader.Address;
        SerialPort = reader.SerialPort;
        BaudRate = reader.BaudRate;
        SecurityMode = reader.SecurityMode;
        SecureChannelKey = reader.SecureChannelKey;
        IsEnabled = reader.IsEnabled;
        
        ShowReaderModal = true;
    }

    [RelayCommand]
    private async Task SaveReaderAsync()
    {
        try
        {
            IsSaving = true;
            ErrorMessage = null;

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
            
            if (EditingReader == null)
            {
                // Add to collection for new reader
                Readers.Add(reader);
                SuccessMessage = "Reader added successfully";
            }
            else
            {
                
                // Update in collection
                var index = Readers.IndexOf(EditingReader);
                if (index >= 0)
                {
                    Readers[index] = reader;
                }
                
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
            Readers.Remove(ReaderToDelete);
            
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
    }

    private async Task InitializeSignalRAsync()
    {
        if (_hubConnection == null) return;

        try
        {
            // Check if connection is in a valid state before setting up handlers
            if (_hubConnection.State == HubConnectionState.Disconnected)
            {
                // Set up event handlers before starting the connection
                _hubConnection.On<Guid, bool>("ReaderStatusChanged", async (readerId, isOnline) =>
                {
                    if (InvokeAsync != null)
                    {
                        await InvokeAsync(async () =>
                        {
                            ReaderStatuses[readerId] = isOnline;
                            StateHasChanged?.Invoke();
                        });
                    }
                });

                _hubConnection.On<string>("ReceiveNotification", async (message) =>
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

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

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