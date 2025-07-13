using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace ApBox.Core.Data;

public class ApBoxDbContext : IApBoxDbContext
{
    private readonly string _connectionString;
    private readonly ILogger<ApBoxDbContext> _logger;

    public ApBoxDbContext(string connectionString, ILogger<ApBoxDbContext> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task<SqliteConnection> CreateConnectionAsync()
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }

    public async Task InitializeDatabaseAsync()
    {
        using var connection = await CreateConnectionAsync();
        
        _logger.LogInformation("Initializing ApBox database...");

        // Create tables
        await CreateTablesAsync(connection);
        
        _logger.LogInformation("Database initialization completed");
    }

    private async Task CreateTablesAsync(SqliteConnection connection)
    {
        var createTablesScript = @"
            -- Reader configurations table
            CREATE TABLE IF NOT EXISTS reader_configurations (
                reader_id TEXT PRIMARY KEY,
                reader_name TEXT NOT NULL,
                default_feedback_json TEXT,
                result_feedback_json TEXT,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL
            );

            -- Card events table
            CREATE TABLE IF NOT EXISTS card_events (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                reader_id TEXT NOT NULL,
                card_number TEXT NOT NULL,
                bit_length INTEGER NOT NULL,
                reader_name TEXT NOT NULL,
                success INTEGER NOT NULL,
                message TEXT,
                processed_by_plugin TEXT,
                timestamp TEXT NOT NULL
            );

            -- Plugin configurations table
            CREATE TABLE IF NOT EXISTS plugin_configurations (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                plugin_name TEXT NOT NULL,
                configuration_key TEXT NOT NULL,
                configuration_value TEXT NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                UNIQUE(plugin_name, configuration_key)
            );

            -- System logs table
            CREATE TABLE IF NOT EXISTS system_logs (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                level TEXT NOT NULL,
                logger TEXT NOT NULL,
                message TEXT NOT NULL,
                exception TEXT,
                properties TEXT,
                timestamp TEXT NOT NULL
            );

            -- Create indexes for better performance
            CREATE INDEX IF NOT EXISTS idx_card_events_reader_id ON card_events(reader_id);
            CREATE INDEX IF NOT EXISTS idx_card_events_timestamp ON card_events(timestamp);
            CREATE INDEX IF NOT EXISTS idx_card_events_card_number ON card_events(card_number);
            CREATE INDEX IF NOT EXISTS idx_plugin_configurations_plugin_name ON plugin_configurations(plugin_name);
            CREATE INDEX IF NOT EXISTS idx_system_logs_timestamp ON system_logs(timestamp);
            CREATE INDEX IF NOT EXISTS idx_system_logs_level ON system_logs(level);
        ";

        var command = new SqliteCommand(createTablesScript, connection);
        await command.ExecuteNonQueryAsync();
    }
}