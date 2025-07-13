using System.Reflection;

namespace ApBox.Plugins;

public interface IPluginLoader
{
    Task<IEnumerable<IApBoxPlugin>> LoadPluginsAsync();
    Task UnloadPluginAsync(string pluginId);
    IEnumerable<PluginMetadata> GetAvailablePlugins();
}

public class PluginLoader : IPluginLoader
{
    private readonly string _pluginDirectory;
    private readonly Dictionary<string, IApBoxPlugin> _loadedPlugins = new();
    private readonly List<PluginMetadata> _availablePlugins = new();
    
    public PluginLoader(string pluginDirectory)
    {
        _pluginDirectory = pluginDirectory;
    }
    
    public async Task<IEnumerable<IApBoxPlugin>> LoadPluginsAsync()
    {
        if (!Directory.Exists(_pluginDirectory))
        {
            return Enumerable.Empty<IApBoxPlugin>();
        }
        
        var loadedPlugins = new List<IApBoxPlugin>();
        var assemblyFiles = Directory.GetFiles(_pluginDirectory, "*.dll");
        
        foreach (var assemblyFile in assemblyFiles)
        {
            try
            {
                var plugins = await LoadPluginsFromAssemblyAsync(assemblyFile);
                loadedPlugins.AddRange(plugins);
            }
            catch (Exception ex)
            {
                // Log exception and continue with other assemblies
                Console.WriteLine($"Failed to load plugins from {assemblyFile}: {ex.Message}");
            }
        }
        
        return loadedPlugins;
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