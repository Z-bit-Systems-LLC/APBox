using ApBox.Core.Models;
using ApBox.Plugins;

namespace ApBox.Core.OSDP;

public interface IOsdpDevice
{
    Guid Id { get; }
    byte Address { get; }
    string Name { get; }
    bool IsOnline { get; }
    bool IsEnabled { get; }
    
    DateTime LastActivity { get; }
    
    Task<bool> ConnectAsync();
    
    Task DisconnectAsync();
    
    Task<bool> SendFeedbackAsync(ReaderFeedback feedback);
    
    event EventHandler<CardReadEvent>? CardRead;
    event EventHandler<OsdpStatusChangedEventArgs>? StatusChanged;
}

public class OsdpStatusChangedEventArgs : EventArgs
{
    public bool IsOnline { get; set; }
    public string? Message { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}