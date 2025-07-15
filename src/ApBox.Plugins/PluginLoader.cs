using System.Reflection;
using Microsoft.Extensions.Logging;

namespace ApBox.Plugins;

public interface IPluginLoader
{
    Task<IEnumerable<IApBoxPlugin>> LoadPluginsAsync();
    Task<IEnumerable<IApBoxPlugin>> ReloadPluginsAsync();
    Task UnloadPluginAsync(string pluginId);
    IEnumerable<PluginMetadata> GetAvailablePlugins();
}

public class PluginLoader : IPluginLoader
{
    private readonly string _pluginDirectory;
    private readonly ILogger<PluginLoader>? _logger;
    private readonly Dictionary<string, IApBoxPlugin> _loadedPlugins = new();
    private readonly List<PluginMetadata> _availablePlugins = new();
    private bool _pluginsLoaded = false;
    
    public PluginLoader(string pluginDirectory, ILogger<PluginLoader>? logger = null)
    {
        _pluginDirectory = pluginDirectory;
        _logger = logger;
    }
    
    public async Task<IEnumerable<IApBoxPlugin>> LoadPluginsAsync()
    {
        // Return already loaded plugins if they exist
        if (_pluginsLoaded)
        {
            _logger?.LogDebug("Plugins already loaded, returning cached plugins ({PluginCount} total)", _loadedPlugins.Count);
            return _loadedPlugins.Values;
        }
        
        _logger?.LogInformation("Loading plugins from directory: {PluginDirectory}", _pluginDirectory);
        
        if (!Directory.Exists(_pluginDirectory))
        {
            _logger?.LogWarning("Plugin directory does not exist: {PluginDirectory}", _pluginDirectory);
            _pluginsLoaded = true; // Mark as loaded even if directory doesn't exist
            return Enumerable.Empty<IApBoxPlugin>();
        }
        
        var loadedPlugins = new List<IApBoxPlugin>();
        var assemblyFiles = Directory.GetFiles(_pluginDirectory, "*.dll");
        
        _logger?.LogInformation("Found {AssemblyCount} assemblies to scan for plugins", assemblyFiles.Length);
        
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
        
        _pluginsLoaded = true; // Mark plugins as loaded
        _logger?.LogInformation("Successfully loaded {TotalPlugins} plugins total", loadedPlugins.Count);
        return loadedPlugins;
    }
    
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
                    Console.WriteLine($"Failed to create instance of plugin {pluginType.Name}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load assembly {assemblyPath}: {ex.Message}");
        }
        
        return plugins;
    }
}