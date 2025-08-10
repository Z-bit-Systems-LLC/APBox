using ApBox.Core.PacketTracing.Services;

namespace ApBox.Core.Services;

/// <summary>
/// Hosted service that automatically starts packet tracing when the application starts
/// </summary>
public class PacketTraceStartupService : IHostedService
{
    private readonly IPacketTraceService _packetTraceService;
    private readonly ILogger<PacketTraceStartupService> _logger;

    public PacketTraceStartupService(
        IPacketTraceService packetTraceService,
        ILogger<PacketTraceStartupService> logger)
    {
        _packetTraceService = packetTraceService;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting packet tracing system...");

        try
        {
            // Start tracing for all configured readers
            _packetTraceService.StartTracingAll();
            
            _logger.LogInformation("Packet tracing system started successfully - tracing all enabled readers");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start packet tracing system");
            // Don't throw - packet tracing failure shouldn't prevent app startup
        }
        
        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping packet tracing system...");

        try
        {
            _packetTraceService.StopTracingAll();
            _logger.LogInformation("Packet tracing system stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping packet tracing system");
        }
        
        await Task.CompletedTask;
    }
}