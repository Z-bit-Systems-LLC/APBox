using System.Data;
using ApBox.Core.Data;
using ApBox.Core.Data.Migrations;
using Microsoft.Extensions.Logging;
using Dapper;
using Moq;

namespace ApBox.Core.Tests;

[TestFixture]
[Category("Unit")]
public class MigrationTests
{
    private string _testConnectionString;
    private ILogger<ApBoxDbContext> _dbLogger;
    private ILogger<MigrationRunner> _migrationLogger;
    private ApBoxDbContext _dbContext;
    private IDbConnection _persistConnection;
    
    [SetUp]
    public void Setup()
    {
        // Use an in-memory SQLite database
        _testConnectionString = $"Data Source=file:memdb{Guid.NewGuid():N}?mode=memory&cache=shared";
        
        // Create mock loggers
        _dbLogger = new Mock<ILogger<ApBoxDbContext>>().Object;
        _migrationLogger = new Mock<ILogger<MigrationRunner>>().Object;
        
        _dbContext = new ApBoxDbContext(_testConnectionString, _dbLogger);
        
        // Create a shared connection for the migration runner to use
        _persistConnection = _dbContext.CreateDbConnectionAsync();
        _persistConnection.Open();
    }
    
    [TearDown]
    public void TearDown()
    {
        _persistConnection?.Close();
        _persistConnection?.Dispose();
    }
    
    [Test]
    public async Task MigrationRunner_CreatesSchemaVersionsTable()
    {
        // Arrange
        var migrationRunner = new MigrationRunner(_dbContext, _migrationLogger);
        
        // Act
        await migrationRunner.RunMigrationsAsync();
        
        // Assert - Check that the schema_migrations table exists using the shared connection
        var sql = "SELECT name FROM sqlite_master WHERE type='table' AND name='schema_migrations'";
        var result = await _persistConnection.QuerySingleOrDefaultAsync<string>(sql);
        
        Assert.That(result, Is.EqualTo("schema_migrations"));
    }
    
    [Test]
    public async Task MigrationRunner_ReturnsCorrectAppliedMigrations()
    {
        // Arrange
        var migrationRunner = new MigrationRunner(_dbContext, _migrationLogger);
        
        // Act
        await migrationRunner.RunMigrationsAsync();
        var appliedMigrations = await migrationRunner.GetAppliedMigrationsAsync();
        
        // Assert
        var migrations = appliedMigrations.ToArray();
        Assert.That(migrations, Is.Not.Null);
        Assert.That(migrations, Is.InstanceOf<IEnumerable<string>>());
    }
    
    [Test]
    public async Task MigrationRunner_FindsAvailableMigrations()
    {
        // Arrange
        var migrationRunner = new MigrationRunner(_dbContext, _migrationLogger);
        
        // Act
        var pendingMigrations = await migrationRunner.GetPendingMigrationsAsync();
        
        // Assert - Should find the existing migration file
        var migrations = pendingMigrations.ToArray();
        Assert.That(migrations, Is.Not.Null);
        Assert.That(migrations, Contains.Item("001"));
    }
}