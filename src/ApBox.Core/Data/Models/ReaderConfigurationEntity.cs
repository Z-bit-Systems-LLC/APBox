using ApBox.Plugins;

namespace ApBox.Core.Data.Models;

public class ReaderConfigurationEntity
{
    public string ReaderId { get; set; } = string.Empty;
    public string ReaderName { get; set; } = string.Empty;
    public string? DefaultFeedbackJson { get; set; }
    public string? ResultFeedbackJson { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public ReaderConfiguration ToReaderConfiguration()
    {
        var config = new ReaderConfiguration
        {
            ReaderId = Guid.Parse(ReaderId),
            ReaderName = ReaderName
        };
        
        if (!string.IsNullOrEmpty(DefaultFeedbackJson))
        {
            config.DefaultFeedback = System.Text.Json.JsonSerializer.Deserialize<ReaderFeedbackConfiguration>(DefaultFeedbackJson);
        }
        
        if (!string.IsNullOrEmpty(ResultFeedbackJson))
        {
            config.ResultFeedback = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, ReaderFeedbackConfiguration>>(ResultFeedbackJson) ?? new();
        }
        
        return config;
    }
    
    public static ReaderConfigurationEntity FromReaderConfiguration(ReaderConfiguration config)
    {
        return new ReaderConfigurationEntity
        {
            ReaderId = config.ReaderId.ToString(),
            ReaderName = config.ReaderName,
            DefaultFeedbackJson = config.DefaultFeedback != null ? System.Text.Json.JsonSerializer.Serialize(config.DefaultFeedback) : null,
            ResultFeedbackJson = config.ResultFeedback.Any() ? System.Text.Json.JsonSerializer.Serialize(config.ResultFeedback) : null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}