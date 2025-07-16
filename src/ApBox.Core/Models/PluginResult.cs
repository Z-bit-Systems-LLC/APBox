namespace ApBox.Core.Models;

/// <summary>
/// Represents the result of a single plugin processing a card read
/// </summary>
public class PluginResult
{
    public string PluginName { get; set; } = string.Empty;
    public Guid PluginId { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Collection of plugin results for display purposes
/// </summary>
public class PluginResultCollection
{
    public List<PluginResult> SuccessfulPlugins { get; set; } = [];
    public List<PluginResult> FailedPlugins { get; set; } = [];
    
    public bool HasAnyResults => SuccessfulPlugins.Any() || FailedPlugins.Any();
    public bool AllSucceeded => HasAnyResults && !FailedPlugins.Any();
    public int TotalPlugins => SuccessfulPlugins.Count + FailedPlugins.Count;
    
    /// <summary>
    /// Parse from comma-separated plugin results string
    /// Format: "PluginName:Success:ErrorMessage|PluginName:Failed:Error"
    /// </summary>
    public static PluginResultCollection Parse(string? pluginResultsString)
    {
        var collection = new PluginResultCollection();
        
        if (string.IsNullOrWhiteSpace(pluginResultsString))
            return collection;
            
        var pluginEntries = pluginResultsString.Split('|', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var entry in pluginEntries)
        {
            var parts = entry.Split(':', StringSplitOptions.None);
            if (parts.Length >= 2)
            {
                var result = new PluginResult
                {
                    PluginName = parts[0].Trim(),
                    Success = parts[1].Trim().Equals("Success", StringComparison.OrdinalIgnoreCase),
                    ErrorMessage = parts.Length > 2 ? parts[2].Trim() : null
                };
                
                if (result.Success)
                    collection.SuccessfulPlugins.Add(result);
                else
                    collection.FailedPlugins.Add(result);
            }
        }
        
        return collection;
    }
    
    /// <summary>
    /// Convert to string format for storage
    /// </summary>
    public string ToStorageString()
    {
        var entries = new List<string>();
        
        foreach (var plugin in SuccessfulPlugins)
        {
            entries.Add($"{plugin.PluginName}:Success:{plugin.ErrorMessage ?? ""}");
        }
        
        foreach (var plugin in FailedPlugins)
        {
            entries.Add($"{plugin.PluginName}:Failed:{plugin.ErrorMessage ?? ""}");
        }
        
        return string.Join("|", entries);
    }
}