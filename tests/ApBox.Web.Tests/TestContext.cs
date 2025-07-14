using Bunit;
using Microsoft.Extensions.DependencyInjection;
using ApBox.Core.Services;
using ApBox.Core.Data.Repositories;
using ApBox.Core.Data.Models;
using ApBox.Plugins;
using Moq;
using Blazorise;
using Blazorise.Bootstrap5;
using Blazorise.Icons.FontAwesome;
using Blazorise.Tests.bUnit;

namespace ApBox.Web.Tests;

public class ApBoxTestContext : Bunit.TestContext
{
    public Mock<IReaderService> MockReaderService { get; private set; }
    public Mock<ICardProcessingService> MockCardProcessingService { get; private set; }
    public Mock<IPluginLoader> MockPluginLoader { get; private set; }
    public Mock<IReaderConfigurationService> MockReaderConfigurationService { get; private set; }
    public Mock<ICardEventRepository> MockCardEventRepository { get; private set; }

    public ApBoxTestContext()
    {
        // Create mocks
        MockReaderService = new Mock<IReaderService>();
        MockCardProcessingService = new Mock<ICardProcessingService>();
        MockPluginLoader = new Mock<IPluginLoader>();
        MockReaderConfigurationService = new Mock<IReaderConfigurationService>();
        MockCardEventRepository = new Mock<ICardEventRepository>();

        // Configure Blazorise for testing
        Services
            .AddBlazoriseTests()
            .AddBootstrap5Providers()
            .AddFontAwesomeIcons();

        // Register mocked services
        Services.AddSingleton(MockReaderService.Object);
        Services.AddSingleton(MockCardProcessingService.Object);
        Services.AddSingleton(MockPluginLoader.Object);
        Services.AddSingleton(MockReaderConfigurationService.Object);
        Services.AddSingleton(MockCardEventRepository.Object);

        // Add other required services
        Services.AddLogging();
    }

    public void ResetMocks()
    {
        MockReaderService.Reset();
        MockCardProcessingService.Reset();
        MockPluginLoader.Reset();
        MockReaderConfigurationService.Reset();
        MockCardEventRepository.Reset();
    }

    public void SetupDefaultMocks()
    {
        // Setup default behaviors for mocks
        MockReaderService.Setup(x => x.GetReadersAsync())
            .ReturnsAsync(GetDefaultReaderConfigurations());

        MockPluginLoader.Setup(x => x.LoadPluginsAsync())
            .ReturnsAsync(GetDefaultPlugins());

        MockCardProcessingService.Setup(x => x.ProcessCardReadAsync(It.IsAny<CardReadEvent>()))
            .ReturnsAsync(new CardReadResult { Success = true, Message = "Success" });

        MockCardEventRepository.Setup(x => x.GetRecentAsync(It.IsAny<int>()))
            .ReturnsAsync(GetDefaultCardEventEntities());

        MockCardEventRepository.Setup(x => x.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>()))
            .ReturnsAsync(GetDefaultCardEventEntities().Take(5));
    }

    private static IEnumerable<ReaderConfiguration> GetDefaultReaderConfigurations()
    {
        return new List<ReaderConfiguration>
        {
            new ReaderConfiguration
            {
                ReaderId = Guid.NewGuid(),
                ReaderName = "Test Reader 1"
            },
            new ReaderConfiguration
            {
                ReaderId = Guid.NewGuid(),
                ReaderName = "Test Reader 2"
            }
        };
    }

    private static IEnumerable<IApBoxPlugin> GetDefaultPlugins()
    {
        var mockPlugin = new Mock<IApBoxPlugin>();
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
}