using System.Text.Json;
using ApBox.Core.Models;
using ApBox.Core.Services.Reader;

namespace ApBox.Core.Services.Configuration;

/// <summary>
/// Implementation of configuration export/import service
/// </summary>
public class ConfigurationExportService(
    IReaderConfigurationService readerConfigurationService,
    IFeedbackConfigurationService feedbackConfigurationService,
    IReaderPluginMappingService readerPluginMappingService,
    ILogger<ConfigurationExportService> logger) : IConfigurationExportService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<ConfigurationExport> ExportConfigurationAsync()
    {
        logger.LogInformation("Starting configuration export");
        
        try
        {
            var export = new ConfigurationExport();
            
            // Export readers
            var readers = await readerConfigurationService.GetAllReadersAsync();
            export.Readers = readers.ToList();
            
            // Export plugin mappings for each reader
            foreach (var reader in export.Readers)
            {
                var mappings = await readerPluginMappingService.GetAllMappingsAsync();
                var readerMappings = mappings
                    .Where(m => m.ReaderId == reader.ReaderId)
                    .OrderBy(m => m.ExecutionOrder)
                    .Select(m => new ReaderPluginMapping
                    {
                        PluginId = m.PluginId,
                        ExecutionOrder = m.ExecutionOrder,
                        IsEnabled = m.IsEnabled
                    })
                    .ToList();
                
                reader.PluginMappings = readerMappings;
            }
            
            // Export feedback configuration
            export.FeedbackConfiguration = await feedbackConfigurationService.GetDefaultConfigurationAsync();
            
            logger.LogInformation("Configuration export completed successfully. Exported {ReaderCount} readers", 
                export.Readers.Count);
            
            return export;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to export configuration");
            throw;
        }
    }

    public async Task<string> ExportToJsonAsync()
    {
        var config = await ExportConfigurationAsync();
        return JsonSerializer.Serialize(config, JsonOptions);
    }

    public async Task<byte[]> ExportToFileAsync()
    {
        var json = await ExportToJsonAsync();
        return System.Text.Encoding.UTF8.GetBytes(json);
    }

    public Task<ValidationResult> ValidateImportAsync(string jsonContent)
    {
        var result = new ValidationResult { IsValid = true };
        
        try
        {
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                result.AddError("Import content cannot be empty");
                result.IsValid = false;
                return Task.FromResult(result);
            }

            var config = JsonSerializer.Deserialize<ConfigurationExport>(jsonContent, JsonOptions);
            if (config == null)
            {
                result.AddError("Invalid JSON format");
                result.IsValid = false;
                return Task.FromResult(result);
            }

            // Validate export version
            if (string.IsNullOrEmpty(config.ExportVersion))
            {
                result.AddWarning("Export version not specified, assuming version 1.0");
            }
            else if (config.ExportVersion != "1.0")
            {
                result.AddWarning($"Import from version {config.ExportVersion} may not be fully compatible");
            }

            // Validate readers
            if (config.Readers.Any())
            {
                var duplicateNames = config.Readers
                    .GroupBy(r => r.ReaderName)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key);
                
                foreach (var name in duplicateNames)
                {
                    result.AddError($"Duplicate reader name: {name}");
                    result.IsValid = false;
                }

                var duplicateAddresses = config.Readers
                    .GroupBy(r => r.Address)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key);
                
                foreach (var address in duplicateAddresses)
                {
                    result.AddError($"Duplicate reader address: {address}");
                    result.IsValid = false;
                }

                // Check for invalid names
                foreach (var reader in config.Readers)
                {
                    if (string.IsNullOrWhiteSpace(reader.ReaderName))
                    {
                        result.AddError("Reader name cannot be empty");
                        result.IsValid = false;
                    }
                    else if (reader.ReaderName.Length > 100)
                    {
                        result.AddError($"Reader name too long: {reader.ReaderName}");
                        result.IsValid = false;
                    }
                }
                
                // Validate plugin mappings
                ValidatePluginMappings(config.Readers, result);
            }

            // Validate feedback configuration
            if (config.FeedbackConfiguration != null)
            {
                ValidateFeedbackConfiguration(config.FeedbackConfiguration, result);
            }

            logger.LogInformation("Configuration validation completed. Valid: {IsValid}, Errors: {ErrorCount}, Warnings: {WarningCount}",
                result.IsValid, result.Errors.Count, result.Warnings.Count);
        }
        catch (JsonException ex)
        {
            result.AddError($"Invalid JSON format: {ex.Message}");
            result.IsValid = false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating import configuration");
            result.AddError($"Validation error: {ex.Message}");
            result.IsValid = false;
        }

        return Task.FromResult(result);
    }

    public async Task ImportConfigurationAsync(ConfigurationExport config)
    {
        logger.LogInformation("Starting configuration import");
        
        try
        {
            // Import feedback configuration first (less likely to cause conflicts)
            if (config.FeedbackConfiguration != null)
            {
                await feedbackConfigurationService.SaveDefaultConfigurationAsync(config.FeedbackConfiguration);
                logger.LogInformation("Imported feedback configuration");
            }

            // Import readers
            if (config.Readers.Any())
            {
                foreach (var reader in config.Readers)
                {
                    // Check if reader already exists
                    var existingReader = await readerConfigurationService.GetReaderAsync(reader.ReaderId);
                    if (existingReader != null)
                    {
                        await readerConfigurationService.SaveReaderAsync(reader);
                        logger.LogInformation("Updated existing reader: {ReaderName}", reader.ReaderName);
                    }
                    else
                    {
                        await readerConfigurationService.SaveReaderAsync(reader);
                        logger.LogInformation("Added new reader: {ReaderName}", reader.ReaderName);
                    }
                    
                    // Import plugin mappings for this reader
                    if (reader.PluginMappings.Any())
                    {
                        await readerPluginMappingService.SetPluginsForReaderAsync(
                            reader.ReaderId, 
                            reader.PluginMappings.Select(pm => pm.PluginId));
                        
                        // Update execution order and enabled state for each plugin
                        foreach (var pluginMapping in reader.PluginMappings)
                        {
                            await readerPluginMappingService.UpdatePluginOrderAsync(
                                reader.ReaderId, 
                                pluginMapping.PluginId, 
                                pluginMapping.ExecutionOrder);
                            
                            if (pluginMapping.IsEnabled)
                            {
                                await readerPluginMappingService.EnablePluginForReaderAsync(
                                    reader.ReaderId, 
                                    pluginMapping.PluginId);
                            }
                            else
                            {
                                await readerPluginMappingService.DisablePluginForReaderAsync(
                                    reader.ReaderId, 
                                    pluginMapping.PluginId);
                            }
                        }
                        
                        logger.LogInformation("Imported {PluginCount} plugin mappings for reader: {ReaderName}", 
                            reader.PluginMappings.Count, reader.ReaderName);
                    }
                }
            }

            logger.LogInformation("Configuration import completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to import configuration");
            throw;
        }
    }

    public async Task ImportFromJsonAsync(string jsonContent)
    {
        var validation = await ValidateImportAsync(jsonContent);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException($"Import validation failed: {string.Join(", ", validation.Errors)}");
        }

        var config = JsonSerializer.Deserialize<ConfigurationExport>(jsonContent, JsonOptions)!;
        await ImportConfigurationAsync(config);
    }

    private static void ValidateFeedbackConfiguration(FeedbackConfiguration config, ValidationResult result)
    {
        // Validate success feedback
        if (config.SuccessFeedback != null)
        {
            if (config.SuccessFeedback.LedDuration <= 0)
            {
                result.AddWarning("Success feedback LED duration should be positive");
            }
            if (config.SuccessFeedback.BeepCount < 0)
            {
                result.AddError("Success feedback beep count cannot be negative");
                result.IsValid = false;
            }
        }

        // Validate failure feedback
        if (config.FailureFeedback != null)
        {
            if (config.FailureFeedback.LedDuration <= 0)
            {
                result.AddWarning("Failure feedback LED duration should be positive");
            }
            if (config.FailureFeedback.BeepCount < 0)
            {
                result.AddError("Failure feedback beep count cannot be negative");
                result.IsValid = false;
            }
        }
    }
    
    private static void ValidatePluginMappings(List<ReaderConfiguration> readers, ValidationResult result)
    {
        foreach (var reader in readers)
        {
            if (!reader.PluginMappings.Any()) continue;
            
            // Check for duplicate execution orders within the same reader
            var duplicateOrders = reader.PluginMappings
                .GroupBy(pm => pm.ExecutionOrder)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);
            
            foreach (var order in duplicateOrders)
            {
                result.AddError($"Reader '{reader.ReaderName}' has duplicate plugin execution order: {order}");
                result.IsValid = false;
            }
            
            // Check for duplicate plugin IDs within the same reader
            var duplicatePlugins = reader.PluginMappings
                .GroupBy(pm => pm.PluginId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);
            
            foreach (var pluginId in duplicatePlugins)
            {
                result.AddError($"Reader '{reader.ReaderName}' has duplicate plugin assignment: {pluginId}");
                result.IsValid = false;
            }
            
            // Check for invalid plugin IDs and execution orders
            foreach (var pluginMapping in reader.PluginMappings)
            {
                if (string.IsNullOrWhiteSpace(pluginMapping.PluginId))
                {
                    result.AddError($"Reader '{reader.ReaderName}' has empty plugin ID");
                    result.IsValid = false;
                }
                
                if (pluginMapping.ExecutionOrder <= 0)
                {
                    result.AddError($"Reader '{reader.ReaderName}' plugin '{pluginMapping.PluginId}' has invalid execution order: {pluginMapping.ExecutionOrder}");
                    result.IsValid = false;
                }
            }
        }
    }
}