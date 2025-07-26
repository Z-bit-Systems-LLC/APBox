using Microsoft.AspNetCore.SignalR.Client;

namespace ApBox.Web.Services;

/// <summary>
/// Abstraction for SignalR HubConnection to enable testing
/// </summary>
public interface IHubConnectionWrapper : IAsyncDisposable
{
    /// <summary>
    /// Gets the current connection state
    /// </summary>
    HubConnectionState State { get; }
    
    /// <summary>
    /// Registers a handler that will be invoked when the method with the specified method name is invoked
    /// </summary>
    IDisposable On<T>(string methodName, Func<T, Task> handler);
    
    /// <summary>
    /// Starts the connection
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Stops the connection
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);
}