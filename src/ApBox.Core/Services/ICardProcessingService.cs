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
    private readonly IFeedbackResolutionService _feedbackService;
    private readonly ILogger<CardProcessingService> _logger;
    
    public CardProcessingService(
        IPluginLoader pluginLoader,
        IFeedbackResolutionService feedbackService,
        ILogger<CardProcessingService> logger)
    {
        _pluginLoader = pluginLoader;
        _feedbackService = feedbackService;
        _logger = logger;
    }
    
    public async Task<CardReadResult> ProcessCardReadAsync(CardReadEvent cardRead)
    {
        _logger.LogInformation("Processing card read for reader {ReaderId}, card {CardNumber}", 
            cardRead.ReaderId, cardRead.CardNumber);
        
        try
        {
            var plugins = await _pluginLoader.LoadPluginsAsync();
            var results = new List<bool>();
            var processedByPlugins = new List<string>();
            
            foreach (var plugin in plugins)
            {
                try
                {
                    var result = await plugin.ProcessCardReadAsync(cardRead);
                    results.Add(result);
                    processedByPlugins.Add(plugin.Name);
                    
                    _logger.LogDebug("Plugin {PluginName} processed card with result: {Result}", 
                        plugin.Name, result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing card read with plugin {PluginName}", plugin.Name);
                    results.Add(false);
                }
            }
            
            // All plugins must approve for success
            var success = results.Count > 0 && results.All(r => r);
            
            return new CardReadResult
            {
                Success = success,
                Message = success ? "Card read processed successfully" : "Card read failed processing",
                ProcessedByPlugin = processedByPlugins.Any() ? string.Join(", ", processedByPlugins) : null
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
            // For now, we'll use the first available plugin for feedback
            // In a real implementation, you might want to use a specific plugin or configuration
            var plugins = await _pluginLoader.LoadPluginsAsync();
            var plugin = plugins.FirstOrDefault();
            
            return await _feedbackService.ResolveFeedbackAsync(readerId, result, plugin);
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