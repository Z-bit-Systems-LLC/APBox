namespace ApBox.Plugins;

public class PinDigitEvent
{
    public Guid ReaderId { get; set; }
    public char Digit { get; set; }
    public DateTime Timestamp { get; set; }
    public string ReaderName { get; set; } = string.Empty;
    public int SequenceNumber { get; set; }  // Position in the PIN sequence
}