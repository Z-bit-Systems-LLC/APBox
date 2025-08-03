namespace ApBox.Core.Models;

/// <summary>
/// Represents feedback to be sent to an OSDP reader (LED, beeper, display).
/// Used to provide visual and audible confirmation of card read results.
/// </summary>
public class ReaderFeedback
{
    /// <summary>
    /// Type of feedback to provide
    /// </summary>
    public ReaderFeedbackType Type { get; set; }
    
    /// <summary>
    /// Number of beeps to sound (0 for no beep)
    /// </summary>
    public int BeepCount { get; set; }
    
    /// <summary>
    /// LED color to display, if any
    /// </summary>
    public LedColor? LedColor { get; set; }
    
    /// <summary>
    /// Duration in milliseconds to display the LED
    /// </summary>
    public int LedDuration { get; set; }
    
    /// <summary>
    /// Text message to display on reader display, if supported
    /// </summary>
    public string? DisplayMessage { get; set; }
}

/// <summary>
/// Defines the type of feedback being provided to the reader
/// </summary>
public enum ReaderFeedbackType
{
    /// <summary>
    /// No feedback provided
    /// </summary>
    None,
    
    /// <summary>
    /// Success feedback (typically green LED, single beep)
    /// </summary>
    Success,
    
    /// <summary>
    /// Failure feedback (typically red LED, multiple beeps)
    /// </summary>
    Failure,
    
    /// <summary>
    /// Custom feedback with user-defined LED/beep patterns
    /// </summary>
    Custom
}

/// <summary>
/// Available LED colors for reader feedback
/// </summary>
public enum LedColor
{
    /// <summary>
    /// LED turned off
    /// </summary>
    Off,
    
    /// <summary>
    /// Red LED
    /// </summary>
    Red,
    
    /// <summary>
    /// Green LED
    /// </summary>
    Green,
    
    /// <summary>
    /// Amber/Yellow LED
    /// </summary>
    Amber,
    
    /// <summary>
    /// Blue LED
    /// </summary>
    Blue
}