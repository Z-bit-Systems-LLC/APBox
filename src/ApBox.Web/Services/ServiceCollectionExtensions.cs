using ApBox.Plugins;
using ApBox.Core.OSDP;
using ApBox.Core.Services;
using ApBox.Core.Data;

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
        services.AddHostedService<CardProcessingBridgeService>();
        
        // Register core application services
        services.AddSingleton<ICardProcessingService, CardProcessingService>();
        services.AddSingleton<IEnhancedCardProcessingService, EnhancedCardProcessingService>();
        services.AddSingleton<IReaderService, ReaderService>();
        
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