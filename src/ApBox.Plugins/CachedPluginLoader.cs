using System.Reflection;
using Microsoft.Extensions.Logging;

namespace ApBox.Plugins;

/// <summary>
/// Enhanced plugin loader with file system monitoring and proper caching
/// </summary>
public class CachedPluginLoader : IPluginLoader, IDisposable
{
    private readonly string _pluginDirectory;
    private readonly ILogger<CachedPluginLoader>? _logger;
    private readonly Dictionary<string, IApBoxPlugin> _loadedPlugins = new();
    private readonly List<PluginMetadata> _availablePlugins = new();
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private FileSystemWatcher? _fileWatcher;
    private bool _pluginsLoaded = false;
    private DateTime _lastLoadTime = DateTime.MinValue;
    
    public CachedPluginLoader(string pluginDirectory, ILogger<CachedPluginLoader>? logger = null)
    {
        _pluginDirectory = pluginDirectory;
        _logger = logger;
        InitializeFileWatcher();
    }
    
    private void InitializeFileWatcher()
    {
        try
        {
            if (Directory.Exists(_pluginDirectory))
            {
                _fileWatcher = new FileSystemWatcher(_pluginDirectory)
                {
                    Filter = "*.dll",
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
                    EnableRaisingEvents = true
                };
                
                _fileWatcher.Changed += OnPluginFileChanged;
                _fileWatcher.Created += OnPluginFileChanged;
                _fileWatcher.Deleted += OnPluginFileChanged;
                _fileWatcher.Renamed += OnPluginFileChanged;
                
                _logger?.LogInformation("File system watcher initialized for plugin directory: {Directory}", _pluginDirectory);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize file system watcher for plugin directory");
        }
    }
    
    private void OnPluginFileChanged(object sender, FileSystemEventArgs e)
    {
        _logger?.LogInformation("Plugin file change detected: {ChangeType} - {FileName}", e.ChangeType, e.Name);
        
        // Mark cache as invalid
        _pluginsLoaded = false;
        
        // Clear plugin cache to force reload on next access
        lock (_loadedPlugins)
        {
            _loadedPlugins.Clear();
            _availablePlugins.Clear();
        }
    }
    
    /// <inheritdoc/>
    public async Task<IEnumerable<IApBoxPlugin>> LoadPluginsAsync()
    {
        // Fast path - return cached plugins if loaded and valid
        if (_pluginsLoaded && _loadedPlugins.Count > 0)
        {
            var timeSinceLastLoad = DateTime.UtcNow - _lastLoadTime;
            if (timeSinceLastLoad.TotalSeconds < 1) // Prevent reload storms
            {
                _logger?.LogDebug("Plugins loaded recently, returning cached plugins ({PluginCount} total)", _loadedPlugins.Count);
                return _loadedPlugins.Values.ToList(); // Return a copy to prevent modification
            }
        }
        
        // Acquire lock for loading
        await _loadLock.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (_pluginsLoaded && _loadedPlugins.Count > 0)
            {
                _logger?.LogDebug("Plugins already loaded by another thread, returning cached plugins ({PluginCount} total)", _loadedPlugins.Count);
                return _loadedPlugins.Values.ToList();
            }
            
            _logger?.LogInformation("Loading plugins from directory: {PluginDirectory}", _pluginDirectory);
            
            if (!Directory.Exists(_pluginDirectory))
            {
                _logger?.LogWarning("Plugin directory does not exist: {PluginDirectory}", _pluginDirectory);
                _pluginsLoaded = true;
                _lastLoadTime = DateTime.UtcNow;
                return Enumerable.Empty<IApBoxPlugin>();
            }
            
            var loadedPlugins = new List<IApBoxPlugin>();
            var assemblyFiles = Directory.GetFiles(_pluginDirectory, "*.dll");
            
            _logger?.LogInformation("Found {AssemblyCount} assemblies to scan for plugins", assemblyFiles.Length);
            
            // Clear existing plugins before loading new ones
            foreach (var plugin in _loadedPlugins.Values)
            {
                try
                {
                    await plugin.ShutdownAsync();
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error shutting down plugin {PluginName}", plugin.Name);
                }
            }
            
            _loadedPlugins.Clear();
            _availablePlugins.Clear();
            
            foreach (var assemblyFile in assemblyFiles)
            {
                try
                {
                    var plugins = await LoadPluginsFromAssemblyAsync(assemblyFile);
                    loadedPlugins.AddRange(plugins);
                    _logger?.LogInformation("Loaded {PluginCount} plugins from {AssemblyFile}", plugins.Count(), Path.GetFileName(assemblyFile));
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to load plugins from {AssemblyFile}", Path.GetFileName(assemblyFile));
                }
            }
            
            _pluginsLoaded = true;
            _lastLoadTime = DateTime.UtcNow;
            _logger?.LogInformation("Successfully loaded {TotalPlugins} plugins total", loadedPlugins.Count);
            return loadedPlugins;
        }
        finally
        {
            _loadLock.Release();
        }
    }
    
    /// <inheritdoc/>
    public async Task<IEnumerable<IApBoxPlugin>> ReloadPluginsAsync()
    {
        _logger?.LogInformation("Force reloading plugins from directory: {PluginDirectory}", _pluginDirectory);
        
        // Mark as not loaded to force reload
        _pluginsLoaded = false;
        
        // Load plugins fresh
        return await LoadPluginsAsync();
    }
    
    /// <inheritdoc/>
    public async Task UnloadPluginAsync(string pluginId)
    {
        if (_loadedPlugins.TryGetValue(pluginId, out var plugin))
        {
            try
            {
                await plugin.ShutdownAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error shutting down plugin {PluginId}", pluginId);
            }
            finally
            {
                _loadedPlugins.Remove(pluginId);
                _availablePlugins.RemoveAll(p => p.Id == pluginId);
            }
        }
    }
    
    /// <inheritdoc/>
    public IEnumerable<PluginMetadata> GetAvailablePlugins()
    {
        return _availablePlugins.AsReadOnly();
    }
    
    private async Task<IEnumerable<IApBoxPlugin>> LoadPluginsFromAssemblyAsync(string assemblyPath)
    {
        var plugins = new List<IApBoxPlugin>();
        
        try
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            var pluginTypes = assembly.GetTypes()
                .Where(t => typeof(IApBoxPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
            
            foreach (var pluginType in pluginTypes)
            {
                try
                {
                    if (Activator.CreateInstance(pluginType) is IApBoxPlugin plugin)
                    {
                        await plugin.InitializeAsync();
                        
                        var metadata = new PluginMetadata
                        {
                            Id = Guid.NewGuid().ToString(),
                            Name = plugin.Name,
                            Version = plugin.Version,
                            Description = plugin.Description,
                            AssemblyPath = assemblyPath,
                            IsEnabled = true
                        };
                        
                        _availablePlugins.Add(metadata);
                        _loadedPlugins[metadata.Id] = plugin;
                        plugins.Add(plugin);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to create instance of plugin {PluginType}", pluginType.Name);
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load assembly {AssemblyPath}", assemblyPath);
        }
        
        return plugins;
    }
    
    /// <summary>
    /// Disposes resources and shuts down all loaded plugins
    /// </summary>
    public void Dispose()
    {
        _fileWatcher?.Dispose();
        
        // Shutdown all plugins
        foreach (var plugin in _loadedPlugins.Values)
        {
            try
            {
                plugin.ShutdownAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error shutting down plugin {PluginName} during disposal", plugin.Name);
            }
        }
        
        _loadedPlugins.Clear();
        _availablePlugins.Clear();
        _loadLock?.Dispose();
    }
}