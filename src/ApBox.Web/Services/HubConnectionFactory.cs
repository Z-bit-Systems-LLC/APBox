using Microsoft.AspNetCore.Components;

namespace ApBox.Web.Services;

/// <summary>
/// Factory implementation for creating HubConnection instances
/// </summary>
public class HubConnectionFactory : IHubConnectionFactory
{
    private readonly NavigationManager _navigationManager;

    public HubConnectionFactory(NavigationManager navigationManager)
    {
        _navigationManager = navigationManager;
    }

    /// <summary>
    /// Creates a new HubConnectionWrapper instance
    /// </summary>
    public IHubConnectionWrapper CreateConnection()
    {
        return new HubConnectionWrapper(_navigationManager);
    }
}