using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using ApBox.Core.Services;
using ApBox.Core.Data.Repositories;
using ApBox.Core.Data.Models;
using ApBox.Core.Models;
using ApBox.Plugins;
using ApBox.Web.Services;
using ApBox.Web.ViewModels;
using Moq;
using Blazorise;
using Blazorise.Bootstrap5;
using Blazorise.Icons.FontAwesome;
using Blazorise.Tests.bUnit;
using Blazorise.Modules;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace ApBox.Web.Tests;

public class ApBoxTestContext : Bunit.TestContext
{
    public Mock<IReaderService> MockReaderService { get; private set; }
    public Mock<ICardProcessingService> MockCardProcessingService { get; private set; }
    public Mock<IPluginLoader> MockPluginLoader { get; private set; }
    public Mock<IReaderConfigurationService> MockReaderConfigurationService { get; private set; }
    public Mock<ICardEventRepository> MockCardEventRepository { get; private set; }
    public Mock<IFeedbackConfigurationService> MockFeedbackConfigurationService { get; private set; }
    public Mock<ILogService> MockLogService { get; private set; }
    public Mock<IConfigurationExportService> MockConfigurationExportService { get; private set; }
    public Mock<ISystemRestartService> MockSystemRestartService { get; private set; }
    public Mock<ICardEventPersistenceService> MockCardEventPersistenceService { get; private set; }
    public Mock<ICardProcessingOrchestrator> MockCardProcessingOrchestrator { get; private set; }
    public Mock<IHubConnectionWrapper> MockHubConnectionWrapper { get; private set; }
    public Mock<IReaderPluginMappingService> MockReaderPluginMappingService { get; private set; }
    public Mock<ISerialPortService> MockSerialPortService { get; private set; }

    public ApBoxTestContext()
    {
        // Create mocks
        MockReaderService = new Mock<IReaderService>();
        MockCardProcessingService = new Mock<ICardProcessingService>();
        MockPluginLoader = new Mock<IPluginLoader>();
        MockReaderConfigurationService = new Mock<IReaderConfigurationService>();
        MockCardEventRepository = new Mock<ICardEventRepository>();
        MockFeedbackConfigurationService = new Mock<IFeedbackConfigurationService>();
        MockLogService = new Mock<ILogService>();
        MockConfigurationExportService = new Mock<IConfigurationExportService>();
        MockSystemRestartService = new Mock<ISystemRestartService>();
        MockCardEventPersistenceService = new Mock<ICardEventPersistenceService>();
        MockCardProcessingOrchestrator = new Mock<ICardProcessingOrchestrator>();
        MockHubConnectionWrapper = new Mock<IHubConnectionWrapper>();
        MockReaderPluginMappingService = new Mock<IReaderPluginMappingService>();
        MockSerialPortService = new Mock<ISerialPortService>();
        
        // Setup hub connection wrapper to return disconnected state by default
        MockHubConnectionWrapper.Setup(x => x.State).Returns(HubConnectionState.Disconnected);
        MockHubConnectionWrapper.Setup(x => x.StartAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        MockHubConnectionWrapper.Setup(x => x.StopAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        MockHubConnectionWrapper.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        
        // Setup event handler registration to return disposable
        MockHubConnectionWrapper.Setup(x => x.On<It.IsAnyType>(It.IsAny<string>(), It.IsAny<Func<It.IsAnyType, Task>>()))
            .Returns(Mock.Of<IDisposable>());
        MockHubConnectionWrapper.Setup(x => x.On<It.IsAnyType, It.IsAnyType>(It.IsAny<string>(), It.IsAny<Func<It.IsAnyType, It.IsAnyType, Task>>()))
            .Returns(Mock.Of<IDisposable>());
        
        // Setup serial port service
        MockSerialPortService.Setup(x => x.GetAvailablePortNames()).Returns(new[] { "COM1", "COM2", "COM3" });

        // Configure Blazorise for testing
        Services
            .AddBlazoriseTests()
            .AddBootstrap5Providers()
            .AddFontAwesomeIcons();
            
        // Add mock FilePicker module for testing
        Services.AddTransient<IJSFilePickerModule, MockFilePickerModule>();
        Services.AddTransient<IJSFileModule, MockFileModule>();

        // Register mocked services
        Services.AddSingleton(MockReaderService.Object);
        Services.AddSingleton(MockCardProcessingService.Object);
        Services.AddSingleton(MockPluginLoader.Object);
        Services.AddSingleton(MockReaderConfigurationService.Object);
        Services.AddSingleton(MockCardEventRepository.Object);
        Services.AddSingleton(MockFeedbackConfigurationService.Object);
        Services.AddSingleton(MockLogService.Object);
        Services.AddSingleton(MockConfigurationExportService.Object);
        Services.AddSingleton(MockSystemRestartService.Object);
        Services.AddSingleton(MockCardEventPersistenceService.Object);
        Services.AddSingleton(MockCardProcessingOrchestrator.Object);
        Services.AddScoped<IHubConnectionWrapper>(_ => MockHubConnectionWrapper.Object);
        Services.AddSingleton(MockReaderPluginMappingService.Object);
        Services.AddSingleton(MockSerialPortService.Object);
        
        // Register ViewModels
        Services.AddScoped<DashboardViewModel>();
        Services.AddScoped<ReadersConfigurationViewModel>();
        Services.AddScoped<FeedbackConfigurationViewModel>();
        Services.AddScoped<PluginsConfigurationViewModel>();
        Services.AddScoped<SystemConfigurationViewModel>();

        // Add other required services
        Services.AddLogging();
        
        // Add mock configuration
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PluginSettings:Directory"] = "plugins/"
        });
        Services.AddSingleton<IConfiguration>(configurationBuilder.Build());
    }

    public void ResetMocks()
    {
        MockReaderService.Reset();
        MockCardProcessingService.Reset();
        MockPluginLoader.Reset();
        MockReaderConfigurationService.Reset();
        MockCardEventRepository.Reset();
        MockFeedbackConfigurationService.Reset();
        MockLogService.Reset();
        MockConfigurationExportService.Reset();
        MockSystemRestartService.Reset();
        MockReaderPluginMappingService.Reset();
        MockSerialPortService.Reset();
        MockHubConnectionWrapper.Reset();
        
        // Re-setup default behaviors after reset
        MockHubConnectionWrapper.Setup(x => x.State).Returns(HubConnectionState.Disconnected);
        MockHubConnectionWrapper.Setup(x => x.StartAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        MockHubConnectionWrapper.Setup(x => x.StopAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        MockHubConnectionWrapper.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        MockHubConnectionWrapper.Setup(x => x.On<It.IsAnyType>(It.IsAny<string>(), It.IsAny<Func<It.IsAnyType, Task>>()))
            .Returns(Mock.Of<IDisposable>());
        MockHubConnectionWrapper.Setup(x => x.On<It.IsAnyType, It.IsAnyType>(It.IsAny<string>(), It.IsAny<Func<It.IsAnyType, It.IsAnyType, Task>>()))
            .Returns(Mock.Of<IDisposable>());
            
        MockSerialPortService.Setup(x => x.GetAvailablePortNames()).Returns(new[] { "COM1", "COM2", "COM3" });
    }

    public void SetupDefaultMocks()
    {
        // Setup default behaviors for mocks
        MockReaderService.Setup(x => x.GetReadersAsync())
            .ReturnsAsync(GetDefaultReaderConfigurations());
            
        MockReaderService.Setup(x => x.GetAllReaderStatusesAsync())
            .ReturnsAsync(GetDefaultReaderStatuses());

        MockPluginLoader.Setup(x => x.LoadPluginsAsync())
            .ReturnsAsync(GetDefaultPlugins());

        MockCardProcessingService.Setup(x => x.ProcessCardReadAsync(It.IsAny<CardReadEvent>()))
            .ReturnsAsync(new CardReadResult { Success = true, Message = "Success" });

        MockCardEventRepository.Setup(x => x.GetRecentAsync(It.IsAny<int>()))
            .ReturnsAsync(GetDefaultCardEventEntities());

        MockCardEventRepository.Setup(x => x.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>()))
            .ReturnsAsync(GetDefaultCardEventEntities().Take(5));

        MockReaderConfigurationService.Setup(x => x.GetAllReadersAsync())
            .ReturnsAsync(GetDefaultReaderConfigurations());

        MockReaderConfigurationService.Setup(x => x.SaveReaderAsync(It.IsAny<ReaderConfiguration>()))
            .Returns(Task.CompletedTask);

        MockReaderConfigurationService.Setup(x => x.DeleteReaderAsync(It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        MockFeedbackConfigurationService.Setup(x => x.GetDefaultConfigurationAsync())
            .ReturnsAsync(GetDefaultFeedbackConfiguration());

        MockFeedbackConfigurationService.Setup(x => x.SaveDefaultConfigurationAsync(It.IsAny<FeedbackConfiguration>()))
            .Returns(Task.CompletedTask);

        MockLogService.Setup(x => x.GetRecentLogsAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<LogEntry>());

        MockConfigurationExportService.Setup(x => x.ExportConfigurationAsync())
            .ReturnsAsync(new ConfigurationExport());

        MockSystemRestartService.Setup(x => x.CanRestartAsync())
            .ReturnsAsync(true);
        
        MockSystemRestartService.Setup(x => x.PrepareRestartAsync())
            .Returns(Task.CompletedTask);
            
        MockSystemRestartService.Setup(x => x.RestartApplicationAsync())
            .Returns(Task.CompletedTask);
            
        MockSystemRestartService.Setup(x => x.GetEstimatedRestartTimeAsync())
            .ReturnsAsync(TimeSpan.FromSeconds(5));
            
        MockSystemRestartService.Setup(x => x.IsRestartInProgress)
            .Returns(false);
    }

    private static readonly Guid Reader1Id = Guid.NewGuid();
    private static readonly Guid Reader2Id = Guid.NewGuid();

    private static IEnumerable<ReaderConfiguration> GetDefaultReaderConfigurations()
    {
        return new List<ReaderConfiguration>
        {
            new ReaderConfiguration
            {
                ReaderId = Reader1Id,
                ReaderName = "Test Reader 1"
            },
            new ReaderConfiguration
            {
                ReaderId = Reader2Id,
                ReaderName = "Test Reader 2"
            }
        };
    }

    private static Dictionary<Guid, bool> GetDefaultReaderStatuses()
    {
        return new Dictionary<Guid, bool>
        {
            { Reader1Id, true },  // First reader online
            { Reader2Id, true }   // Second reader online
        };
    }

    private static IEnumerable<IApBoxPlugin> GetDefaultPlugins()
    {
        var mockPlugin = new Mock<IApBoxPlugin>();
        mockPlugin.Setup(x => x.Id).Returns(new Guid("F6A7B8C9-ABCD-EF01-2345-123456789999"));
        mockPlugin.Setup(x => x.Name).Returns("Test Plugin");
        mockPlugin.Setup(x => x.Version).Returns("1.0.0");
        mockPlugin.Setup(x => x.Description).Returns("Test plugin for unit tests");

        return new List<IApBoxPlugin> { mockPlugin.Object };
    }

    private static IEnumerable<CardEventEntity> GetDefaultCardEventEntities()
    {
        var now = DateTime.Now;
        return new List<CardEventEntity>
        {
            new CardEventEntity
            {
                Id = 1,
                ReaderId = Guid.NewGuid().ToString(),
                CardNumber = "123456789",
                BitLength = 26,
                ReaderName = "Test Reader 1",
                Success = true,
                Message = "Success",
                ProcessedByPlugin = "Test Plugin",
                Timestamp = now.AddMinutes(-5)
            },
            new CardEventEntity
            {
                Id = 2,
                ReaderId = Guid.NewGuid().ToString(),
                CardNumber = "987654321",
                BitLength = 26,
                ReaderName = "Test Reader 2",
                Success = true,
                Message = "Success",
                ProcessedByPlugin = "Test Plugin",
                Timestamp = now.AddMinutes(-10)
            },
            new CardEventEntity
            {
                Id = 3,
                ReaderId = Guid.NewGuid().ToString(),
                CardNumber = "456789123",
                BitLength = 26,
                ReaderName = "Test Reader 1",
                Success = true,
                Message = "Success",
                ProcessedByPlugin = "Test Plugin",
                Timestamp = now.AddMinutes(-15)
            }
        };
    }

    private static FeedbackConfiguration GetDefaultFeedbackConfiguration()
    {
        return new FeedbackConfiguration
        {
            SuccessFeedback = new ReaderFeedback
            {
                Type = ReaderFeedbackType.Success,
                LedColor = LedColor.Green,
                LedDuration = 1000,
                BeepCount = 1,
                DisplayMessage = "ACCESS GRANTED"
            },
            FailureFeedback = new ReaderFeedback
            {
                Type = ReaderFeedbackType.Failure,
                LedColor = LedColor.Red,
                LedDuration = 2000,
                BeepCount = 3,
                DisplayMessage = "ACCESS DENIED"
            },
            IdleState = new IdleStateFeedback
            {
                PermanentLedColor = LedColor.Blue,
                HeartbeatFlashColor = LedColor.Green
            }
        };
    }
}

/// <summary>
/// Mock implementation of IJSFilePickerModule for testing
/// </summary>
public class MockFilePickerModule : IJSFilePickerModule
{
    public string ModuleFileName => "filePickerMock.js";
    
    private IJSObjectReference? _module;
    
    public Task<IJSObjectReference> Module => _module != null ? Task.FromResult(_module) : Task.FromResult<IJSObjectReference>(null!);
    
    public ValueTask<IJSObjectReference> Initialize(IJSRuntime jsRuntime, CancellationToken cancellationToken = default)
    {
        var mockModule = new Mock<IJSObjectReference>();
        _module = mockModule.Object;
        return ValueTask.FromResult(mockModule.Object);
    }
    
    public ValueTask Initialize(ElementReference elementReference, string elementId)
    {
        return ValueTask.CompletedTask;
    }
    
    public ValueTask Destroy(ElementReference elementReference, string elementId)
    {
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Mock implementation of IJSFileModule for testing
/// </summary>
public class MockFileModule : IJSFileModule
{
    public string ModuleFileName => "fileMock.js";
    
    private IJSObjectReference? _module;
    
    public Task<IJSObjectReference> Module => _module != null ? Task.FromResult(_module) : Task.FromResult<IJSObjectReference>(null!);
    
    public ValueTask<IJSObjectReference> Initialize(IJSRuntime jsRuntime, CancellationToken cancellationToken = default)
    {
        var mockModule = new Mock<IJSObjectReference>();
        _module = mockModule.Object;
        return ValueTask.FromResult(mockModule.Object);
    }
    
    public ValueTask Initialize(ElementReference elementReference, string elementId)
    {
        return ValueTask.CompletedTask;
    }
    
    public ValueTask Destroy(ElementReference elementReference, string elementId)
    {
        return ValueTask.CompletedTask;
    }
    
    public ValueTask<IJSStreamReference> ReadDataAsync(ElementReference elementReference, int fileId, CancellationToken cancellationToken = default)
    {
        var mockStream = new Mock<IJSStreamReference>();
        return ValueTask.FromResult(mockStream.Object);
    }
    
    public ValueTask<byte[]> ReadDataAsync(ElementReference elementReference, int fileId, long position, long length, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(Array.Empty<byte>());
    }
    
    public ValueTask RemoveFileEntry(ElementReference elementReference, int fileId)
    {
        return ValueTask.CompletedTask;
    }
}