namespace ApBox.Core.Data.Repositories;

public interface IPluginConfigurationRepository
{
    Task<string?> GetConfigurationAsync(string pluginName, string key);
    Task SetConfigurationAsync(string pluginName, string key, string value);
    Task<Dictionary<string, string>> GetAllConfigurationAsync(string pluginName);
    Task<bool> DeleteConfigurationAsync(string pluginName, string key);
    Task<bool> DeleteAllConfigurationAsync(string pluginName);
}