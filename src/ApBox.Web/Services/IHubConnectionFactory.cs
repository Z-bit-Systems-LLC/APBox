namespace ApBox.Web.Services;

/// <summary>
/// Factory for creating HubConnection instances
/// </summary>
public interface IHubConnectionFactory
{
    /// <summary>
    /// Creates a new HubConnectionWrapper instance
    /// </summary>
    IHubConnectionWrapper CreateConnection();
}