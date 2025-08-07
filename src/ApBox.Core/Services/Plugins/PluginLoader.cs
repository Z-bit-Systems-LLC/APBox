using System.Reflection;
using ApBox.Plugins;
using ApBox.Core.Services.Infrastructure;

namespace ApBox.Core.Services.Plugins;

/// <summary>
/// Default implementation of IPluginLoader that loads plugins from a specified directory.
/// Supports caching of loaded plugins and provides metadata about available plugins.
/// </summary>
public class PluginLoader : IPluginLoader
{
    private readonly string _pluginDirectory;
    private readonly ILogger<PluginLoader>? _logger;
    private readonly ILoggerFactory? _loggerFactory;
    private readonly IFileSystem _fileSystem;
    private readonly Dictionary<string, IApBoxPlugin> _loadedPlugins = new();
    private readonly List<PluginMetadata> _availablePlugins = new();
    private bool _pluginsLoaded = false;
    
    /// <summary>
    /// Initializes a new instance of the PluginLoader class.
    /// </summary>
    /// <param name="pluginDirectory">Directory path where plugin assemblies are located</param>
    /// <param name="logger">Optional logger for diagnostic information</param>
    public PluginLoader(string pluginDirectory, ILogger<PluginLoader>? logger = null) 
        : this(pluginDirectory, new FileSystem(), logger, null)
    {
    }
    
    /// <summary>
    /// Initializes a new instance of the PluginLoader class with a custom file system implementation.
    /// </summary>
    /// <param name="pluginDirectory">Directory path where plugin assemblies are located</param>
    /// <param name="fileSystem">File system abstraction for testing purposes</param>
    /// <param name="logger">Optional logger for diagnostic information</param>
    public PluginLoader(string pluginDirectory, IFileSystem fileSystem, ILogger<PluginLoader>? logger = null)
        : this(pluginDirectory, fileSystem, logger, null)
    {
    }
    
    /// <summary>
    /// Initializes a new instance of the PluginLoader class with dependency injection support.
    /// </summary>
    /// <param name="pluginDirectory">Directory path where plugin assemblies are located</param>
    /// <param name="fileSystem">File system abstraction for testing purposes</param>
    /// <param name="logger">Optional logger for diagnostic information</param>
    /// <param name="loggerFactory">Optional logger factory for plugin dependency injection</param>
    public PluginLoader(string pluginDirectory, IFileSystem fileSystem, ILogger<PluginLoader>? logger, ILoggerFactory? loggerFactory)
    {
        _pluginDirectory = pluginDirectory;
        _fileSystem = fileSystem;
        _logger = logger;
        _loggerFactory = loggerFactory;
    }
    
    /// <inheritdoc/>
    public async Task<IEnumerable<IApBoxPlugin>> LoadPluginsAsync()
    {
        // Return already loaded plugins if they exist
        if (_pluginsLoaded)
        {
            _logger?.LogDebug("Plugins already loaded, returning cached plugins ({PluginCount} total)", _loadedPlugins.Count);
            return _loadedPlugins.Values;
        }
        
        _logger?.LogInformation("Loading plugins from directory: {PluginDirectory}", _pluginDirectory);
        
        if (!_fileSystem.DirectoryExists(_pluginDirectory))
        {
            _logger?.LogWarning("Plugin directory does not exist: {PluginDirectory}", _pluginDirectory);
            _pluginsLoaded = true; // Mark as loaded even if directory doesn't exist
            return Enumerable.Empty<IApBoxPlugin>();
        }
        
        var loadedPlugins = new List<IApBoxPlugin>();
        var assemblyFiles = _fileSystem.GetFiles(_pluginDirectory, "*.dll");
        
        _logger?.LogInformation("Found {AssemblyCount} assemblies to scan for plugins", assemblyFiles.Length);
        
        foreach (var assemblyFile in assemblyFiles)
        {
            try
            {
                var plugins = await LoadPluginsFromAssemblyAsync(assemblyFile);
                loadedPlugins.AddRange(plugins);
                _logger?.LogInformation("Loaded {PluginCount} plugins from {AssemblyFile}", plugins.Count(), _fileSystem.GetFileName(assemblyFile));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load plugins from {AssemblyFile}", _fileSystem.GetFileName(assemblyFile));
            }
        }
        
        _pluginsLoaded = true; // Mark plugins as loaded
        _logger?.LogInformation("Successfully loaded {TotalPlugins} plugins total", loadedPlugins.Count);
        return loadedPlugins;
    }
    
    /// <inheritdoc/>
    public async Task<IEnumerable<IApBoxPlugin>> ReloadPluginsAsync()
    {
        _logger?.LogInformation("Force reloading plugins from directory: {PluginDirectory}", _pluginDirectory);
        
        // Clear existing plugins
        _loadedPlugins.Clear();
        _availablePlugins.Clear();
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
                Console.WriteLine($"Error shutting down plugin {pluginId}: {ex.Message}");
            }
            finally
            {
                _loadedPlugins.Remove(pluginId);
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
            var pluginTypes = PluginInstanceFactory.GetPluginTypes(assembly);
            
            foreach (var pluginType in pluginTypes)
            {
                try
                {
                    var plugin = PluginInstanceFactory.CreateInstance(pluginType, _loggerFactory, _logger);
                    if (plugin != null)
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
}