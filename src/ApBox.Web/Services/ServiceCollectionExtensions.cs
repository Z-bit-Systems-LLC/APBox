using ApBox.Plugins;
using ApBox.Core.OSDP;
using ApBox.Core.Services;
using ApBox.Core.Data;
using Microsoft.Extensions.Logging;

namespace ApBox.Web.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApBoxServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register plugin system services
        services.AddSingleton<IPluginLoader>(provider =>
        {
            var pluginDirectory = configuration.GetValue<string>("PluginSettings:Directory") ?? "plugins";
            var logger = provider.GetService<ILogger<PluginLoader>>();
            return new PluginLoader(pluginDirectory, logger);
        });
        
        
        // Register OSDP services
        services.AddSingleton<IOsdpCommunicationManager, OsdpCommunicationManager>();
        services.AddHostedService<OsdpStartupService>();
        services.AddHostedService<OsdpStatusBridgeService>();
        
        // Register core application services
        services.AddScoped<ICardProcessingService, CardProcessingService>();
        services.AddScoped<IEnhancedCardProcessingService, EnhancedCardProcessingService>();
        services.AddScoped<IReaderService, ReaderService>();
        
        // Register SignalR notification service
        services.AddSingleton<ICardEventNotificationService, CardEventNotificationService>();
        
        // Register system management services
        services.AddScoped<IConfigurationExportService, ConfigurationExportService>();
        services.AddScoped<ISystemRestartService, SystemRestartService>();
        services.AddSingleton<ILogService, LogService>();
        
        // Register database services
        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=apbox.db";
        services.AddApBoxDatabase(connectionString);
        
        return services;
    }
}