namespace ApBox.Web.Constants;

/// <summary>
/// Constants for SignalR hubs and API endpoints
/// </summary>
public static class HubConstants
{
    /// <summary>
    /// URL path for the main notification hub
    /// </summary>
    public const string NotificationHubUrl = "/notification";
    
    /// <summary>
    /// Base URL path for card events API endpoints
    /// </summary>
    public const string CardEventsApiBase = "/api/cardevents";
    
    /// <summary>
    /// URL path for real-time card processing endpoint
    /// </summary>
    public const string ProcessRealtimeEndpoint = CardEventsApiBase + "/process-realtime";
}