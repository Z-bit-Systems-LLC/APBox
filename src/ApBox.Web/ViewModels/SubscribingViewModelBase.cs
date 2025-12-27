using CommunityToolkit.Mvvm.ComponentModel;

namespace ApBox.Web.ViewModels;

/// <summary>
/// Base class for ViewModels that subscribe to notifications.
/// Provides automatic subscription management to prevent duplicate subscriptions
/// when components are re-initialized (e.g., during navigation).
/// </summary>
public abstract class SubscribingViewModelBase : ObservableObject, IDisposable
{
    private readonly List<IDisposable> _subscriptions = new();
    private bool _disposed;

    /// <summary>
    /// Clears all existing subscriptions. Call this at the start of initialization
    /// to prevent duplicate subscriptions when the ViewModel is re-initialized.
    /// </summary>
    protected void ClearSubscriptions()
    {
        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }
        _subscriptions.Clear();
    }

    /// <summary>
    /// Adds a subscription to be tracked and automatically disposed.
    /// </summary>
    /// <param name="subscription">The subscription disposable token. Null values are ignored.</param>
    protected void AddSubscription(IDisposable? subscription)
    {
        if (subscription != null)
        {
            _subscriptions.Add(subscription);
        }
    }

    /// <summary>
    /// Adds a subscription to be tracked and automatically disposed.
    /// Fluent API that returns the subscription for inline use.
    /// </summary>
    /// <typeparam name="T">The type of disposable.</typeparam>
    /// <param name="subscription">The subscription disposable token.</param>
    /// <returns>The same subscription for chaining.</returns>
    protected T Track<T>(T subscription) where T : IDisposable
    {
        ArgumentNullException.ThrowIfNull(subscription);
        _subscriptions.Add(subscription);
        return subscription;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                ClearSubscriptions();
            }
            _disposed = true;
        }
    }
}
