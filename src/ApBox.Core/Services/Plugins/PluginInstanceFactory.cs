using System.Reflection;
using Microsoft.Extensions.Logging;
using ApBox.Plugins;

namespace ApBox.Core.Services.Plugins;

/// <summary>
/// Static factory for creating plugin instances with proper dependency injection support.
/// Centralizes the plugin instantiation logic to avoid duplication across multiple plugin loaders.
/// </summary>
public static class PluginInstanceFactory
{
    /// <summary>
    /// Creates a plugin instance with proper dependency injection support
    /// </summary>
    /// <param name="pluginType">The plugin type to instantiate</param>
    /// <param name="loggerFactory">Optional logger factory for dependency injection</param>
    /// <param name="logger">Optional logger for error reporting</param>
    /// <returns>A new plugin instance, or null if instantiation failed</returns>
    public static IApBoxPlugin? CreateInstance(Type pluginType, ILoggerFactory? loggerFactory = null, ILogger? logger = null)
    {
        try
        {
            // Try to inject logger if LoggerFactory is available
            if (loggerFactory != null)
            {
                // Look for constructors that take ILogger<T> or ILogger
                var genericLoggerType = typeof(ILogger<>).MakeGenericType(pluginType);
                var nonGenericLoggerType = typeof(ILogger);
                
                var constructorWithLogger = pluginType.GetConstructor(new[] { genericLoggerType }) 
                                           ?? pluginType.GetConstructor(new[] { nonGenericLoggerType });
                
                if (constructorWithLogger != null)
                {
                    // Create logger using string-based method
                    var stringLogger = loggerFactory.CreateLogger(pluginType.FullName ?? pluginType.Name);
                    return (IApBoxPlugin?)Activator.CreateInstance(pluginType, stringLogger);
                }
            }
            
            // Fall back to parameterless constructor
            return (IApBoxPlugin?)Activator.CreateInstance(pluginType);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to create instance of plugin {PluginType}", pluginType.Name);
            return null;
        }
    }
    
    /// <summary>
    /// Checks if a type is a valid plugin type
    /// </summary>
    /// <param name="type">The type to check</param>
    /// <returns>True if the type implements IApBoxPlugin and can be instantiated</returns>
    public static bool IsValidPluginType(Type type)
    {
        return typeof(IApBoxPlugin).IsAssignableFrom(type) && 
               !type.IsInterface && 
               !type.IsAbstract;
    }
    
    /// <summary>
    /// Gets all plugin types from an assembly
    /// </summary>
    /// <param name="assembly">The assembly to scan</param>
    /// <returns>Enumerable of plugin types found in the assembly</returns>
    public static IEnumerable<Type> GetPluginTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes().Where(IsValidPluginType);
        }
        catch (ReflectionTypeLoadException ex)
        {
            // Return only the types that loaded successfully
            return ex.Types.Where(t => t != null && IsValidPluginType(t))!;
        }
    }
}