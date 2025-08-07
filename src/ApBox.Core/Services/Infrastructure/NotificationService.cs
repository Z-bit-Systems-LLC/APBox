using ApBox.Core.Models;

namespace ApBox.Core.Services.Infrastructure;

public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;
    }

    public async Task BroadcastReaderConfigurationAsync(ReaderConfiguration reader, string changeType)
    {
        _logger.LogInformation("Reader configuration {ChangeType}: {ReaderName} ({ReaderId})", 
            changeType, reader.ReaderName, reader.ReaderId);
        
        // TODO: Implement actual notification mechanism (SignalR, email, etc.)
        await Task.CompletedTask;
    }

    public async Task SendErrorNotificationAsync(string error, Exception? exception = null)
    {
        _logger.LogError(exception, "Error notification: {Error}", error);
        
        // TODO: Implement actual error notification mechanism
        await Task.CompletedTask;
    }
}