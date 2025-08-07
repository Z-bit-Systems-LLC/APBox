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
public class PinEventRepositoryTests
{
    private string _testConnectionString;
    private ApBoxDbContext _context;
    private IPinEventRepository _repository;
    private Mock<ILogger<ApBoxDbContext>> _mockDbLogger;
    private Mock<ILogger<PinEventRepository>> _mockRepoLogger;
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
        _mockRepoLogger = new Mock<ILogger<PinEventRepository>>();
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
        _repository = new PinEventRepository(_context, _mockRepoLogger.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _persistConnection?.Dispose();
    }

    [Test]
    public async Task CreatePinEventAsync_CreatesNewPinEvent()
    {
        // Arrange
        var readerId = Guid.NewGuid();
        await EnsureReaderExists(readerId);
        
        var pinEvent = new PinEventEntity
        {
            ReaderId = readerId.ToString(),
            ReaderName = "Test Reader",
            EncryptedPin = "encrypted_pin_data",
            PinLength = 4,
            CompletionReason = 1, // PoundKey
            Success = true,
            Message = "PIN accepted",
            ProcessedByPlugin = "TestPlugin",
            Timestamp = DateTime.UtcNow
        };

        // Act
        await _repository.CreatePinEventAsync(pinEvent);

        // Assert
        using var connection = _context.CreateDbConnectionAsync();
        var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT * FROM pin_events WHERE reader_id = @ReaderId",
            new { ReaderId = pinEvent.ReaderId });
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result.reader_name, Is.EqualTo(pinEvent.ReaderName));
        Assert.That(result.encrypted_pin, Is.EqualTo(pinEvent.EncryptedPin));
        Assert.That((int)result.pin_length, Is.EqualTo(pinEvent.PinLength));
        Assert.That((int)result.completion_reason, Is.EqualTo(pinEvent.CompletionReason));
        Assert.That((int)result.success, Is.EqualTo(1));
        Assert.That(result.message, Is.EqualTo(pinEvent.Message));
        Assert.That(result.processed_by_plugin, Is.EqualTo(pinEvent.ProcessedByPlugin));
    }

    [Test]
    public async Task GetPinEventsForReaderAsync_ReturnsOnlyReaderEvents()
    {
        // Arrange
        var readerId1 = Guid.NewGuid();
        var readerId2 = Guid.NewGuid();

        await InsertTestPinEvent(readerId1, "encrypted1", true);
        await InsertTestPinEvent(readerId1, "encrypted2", false);
        await InsertTestPinEvent(readerId2, "encrypted3", true);

        // Act
        var result = await _repository.GetPinEventsForReaderAsync(readerId1);

        // Assert
        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.All(e => e.ReaderId == readerId1.ToString()), Is.True);
    }

    [Test]
    public async Task GetPinEventsForReaderAsync_RespectsLimit()
    {
        // Arrange
        var readerId = Guid.NewGuid();

        for (int i = 0; i < 5; i++)
        {
            await InsertTestPinEvent(readerId, $"encrypted{i}", true);
        }

        // Act
        var result = await _repository.GetPinEventsForReaderAsync(readerId, limit: 3);

        // Assert
        Assert.That(result.Count(), Is.EqualTo(3));
    }

    [Test]
    public async Task GetPinEventsByDateRangeAsync_ReturnsEventsInRange()
    {
        // Arrange
        var readerId = Guid.NewGuid();
        var startDate = DateTime.UtcNow.AddDays(-2);
        var endDate = DateTime.UtcNow.AddDays(-1);
        
        await InsertTestPinEvent(readerId, "old_event", true, DateTime.UtcNow.AddDays(-3));
        await InsertTestPinEvent(readerId, "in_range", true, DateTime.UtcNow.AddDays(-1.5));
        await InsertTestPinEvent(readerId, "new_event", true, DateTime.UtcNow);

        // Act
        var result = await _repository.GetPinEventsByDateRangeAsync(startDate, endDate);

        // Assert
        Assert.That(result.Count(), Is.EqualTo(1));
        Assert.That(result.First().EncryptedPin, Is.EqualTo("in_range"));
    }

    [Test]
    public async Task GetRecentPinEventsAsync_ReturnsRecentEvents()
    {
        // Arrange
        var readerId = Guid.NewGuid();

        await InsertTestPinEvent(readerId, "old", true, DateTime.UtcNow.AddHours(-2));
        await InsertTestPinEvent(readerId, "recent1", true, DateTime.UtcNow.AddMinutes(-10));
        await InsertTestPinEvent(readerId, "recent2", true, DateTime.UtcNow.AddMinutes(-5));

        // Act
        var result = await _repository.GetRecentPinEventsAsync(limit: 2);

        // Assert
        Assert.That(result.Count(), Is.EqualTo(2));
        // Should be ordered by timestamp descending (most recent first)
        var resultList = result.ToList();
        Assert.That(resultList[0].EncryptedPin, Is.EqualTo("recent2"));
        Assert.That(resultList[1].EncryptedPin, Is.EqualTo("recent1"));
    }

    [Test]
    public async Task GetPinEventCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        var readerId = Guid.NewGuid();
        var startDate = DateTime.UtcNow.AddDays(-1);
        var endDate = DateTime.UtcNow.AddDays(1);

        await InsertTestPinEvent(readerId, "event1", true, DateTime.UtcNow.AddHours(-12));
        await InsertTestPinEvent(readerId, "event2", false, DateTime.UtcNow.AddHours(-6));
        await InsertTestPinEvent(Guid.NewGuid(), "other_reader", true, DateTime.UtcNow.AddHours(-3));

        // Act
        var count = await _repository.GetPinEventCountAsync(readerId, startDate, endDate);

        // Assert
        Assert.That(count, Is.EqualTo(2));
    }

    [Test]
    public async Task DeleteOldPinEventsAsync_DeletesOldEvents()
    {
        // Arrange
        var readerId = Guid.NewGuid();
        var cutoffDate = DateTime.UtcNow.AddDays(-1);

        await InsertTestPinEvent(readerId, "old1", true, DateTime.UtcNow.AddDays(-2));
        await InsertTestPinEvent(readerId, "old2", true, DateTime.UtcNow.AddDays(-1.5));
        await InsertTestPinEvent(readerId, "recent", true, DateTime.UtcNow.AddHours(-12));

        // Act
        await _repository.DeleteOldPinEventsAsync(cutoffDate);

        // Assert
        using var connection = _context.CreateDbConnectionAsync();
        var remaining = await connection.QueryAsync<dynamic>("SELECT * FROM pin_events");
        Assert.That(remaining.Count(), Is.EqualTo(1));
        Assert.That(remaining.First().encrypted_pin, Is.EqualTo("recent"));
    }

    private async Task InsertTestPinEvent(Guid readerId, string encryptedPin, bool success, DateTime? timestamp = null)
    {
        // First ensure the reader exists
        await EnsureReaderExists(readerId);
        
        var eventTimestamp = timestamp ?? DateTime.UtcNow;
        
        using var connection = _context.CreateDbConnectionAsync();
        await connection.ExecuteAsync(@"
            INSERT INTO pin_events 
            (reader_id, reader_name, encrypted_pin, pin_length, completion_reason, success, message, processed_by_plugin, timestamp)
            VALUES 
            (@ReaderId, @ReaderName, @EncryptedPin, @PinLength, @CompletionReason, @Success, @Message, @ProcessedByPlugin, @Timestamp)",
            new 
            { 
                ReaderId = readerId.ToString(),
                ReaderName = $"Test Reader {readerId}",
                EncryptedPin = encryptedPin,
                PinLength = 4,
                CompletionReason = 1, // PoundKey
                Success = success ? 1 : 0,
                Message = success ? "PIN accepted" : "PIN rejected",
                ProcessedByPlugin = "TestPlugin",
                Timestamp = eventTimestamp
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