namespace ApBox.Core.Data.Models;

public class PluginConfigurationEntity
{
    public long Id { get; set; }
    public string PluginName { get; set; } = string.Empty;
    public string ConfigurationKey { get; set; } = string.Empty;
    public string ConfigurationValue { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}