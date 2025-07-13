# ApBox

An industrial OSDP (Open Supervised Device Protocol) gateway with a plugin ecosystem for access control systems.

## Overview

ApBox is an open-source gateway solution that bridges OSDP card readers with custom business logic through a flexible plugin architecture. Built on .NET 8 with a modern Blazor Server web interface, ApBox provides real-time monitoring, configuration management, and extensible processing of card read events.

## Features

- **OSDP Protocol Support**: Native support for OSDP card readers and communication
- **Plugin Architecture**: Extensible system for custom card processing logic
- **Dual Feedback Sources**: Reader feedback from both plugins and local configuration
- **Web Management Interface**: Modern Blazor Server UI with Blazorise components
- **Real-time Dashboard**: Live monitoring of reader status and card events
- **Priority-based Resolution**: Intelligent feedback resolution system
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
    Task<ReaderFeedback?> GetFeedbackAsync(CardReadResult result);
    
    Task InitializeAsync();
    Task ShutdownAsync();
}
```

### Feedback Resolution

ApBox supports dual feedback sources with priority-based resolution:

1. **Plugin Feedback** (Priority 100): Custom feedback from plugin logic
2. **Configuration Feedback** (Priority 50): Pre-configured responses
3. **Default Feedback**: Fallback success/failure responses

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
   
   Open your browser and navigate to `https://localhost:5001`

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
  "DefaultFeedback": {
    "Type": "Success",
    "BeepCount": 1,
    "LedColor": "Green",
    "LedDurationMs": 1000
  },
  "ResultFeedback": {
    "AccessDenied": {
      "Type": "Failure",
      "BeepCount": 3,
      "LedColor": "Red",
      "LedDurationMs": 2000
    }
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

- **Documentation**: [Wiki](https://github.com/your-org/ApBox/wiki)
- **Issues**: [GitHub Issues](https://github.com/your-org/ApBox/issues)
- **Discussions**: [GitHub Discussions](https://github.com/your-org/ApBox/discussions)

## Roadmap

### MVP (Weeks 1-8)
- [x] Core foundation and plugin system
- [x] Plugin interfaces and feedback resolution
- [ ] OSDP integration and communication
- [ ] Basic web interface with Blazorise
- [ ] Sample plugins and configuration
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