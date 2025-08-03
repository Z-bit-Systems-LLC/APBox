using ApBox.Plugins;
using ApBox.Core.Models;

namespace ApBox.Core.Data.Models;

public class PinEventEntity
{
    public int Id { get; set; }
    public string ReaderId { get; set; } = string.Empty;
    public string ReaderName { get; set; } = string.Empty;
    public string EncryptedPin { get; set; } = string.Empty;
    public int PinLength { get; set; }
    public int CompletionReason { get; set; } // Maps to PinCompletionReason enum
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? ProcessedByPlugin { get; set; }
    public DateTime Timestamp { get; set; }

    public static PinEventEntity FromPinReadEvent(PinReadEvent pinRead, string encryptedPin, bool success, string? message = null, string? processedByPlugin = null)
    {
        return new PinEventEntity
        {
            ReaderId = pinRead.ReaderId.ToString(),
            ReaderName = pinRead.ReaderName,
            EncryptedPin = encryptedPin,
            PinLength = pinRead.Pin.Length,
            CompletionReason = (int)pinRead.CompletionReason,
            Success = success,
            Message = message,
            ProcessedByPlugin = processedByPlugin,
            Timestamp = pinRead.Timestamp
        };
    }

    public PinEventDisplay ToPinEventDisplay()
    {
        return new PinEventDisplay
        {
            ReaderId = Guid.Parse(ReaderId),
            ReaderName = ReaderName,
            PinLength = PinLength,
            CompletionReason = (PinCompletionReason)CompletionReason,
            Timestamp = Timestamp,
            Success = Success,
            Message = Message,
            ProcessedByPlugins = ProcessedByPlugin
        };
    }
}