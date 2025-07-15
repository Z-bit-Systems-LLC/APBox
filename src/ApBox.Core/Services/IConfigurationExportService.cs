using ApBox.Core.Models;

namespace ApBox.Core.Services;

/// <summary>
/// Service for exporting and importing system configuration
/// </summary>
public interface IConfigurationExportService
{
    /// <summary>
    /// Export the complete system configuration
    /// </summary>
    Task<ConfigurationExport> ExportConfigurationAsync();
    
    /// <summary>
    /// Export configuration as JSON string
    /// </summary>
    Task<string> ExportToJsonAsync();
    
    /// <summary>
    /// Export configuration as downloadable file bytes
    /// </summary>
    Task<byte[]> ExportToFileAsync();
    
    /// <summary>
    /// Validate imported configuration JSON
    /// </summary>
    Task<ValidationResult> ValidateImportAsync(string jsonContent);
    
    /// <summary>
    /// Import configuration from parsed object
    /// </summary>
    Task ImportConfigurationAsync(ConfigurationExport config);
    
    /// <summary>
    /// Import configuration from JSON string
    /// </summary>
    Task ImportFromJsonAsync(string jsonContent);
}