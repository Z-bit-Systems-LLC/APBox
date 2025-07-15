namespace ApBox.Core.Models;

/// <summary>
/// Core reader configuration model
/// </summary>
public class ReaderConfiguration
{
    public Guid ReaderId { get; set; }
    public string ReaderName { get; set; } = string.Empty;
    public byte Address { get; set; } = 1;
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}