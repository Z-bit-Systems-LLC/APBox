using Microsoft.Data.Sqlite;

namespace ApBox.Core.Data;

public interface IApBoxDbContext
{
    Task<SqliteConnection> CreateConnectionAsync();
    Task InitializeDatabaseAsync();
}