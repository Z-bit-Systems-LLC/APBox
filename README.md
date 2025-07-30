# ApBox

An industrial OSDP (Open Supervised Device Protocol) gateway that bridges card readers with existing access control systems through a flexible plugin architecture.

## Overview

Unlike traditional access controllers that limit third-party code execution, ApBox is designed as a pass-through gateway that preserves existing access control investments while enabling custom workflows and new technologies. Rather than making access decisions itself, ApBox typically forwards processed card data to existing access control systems, allowing organizations to enhance their current infrastructure without replacement.

### Key Use Cases

- **Elevator Dispatch Integration**: Connect any OSDP reader to elevator dispatch systems by receiving card reads and calling elevator APIs
- **Multi-Factor Authentication**: Add facial recognition, biometric validation, or other security layers before forwarding credentials to access panels  
- **Custom Enrollment Workflows**: Create specialized enrollment readers with validation logic tailored to organizational requirements
- **Multi-System Lookup**: Enable parking gate readers to query multiple databases and systems for comprehensive card validation
- **New Credential Technologies**: Integrate emerging credential formats not yet supported by legacy access controllers

ApBox fills the gap between modern OSDP readers and existing access infrastructure, providing the flexibility to adopt new technologies and implement custom business logic without disrupting established access control investments.

## Features

- **OSDP Protocol Support**: Native support for OSDP card readers and communication
- **Plugin Architecture**: Extensible system for custom card processing logic
- **Centralized Feedback Configuration**: Unified system for managing success/failure feedback
- **Web Management Interface**: Modern Blazor Server UI with Blazorise components
- **Real-time Dashboard**: Live monitoring of reader status and card events
- **Default Feedback Patterns**: Pre-configured LED, beep, and display responses
- **Idle State Management**: Configurable permanent and heartbeat LED patterns
- **System Configuration**: Export/import system configuration and restart management
- **Real-time Log Viewer**: Live streaming logs with filtering and search capabilities
- **Docker Deployment**: Containerized deployment for easy scaling

## Technology Stack

- **.NET 8**: Core runtime and framework
- **Blazor Server**: Web interface with real-time updates
- **Blazorise**: Bootstrap-based UI component library
- **MVVM Toolkit**: Clean separation of UI and business logic
- **SQLite**: Lightweight database for configuration and logging
- **NUnit**: Test-driven development with comprehensive test coverage
- **bUnit**: Blazor component testing framework

## Architecture

### Core Components

- **ApBox.Core**: Main application hosting OSDP integration and plugin infrastructure
- **ApBox.Web**: Blazor Server management interface with MVVM pattern
- **ApBox.Plugins**: Plugin interfaces and reference implementations

### Plugin System

```csharp
public interface IApBoxPlugin
{
    string Name { get; }
    string Version { get; }
    string Description { get; }
    
    Task<bool> ProcessCardReadAsync(CardReadEvent cardRead);
    
    Task InitializeAsync();
    Task ShutdownAsync();
}
```

### Feedback System

ApBox provides a centralized feedback configuration system:

1. **Success Feedback**: Configurable LED color, duration, beep count, and display message
2. **Failure Feedback**: Separate configuration for failed card reads
3. **Idle State**: Permanent LED and heartbeat flash patterns for inactive readers
4. **Real-time Configuration**: Web-based configuration with live preview and auto-save

## Web Interface

ApBox includes a comprehensive web management interface with the following sections:

### Dashboard
- **Real-time metrics**: Live card read statistics and system status
- **Reader monitoring**: Current status and configuration of all readers
- **Recent events**: Latest card read events with success/failure indicators
- **Plugin status**: Information about loaded plugins and their versions

### Configuration Management
- **Readers**: Add, edit, and manage OSDP reader configurations
- **Feedback**: Configure success/failure LED patterns, beeps, and display messages
- **System**: Export/import system configuration, restart management, and log viewer

### Testing Tools
- **Card Simulation**: Test card reads without physical hardware
- **Batch Testing**: Generate multiple test events for system validation
- **Continuous Simulation**: Ongoing random card reads for stress testing

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://www.docker.com/get-started) (optional, for containerized deployment)

### Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/your-org/ApBox.git
   cd ApBox
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Build the solution**
   ```bash
   dotnet build
   ```

4. **Run tests**
   ```bash
   dotnet test
   ```

5. **Start the web application**
   ```bash
   dotnet run --project src/ApBox.Web
   ```

6. **Access the web interface**
   
   Open your browser and navigate to `http://localhost:5271`

### Development Workflow

ApBox follows Test-Driven Development (TDD) practices:

```bash
# Watch mode for TDD workflow
dotnet watch test --project tests/ApBox.Core.Tests

# Run tests with detailed output
dotnet test -v normal

# Run only unit tests
dotnet test --filter "Category=Unit"

# Generate coverage report
dotnet-coverage collect "dotnet test" -f xml -o coverage.xml
```

## Configuration

### Reader Configuration

```json
{
  "ReaderId": "12345678-1234-1234-1234-123456789abc",
  "ReaderName": "Main Entrance",
  "Address": 1,
  "IsEnabled": true,
  "CreatedAt": "2024-01-01T00:00:00Z",
  "UpdatedAt": "2024-01-01T00:00:00Z"
}
```

### Feedback Configuration

```json
{
  "SuccessFeedback": {
    "Type": "Success",
    "LedColor": "Green",
    "LedDurationMs": 1000,
    "BeepCount": 1,
    "DisplayMessage": "ACCESS GRANTED"
  },
  "FailureFeedback": {
    "Type": "Failure",
    "LedColor": "Red",
    "LedDurationMs": 2000,
    "BeepCount": 3,
    "DisplayMessage": "ACCESS DENIED"
  },
  "IdleState": {
    "PermanentLedColor": "Blue",
    "HeartbeatFlashColor": "Green"
  }
}
```

### Card Read Event Structure

```csharp
public class CardReadEvent
{
    public Guid ReaderId { get; set; }
    public string CardNumber { get; set; }
    public int BitLength { get; set; }
    public DateTime Timestamp { get; set; }
    public string ReaderName { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; }
}
```

## Deployment

### Docker Deployment

```bash
# Build Docker image
docker build -t apbox:latest .

# Run container
docker run -d -p 8080:80 --name apbox apbox:latest
```

### Hardware Support

- **Development**: Raspberry Pi 4 or compatible
- **Production**: Strato Pi CM Duo v3 (industrial-grade)

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Write tests for your changes
4. Implement your feature following TDD practices
5. Ensure all tests pass (`dotnet test`)
6. Commit your changes (`git commit -m 'Add amazing feature'`)
7. Push to the branch (`git push origin feature/amazing-feature`)
8. Open a Pull Request

## Testing

ApBox maintains comprehensive test coverage:

- **Unit Tests**: Core business logic and plugin interfaces
- **Integration Tests**: OSDP communication and database operations
- **Component Tests**: Blazor UI components with bUnit
- **End-to-End Tests**: Complete workflow validation

## License

This project is licensed under the Eclipse Public License v2.0 - see the [LICENSE](LICENSE) file for details.

## Support

- **Documentation**: [Wiki](https://github.com/Z-bit-Systems-LLC/ApBox/wiki)
- **Issues**: [GitHub Issues](https://github.com/Z-bit-Systems-LLC/ApBox/issues)

## Testing with Sample Plugins

ApBox includes 4 comprehensive sample plugins that demonstrate the plugin architecture. Here's how to test them:

### 1. Build and Deploy Sample Plugins

```bash
# Build the sample plugins
dotnet build src/ApBox.SamplePlugins

# Copy plugins to the web application plugins directory
mkdir -p src/ApBox.Web/plugins
cp src/ApBox.SamplePlugins/bin/Debug/net8.0/ApBox.SamplePlugins.dll src/ApBox.Web/plugins/
```

### 2. Start the Web Application

```bash
# Run the web application
dotnet run --project src/ApBox.Web

# The application will start on http://localhost:5271
```

### 3. Access the Dashboard

Open your browser to `http://localhost:5271` and you'll see:

- **Dashboard**: Real-time metrics showing active readers and loaded plugins
- **Recent Card Events**: Live table of card read events
- **Reader Status**: Current status of configured readers

### 4. Simulate Card Reads

Navigate to `http://localhost:5271/test-card-reads` to use the card simulation interface:

#### **Single Card Simulation**
1. Select a reader from the dropdown
2. Enter a card number or click "Generate Random"
3. Choose bit length (26-bit or 37-bit)
4. Click "Simulate Card Read"

#### **Batch Testing**
- **"Simulate 5 Random Reads"**: Quick batch of 5 random events
- **"Simulate 10 Random Reads"**: Larger batch for stress testing
- **"Start Continuous Simulation"**: Ongoing random events every 2-5 seconds
- **"Stop Continuous"**: Stop the continuous simulation

### 5. Sample Plugin Behavior

The included sample plugins will process your card reads:

#### **🔐 Access Control Plugin**
- **Authorized cards**: `12345678`, `87654321`, `11111111`, `22222222`, `12345123`, `98765987`
- **Authorized**: Green LED, 1 beep, "ACCESS GRANTED"
- **Unauthorized**: Red LED, 3 beeps, "ACCESS DENIED"

#### **⏰ Time-Based Access Plugin**
- **Card `12345678`**: Business hours only (Mon-Fri, 8 AM - 5 PM)
- **Card `87654321`**: Extended hours (Mon-Sat, 6 AM - 10 PM)
- **Card `11111111`**: 24/7 access
- **Card `22222222`**: Weekends only (Sat-Sun, 7 AM - 3 PM)
- **Time allowed**: Green LED, 2 beeps, "TIME ACCESS OK"
- **Time restricted**: Amber LED, 2 beeps, "TIME RESTRICTED"

#### **📋 Audit Logging Plugin**
- Records all events to `logs/audit/audit-YYYY-MM-DD.jsonl`
- Provides brief blue LED flash (100ms)
- Check the logs directory for JSON audit entries

#### **📊 Event Logging Plugin**
- Logs all events to the standard .NET logging system
- Tracks statistics (total, successful, failed events)
- No visual feedback (passive monitoring)
- Check console output for log entries

### 6. Real-Time Dashboard Updates

With the simulation running:

1. **Open multiple browser tabs** to `http://localhost:5271`
2. **Start continuous simulation** in the test page
3. **Watch the dashboard** update in real-time:
   - Recent Card Events table shows new entries instantly
   - Total Events counter increments
   - No page refresh needed - SignalR provides live updates

### 7. Log Monitoring

Monitor the application logs to see plugin activity:

```bash
# The console will show detailed plugin processing:
# - Access Control Plugin processing card 12345678
# - Time-Based Access Plugin checking schedule
# - Audit entries being written
# - Event statistics being tracked
```

### 8. Testing Different Scenarios

Try these test scenarios:

#### **Authorized Access**
- Use card `12345678` during business hours
- Should get green LED from both Access Control and Time-Based plugins

#### **Unauthorized Card**
- Use card `99999999` (not in authorized list)
- Should get red LED from Access Control plugin

#### **Time Restrictions**
- Use card `12345678` outside business hours (evenings/weekends)
- Should get amber LED from Time-Based plugin

#### **24/7 Access**
- Use card `11111111` any time
- Should always get green LED

### 9. Plugin Development Testing

Use the Test Card Reads page to develop and test your own plugins:

1. **Create your plugin** following the samples in `src/ApBox.SamplePlugins/`
2. **Build and deploy** to the plugins directory
3. **Restart the application** to load new plugins
4. **Test with various card numbers** and scenarios
5. **Check the dashboard** for real-time results

This testing environment lets you validate plugin behavior without physical card readers, making development fast and reliable.

## Roadmap

### MVP (Weeks 1-8)
- [x] Core foundation and plugin system
- [x] Simplified plugin interface with centralized feedback
- [x] Real-time SignalR dashboard with live updates
- [x] Comprehensive Blazor web interface with Blazorise components
- [x] Sample plugins and testing infrastructure
- [x] bUnit UI testing with comprehensive test coverage
- [x] Centralized feedback configuration system
- [x] System configuration export/import functionality
- [x] Real-time log viewer with filtering
- [x] System restart management
- [x] OSDP integration and communication
- [ ] Docker deployment support

### Post-MVP
- [ ] Advanced plugin marketplace
- [ ] Enhanced security features
- [ ] Production hardware optimization
- [ ] Cloud integration capabilities
- [ ] Advanced analytics and reporting

## Acknowledgments

- [OSDP.Net](https://github.com/bytedreamer/OSDP.Net) - OSDP protocol implementation
- [Blazorise](https://blazorise.com/) - Bootstrap Blazor components
- [Aporta](https://github.com/bytedreamer/Aporta) - Reference implementation and inspiration