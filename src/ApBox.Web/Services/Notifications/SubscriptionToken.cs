namespace ApBox.Web.Services.Notifications;

/// <summary>
/// Represents a disposable subscription token that automatically unsubscribes when disposed
/// </summary>
public sealed class SubscriptionToken : IDisposable
{
    private readonly Action _unsubscribe;
    private bool _disposed;

    /// <summary>
    /// Creates a new subscription token
    /// </summary>
    /// <param name="unsubscribe">Action to call when disposing (unsubscribing)</param>
    public SubscriptionToken(Action unsubscribe)
    {
        _unsubscribe = unsubscribe ?? throw new ArgumentNullException(nameof(unsubscribe));
    }

    /// <summary>
    /// Unsubscribes from the notification
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _unsubscribe();
    }
}
