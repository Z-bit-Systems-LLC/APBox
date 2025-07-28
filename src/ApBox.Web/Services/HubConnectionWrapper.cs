using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Components;
using ApBox.Web.Constants;

namespace ApBox.Web.Services;

/// <summary>
/// Implementation of IHubConnectionWrapper that wraps SignalR HubConnection
/// </summary>
public class HubConnectionWrapper : IHubConnectionWrapper
{
    private readonly HubConnection _hubConnection;
    private bool _disposed = false;
    
    public HubConnectionWrapper(NavigationManager navigationManager)
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(navigationManager.ToAbsoluteUri(HubConstants.NotificationHubUrl))
            .WithAutomaticReconnect()
            .Build();
    }
    
    /// <summary>
    /// Gets the current connection state
    /// </summary>
    public HubConnectionState State => _disposed ? HubConnectionState.Disconnected : _hubConnection.State;
    
    /// <summary>
    /// Registers a handler that will be invoked when the method with the specified method name is invoked
    /// </summary>
    public IDisposable On<T>(string methodName, Func<T, Task> handler)
    {
        if (_disposed)
            return new EmptyDisposable();
            
        return _hubConnection.On(methodName, handler);
    }
    
    /// <summary>
    /// Registers a handler that will be invoked when the method with the specified method name is invoked with two parameters
    /// </summary>
    public IDisposable On<T1, T2>(string methodName, Func<T1, T2, Task> handler)
    {
        if (_disposed)
            return new EmptyDisposable();
            
        return _hubConnection.On(methodName, handler);
    }
    
    /// <summary>
    /// Starts the connection
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            return Task.CompletedTask;
            
        return _hubConnection.StartAsync(cancellationToken);
    }
    
    /// <summary>
    /// Stops the connection
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            return Task.CompletedTask;
            
        return _hubConnection.StopAsync(cancellationToken);
    }
    
    /// <summary>
    /// Disposes the hub connection
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;
            if (_hubConnection is not null)
            {
                await _hubConnection.DisposeAsync();
            }
        }
    }
    
    private class EmptyDisposable : IDisposable
    {
        public void Dispose() { }
    }
}