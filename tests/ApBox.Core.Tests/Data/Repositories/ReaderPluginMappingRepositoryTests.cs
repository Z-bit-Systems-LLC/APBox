using Microsoft.Extensions.Logging;
using Moq;
using ApBox.Core.Data;
using ApBox.Core.Data.Models;
using ApBox.Core.Data.Repositories;
using ApBox.Core.Data.Migrations;
using ApBox.Core.Services.Infrastructure;
using Dapper;
using System.Data;

namespace ApBox.Core.Tests.Data.Repositories;

[TestFixture]
public class ReaderPluginMappingRepositoryTests
{
    private string _testConnectionString;
    private ApBoxDbContext _context;
    private IReaderPluginMappingRepository _repository;
    private Mock<ILogger<ApBoxDbContext>> _mockDbLogger;
    private Mock<ILogger<ReaderPluginMappingRepository>> _mockRepoLogger;
    private Mock<ILogger<MigrationRunner>> _mockMigrationLogger;
    private IFileSystem _fileSystem;
    private IDbConnection _persistConnection;

    [SetUp]
    public async Task Setup()
    {
        // Use a shared in-memory SQLite database
        _testConnectionString = $"Data Source=file:memdb{Guid.NewGuid():N}?mode=memory&cache=shared";

        // Create mocks
        _mockDbLogger = new Mock<ILogger<ApBoxDbContext>>();
        _mockRepoLogger = new Mock<ILogger<ReaderPluginMappingRepository>>();
        _mockMigrationLogger = new Mock<ILogger<MigrationRunner>>();
        _fileSystem = new FileSystem();

        // Create context
        _context = new ApBoxDbContext(_testConnectionString, _mockDbLogger.Object);
        
        // Create a shared connection to keep the database alive
        _persistConnection = _context.CreateDbConnectionAsync();
        _persistConnection.Open();
        
        // Run migrations to set up the database schema
        var migrationRunner = new MigrationRunner(_context, _fileSystem, _mockMigrationLogger.Object);
        await migrationRunner.RunMigrationsAsync();
        
        // Create repository
        _repository = new ReaderPluginMappingRepository(_context, _mockRepoLogger.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _persistConnection?.Dispose();
    }

    [Test]
    public async Task CreateMappingAsync_CreatesNewMapping()
    {
        // Arrange
        var readerId = Guid.NewGuid();
        var mapping = new ReaderPluginMappingEntity
        {
            ReaderId = readerId.ToString(),
            PluginId = "TestPlugin",
            ExecutionOrder = 1,
            IsEnabled = true
        };
        
        // Ensure reader exists first
        await EnsureReaderExists(readerId);

        // Act
        await _repository.CreateMappingAsync(mapping);

        // Assert
        using var connection = _context.CreateDbConnectionAsync();
        var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT * FROM reader_plugin_mappings WHERE reader_id = @ReaderId",
            new { ReaderId = mapping.ReaderId });
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result.plugin_id, Is.EqualTo(mapping.PluginId));
        Assert.That((int)result.execution_order, Is.EqualTo(mapping.ExecutionOrder));
        Assert.That((int)result.is_enabled, Is.EqualTo(1));
    }

    [Test]
    public async Task GetMappingsForReaderAsync_ReturnsOnlyReaderMappings()
    {
        // Arrange
        var readerId1 = Guid.NewGuid();
        var readerId2 = Guid.NewGuid();

        await InsertTestMapping(readerId1, "Plugin1", 1, true);
        await InsertTestMapping(readerId1, "Plugin2", 2, true);
        await InsertTestMapping(readerId2, "Plugin1", 1, true);

        // Act
        var result = await _repository.GetMappingsForReaderAsync(readerId1);

        // Assert
        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.All(m => m.ReaderId == readerId1.ToString()), Is.True);
    }

    [Test]
    public async Task GetMappingsForReaderAsync_ReturnsInExecutionOrder()
    {
        // Arrange
        var readerId = Guid.NewGuid();

        await InsertTestMapping(readerId, "Plugin3", 3, true);
        await InsertTestMapping(readerId, "Plugin1", 1, true);
        await InsertTestMapping(readerId, "Plugin2", 2, true);

        // Act
        var result = (await _repository.GetMappingsForReaderAsync(readerId)).ToList();

        // Assert
        Assert.That(result[0].PluginId, Is.EqualTo("Plugin1"));
        Assert.That(result[1].PluginId, Is.EqualTo("Plugin2"));
        Assert.That(result[2].PluginId, Is.EqualTo("Plugin3"));
    }

    [Test]
    public async Task GetMappingsForPluginAsync_ReturnsOnlyPluginMappings()
    {
        // Arrange
        var pluginId = "TestPlugin";

        await InsertTestMapping(Guid.NewGuid(), pluginId, 1, true);
        await InsertTestMapping(Guid.NewGuid(), pluginId, 1, true);
        await InsertTestMapping(Guid.NewGuid(), "OtherPlugin", 1, true);

        // Act
        var result = await _repository.GetMappingsForPluginAsync(pluginId);

        // Assert
        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.All(m => m.PluginId == pluginId), Is.True);
    }

    [Test]
    public async Task DeleteMappingsForReaderAsync_RemovesAllReaderMappings()
    {
        // Arrange
        var readerId = Guid.NewGuid();
        var otherReaderId = Guid.NewGuid();

        await InsertTestMapping(readerId, "Plugin1", 1, true);
        await InsertTestMapping(readerId, "Plugin2", 2, true);
        await InsertTestMapping(otherReaderId, "Plugin1", 1, true);

        // Act
        await _repository.DeleteMappingsForReaderAsync(readerId);

        // Assert
        using var connection = _context.CreateDbConnectionAsync();
        var remainingMappings = await connection.QueryAsync<dynamic>("SELECT * FROM reader_plugin_mappings");
        Assert.That(remainingMappings.Count(), Is.EqualTo(1));
        Assert.That(remainingMappings.First().reader_id, Is.EqualTo(otherReaderId.ToString()));
    }

    [Test]
    public async Task UpdateExecutionOrderAsync_UpdatesOrder()
    {
        // Arrange
        var readerId = Guid.NewGuid();
        var pluginId = "TestPlugin";

        await InsertTestMapping(readerId, pluginId, 1, true);

        // Act
        await _repository.UpdateExecutionOrderAsync(readerId, pluginId, 5);

        // Assert
        using var connection = _context.CreateDbConnectionAsync();
        var updated = await connection.QueryFirstAsync<dynamic>(
            "SELECT execution_order FROM reader_plugin_mappings WHERE reader_id = @ReaderId AND plugin_id = @PluginId",
            new { ReaderId = readerId.ToString(), PluginId = pluginId });
        Assert.That((int)updated.execution_order, Is.EqualTo(5));
    }

    [Test]
    public async Task SetPluginEnabledAsync_UpdatesEnabledStatus()
    {
        // Arrange
        var readerId = Guid.NewGuid();
        var pluginId = "TestPlugin";

        await InsertTestMapping(readerId, pluginId, 1, true);

        // Act
        await _repository.SetPluginEnabledAsync(readerId, pluginId, false);

        // Assert
        using var connection = _context.CreateDbConnectionAsync();
        var updated = await connection.QueryFirstAsync<dynamic>(
            "SELECT is_enabled FROM reader_plugin_mappings WHERE reader_id = @ReaderId AND plugin_id = @PluginId",
            new { ReaderId = readerId.ToString(), PluginId = pluginId });
        Assert.That((int)updated.is_enabled, Is.EqualTo(0));
    }

    [Test]
    public async Task GetAllMappingsAsync_ReturnsAllMappings()
    {
        // Arrange
        await InsertTestMapping(Guid.NewGuid(), "Plugin1", 1, true);
        await InsertTestMapping(Guid.NewGuid(), "Plugin2", 1, true);
        await InsertTestMapping(Guid.NewGuid(), "Plugin3", 1, false);

        // Act
        var result = await _repository.GetAllMappingsAsync();

        // Assert
        Assert.That(result.Count(), Is.EqualTo(3));
    }

    [Test]
    public async Task DeleteMappingAsync_RemovesSpecificMapping()
    {
        // Arrange
        var readerId = Guid.NewGuid();
        var pluginId = "TestPlugin";

        await InsertTestMapping(readerId, pluginId, 1, true);
        await InsertTestMapping(readerId, "OtherPlugin", 2, true);

        // Act
        await _repository.DeleteMappingAsync(readerId, pluginId);

        // Assert
        using var connection = _context.CreateDbConnectionAsync();
        var remaining = await connection.QueryAsync<dynamic>("SELECT * FROM reader_plugin_mappings WHERE reader_id = @ReaderId", 
            new { ReaderId = readerId.ToString() });
        Assert.That(remaining.Count(), Is.EqualTo(1));
        Assert.That(remaining.First().plugin_id, Is.EqualTo("OtherPlugin"));
    }

    [Test]
    public async Task ExistsAsync_ReturnsTrueWhenExists()
    {
        // Arrange
        var readerId = Guid.NewGuid();
        var pluginId = "TestPlugin";

        await InsertTestMapping(readerId, pluginId, 1, true);

        // Act
        var exists = await _repository.ExistsAsync(readerId, pluginId);

        // Assert
        Assert.That(exists, Is.True);
    }

    [Test]
    public async Task ExistsAsync_ReturnsFalseWhenNotExists()
    {
        // Arrange
        var readerId = Guid.NewGuid();
        var pluginId = "NonExistentPlugin";

        // Act
        var exists = await _repository.ExistsAsync(readerId, pluginId);

        // Assert
        Assert.That(exists, Is.False);
    }

    private async Task InsertTestMapping(Guid readerId, string pluginId, int executionOrder, bool isEnabled)
    {
        // First ensure the reader exists
        await EnsureReaderExists(readerId);
        
        using var connection = _context.CreateDbConnectionAsync();
        await connection.ExecuteAsync(@"
            INSERT INTO reader_plugin_mappings 
            (reader_id, plugin_id, execution_order, is_enabled, created_at, updated_at)
            VALUES 
            (@ReaderId, @PluginId, @ExecutionOrder, @IsEnabled, datetime('now'), datetime('now'))",
            new 
            { 
                ReaderId = readerId.ToString(), 
                PluginId = pluginId, 
                ExecutionOrder = executionOrder, 
                IsEnabled = isEnabled ? 1 : 0 
            });
    }
    
    private async Task EnsureReaderExists(Guid readerId)
    {
        using var connection = _context.CreateDbConnectionAsync();
        await connection.ExecuteAsync(@"
            INSERT OR IGNORE INTO reader_configurations 
            (reader_id, reader_name, address, is_enabled, created_at, updated_at)
            VALUES 
            (@ReaderId, @ReaderName, 1, 1, datetime('now'), datetime('now'))",
            new 
            { 
                ReaderId = readerId.ToString(), 
                ReaderName = $"Test Reader {readerId}" 
            });
    }
}