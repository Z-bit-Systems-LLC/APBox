using ApBox.Core.Models;
using ApBox.Core.Services;

namespace ApBox.Web.Services;

/// <summary>
/// Web implementation of notification service that broadcasts via SignalR
/// </summary>
public class NotificationService : INotificationService
{
    private readonly ICardEventNotificationService _cardEventNotificationService;

    public NotificationService(ICardEventNotificationService cardEventNotificationService)
    {
        _cardEventNotificationService = cardEventNotificationService;
    }

    public async Task BroadcastReaderConfigurationAsync(ReaderConfiguration reader, string changeType)
    {
        await _cardEventNotificationService.BroadcastReaderConfigurationAsync(reader, changeType);
    }
}