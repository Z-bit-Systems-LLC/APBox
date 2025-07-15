using ApBox.Core.Models;

namespace ApBox.Core.Data.Models;

public class ReaderConfigurationEntity
{
    public string ReaderId { get; set; } = string.Empty;
    public string ReaderName { get; set; } = string.Empty;
    public byte Address { get; set; } = 1;
    public bool IsEnabled { get; set; } = true;
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
            ReaderName = ReaderName,
            Address = Address,
            IsEnabled = IsEnabled,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt
        };
    }
    
    public static ReaderConfigurationEntity FromReaderConfiguration(ReaderConfiguration config)
    {
        return new ReaderConfigurationEntity
        {
            ReaderId = config.ReaderId.ToString(),
            ReaderName = config.ReaderName,
            Address = config.Address,
            IsEnabled = config.IsEnabled,
            CreatedAt = config.CreatedAt,
            UpdatedAt = config.UpdatedAt
        };
    }
}