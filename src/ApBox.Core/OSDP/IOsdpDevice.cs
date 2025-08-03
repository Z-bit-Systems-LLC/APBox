using ApBox.Core.Models;
using ApBox.Plugins;

namespace ApBox.Core.OSDP;

/// <summary>
/// Interface representing an OSDP device (reader) with communication capabilities.
/// Provides methods for connection management, feedback control, and event handling.
/// </summary>
public interface IOsdpDevice
{
    /// <summary>
    /// Unique identifier of the OSDP device
    /// </summary>
    Guid Id { get; }
    
    /// <summary>
    /// OSDP address of the device (0-126)
    /// </summary>
    byte Address { get; }
    
    /// <summary>
    /// Human-readable name of the device
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Whether the device is currently online and responding
    /// </summary>
    bool IsOnline { get; }
    
    /// <summary>
    /// Whether the device is enabled for operation
    /// </summary>
    bool IsEnabled { get; }
    
    /// <summary>
    /// Timestamp of the last communication activity with the device
    /// </summary>
    DateTime LastActivity { get; }
    
    /// <summary>
    /// Establish connection to the OSDP device.
    /// </summary>
    /// <returns>True if connection was established successfully, false otherwise</returns>
    Task<bool> ConnectAsync();
    
    /// <summary>
    /// Disconnect from the OSDP device.
    /// </summary>
    /// <returns>A task representing the disconnect operation</returns>
    Task DisconnectAsync();
    
    /// <summary>
    /// Send feedback (LED, beep, display) to the OSDP device.
    /// </summary>
    /// <param name="feedback">The feedback configuration to send</param>
    /// <returns>True if feedback was sent successfully, false otherwise</returns>
    Task<bool> SendFeedbackAsync(ReaderFeedback feedback);
    
    /// <summary>
    /// Event raised when a card is read by the device
    /// </summary>
    event EventHandler<CardReadEvent>? CardRead;
    
    /// <summary>
    /// Event raised when a PIN digit is entered on the device
    /// </summary>
    event EventHandler<PinDigitEvent>? PinDigitReceived;
    
    /// <summary>
    /// Event raised when the device's online status changes
    /// </summary>
    event EventHandler<OsdpStatusChangedEventArgs>? StatusChanged;
}

/// <summary>
/// Event arguments for OSDP device status changes
/// </summary>
public class OsdpStatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// Whether the device is now online
    /// </summary>
    public bool IsOnline { get; set; }
    
    /// <summary>
    /// Optional message describing the status change
    /// </summary>
    public string? Message { get; set; }
    
    /// <summary>
    /// UTC timestamp when the status change occurred
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}