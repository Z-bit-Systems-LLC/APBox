using ApBox.Core.Models;
using ApBox.Plugins;

namespace ApBox.Core.OSDP;

public interface IOsdpDevice
{
    Guid Id { get; }
    byte Address { get; }
    string Name { get; }
    bool IsOnline { get; }
    DateTime LastActivity { get; }
    
    Task<bool> ConnectAsync();
    Task DisconnectAsync();
    Task<bool> SendCommandAsync(OsdpCommand command);
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

public abstract class OsdpCommand
{
    public abstract byte CommandCode { get; }
    public abstract byte[] GetPayload();
}

public class LedCommand : OsdpCommand
{
    public override byte CommandCode => 0x41; // LED command code
    
    public LedColor Color { get; set; }
    public byte OnTime { get; set; } = 10; // 100ms units
    public byte OffTime { get; set; } = 10; // 100ms units
    public byte Count { get; set; } = 1;
    
    public override byte[] GetPayload()
    {
        return new byte[]
        {
            0x00, // Reader number (0 = first reader)
            0x01, // Temporary control code
            OnTime,
            OffTime,
            (byte)Color,
            Count
        };
    }
}

public class BuzzerCommand : OsdpCommand
{
    public override byte CommandCode => 0x42; // Buzzer command code
    
    public byte OnTime { get; set; } = 5; // 100ms units
    public byte OffTime { get; set; } = 5; // 100ms units
    public byte Count { get; set; } = 1;
    
    public override byte[] GetPayload()
    {
        return new byte[]
        {
            0x01, // Reader number (0 = first reader)
            0x02, // Tone code (standard tone)
            OnTime,
            OffTime,
            Count
        };
    }
}