using ApBox.Core.Models;
using ApBox.Core.Services.Configuration;
using ApBox.Core.Services.Plugins;
using ApBox.Plugins;

namespace ApBox.Core.Services.Core;

/// <summary>
/// Service that processes PIN reads through configured plugins
/// </summary>
public class PinProcessingService : IPinProcessingService
{
    private readonly IPluginLoader _pluginLoader;
    private readonly IReaderPluginMappingService _mappingService;
    private readonly IFeedbackConfigurationService _feedbackService;
    private readonly ILogger<PinProcessingService> _logger;

    public PinProcessingService(
        IPluginLoader pluginLoader,
        IReaderPluginMappingService mappingService,
        IFeedbackConfigurationService feedbackService,
        ILogger<PinProcessingService> logger)
    {
        _pluginLoader = pluginLoader;
        _mappingService = mappingService;
        _feedbackService = feedbackService;
        _logger = logger;
    }

    public async Task<PinReadResult> ProcessPinReadAsync(PinReadEvent pinRead)
    {
        _logger.LogInformation("Processing PIN read from reader {ReaderName}: {PinLength} digits", 
            pinRead.ReaderName, pinRead.Pin.Length);

        var result = new PinReadResult
        {
            Success = false,
            Message = "No plugins processed the PIN",
            PluginResults = new Dictionary<string, PluginResult>()
        };

        try
        {
            // Get plugins configured for this reader
            var readerPluginIds = await _mappingService.GetPluginsForReaderAsync(pinRead.ReaderId);
            if (!readerPluginIds.Any())
            {
                _logger.LogWarning("No plugins configured for reader {ReaderId}", pinRead.ReaderId);
                result.Message = "No plugins configured for this reader";
                return result;
            }

            // Load plugins
            var allPlugins = await _pluginLoader.LoadPluginsAsync();
            var configuredPlugins = allPlugins.Where(p => readerPluginIds.Contains(p.Id.ToString())).ToList();

            if (!configuredPlugins.Any())
            {
                _logger.LogWarning("No valid plugins found for reader {ReaderId}", pinRead.ReaderId);
                result.Message = "No valid plugins found";
                return result;
            }

            _logger.LogInformation("Processing PIN through {Count} plugins for reader {ReaderName}", 
                configuredPlugins.Count, pinRead.ReaderName);

            // Process PIN through each plugin
            bool anySuccess = false;
            var pluginResults = new List<string>();

            foreach (var plugin in configuredPlugins)
            {
                try
                {
                    var pluginResult = await plugin.ProcessPinReadAsync(pinRead);
                    
                    result.PluginResults[plugin.Name] = new PluginResult
                    {
                        PluginId = plugin.Id,
                        PluginName = plugin.Name,
                        Success = pluginResult,
                        ProcessedAt = DateTime.UtcNow,
                        ErrorMessage = pluginResult ? null : "Plugin denied access"
                    };

                    if (pluginResult)
                    {
                        anySuccess = true;
                        pluginResults.Add($"✓{plugin.Name}");
                        _logger.LogInformation("Plugin {PluginName} approved PIN access for reader {ReaderName}", 
                            plugin.Name, pinRead.ReaderName);
                    }
                    else
                    {
                        pluginResults.Add($"✗{plugin.Name}");
                        _logger.LogInformation("Plugin {PluginName} denied PIN access for reader {ReaderName}", 
                            plugin.Name, pinRead.ReaderName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing PIN in plugin {PluginName} for reader {ReaderName}", 
                        plugin.Name, pinRead.ReaderName);
                    
                    result.PluginResults[plugin.Name] = new PluginResult
                    {
                        PluginId = plugin.Id,
                        PluginName = plugin.Name,
                        Success = false,
                        ProcessedAt = DateTime.UtcNow,
                        ErrorMessage = ex.Message
                    };
                    
                    pluginResults.Add($"✗{plugin.Name}");
                }
            }

            result.Success = anySuccess;
            result.Message = anySuccess ? "PIN access granted" : "PIN access denied";
            result.AdditionalData["ProcessedByPlugins"] = string.Join(",", pluginResults);

            _logger.LogInformation("PIN processing completed for reader {ReaderName}: Success={Success}, Plugins={PluginCount}", 
                pinRead.ReaderName, result.Success, configuredPlugins.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PIN read for reader {ReaderName}", pinRead.ReaderName);
            
            result.Success = false;
            result.Message = "PIN processing error occurred";
            result.AdditionalData["Error"] = ex.Message;
            
            return result;
        }
    }

}