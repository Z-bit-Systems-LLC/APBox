using ApBox.Plugins;

namespace ApBox.Core.Data.Models;

public class ReaderConfigurationEntity
{
    public string ReaderId { get; set; } = string.Empty;
    public string ReaderName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public ReaderConfiguration ToReaderConfiguration()
    {
        if (!Guid.TryParse(ReaderId, out var readerId))
        {
            throw new InvalidOperationException($"Invalid GUID format for ReaderId: '{ReaderId}'");
        }
        
        return new ReaderConfiguration
        {
            ReaderId = readerId,
            ReaderName = ReaderName
        };
    }
    
    public static ReaderConfigurationEntity FromReaderConfiguration(ReaderConfiguration config)
    {
        return new ReaderConfigurationEntity
        {
            ReaderId = config.ReaderId.ToString(),
            ReaderName = config.ReaderName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}