namespace ApBox.Core.OSDP;

/// <summary>
/// Event arguments for reader status change events
/// </summary>
public class ReaderStatusChangedEventArgs : EventArgs
{
    public required Guid ReaderId { get; init; }
    public required string ReaderName { get; init; }
    public bool IsOnline { get; init; }
    public string? ErrorMessage { get; init; }
}