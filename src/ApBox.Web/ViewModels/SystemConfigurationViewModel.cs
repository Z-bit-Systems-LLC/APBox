using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ApBox.Core.Services.Reader;
using ApBox.Core.Services.Configuration;
using ApBox.Core.Services.Infrastructure;
using ApBox.Core.Models;
using ApBox.Core.Services.Plugins;
using Microsoft.JSInterop;
using Blazorise;

namespace ApBox.Web.ViewModels;

/// <summary>
/// ViewModel for the System Configuration page using MVVM pattern
/// </summary>
public partial class SystemConfigurationViewModel(
    IReaderConfigurationService readerConfigurationService,
    IPluginLoader pluginLoader,
    IConfigurationExportService configurationExportService,
    ISystemRestartService systemRestartService,
    ILogService logService,
    IConfiguration configuration,
    IJSRuntime jsRuntime,
    ILogger<SystemConfigurationViewModel> logger)
    : ObservableObject, IAsyncDisposable
{
    private bool _disposed;
    private Timer? _uptimeTimer;
    private Timer? _logRefreshTimer;

    // System Information Properties
    [ObservableProperty]
    private int _readerCount;

    [ObservableProperty]
    private int _pluginCount;

    [ObservableProperty]
    private SystemInfo _systemInfo = new();

    [ObservableProperty]
    private string _pluginDirectory = "plugins/";

    // Loading States
    [ObservableProperty]
    private bool _refreshing;

    [ObservableProperty]
    private bool _exporting;

    [ObservableProperty]
    private bool _importing;

    [ObservableProperty]
    private bool _restarting;

    [ObservableProperty]
    private bool _exportingLogs;

    [ObservableProperty]
    private bool _refreshingLogs;

    // File Import
    [ObservableProperty]
    private IFileEntry? _selectedFile;

    // Log Viewer State
    [ObservableProperty]
    private ObservableCollection<LogEntry> _recentLogs = new();

    [ObservableProperty]
    private ObservableCollection<LogEntry> _filteredLogs = new();

    [ObservableProperty]
    private LogLevel _selectedLogLevel = LogLevel.Information;

    [ObservableProperty]
    private string _searchTerm = string.Empty;

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _pageSize = 50;

    [ObservableProperty]
    private int _totalLogCount;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _successMessage;

    // Component callbacks
    public Action? StateHasChanged { get; set; }
    public Func<Func<Task>, Task>? InvokeAsync { get; set; }
    public Func<string, Task>? OnShowMessage { get; set; }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        await LoadSystemInfoAsync();
        await LoadPluginInfoAsync();
        await LoadRecentLogsAsync();
        
        // Start uptime timer to update every 5 seconds
        _uptimeTimer = new Timer(_ => InvokeAsync?.Invoke(() =>
        {
            StateHasChanged?.Invoke();
            return Task.CompletedTask;
        }), null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        
        // Start log refresh timer to update every 2 seconds
        _logRefreshTimer = new Timer(_ => InvokeAsync?.Invoke(LoadRecentLogsAsync), null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
    }

    [RelayCommand]
    private async Task RefreshSystemInfoAsync()
    {
        Refreshing = true;
        
        try
        {
            await LoadSystemInfoAsync();
            await ReloadPluginInfoAsync();
            SuccessMessage = "System information refreshed";
            await ShowSuccessMessageAsync("System information refreshed|success");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error refreshing system info");
            ErrorMessage = $"Error refreshing system info: {ex.Message}";
            await ShowErrorMessageAsync($"Error refreshing system info: {ex.Message}|danger");
        }
        finally
        {
            Refreshing = false;
        }
    }

    [RelayCommand]
    private async Task ExportConfigurationAsync()
    {
        Exporting = true;
        
        try
        {
            var exportData = await configurationExportService.ExportConfigurationAsync();
            var json = System.Text.Json.JsonSerializer.Serialize(exportData, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });
            
            var fileName = $"apbox-config-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            
            await jsRuntime.InvokeVoidAsync("downloadFile", fileName, "application/json", bytes);
            await ShowSuccessMessageAsync("Configuration exported successfully|success");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error exporting configuration");
            ErrorMessage = $"Error exporting configuration: {ex.Message}";
            await ShowErrorMessageAsync($"Error exporting configuration: {ex.Message}|danger");
        }
        finally
        {
            Exporting = false;
        }
    }

    [RelayCommand]
    private async Task ConfirmImportAsync()
    {
        if (SelectedFile == null)
            return;

        Importing = true;
        
        try
        {
            await using var stream = SelectedFile.OpenReadStream(maxAllowedSize: 1024 * 1024); // 1MB limit
            using var reader = new StreamReader(stream);
            await reader.ReadToEndAsync();
            
            // Here you would call your configuration import service
            // For now, we'll show a success message
            await ShowSuccessMessageAsync($"Configuration file '{SelectedFile.Name}' imported successfully. Import functionality will be implemented.|info");
            
            // Reset selected file
            SelectedFile = null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error importing configuration");
            ErrorMessage = $"Error importing configuration: {ex.Message}";
            await ShowErrorMessageAsync($"Error importing configuration: {ex.Message}|danger");
        }
        finally
        {
            Importing = false;
        }
    }

    [RelayCommand]
    private async Task ConfirmRestartAsync()
    {
        Restarting = true;
        
        try
        {
            var canRestart = await systemRestartService.CanRestartAsync();
            if (!canRestart)
            {
                await ShowErrorMessageAsync("System cannot be restarted at this time|warning");
                return;
            }
            
            await ShowSuccessMessageAsync("Preparing system for restart...|info");
            await systemRestartService.PrepareRestartAsync();
            
            await ShowSuccessMessageAsync("Restarting system...|warning");
            await systemRestartService.RestartApplicationAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error restarting system");
            ErrorMessage = $"Error restarting system: {ex.Message}";
            await ShowErrorMessageAsync($"Error restarting system: {ex.Message}|danger");
        }
        finally
        {
            Restarting = false;
        }
    }

    [RelayCommand]
    private async Task RefreshLogsAsync()
    {
        RefreshingLogs = true;
        
        try
        {
            await LoadRecentLogsAsync();
        }
        finally
        {
            RefreshingLogs = false;
        }
    }

    [RelayCommand]
    private async Task ExportLogsAsync()
    {
        ExportingLogs = true;
        
        try
        {
            var logData = await logService.ExportLogsAsync();
            var fileName = $"apbox-logs-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
            
            await jsRuntime.InvokeVoidAsync("downloadFile", fileName, "application/json", logData);
            await ShowSuccessMessageAsync("Logs exported successfully|success");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error exporting logs");
            ErrorMessage = $"Error exporting logs: {ex.Message}";
            await ShowErrorMessageAsync($"Error exporting logs: {ex.Message}|danger");
        }
        finally
        {
            ExportingLogs = false;
        }
    }

    [RelayCommand]
    private void PreviousPage()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            FilterLogs();
        }
    }

    [RelayCommand]
    private void NextPage()
    {
        var maxPage = (int)Math.Ceiling((double)TotalLogCount / PageSize);
        if (CurrentPage < maxPage)
        {
            CurrentPage++;
            FilterLogs();
        }
    }

    public async Task OnFileSelectedAsync(IFileEntry file)
    {
        if (file.Type != "application/json" && !file.Name.EndsWith(".json"))
        {
            await ShowErrorMessageAsync("Please select a JSON file|warning");
            return;
        }

        SelectedFile = file;
    }

    public void ClearSelectedFile()
    {
        SelectedFile = null;
    }

    public void OnSearchTermChanged()
    {
        FilterLogs();
    }

    public void OnLogLevelChanged()
    {
        FilterLogs();
    }

    public string GetSystemUptime()
    {
        try
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            var uptime = DateTime.UtcNow - process.StartTime.ToUniversalTime();
            
            if (uptime.Days > 0)
                return $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m";
            else if (uptime.Hours > 0)
                return $"{uptime.Hours}h {uptime.Minutes}m";
            else
                return $"{uptime.Minutes}m {uptime.Seconds}s";
        }
        catch
        {
            return "Unknown";
        }
    }

    public string FormatFileSize(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";
        else if (bytes < 1024 * 1024)
            return $"{bytes / 1024.0:F1} KB";
        else if (bytes < 1024 * 1024 * 1024)
            return $"{bytes / (1024.0 * 1024.0):F1} MB";
        else
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):F1} GB";
    }

    public Color GetLogLevelColor(LogLevel level) => level switch
    {
        LogLevel.Trace => Color.Light,
        LogLevel.Debug => Color.Secondary,
        LogLevel.Information => Color.Info,
        LogLevel.Warning => Color.Warning,
        LogLevel.Error => Color.Danger,
        LogLevel.Critical => Color.Dark,
        _ => Color.Light
    };

    public TextColor GetLogLevelTextColor(LogLevel level) => level switch
    {
        LogLevel.Trace => TextColor.Muted,
        LogLevel.Debug => TextColor.Secondary,
        LogLevel.Information => TextColor.Info,
        LogLevel.Warning => TextColor.Warning,
        LogLevel.Error => TextColor.Danger,
        LogLevel.Critical => TextColor.Dark,
        _ => TextColor.Body
    };

    private async Task LoadSystemInfoAsync()
    {
        try
        {
            var readers = await readerConfigurationService.GetAllReadersAsync();
            ReaderCount = readers.Count();
            
            // Get plugin directory from configuration
            PluginDirectory = configuration.GetValue<string>("PluginSettings:Directory") ?? "plugins/";
            
            // Initialize system info with current values
            SystemInfo = new SystemInfo();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading system info");
            ErrorMessage = $"Error loading system info: {ex.Message}";
        }
    }

    private async Task LoadPluginInfoAsync()
    {
        try
        {
            var plugins = await pluginLoader.LoadPluginsAsync();
            PluginCount = plugins.Count();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading plugin info");
            ErrorMessage = $"Error loading plugin info: {ex.Message}";
        }
    }

    private async Task ReloadPluginInfoAsync()
    {
        try
        {
            var plugins = await pluginLoader.ReloadPluginsAsync();
            PluginCount = plugins.Count();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reloading plugin info");
            ErrorMessage = $"Error reloading plugin info: {ex.Message}";
        }
    }

    private async Task LoadRecentLogsAsync()
    {
        try
        {
            var logs = await logService.GetRecentLogsAsync();
            RecentLogs = new ObservableCollection<LogEntry>(logs);
            FilterLogs();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading logs");
            ErrorMessage = $"Error loading logs: {ex.Message}";
        }
    }

    private void FilterLogs()
    {
        if (!RecentLogs.Any())
        {
            FilteredLogs = new ObservableCollection<LogEntry>();
            TotalLogCount = 0;
            StateHasChanged?.Invoke();
            return;
        }
        
        var filtered = RecentLogs.Where(l => l.Level >= SelectedLogLevel);
        
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            filtered = filtered.Where(l => 
                l.Message.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                l.Source.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                (l.Exception?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false));
        }
        
        var orderedFiltered = filtered.OrderByDescending(l => l.Timestamp).ToArray();
        TotalLogCount = orderedFiltered.Count();
        
        // Apply pagination
        var pagedLogs = orderedFiltered
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize);

        FilteredLogs = new ObservableCollection<LogEntry>(pagedLogs);
        StateHasChanged?.Invoke();
    }

    private async Task ShowSuccessMessageAsync(string message)
    {
        if (OnShowMessage != null)
        {
            await OnShowMessage(message);
        }
    }

    private async Task ShowErrorMessageAsync(string message)
    {
        if (OnShowMessage != null)
        {
            await OnShowMessage(message);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        _uptimeTimer?.Dispose();
        _logRefreshTimer?.Dispose();

        await Task.CompletedTask;
    }
}