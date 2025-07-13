# ApBox Sample Plugins

This library contains sample plugin implementations that demonstrate the ApBox plugin architecture and provide common functionality for card reader management systems.

## Available Plugins

### 1. Access Control Plugin
**Purpose**: Basic allow/deny access control based on predefined card numbers  
**Category**: Access Control  
**Features**:
- Maintains a list of authorized card numbers
- Grants or denies access based on card authorization status
- Provides appropriate visual and audible feedback
- Includes methods to add/remove authorized cards

**Feedback**:
- ✅ **Authorized**: Green LED, single beep, "ACCESS GRANTED"
- ❌ **Unauthorized**: Red LED, triple beep, "ACCESS DENIED"

### 2. Time-Based Access Plugin
**Purpose**: Access control with time and day-of-week restrictions  
**Category**: Access Control  
**Features**:
- Configurable schedules per card (days of week + time ranges)
- Pre-configured sample schedules (business hours, extended hours, 24/7, weekends)
- Detailed denial reasons in response data
- Support for complex time-based access patterns

**Sample Schedules**:
- **Standard Business**: Mon-Fri, 8:00 AM - 5:00 PM
- **Manager Extended**: Mon-Sat, 6:00 AM - 10:00 PM  
- **Security 24/7**: All days, all hours
- **Weekend Maintenance**: Sat-Sun, 7:00 AM - 3:00 PM

**Feedback**:
- ✅ **Authorized**: Green LED, double beep, "TIME ACCESS OK"
- ❌ **Time Restricted**: Orange LED, double beep, "TIME RESTRICTED"

### 3. Audit Logging Plugin
**Purpose**: Security auditing and compliance logging  
**Category**: Security & Auditing  
**Features**:
- Records all card access attempts to JSON log files
- Daily log file rotation (audit-YYYY-MM-DD.jsonl format)
- Retrieval methods for audit history queries
- Automatic log cleanup for retention management
- Thread-safe logging operations

**Log Format**: Each entry is a JSON object with:
- Timestamp, EventId, ReaderId, ReaderName
- CardNumber, BitLength, AdditionalData

**Feedback**:
- 🔵 **Logged**: Brief blue LED flash (100ms), no beep, no display message

### 4. Event Logging Plugin
**Purpose**: Integration with .NET logging infrastructure  
**Category**: Logging & Monitoring  
**Features**:
- Logs events to standard .NET ILogger
- Tracks statistics (total, successful, failed events)
- Provides no visual feedback to avoid interference
- Includes utility methods for custom logging

**Statistics Tracked**:
- Total events processed
- Successful vs failed events
- Success rate percentage

**Feedback**:
- No visual feedback (passive monitoring)

## Plugin Development Patterns

### 1. Constructor Patterns
```csharp
// Default constructor (required)
public MyPlugin() { }

// Constructor with logging
public MyPlugin(ILogger<MyPlugin> logger) : this() 
{
    _logger = logger;
}

// Constructor with custom configuration
public MyPlugin(MyConfiguration config, ILogger<MyPlugin> logger) : this(logger)
{
    _config = config;
}
```

### 2. Metadata Definition
```csharp
public PluginMetadata Metadata { get; } = new()
{
    Id = Guid.Parse("your-unique-guid-here"),
    Name = "Your Plugin Name",
    Description = "What your plugin does",
    Version = new Version(1, 0, 0),
    Author = "Your Name/Organization",
    Category = "Your Category"
};
```

### 3. Processing Pattern
```csharp
public async Task<CardReadResult> ProcessCardReadAsync(CardReadEvent cardRead)
{
    // 1. Log the incoming request
    _logger?.LogInformation("Processing card {CardNumber}", cardRead.CardNumber);
    
    // 2. Perform your logic
    bool allowed = YourBusinessLogic(cardRead);
    
    // 3. Return appropriate result
    return new CardReadResult
    {
        Success = allowed,
        Message = allowed ? "Access granted" : "Access denied",
        ProcessedBy = Metadata.Name,
        ProcessedAt = DateTime.UtcNow,
        AdditionalData = new Dictionary<string, object>
        {
            ["YourCustomField"] = "YourCustomValue"
        }
    };
}
```

### 4. Feedback Patterns
```csharp
public async Task<ReaderFeedback> GetFeedbackAsync(Guid readerId, CardReadResult result)
{
    if (result.Success)
    {
        return new ReaderFeedback
        {
            LedState = ReaderFeedback.LedColor.Green,
            BeepPattern = ReaderFeedback.BeepType.Single,
            DisplayMessage = "ACCESS GRANTED",
            Duration = TimeSpan.FromSeconds(3)
        };
    }
    else
    {
        return new ReaderFeedback
        {
            LedState = ReaderFeedback.LedColor.Red,
            BeepPattern = ReaderFeedback.BeepType.Triple,
            DisplayMessage = "ACCESS DENIED",
            Duration = TimeSpan.FromSeconds(2)
        };
    }
}
```

## Plugin Categories

- **Access Control**: Make authorization decisions
- **Security & Auditing**: Record events for compliance and security
- **Logging & Monitoring**: Track system activity and performance
- **Notifications**: Send alerts or notifications
- **Integration**: Connect with external systems
- **Validation**: Verify card data integrity

## Best Practices

1. **Always provide a parameterless constructor** for plugin loading
2. **Use dependency injection** for services like ILogger
3. **Handle exceptions gracefully** and return appropriate error results
4. **Log important events** for debugging and monitoring
5. **Provide meaningful feedback** for different scenarios
6. **Include detailed metadata** for plugin identification
7. **Use AdditionalData** for extensible result information
8. **Consider thread safety** for shared resources
9. **Implement proper disposal** in ShutdownAsync if needed
10. **Test with various card scenarios** (valid, invalid, edge cases)

## Testing Your Plugins

Use the Test Card Reads page in the ApBox web interface to:
1. Load your plugins into the `plugins` directory
2. Restart the application to load new plugins
3. Use the simulator to test different card numbers and scenarios
4. Check the real-time dashboard for results
5. Review logs for detailed plugin behavior

## Deployment

1. Build the ApBox.SamplePlugins project
2. Copy the compiled DLL to the `plugins` directory
3. Restart the ApBox application
4. Verify plugins are loaded in the dashboard metrics