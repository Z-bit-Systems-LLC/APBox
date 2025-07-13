using ApBox.Plugins;

namespace ApBox.Core.Data.Models;

public class CardEventEntity
{
    public long Id { get; set; }
    public string ReaderId { get; set; } = string.Empty;
    public string CardNumber { get; set; } = string.Empty;
    public int BitLength { get; set; }
    public string ReaderName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? ProcessedByPlugin { get; set; }
    public DateTime Timestamp { get; set; }
    
    public CardReadEvent ToCardReadEvent()
    {
        return new CardReadEvent
        {
            ReaderId = Guid.Parse(ReaderId),
            CardNumber = CardNumber,
            BitLength = BitLength,
            ReaderName = ReaderName,
            Timestamp = Timestamp
        };
    }
    
    public static CardEventEntity FromCardReadEvent(CardReadEvent cardEvent, CardReadResult? result = null)
    {
        return new CardEventEntity
        {
            ReaderId = cardEvent.ReaderId.ToString(),
            CardNumber = cardEvent.CardNumber,
            BitLength = cardEvent.BitLength,
            ReaderName = cardEvent.ReaderName,
            Success = result?.Success ?? false,
            Message = result?.Message,
            ProcessedByPlugin = result?.ProcessedByPlugin,
            Timestamp = cardEvent.Timestamp
        };
    }
}