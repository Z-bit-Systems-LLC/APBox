using System.Data;

namespace ApBox.Core.Data;

public interface IApBoxDbContext
{
    IDbConnection CreateDbConnectionAsync();
    
    Task InitializeDatabaseAsync();
}