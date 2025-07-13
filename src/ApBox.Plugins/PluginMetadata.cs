namespace ApBox.Plugins;

public class PluginMetadata
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string AssemblyPath { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public Dictionary<string, object> Configuration { get; set; } = new();
}