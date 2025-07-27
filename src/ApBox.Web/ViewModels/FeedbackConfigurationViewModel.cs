using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ApBox.Core.Services;
using ApBox.Core.Models;
using Microsoft.Extensions.Logging;

namespace ApBox.Web.ViewModels;

/// <summary>
/// ViewModel for the Feedback Configuration page using MVVM pattern
/// </summary>
public partial class FeedbackConfigurationViewModel : ObservableObject, IAsyncDisposable
{
    private readonly IFeedbackConfigurationService _feedbackConfigurationService;
    private readonly ILogger<FeedbackConfigurationViewModel> _logger;

    public FeedbackConfigurationViewModel(
        IFeedbackConfigurationService feedbackConfigurationService,
        ILogger<FeedbackConfigurationViewModel> logger)
    {
        _feedbackConfigurationService = feedbackConfigurationService;
        _logger = logger;

        // Initialize with defaults
        ResetFormDefaults();
    }

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private bool _isResetting;

    [ObservableProperty]
    private bool _autoSaveEnabled = true;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _successMessage;

    // Success feedback form properties
    [ObservableProperty]
    private LedColor _successLedColor = LedColor.Green;

    [ObservableProperty]
    private int _successLedDurationSeconds = 1;

    [ObservableProperty]
    private int _successBeepCount = 1;

    [ObservableProperty]
    private string _successDisplayMessage = "ACCESS GRANTED";

    // Failure feedback form properties
    [ObservableProperty]
    private LedColor _failureLedColor = LedColor.Red;

    [ObservableProperty]
    private int _failureLedDurationSeconds = 2;

    [ObservableProperty]
    private int _failureBeepCount = 3;

    [ObservableProperty]
    private string _failureDisplayMessage = "ACCESS DENIED";

    // Idle state form properties
    [ObservableProperty]
    private LedColor _idlePermanentLedColor = LedColor.Blue;

    [ObservableProperty]
    private LedColor _idleHeartbeatFlashColor = LedColor.Green;

    // Modal visibility
    [ObservableProperty]
    private bool _showResetModal;

    // Component callbacks
    public Action? StateHasChanged { get; set; }
    public Func<Func<Task>, Task>? InvokeAsync { get; set; }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            AutoSaveEnabled = false; // Disable auto-save during loading

            await LoadConfiguration();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing feedback configuration");
            ErrorMessage = $"Error loading configuration: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            AutoSaveEnabled = true;
        }
    }

    private async Task LoadConfiguration()
    {
        var configuration = await _feedbackConfigurationService.GetDefaultConfigurationAsync();

        // Map success feedback
        var successFeedback = configuration.SuccessFeedback;
        SuccessLedColor = successFeedback.LedColor ?? LedColor.Green;
        SuccessLedDurationSeconds = successFeedback.LedDuration;
        SuccessBeepCount = successFeedback.BeepCount;
        SuccessDisplayMessage = successFeedback.DisplayMessage ?? "ACCESS GRANTED";

        // Map failure feedback
        var failureFeedback = configuration.FailureFeedback;
        FailureLedColor = failureFeedback.LedColor ?? LedColor.Red;
        FailureLedDurationSeconds = failureFeedback.LedDuration;
        FailureBeepCount = failureFeedback.BeepCount;
        FailureDisplayMessage = failureFeedback.DisplayMessage ?? "ACCESS DENIED";

        // Map idle state
        var idleState = configuration.IdleState;
        IdlePermanentLedColor = idleState.PermanentLedColor ?? LedColor.Blue;
        IdleHeartbeatFlashColor = idleState.HeartbeatFlashColor ?? LedColor.Green;
    }

    [RelayCommand]
    private void ShowResetConfirmation()
    {
        ShowResetModal = true;
    }

    [RelayCommand]
    private async Task ResetToDefaultsAsync()
    {
        try
        {
            IsResetting = true;
            ErrorMessage = null;

            // Reset to defaults
            ResetFormDefaults();

            // Save the default configuration
            await SaveConfiguration();

            SuccessMessage = "Configuration reset to defaults successfully";
            ShowResetModal = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting to defaults");
            ErrorMessage = $"Error resetting configuration: {ex.Message}";
        }
        finally
        {
            IsResetting = false;
        }
    }

    [RelayCommand]
    private void CancelReset()
    {
        ShowResetModal = false;
    }

    [RelayCommand]
    private async Task SaveConfigurationAsync()
    {
        if (AutoSaveEnabled)
        {
            await SaveConfiguration();
        }
    }

    private async Task SaveConfiguration()
    {
        try
        {
            var configuration = new FeedbackConfiguration
            {
                SuccessFeedback = new ReaderFeedback
                {
                    Type = ReaderFeedbackType.Success,
                    LedColor = SuccessLedColor,
                    LedDuration = SuccessLedDurationSeconds,
                    BeepCount = SuccessBeepCount,
                    DisplayMessage = string.IsNullOrWhiteSpace(SuccessDisplayMessage) ? null : SuccessDisplayMessage
                },
                FailureFeedback = new ReaderFeedback
                {
                    Type = ReaderFeedbackType.Failure,
                    LedColor = FailureLedColor,
                    LedDuration = FailureLedDurationSeconds,
                    BeepCount = FailureBeepCount,
                    DisplayMessage = string.IsNullOrWhiteSpace(FailureDisplayMessage) ? null : FailureDisplayMessage
                },
                IdleState = new IdleStateFeedback
                {
                    PermanentLedColor = IdlePermanentLedColor,
                    HeartbeatFlashColor = IdleHeartbeatFlashColor
                }
            };

            await _feedbackConfigurationService.SaveDefaultConfigurationAsync(configuration);
            SuccessMessage = "Configuration saved successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving feedback configuration");
            ErrorMessage = $"Error saving configuration: {ex.Message}";
        }
    }

    private void ResetFormDefaults()
    {
        // Success defaults
        SuccessLedColor = LedColor.Green;
        SuccessLedDurationSeconds = 1;
        SuccessBeepCount = 1;
        SuccessDisplayMessage = "ACCESS GRANTED";

        // Failure defaults
        FailureLedColor = LedColor.Red;
        FailureLedDurationSeconds = 2;
        FailureBeepCount = 3;
        FailureDisplayMessage = "ACCESS DENIED";

        // Idle state defaults
        IdlePermanentLedColor = LedColor.Blue;
        IdleHeartbeatFlashColor = LedColor.Green;
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        // Auto-save when form properties change
        if (AutoSaveEnabled && IsFormProperty(e.PropertyName))
        {
            _ = SaveConfigurationAsync();
        }
    }

    private bool IsFormProperty(string? propertyName)
    {
        return propertyName?.StartsWith("Success") == true ||
               propertyName?.StartsWith("Failure") == true ||
               propertyName?.StartsWith("Idle") == true;
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}