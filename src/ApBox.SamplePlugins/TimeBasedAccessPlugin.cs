using ApBox.Plugins;
using Microsoft.Extensions.Logging;

namespace ApBox.SamplePlugins;

/// <summary>
/// Sample time-based access control plugin that restricts access based on time of day and day of week
/// </summary>
public class TimeBasedAccessPlugin : IApBoxPlugin
{
    private readonly Dictionary<string, AccessSchedule> _cardSchedules;
    private readonly ILogger<TimeBasedAccessPlugin>? _logger;

    public TimeBasedAccessPlugin()
    {
        // Initialize with sample card schedules
        _cardSchedules = new Dictionary<string, AccessSchedule>(StringComparer.OrdinalIgnoreCase)
        {
            // Standard business hours cards
            ["12345678"] = new AccessSchedule
            {
                AllowedDays = DayOfWeek.Monday | DayOfWeek.Tuesday | DayOfWeek.Wednesday | DayOfWeek.Thursday | DayOfWeek.Friday,
                StartTime = new TimeOnly(8, 0),  // 8:00 AM
                EndTime = new TimeOnly(17, 0),   // 5:00 PM
                Description = "Standard Business Hours"
            },
            
            // Extended hours for managers
            ["87654321"] = new AccessSchedule
            {
                AllowedDays = DayOfWeek.Monday | DayOfWeek.Tuesday | DayOfWeek.Wednesday | DayOfWeek.Thursday | DayOfWeek.Friday | DayOfWeek.Saturday,
                StartTime = new TimeOnly(6, 0),  // 6:00 AM
                EndTime = new TimeOnly(22, 0),   // 10:00 PM
                Description = "Extended Hours - Manager"
            },
            
            // 24/7 access for security/maintenance
            ["11111111"] = new AccessSchedule
            {
                AllowedDays = DayOfWeek.Monday | DayOfWeek.Tuesday | DayOfWeek.Wednesday | DayOfWeek.Thursday | DayOfWeek.Friday | DayOfWeek.Saturday | DayOfWeek.Sunday,
                StartTime = new TimeOnly(0, 0),  // Midnight
                EndTime = new TimeOnly(23, 59),  // 11:59 PM
                Description = "24/7 Access - Security"
            },
            
            // Weekend maintenance crew
            ["22222222"] = new AccessSchedule
            {
                AllowedDays = DayOfWeek.Saturday | DayOfWeek.Sunday,
                StartTime = new TimeOnly(7, 0),  // 7:00 AM
                EndTime = new TimeOnly(15, 0),   // 3:00 PM
                Description = "Weekend Maintenance"
            }
        };
    }

    public TimeBasedAccessPlugin(ILogger<TimeBasedAccessPlugin> logger) : this()
    {
        _logger = logger;
    }

    public Guid Id => new Guid("D4E5F6A7-89AB-CDEF-0123-123456789004");
    public string Name => "Time-Based Access Plugin";
    public string Version => "1.0.0";
    public string Description => "Controls access based on time of day and day of week restrictions";

    public async Task<bool> ProcessCardReadAsync(CardReadEvent cardRead)
    {
        await Task.CompletedTask; // Async signature for future extensibility

        _logger?.LogInformation("Time-Based Access Plugin processing card {CardNumber} at {Timestamp}", 
            cardRead.CardNumber, cardRead.Timestamp);

        // Check if card has a schedule defined
        if (!_cardSchedules.TryGetValue(cardRead.CardNumber, out var schedule))
        {
            _logger?.LogWarning("Card {CardNumber} has no time-based schedule defined", cardRead.CardNumber);
            
            return false;
        }

        var now = DateTime.Now;
        var currentDay = now.DayOfWeek;
        var currentTime = TimeOnly.FromDateTime(now);

        // Check if current day is allowed
        if (!schedule.AllowedDays.HasFlag(GetDayOfWeekFlag(currentDay)))
        {
            _logger?.LogInformation("Card {CardNumber} denied - {Day} not in allowed days ({AllowedDays})", 
                cardRead.CardNumber, currentDay, schedule.AllowedDays);
            
            return false;
        }

        // Check if current time is within allowed hours
        if (currentTime < schedule.StartTime || currentTime > schedule.EndTime)
        {
            _logger?.LogInformation("Card {CardNumber} denied - time {CurrentTime} outside allowed hours {StartTime}-{EndTime}", 
                cardRead.CardNumber, currentTime, schedule.StartTime, schedule.EndTime);
            
            return false;
        }

        // Access granted
        _logger?.LogInformation("Card {CardNumber} granted time-based access - {Schedule}", 
            cardRead.CardNumber, schedule.Description);
        
        return true;
    }

    public async Task<bool> ProcessPinReadAsync(PinReadEvent pinRead)
    {
        await Task.CompletedTask; // Async signature for future extensibility

        _logger?.LogInformation("Time-Based Access Plugin processing PIN from reader {ReaderName} at {Timestamp}", 
            pinRead.ReaderName, pinRead.Timestamp);

        // For demonstration: PIN access is also time-based but with different rules
        var now = DateTime.Now;
        var currentDay = now.DayOfWeek;
        var currentTime = TimeOnly.FromDateTime(now);

        // PIN access allowed only during specific hours (simulating emergency/after-hours access)
        var emergencySchedule = new AccessSchedule
        {
            AllowedDays = DayOfWeek.Monday | DayOfWeek.Tuesday | DayOfWeek.Wednesday | DayOfWeek.Thursday | DayOfWeek.Friday | DayOfWeek.Saturday | DayOfWeek.Sunday,
            StartTime = new TimeOnly(18, 0),  // 6:00 PM
            EndTime = new TimeOnly(7, 0),     // 7:00 AM next day (after hours)
            Description = "Emergency/After-Hours PIN Access"
        };

        // Check if current day is allowed
        if (!emergencySchedule.AllowedDays.HasFlag(GetDayOfWeekFlag(currentDay)))
        {
            _logger?.LogInformation("PIN from reader {ReaderName} denied - {Day} not in allowed days", 
                pinRead.ReaderName, currentDay);
            
            return false;
        }

        // Check if current time is within allowed hours (handling overnight schedule)
        bool isWithinHours = false;
        if (emergencySchedule.EndTime < emergencySchedule.StartTime)
        {
            // Overnight schedule (e.g., 18:00 to 07:00)
            isWithinHours = currentTime >= emergencySchedule.StartTime || currentTime <= emergencySchedule.EndTime;
        }
        else
        {
            // Same-day schedule
            isWithinHours = currentTime >= emergencySchedule.StartTime && currentTime <= emergencySchedule.EndTime;
        }

        if (!isWithinHours)
        {
            _logger?.LogInformation("PIN from reader {ReaderName} denied - time {CurrentTime} outside emergency access hours {StartTime}-{EndTime}", 
                pinRead.ReaderName, currentTime, emergencySchedule.StartTime, emergencySchedule.EndTime);
            
            return false;
        }

        // Access granted for emergency/after-hours PIN
        _logger?.LogInformation("PIN from reader {ReaderName} granted time-based emergency access", pinRead.ReaderName);
        
        return true;
    }

    public Task InitializeAsync()
    {
        _logger?.LogInformation("Time-Based Access Plugin initialized with {Count} scheduled cards", 
            _cardSchedules.Count);
        return Task.CompletedTask;
    }

    public Task ShutdownAsync()
    {
        _logger?.LogInformation("Time-Based Access Plugin shutting down");
        return Task.CompletedTask;
    }

    private static DayOfWeek GetDayOfWeekFlag(DayOfWeek day)
    {
        return day;
    }

    /// <summary>
    /// Add or update a card's access schedule
    /// </summary>
    public void SetCardSchedule(string cardNumber, AccessSchedule schedule)
    {
        _cardSchedules[cardNumber] = schedule;
        _logger?.LogInformation("Updated schedule for card {CardNumber}: {Description}", 
            cardNumber, schedule.Description);
    }

    /// <summary>
    /// Get a card's access schedule
    /// </summary>
    public AccessSchedule? GetCardSchedule(string cardNumber)
    {
        return _cardSchedules.TryGetValue(cardNumber, out var schedule) ? schedule : null;
    }

    /// <summary>
    /// Remove a card's schedule
    /// </summary>
    public void RemoveCardSchedule(string cardNumber)
    {
        if (_cardSchedules.Remove(cardNumber))
        {
            _logger?.LogInformation("Removed schedule for card {CardNumber}", cardNumber);
        }
    }
}

/// <summary>
/// Represents an access schedule for time-based access control
/// </summary>
public class AccessSchedule
{
    /// <summary>
    /// Days of the week when access is allowed (can be combined with flags)
    /// </summary>
    public DayOfWeek AllowedDays { get; set; }
    
    /// <summary>
    /// Start time for daily access window
    /// </summary>
    public TimeOnly StartTime { get; set; }
    
    /// <summary>
    /// End time for daily access window
    /// </summary>
    public TimeOnly EndTime { get; set; }
    
    /// <summary>
    /// Human-readable description of this schedule
    /// </summary>
    public string Description { get; set; } = string.Empty;
}