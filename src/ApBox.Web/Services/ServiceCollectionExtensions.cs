using ApBox.Core.OSDP;
using ApBox.Core.Services;
using ApBox.Core.Services.Core;
using ApBox.Core.Services.Reader;
using ApBox.Core.Services.Configuration;
using ApBox.Core.Services.Persistence;
using ApBox.Core.Services.Infrastructure;
using ApBox.Core.Services.Plugins;
using ApBox.Core.Services.Events;
using ApBox.Core.Data;
using ApBox.Core.PacketTracing.Services;
using ApBox.Plugins;
using ApBox.Web.ViewModels;
using ApBox.Web.Services.Notifications;

namespace ApBox.Web.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApBoxServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register caching service
        services.AddSingleton<ICacheService, MemoryCacheService>();
        
        // Register plugin system services with enhanced caching
        services.AddSingleton<IPluginLoader>(provider =>
        {
            var pluginDirectory = configuration.GetValue<string>("PluginSettings:Directory") ?? "plugins";
            var logger = provider.GetService<ILogger<CachedPluginLoader>>();
            var loggerFactory = provider.GetService<ILoggerFactory>();
            return new CachedPluginLoader(pluginDirectory, logger, loggerFactory);
        });
        
        // Register OSDP services
        services.AddSingleton<IOsdpCommunicationManager, OsdpCommunicationManager>();
        services.AddHostedService<OsdpStartupService>();
        
        // Register unified notification service (combines server-side and SignalR notifications)
        services.AddSingleton<UnifiedNotificationService>();
        services.AddSingleton<INotificationAggregator>(provider => 
            provider.GetRequiredService<UnifiedNotificationService>());
        services.AddHostedService<UnifiedNotificationService>(provider => 
            provider.GetRequiredService<UnifiedNotificationService>());
        
        
        // Register packet tracing services
        services.AddSingleton<IPacketTraceService>(provider =>
        {
            var readerService = provider.GetRequiredService<IReaderConfigurationService>();
            return new PacketTraceService(readerService);
        });
        
        // Register packet tracing startup service
        services.AddHostedService<PacketTraceStartupService>();
        
        // Register ViewModels
        services.AddScoped<DashboardViewModel>();
        services.AddScoped<CardEventsViewModel>();
        services.AddScoped<ReadersConfigurationViewModel>();
        services.AddScoped<FeedbackConfigurationViewModel>();
        services.AddScoped<PluginsConfigurationViewModel>();
        services.AddScoped<SystemConfigurationViewModel>();
        services.AddScoped<PacketTraceViewModel>();
        
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