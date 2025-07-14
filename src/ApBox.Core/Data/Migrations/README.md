# ApBox Database Migrations

This directory contains SQL migration files for the ApBox database schema. The migration system automatically tracks and applies schema changes.

## How it Works

1. **Version Tracking**: A `schema_migrations` table tracks which migrations have been applied
2. **Sequential Application**: Migrations are applied in version order (001, 002, 003, etc.)
3. **Automatic Execution**: Migrations run automatically during application startup
4. **Transaction Safety**: Each migration runs in a transaction and can be rolled back on failure

## Migration File Format

Migration files must follow this naming convention:
```
{version}_{description}.sql
```

Examples:
- `001_initial_schema.sql`
- `002_add_user_permissions.sql`
- `003_update_card_events_index.sql`

Each migration file should include a description comment:
```sql
-- Migration 002: Add User Permissions
-- Description: Add user authentication and permission tables

CREATE TABLE users (
    id INTEGER PRIMARY KEY,
    username TEXT UNIQUE NOT NULL,
    -- ...
);
```

## Creating New Migrations

1. **Determine Next Version**: Look at existing migration files and increment the version number
2. **Create SQL File**: Write your schema changes as SQL statements
3. **Test Migration**: Use the PowerShell test script to verify your SQL is valid
4. **Commit Changes**: Add the migration file to source control

## Migration Commands

The migration system provides these interfaces:

```csharp
public interface IMigrationRunner
{
    Task RunMigrationsAsync();                    // Apply pending migrations
    Task<IEnumerable<string>> GetAppliedMigrationsAsync();   // List applied versions
    Task<IEnumerable<string>> GetPendingMigrationsAsync();   // List pending versions
}
```

## Testing Migrations

Use the PowerShell test script to validate migration SQL:
```powershell
.\tools\MigrationTest.ps1
```

This script:
- Creates a temporary test database
- Applies the migration SQL
- Verifies table creation
- Reports any SQL errors

## Best Practices

1. **Always Backwards Compatible**: Don't drop columns or tables that might be in use
2. **Use Transactions**: The migration system automatically wraps each migration in a transaction
3. **Include Indexes**: Add necessary indexes for performance in the same migration
4. **Test Thoroughly**: Test migrations on a copy of production data before deploying
5. **Small Changes**: Prefer many small migrations over large complex ones

## Future Enhancements

For production releases, consider:
- Migration rollback capabilities
- Schema validation checks
- Migration approval workflow
- Backup creation before migrations
- Migration timing and performance monitoring