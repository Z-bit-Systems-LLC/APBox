namespace ApBox.Plugins;

/// <summary>
/// Idle state LED configuration for when no card activity is occurring
/// </summary>
public class IdleStateFeedback
{
    /// <summary>
    /// LED color that stays on permanently when reader is idle
    /// </summary>
    public LedColor? PermanentLedColor { get; set; }
    
    /// <summary>
    /// LED color that flashes every 5 seconds during idle state
    /// </summary>
    public LedColor? HeartbeatFlashColor { get; set; }
}