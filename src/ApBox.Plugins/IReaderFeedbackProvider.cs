namespace ApBox.Plugins;

public interface IReaderFeedbackProvider
{
    Task<ReaderFeedback?> GetFeedbackAsync(Guid readerId, CardReadResult result);
    int Priority { get; }
}

public class PluginFeedbackProvider : IReaderFeedbackProvider
{
    private readonly IApBoxPlugin _plugin;
    
    public PluginFeedbackProvider(IApBoxPlugin plugin)
    {
        _plugin = plugin;
    }
    
    public int Priority => 100;
    
    public async Task<ReaderFeedback?> GetFeedbackAsync(Guid readerId, CardReadResult result)
    {
        return await _plugin.GetFeedbackAsync(result);
    }
}

public class ConfigurationFeedbackProvider : IReaderFeedbackProvider
{
    public int Priority => 50;
    
    public Task<ReaderFeedback?> GetFeedbackAsync(Guid readerId, CardReadResult result)
    {
        // This will be implemented to read from local configuration
        // For now, return null to indicate no configured feedback
        return Task.FromResult<ReaderFeedback?>(null);
    }
}