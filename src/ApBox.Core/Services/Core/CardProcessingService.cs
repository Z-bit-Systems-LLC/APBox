using ApBox.Core.Models;
using ApBox.Core.Services.Configuration;
using ApBox.Core.Services.Plugins;
using ApBox.Plugins;

namespace ApBox.Core.Services.Core;

public class CardProcessingService : ICardProcessingService
{
    private readonly IPluginLoader _pluginLoader;
    private readonly IFeedbackConfigurationService _feedbackConfigurationService;
    private readonly IReaderPluginMappingService _readerPluginMappingService;
    private readonly ILogger<CardProcessingService> _logger;
    
    public CardProcessingService(
        IPluginLoader pluginLoader,
        IFeedbackConfigurationService feedbackConfigurationService,
        IReaderPluginMappingService readerPluginMappingService,
        ILogger<CardProcessingService> logger)
    {
        _pluginLoader = pluginLoader;
        _feedbackConfigurationService = feedbackConfigurationService;
        _readerPluginMappingService = readerPluginMappingService;
        _logger = logger;
    }
    
    public async Task<CardReadResult> ProcessCardReadAsync(CardReadEvent cardRead)
    {
        _logger.LogInformation("Processing card read for reader {ReaderId}, card {CardNumber}", 
            cardRead.ReaderId, cardRead.CardNumber);
        
        try
        {
            // Get plugins assigned to this reader
            var readerPluginIds = await _readerPluginMappingService.GetPluginsForReaderAsync(cardRead.ReaderId);
            var allPlugins = await _pluginLoader.LoadPluginsAsync();
            
            // Filter to only plugins configured for this reader
            var plugins = allPlugins.Where(p => readerPluginIds.Contains(p.Id.ToString())).ToList();
            
            _logger.LogDebug("Reader {ReaderId} has {ConfiguredCount} plugins configured out of {TotalCount} available", 
                cardRead.ReaderId, plugins.Count, allPlugins.Count());
            
            var results = new List<(string PluginName, bool Success)>();
            var pluginResults = new List<PluginResult>();
            
            foreach (var plugin in plugins)
            {
                try
                {
                    var result = await plugin.ProcessCardReadAsync(cardRead);
                    results.Add((plugin.Name, result));
                    
                    pluginResults.Add(new PluginResult
                    {
                        PluginName = plugin.Name,
                        PluginId = plugin.Id,
                        Success = result,
                        ErrorMessage = result ? null : "Plugin denied access"
                    });
                    
                    if (result)
                    {
                        _logger.LogDebug("Plugin {PluginName} approved card {CardNumber}", 
                            plugin.Name, cardRead.CardNumber);
                    }
                    else
                    {
                        _logger.LogWarning("Plugin {PluginName} denied card {CardNumber}", 
                            plugin.Name, cardRead.CardNumber);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Plugin {PluginName} failed with exception while processing card {CardNumber}", 
                        plugin.Name, cardRead.CardNumber);
                    results.Add((plugin.Name, false));
                    
                    pluginResults.Add(new PluginResult
                    {
                        PluginName = plugin.Name,
                        PluginId = plugin.Id,
                        Success = false,
                        ErrorMessage = ex.Message
                    });
                }
            }
            
            // All plugins must approve for success
            var success = results.Count > 0 && results.All(r => r.Success);
            
            // Log detailed results
            if (!success && results.Any())
            {
                var failedPlugins = results.Where(r => !r.Success).Select(r => r.PluginName).ToList();
                _logger.LogWarning("Card {CardNumber} processing failed. Failed plugins: {FailedPlugins}", 
                    cardRead.CardNumber, string.Join(", ", failedPlugins));
            }
            else if (success)
            {
                _logger.LogInformation("Card {CardNumber} processing succeeded. All {PluginCount} plugins approved.", 
                    cardRead.CardNumber, results.Count);
            }
            else
            {
                _logger.LogWarning("Card {CardNumber} processing failed. No plugins were loaded.", 
                    cardRead.CardNumber);
            }
            
            // Create plugin result collection and convert to storage format
            var pluginResultCollection = new PluginResultCollection();
            foreach (var pluginResult in pluginResults)
            {
                if (pluginResult.Success)
                    pluginResultCollection.SuccessfulPlugins.Add(pluginResult);
                else
                    pluginResultCollection.FailedPlugins.Add(pluginResult);
            }
            
            return new CardReadResult
            {
                Success = success,
                Message = success ? "Card read processed successfully" : "Card read failed processing",
                ProcessedByPlugin = pluginResultCollection.HasAnyResults ? pluginResultCollection.ToStorageString() : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing card read for reader {ReaderId}", cardRead.ReaderId);
            return new CardReadResult
            {
                Success = false,
                Message = "Internal processing error"
            };
        }
    }
    
}