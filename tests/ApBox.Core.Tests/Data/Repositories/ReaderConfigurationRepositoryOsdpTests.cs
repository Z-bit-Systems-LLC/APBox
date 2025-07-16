using System.Data;
using ApBox.Core.Data;
using ApBox.Core.Data.Migrations;
using ApBox.Core.Data.Repositories;
using ApBox.Core.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace ApBox.Core.Tests.Data.Repositories;

[TestFixture]
public class ReaderConfigurationRepositoryOsdpTests
{
    private ApBoxDbContext _dbContext = null!;
    private ReaderConfigurationRepository _repository = null!;
    private IDbConnection _persistConnection = null!;
    private string _testConnectionString = null!;

    [SetUp]
    public void SetUp()
    {
        // Use an in-memory SQLite database (following pattern from existing tests)
        _testConnectionString = $"Data Source=file:memdb{Guid.NewGuid():N}?mode=memory&cache=shared";
        
        var logger = new Mock<ILogger<ApBoxDbContext>>().Object;
        var migrationLogger = new Mock<ILogger<MigrationRunner>>().Object;
        var repoLogger = new Mock<ILogger<ReaderConfigurationRepository>>().Object;
        
        // Create migration runner with proper context
        var migrationContext = new ApBoxDbContext(_testConnectionString, logger);
        var migrationRunner = new MigrationRunner(migrationContext, migrationLogger);
        
        _dbContext = new ApBoxDbContext(_testConnectionString, logger, migrationRunner);
        _repository = new ReaderConfigurationRepository(_dbContext, repoLogger);
        
        // Create a shared connection to keep the in-memory database alive
        _persistConnection = _dbContext.CreateDbConnectionAsync();
        _persistConnection.Open();
        
        // Initialize the database
        _dbContext.InitializeDatabaseAsync().Wait();
    }

    [TearDown]
    public void TearDown()
    {
        // Close the persistent connection to clean up the in-memory database
        _persistConnection?.Close();
        _persistConnection?.Dispose();
    }

    [Test]
    public async Task CreateAsync_WithOsdpProperties_SavesCorrectly()
    {
        // Arrange
        var reader = new ReaderConfiguration
        {
            ReaderId = Guid.NewGuid(),
            ReaderName = "OSDP Test Reader",
            Address = 5,
            SerialPort = "COM3",
            BaudRate = 19200,
            SecurityMode = OsdpSecurityMode.Secure,
            SecureChannelKey = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 },
            IsEnabled = true
        };

        // Act
        var result = await _repository.CreateAsync(reader);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ReaderId, Is.EqualTo(reader.ReaderId));
        Assert.That(result.ReaderName, Is.EqualTo(reader.ReaderName));
        Assert.That(result.Address, Is.EqualTo(reader.Address));
        Assert.That(result.SerialPort, Is.EqualTo(reader.SerialPort));
        Assert.That(result.BaudRate, Is.EqualTo(reader.BaudRate));
        Assert.That(result.SecurityMode, Is.EqualTo(reader.SecurityMode));
        Assert.That(result.SecureChannelKey, Is.EqualTo(reader.SecureChannelKey));
        Assert.That(result.IsEnabled, Is.EqualTo(reader.IsEnabled));
    }

    [Test]
    public async Task GetByIdAsync_WithOsdpProperties_RetrievesCorrectly()
    {
        // Arrange
        var reader = new ReaderConfiguration
        {
            ReaderId = Guid.NewGuid(),
            ReaderName = "OSDP Test Reader",
            Address = 10,
            SerialPort = "/dev/ttyUSB0",
            BaudRate = 115200,
            SecurityMode = OsdpSecurityMode.Install,
            IsEnabled = false
        };

        await _repository.CreateAsync(reader);

        // Act
        var result = await _repository.GetByIdAsync(reader.ReaderId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.ReaderId, Is.EqualTo(reader.ReaderId));
        Assert.That(result.ReaderName, Is.EqualTo(reader.ReaderName));
        Assert.That(result.Address, Is.EqualTo(reader.Address));
        Assert.That(result.SerialPort, Is.EqualTo(reader.SerialPort));
        Assert.That(result.BaudRate, Is.EqualTo(reader.BaudRate));
        Assert.That(result.SecurityMode, Is.EqualTo(reader.SecurityMode));
        Assert.That(result.SecureChannelKey, Is.Null); // No key for Install mode
        Assert.That(result.IsEnabled, Is.EqualTo(reader.IsEnabled));
    }

    [Test]
    public async Task UpdateAsync_WithOsdpProperties_UpdatesCorrectly()
    {
        // Arrange
        var reader = new ReaderConfiguration
        {
            ReaderId = Guid.NewGuid(),
            ReaderName = "Original Reader",
            Address = 1,
            SerialPort = "COM1",
            BaudRate = 9600,
            SecurityMode = OsdpSecurityMode.ClearText,
            IsEnabled = true
        };

        await _repository.CreateAsync(reader);

        // Modify OSDP properties
        reader.ReaderName = "Updated Reader";
        reader.Address = 7;
        reader.SerialPort = "COM5";
        reader.BaudRate = 57600;
        reader.SecurityMode = OsdpSecurityMode.Secure;
        reader.SecureChannelKey = new byte[] { 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 };
        reader.IsEnabled = false;

        // Act
        var result = await _repository.UpdateAsync(reader);

        // Assert
        Assert.That(result, Is.Not.Null);
        
        // Verify by retrieving
        var retrieved = await _repository.GetByIdAsync(reader.ReaderId);
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.ReaderName, Is.EqualTo("Updated Reader"));
        Assert.That(retrieved.Address, Is.EqualTo(7));
        Assert.That(retrieved.SerialPort, Is.EqualTo("COM5"));
        Assert.That(retrieved.BaudRate, Is.EqualTo(57600));
        Assert.That(retrieved.SecurityMode, Is.EqualTo(OsdpSecurityMode.Secure));
        Assert.That(retrieved.SecureChannelKey, Is.EqualTo(reader.SecureChannelKey));
        Assert.That(retrieved.IsEnabled, Is.False);
    }

    [Test]
    public async Task GetAllAsync_WithMultipleOsdpReaders_RetrievesAll()
    {
        // Arrange
        var reader1 = new ReaderConfiguration
        {
            ReaderId = Guid.NewGuid(),
            ReaderName = "Reader 1",
            Address = 1,
            SerialPort = "COM1",
            BaudRate = 9600,
            SecurityMode = OsdpSecurityMode.ClearText,
        };

        var reader2 = new ReaderConfiguration
        {
            ReaderId = Guid.NewGuid(),
            ReaderName = "Reader 2",
            Address = 2,
            SerialPort = "COM2",
            BaudRate = 19200,
            SecurityMode = OsdpSecurityMode.Install,
        };

        var reader3 = new ReaderConfiguration
        {
            ReaderId = Guid.NewGuid(),
            ReaderName = "Reader 3",
            Address = 3,
            SerialPort = "/dev/ttyUSB0",
            BaudRate = 115200,
            SecurityMode = OsdpSecurityMode.Secure,
            SecureChannelKey = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 },
        };

        await _repository.CreateAsync(reader1);
        await _repository.CreateAsync(reader2);
        await _repository.CreateAsync(reader3);

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.That(result.Count(), Is.EqualTo(3));
        
        var resultList = result.ToList();
        Assert.That(resultList.Any(r => r.ReaderId == reader1.ReaderId), Is.True);
        Assert.That(resultList.Any(r => r.ReaderId == reader2.ReaderId), Is.True);
        Assert.That(resultList.Any(r => r.ReaderId == reader3.ReaderId), Is.True);
        
        // Verify OSDP properties are preserved
        var retrievedReader3 = resultList.First(r => r.ReaderId == reader3.ReaderId);
        Assert.That(retrievedReader3.SerialPort, Is.EqualTo("/dev/ttyUSB0"));
        Assert.That(retrievedReader3.BaudRate, Is.EqualTo(115200));
        Assert.That(retrievedReader3.SecurityMode, Is.EqualTo(OsdpSecurityMode.Secure));
        Assert.That(retrievedReader3.SecureChannelKey, Is.EqualTo(reader3.SecureChannelKey));
    }

    [Test]
    public async Task CreateAndRetrieve_WithNullSerialPort_HandlesCorrectly()
    {
        // Arrange
        var reader = new ReaderConfiguration
        {
            ReaderId = Guid.NewGuid(),
            ReaderName = "No Serial Port Reader",
            Address = 1,
            SerialPort = null, // Null serial port
            BaudRate = 9600,
            SecurityMode = OsdpSecurityMode.ClearText,
        };

        // Act
        await _repository.CreateAsync(reader);
        var result = await _repository.GetByIdAsync(reader.ReaderId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.SerialPort, Is.Null);
    }
}