using ApBox.Plugins;

namespace ApBox.Core.Services;

public interface IReaderConfigurationService
{
    Task<IEnumerable<ReaderConfiguration>> GetAllReadersAsync();
    Task<ReaderConfiguration?> GetReaderAsync(Guid readerId);
    Task SaveReaderAsync(ReaderConfiguration reader);
    Task DeleteReaderAsync(Guid readerId);
}

public class ReaderConfigurationService : IReaderConfigurationService
{
    private readonly ILogger<ReaderConfigurationService> _logger;
    private readonly List<ReaderConfiguration> _readers = new();
    
    public ReaderConfigurationService(ILogger<ReaderConfigurationService> logger)
    {
        _logger = logger;
        
        // Initialize with some default readers for testing
        InitializeDefaultReaders();
    }
    
    public Task<IEnumerable<ReaderConfiguration>> GetAllReadersAsync()
    {
        return Task.FromResult<IEnumerable<ReaderConfiguration>>(_readers.AsReadOnly());
    }
    
    public Task<ReaderConfiguration?> GetReaderAsync(Guid readerId)
    {
        var reader = _readers.FirstOrDefault(r => r.ReaderId == readerId);
        return Task.FromResult(reader);
    }
    
    public Task SaveReaderAsync(ReaderConfiguration reader)
    {
        var existingIndex = _readers.FindIndex(r => r.ReaderId == reader.ReaderId);
        
        if (existingIndex >= 0)
        {
            _readers[existingIndex] = reader;
            _logger.LogInformation("Updated reader configuration for {ReaderId}", reader.ReaderId);
        }
        else
        {
            _readers.Add(reader);
            _logger.LogInformation("Added new reader configuration for {ReaderId}", reader.ReaderId);
        }
        
        return Task.CompletedTask;
    }
    
    public Task DeleteReaderAsync(Guid readerId)
    {
        var removed = _readers.RemoveAll(r => r.ReaderId == readerId);
        
        if (removed > 0)
        {
            _logger.LogInformation("Deleted reader configuration for {ReaderId}", readerId);
        }
        
        return Task.CompletedTask;
    }
    
    private void InitializeDefaultReaders()
    {
        _readers.AddRange(new[]
        {
            new ReaderConfiguration
            {
                ReaderId = Guid.Parse("12345678-1234-1234-1234-123456789abc"),
                ReaderName = "Main Entrance",
                DefaultFeedback = new ReaderFeedbackConfiguration
                {
                    Type = ReaderFeedbackType.Success,
                    BeepCount = 1,
                    LedColor = LedColor.Green,
                    LedDurationMs = 1000
                },
                ResultFeedback = new Dictionary<string, ReaderFeedbackConfiguration>
                {
                    ["AccessDenied"] = new ReaderFeedbackConfiguration
                    {
                        Type = ReaderFeedbackType.Failure,
                        BeepCount = 3,
                        LedColor = LedColor.Red,
                        LedDurationMs = 2000
                    }
                }
            },
            new ReaderConfiguration
            {
                ReaderId = Guid.Parse("87654321-4321-4321-4321-cba987654321"),
                ReaderName = "Loading Dock",
                DefaultFeedback = new ReaderFeedbackConfiguration
                {
                    Type = ReaderFeedbackType.Success,
                    BeepCount = 2,
                    LedColor = LedColor.Amber,
                    LedDurationMs = 1500
                }
            }
        });
        
        _logger.LogInformation("Initialized {Count} default reader configurations", _readers.Count);
    }
}