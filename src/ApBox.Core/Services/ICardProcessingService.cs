using ApBox.Core.Models;
using ApBox.Plugins;

namespace ApBox.Core.Services;

public interface ICardProcessingService
{
    Task<CardReadResult> ProcessCardReadAsync(CardReadEvent cardRead);
    Task<ReaderFeedback> GetFeedbackAsync(Guid readerId, CardReadResult result);
}

public class CardProcessingService : ICardProcessingService
{
    private readonly IPluginLoader _pluginLoader;
    private readonly IFeedbackConfigurationService _feedbackConfigurationService;
    private readonly ILogger<CardProcessingService> _logger;
    
    public CardProcessingService(
        IPluginLoader pluginLoader,
        IFeedbackConfigurationService feedbackConfigurationService,
        ILogger<CardProcessingService> logger)
    {
        _pluginLoader = pluginLoader;
        _feedbackConfigurationService = feedbackConfigurationService;
        _logger = logger;
    }
    
    public async Task<CardReadResult> ProcessCardReadAsync(CardReadEvent cardRead)
    {
        _logger.LogInformation("Processing card read for reader {ReaderId}, card {CardNumber}", 
            cardRead.ReaderId, cardRead.CardNumber);
        
        try
        {
            var plugins = await _pluginLoader.LoadPluginsAsync();
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
    
    public async Task<ReaderFeedback> GetFeedbackAsync(Guid readerId, CardReadResult result)
    {
        try
        {
            // Get appropriate feedback based on result
            if (result.Success)
            {
                return await _feedbackConfigurationService.GetSuccessFeedbackAsync();
            }
            else
            {
                return await _feedbackConfigurationService.GetFailureFeedbackAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving feedback for reader {ReaderId}", readerId);
            return new ReaderFeedback
            {
                Type = result.Success ? ReaderFeedbackType.Success : ReaderFeedbackType.Failure
            };
        }
    }
}