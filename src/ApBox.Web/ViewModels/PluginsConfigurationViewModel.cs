using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ApBox.Plugins;

namespace ApBox.Web.ViewModels;

/// <summary>
/// ViewModel for the Plugins Configuration page using MVVM pattern
/// </summary>
public partial class PluginsConfigurationViewModel(
    IPluginLoader pluginLoader,
    ILogger<PluginsConfigurationViewModel> logger)
    : ObservableObject, IAsyncDisposable
{
    private bool _disposed;

    [ObservableProperty]
    private ObservableCollection<IApBoxPlugin> _loadedPlugins = new();

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _successMessage;

    // Component callbacks
    public Action? StateHasChanged { get; set; }
    public Func<Func<Task>, Task>? InvokeAsync { get; set; }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        await LoadPluginsAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadPluginsAsync();
    }

    private async Task LoadPluginsAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var plugins = await pluginLoader.LoadPluginsAsync();
            LoadedPlugins = new ObservableCollection<IApBoxPlugin>(plugins);
            
            logger.LogInformation("Loaded {PluginCount} plugins", LoadedPlugins.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading plugins");
            ErrorMessage = $"Error loading plugins: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        // No resources to dispose for this ViewModel
        await Task.CompletedTask;
    }
}