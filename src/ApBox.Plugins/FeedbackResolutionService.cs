namespace ApBox.Plugins;

public interface IFeedbackResolutionService
{
    Task<ReaderFeedback> ResolveFeedbackAsync(Guid readerId, CardReadResult result, IApBoxPlugin? plugin = null);
}

public class FeedbackResolutionService : IFeedbackResolutionService
{
    private readonly List<IReaderFeedbackProvider> _providers = new();
    
    public FeedbackResolutionService()
    {
        // Configuration provider is always available
        _providers.Add(new ConfigurationFeedbackProvider());
    }
    
    public async Task<ReaderFeedback> ResolveFeedbackAsync(Guid readerId, CardReadResult result, IApBoxPlugin? plugin = null)
    {
        var providers = new List<IReaderFeedbackProvider>(_providers);
        
        // Add plugin provider if plugin is provided
        if (plugin != null)
        {
            providers.Add(new PluginFeedbackProvider(plugin));
        }
        
        // Sort by priority (higher priority first)
        providers.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        
        // Try each provider in priority order
        foreach (var provider in providers)
        {
            var feedback = await provider.GetFeedbackAsync(readerId, result);
            if (feedback != null)
            {
                return feedback;
            }
        }
        
        // Return default feedback if no provider returns feedback
        return new ReaderFeedback
        {
            Type = result.Success ? ReaderFeedbackType.Success : ReaderFeedbackType.Failure
        };
    }
}