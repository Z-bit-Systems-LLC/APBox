# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

ApBox is an industrial OSDP (Open Supervised Device Protocol) gateway with a plugin ecosystem. Currently in the initial planning phase with no code implementation yet.

**Technology Stack:**
- .NET 8
- Blazor Server with Blazorise UI
- SQLite database
- Docker deployment
- Target hardware: Raspberry Pi (dev) and Strato Pi CM Duo v3 (production)

## Development Setup Commands

Since the project hasn't been initialized yet, here are the commands to set up the development environment:

```bash
# Create solution and projects structure
dotnet new sln -n ApBox
dotnet new webapi -n ApBox.Core -o src/ApBox.Core
dotnet new blazorserver -n ApBox.Web -o src/ApBox.Web
dotnet new classlib -n ApBox.Plugins -o src/ApBox.Plugins
dotnet new nunit -n ApBox.Core.Tests -o tests/ApBox.Core.Tests
dotnet new nunit -n ApBox.Web.Tests -o tests/ApBox.Web.Tests
dotnet new nunit -n ApBox.Plugins.Tests -o tests/ApBox.Plugins.Tests

# Add projects to solution
dotnet sln add src/ApBox.Core/ApBox.Core.csproj
dotnet sln add src/ApBox.Web/ApBox.Web.csproj
dotnet sln add src/ApBox.Plugins/ApBox.Plugins.csproj
dotnet sln add tests/ApBox.Core.Tests/ApBox.Core.Tests.csproj
dotnet sln add tests/ApBox.Web.Tests/ApBox.Web.Tests.csproj
dotnet sln add tests/ApBox.Plugins.Tests/ApBox.Plugins.Tests.csproj

# Add Blazorise packages
dotnet add src/ApBox.Web package Blazorise.Bootstrap
dotnet add src/ApBox.Web package Blazorise.Icons.FontAwesome

# Add MVVM Toolkit for clean separation of concerns
dotnet add src/ApBox.Web package CommunityToolkit.Mvvm

# Add bUnit for Blazor component testing
dotnet add tests/ApBox.Web.Tests package bunit
dotnet add tests/ApBox.Web.Tests package bunit.web

# Add test project references
dotnet add tests/ApBox.Core.Tests reference src/ApBox.Core
dotnet add tests/ApBox.Web.Tests reference src/ApBox.Web
dotnet add tests/ApBox.Plugins.Tests reference src/ApBox.Plugins

# Build and test
dotnet build
dotnet test

# Run specific tests
dotnet test --filter "FullyQualifiedName~ApBox.Core.Tests"
dotnet test --filter "TestCategory=Unit"

# Run with coverage (requires dotnet-coverage tool)
dotnet tool install --global dotnet-coverage
dotnet-coverage collect "dotnet test" -f xml -o coverage.xml

# Run application
dotnet run --project src/ApBox.Web
```

## Development Practices

**Test-Driven Development (TDD)**
- Use NUnit as the testing framework
- Write tests first, then implementation
- Follow Red-Green-Refactor cycle
- Aim for high test coverage on business logic
- Use test categories: [Category("Unit")], [Category("Integration")]

**Testing Commands:**
```bash
# Watch mode for TDD workflow
dotnet watch test --project tests/ApBox.Core.Tests

# Run tests with detailed output
dotnet test -v normal

# Run only unit tests
dotnet test --filter "Category=Unit"
```

**MVVM Example Structure:**
```csharp
// ViewModel with MVVM Toolkit
[ObservableObject]
public partial class DashboardViewModel
{
    [ObservableProperty]
    private string _status;
    
    [ObservableProperty]
    private ObservableCollection<CardReadEvent> _recentReads;
    
    [RelayCommand]
    private async Task RefreshAsync()
    {
        // Business logic here
    }
}
```

**Blazor Component Testing with bUnit:**
```csharp
// Example bUnit test for Blazor components
[Test]
public void Dashboard_ShowsReaderStatus()
{
    // Arrange
    using var ctx = new TestContext();
    var viewModel = new DashboardViewModel();
    ctx.Services.AddSingleton(viewModel);
    
    // Act
    var component = ctx.RenderComponent<Dashboard>();
    
    // Assert
    Assert.That(component.Find(".reader-status").TextContent, Is.EqualTo("Connected"));
}
```

## Architecture

The planned architecture follows a plugin-based design with MVVM pattern:

- **ApBox.Core**: Main application hosting OSDP integration and plugin infrastructure
- **ApBox.Web**: Blazor Server web interface using Blazorise components with MVVM pattern
  - ViewModels: Using CommunityToolkit.Mvvm for observable properties and commands
  - Views: Blazor components bound to ViewModels
  - Services: Business logic separated from UI concerns
- **ApBox.Plugins**: Plugin interfaces and reference implementations
- **Plugin Interface**: Simple async interface for card read events and reader feedback

**MVVM Pattern Usage:**
- ViewModels inherit from `ObservableObject` or use `[ObservableObject]` partial class
- Properties use `[ObservableProperty]` for automatic INotifyPropertyChanged
- Commands use `[RelayCommand]` for automatic ICommand implementation
- Services are injected into ViewModels via DI

Core plugin interface:
```csharp
public interface IApBoxPlugin
{
    string Name { get; }
    Task<bool> ProcessCardReadAsync(CardReadEvent cardRead);
    Task<ReaderFeedback> GetFeedbackAsync(CardReadResult result);
}
```

## Key Implementation Details

1. **OSDP Integration**: Will use OSDP.Net library (to be copied from existing Aporta codebase)
2. **UI Framework**: Blazorise with Bootstrap theme for rapid development
3. **UI Testing**: bUnit for Blazor component testing (see Aporta project for examples)
4. **Real-time Updates**: SignalR for dashboard updates
5. **Database**: SQLite for configuration and event logging
6. **Deployment**: Docker containers for both development and production

## Reference Projects

- **Aporta** (https://github.com/bytedreamer/Aporta): Reference implementation for:
  - OSDP.Net library usage
  - bUnit testing patterns for Blazor components
  - Project structure and organization

## Project Plan Reference

The detailed implementation plan is in `apbox_project_plan.md` (excluded from git). Key MVP milestones:
- Weeks 1-2: Core foundation and OSDP integration
- Weeks 3-4: Basic web interface
- Weeks 5-6: Plugin system
- Weeks 7-8: MVP polish and testing

## Important Notes

- Project uses Eclipse Public License v2.0
- No code exists yet - this is a greenfield project
- Focus on MVP implementation following the 8-week sprint plan
- Hardware dependencies: ensure compatibility with both Raspberry Pi and Strato Pi CM Duo v3

## Testing Best Practices

- Use ElementId attribute for finding components in UI tests

## Development Memories

- Use Blazorise components to render HTML pages
- Use the SnackBar component to notifify user of operation status
- Use Validation component for forms