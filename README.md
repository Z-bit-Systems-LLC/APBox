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

> **⚠️ Early Development Notice**  
> This project is in early development. After pulling code from Git, you must delete the SQLite database file (`apbox.db`) as there are currently no migration scripts. The database will be recreated automatically on the next run.

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://www.docker.com/get-started) (optional, for containerized deployment)

### Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/Z-bit-Systems-LLC/APBox.git
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

## Building Single Executable

ApBox can be built as a self-contained single executable file that includes all .NET libraries, eliminating the need to install .NET runtime on target systems.

### Self-Contained Single File Build

Build a single executable with all dependencies included:

```bash
# Windows x64
dotnet publish src/ApBox.Web -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish/win-x64

# Linux x64 (for Raspberry Pi)
dotnet publish src/ApBox.Web -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true -o publish/linux-x64

# Linux ARM64 (for ARM-based systems)
dotnet publish src/ApBox.Web -c Release -r linux-arm64 --self-contained -p:PublishSingleFile=true -o publish/linux-arm64
```

### Optimized Production Build

For smaller file sizes and better performance:

```bash
# Trimmed build (removes unused code)
dotnet publish src/ApBox.Web -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:PublishTrimmed=true -o publish/win-x64-trimmed

# Ready-to-run (faster startup)
dotnet publish src/ApBox.Web -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:PublishReadyToRun=true -o publish/win-x64-r2r
```

### Plugin Handling

The single-file executable **does not automatically include plugin DLLs** since they are loaded dynamically at runtime. You have two options:

#### Option 1: Deploy Plugins Separately (Recommended)
```bash
# Build and publish main application
dotnet publish src/ApBox.Web -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish/win-x64

# Build plugins separately
dotnet build src/ApBox.SamplePlugins -c Release -o publish/win-x64/plugins

# Deploy both executable and plugins folder
copy publish/win-x64/ApBox.Web.exe C:\ApBox\
xcopy publish/win-x64/plugins C:\ApBox\plugins\ /E /I
```

#### Option 2: Include Plugins in Build
Add plugin projects as dependencies to force inclusion:

```xml
<!-- Add to src/ApBox.Web/ApBox.Web.csproj -->
<ItemGroup>
  <ProjectReference Include="../ApBox.SamplePlugins/ApBox.SamplePlugins.csproj" />
</ItemGroup>
```

Then publish normally - plugins will be embedded in the executable.

### Deployment

The published executable includes:
- All .NET runtime components
- Application code and dependencies
- SQLite database provider
- Web assets and static files
- **Plugin DLLs** (only if using Option 2 above)

Deployment examples:

```bash
# Windows (with separate plugins)
copy publish/win-x64/ApBox.Web.exe C:\ApBox\
xcopy publish/win-x64/plugins C:\ApBox\plugins\ /E /I
C:\ApBox\ApBox.Web.exe

# Linux (with separate plugins)
cp publish/linux-x64/ApBox.Web /opt/apbox/
cp -r publish/linux-x64/plugins /opt/apbox/
/opt/apbox/ApBox.Web
```

### Runtime Identifiers (RIDs)

Common runtime identifiers for ApBox deployment:

| Platform | RID | Use Case |
|----------|-----|----------|
| `win-x64` | Windows 64-bit | Development, Windows servers |
| `win-arm64` | Windows ARM64 | Windows on ARM devices |
| `linux-x64` | Linux 64-bit | Ubuntu, Debian, CentOS |
| `linux-arm` | Linux ARM32 | Raspberry Pi 3/4 (32-bit) |
| `linux-arm64` | Linux ARM64 | Raspberry Pi 4 (64-bit), Strato Pi |

### Build Options

| Option | Description | Impact |
|--------|-------------|--------|
| `--self-contained` | Includes .NET runtime | Larger size, no runtime dependency |
| `-p:PublishSingleFile=true` | Bundles everything into one file | Single executable |
| `-p:PublishTrimmed=true` | Removes unused assemblies | Smaller size, faster startup |
| `-p:PublishReadyToRun=true` | Pre-compiles to native code | Faster startup, larger size |

## Configuration

### System Configuration Export/Import

ApBox supports exporting and importing complete system configurations through the web interface or API. The configuration export includes all readers, feedback settings, and system information.

#### Export Schema

The complete configuration export follows this JSON schema:

```json
{
  "exportVersion": "1.0",
  "exportedAt": "2024-01-01T12:00:00.000Z",
  "systemInfo": {
    "apBoxVersion": "1.0.0.0",
    "framework": ".NET 8.0.0",
    "platform": "Win32NT (Microsoft Windows 10.0.22631)",
    "machineName": "PRODUCTION-SERVER",
    "osVersion": "Microsoft Windows NT 10.0.22631.0",
    "processorCount": 8,
    "workingDirectory": "C:\\ApBox",
    "startTime": "2024-01-01T12:00:00.000Z"
  },
  "readers": [
    {
      "readerId": "12345678-1234-1234-1234-123456789abc",
      "readerName": "Main Entrance",
      "address": 1,
      "isEnabled": true,
      "createdAt": "2024-01-01T00:00:00.000Z",
      "updatedAt": "2024-01-01T00:00:00.000Z",
      "serialPort": "COM1",
      "baudRate": 9600,
      "securityMode": "ClearText",
      "secureChannelKey": null,
      "pluginMappings": [
        {
          "pluginId": "AccessControlPlugin",
          "executionOrder": 1,
          "isEnabled": true
        },
        {
          "pluginId": "TimeBasedAccessPlugin", 
          "executionOrder": 2,
          "isEnabled": true
        },
        {
          "pluginId": "AuditLoggingPlugin",
          "executionOrder": 3,
          "isEnabled": false
        }
      ]
    }
  ],
  "feedbackConfiguration": {
    "successFeedback": {
      "type": "Success",
      "beepCount": 1,
      "ledColor": "Green",
      "ledDuration": 1000,
      "displayMessage": "ACCESS GRANTED"
    },
    "failureFeedback": {
      "type": "Failure",
      "beepCount": 3,
      "ledColor": "Red", 
      "ledDuration": 2000,
      "displayMessage": "ACCESS DENIED"
    },
    "idleState": {
      "permanentLedColor": "Blue",
      "heartbeatFlashColor": "Green"
    }
  }
}
```

#### Reader Configuration Properties

| Property | Type | Description |
|----------|------|-------------|
| `readerId` | `string` (GUID) | Unique identifier for the reader |
| `readerName` | `string` | Human-readable name (max 100 chars) |
| `address` | `number` (byte) | OSDP bus address (1-127) |
| `isEnabled` | `boolean` | Whether reader is active |
| `serialPort` | `string` | Serial port (e.g., "COM1", "/dev/ttyUSB0") |
| `baudRate` | `number` | Communication speed (default: 9600) |
| `securityMode` | `string` | OSDP security: "ClearText", "Install", "Secure" |
| `secureChannelKey` | `byte[]` | Encryption key for secure mode |
| `pluginMappings` | `array` | List of plugins assigned to this reader |

#### Plugin Mapping Properties

| Property | Type | Description |
|----------|------|-------------|
| `pluginId` | `string` | Unique identifier of the plugin |
| `executionOrder` | `number` | Plugin execution sequence (1-based) |
| `isEnabled` | `boolean` | Whether plugin is active for this reader |

#### Feedback Configuration Properties

| Property | Type | Description |
|----------|------|-------------|
| `type` | `string` | "None", "Success", "Failure", "Custom" |
| `beepCount` | `number` | Number of beeps (0+ for success/failure) |
| `ledColor` | `string` | "Off", "Red", "Green", "Amber", "Blue" |
| `ledDuration` | `number` | LED duration in milliseconds |
| `displayMessage` | `string` | Text shown on reader display |

#### Import Validation

The system validates imported configurations for:

- **JSON Format**: Valid JSON structure and required fields
- **Version Compatibility**: Warns about version mismatches  
- **Reader Validation**: No duplicate names or addresses, valid names
- **Plugin Mappings**: Valid plugin IDs, unique execution orders per reader
- **Feedback Validation**: Positive durations, non-negative beep counts
- **Data Integrity**: Proper GUIDs, valid enums, range checking

#### Export/Import Usage

**Web Interface:**
1. Navigate to Configuration → System
2. Click "Export Configuration" to download JSON file
3. Click "Import Configuration" to upload and validate JSON file
4. Review validation results before confirming import

**File Operations:**
- Export creates timestamped backup with system information and plugin assignments
- Import overwrites existing configurations (readers and plugin mappings are updated if they exist)
- Plugin assignments are preserved and validated during import
- Validation prevents importing invalid or conflicting data
- System restart may be required after major configuration changes

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

### Linux Deployment Requirements

**Raspberry Pi Configuration (Raspbian)**

When deploying on Raspberry Pi 4 with Raspbian, the application requires proper permissions to access serial ports for OSDP communication. Without these permissions, ApBox will run but produce no OSDP output.

**Solution 1: Add User to dialout Group (Recommended)**
```bash
# Add current user to dialout group
sudo usermod -a -G dialout $USER

# Verify group membership
groups $USER

# Log out and back in for changes to take effect
```

**Solution 2: Run with sudo (Not Recommended for Production)**
```bash
# Run with elevated privileges
sudo dotnet run --project src/ApBox.Web
# or for single executable:
sudo ./ApBox.Web
```

**Note**: The dialout group approach is preferred for production deployments as it follows the principle of least privilege and doesn't require running the entire application as root.

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