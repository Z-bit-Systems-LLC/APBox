# PowerShell script to test database migrations

Write-Host "Testing ApBox Database Migrations..." -ForegroundColor Green

# Test database path
$testDbPath = Join-Path $env:TEMP "migration_test.db"

# Remove existing test database
if (Test-Path $testDbPath) {
    Remove-Item $testDbPath -Force
    Write-Host "Removed existing test database" -ForegroundColor Yellow
}

# Create test connection string
$connectionString = "Data Source=$testDbPath"

Write-Host "Creating test database at: $testDbPath" -ForegroundColor Cyan

# Test the initial migration SQL directly
$migrationFile = Join-Path $PSScriptRoot "..\src\ApBox.Core\Data\Migrations\001_initial_schema.sql"

if (-not (Test-Path $migrationFile)) {
    Write-Host "Migration file not found: $migrationFile" -ForegroundColor Red
    exit 1
}

Write-Host "Testing migration file: $migrationFile" -ForegroundColor Cyan

# Read and execute the migration SQL
try {
    Add-Type -AssemblyName System.Data.SQLite
    $connection = New-Object System.Data.SQLite.SQLiteConnection($connectionString)
    $connection.Open()
    
    $migrationSql = Get-Content $migrationFile -Raw
    $command = $connection.CreateCommand()
    $command.CommandText = $migrationSql
    $command.ExecuteNonQuery() | Out-Null
    
    Write-Host "✓ Migration executed successfully" -ForegroundColor Green
    
    # Verify tables were created
    $command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name"
    $reader = $command.ExecuteReader()
    
    Write-Host "Created tables:" -ForegroundColor Cyan
    while ($reader.Read()) {
        Write-Host "  - $($reader["name"])" -ForegroundColor White
    }
    $reader.Close()
    
    $connection.Close()
    Write-Host "✓ Database migration test completed successfully" -ForegroundColor Green
}
catch {
    Write-Host "✗ Migration test failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
finally {
    if ($connection -and $connection.State -eq 'Open') {
        $connection.Close()
    }
    
    # Clean up test database
    if (Test-Path $testDbPath) {
        Remove-Item $testDbPath -Force
        Write-Host "Cleaned up test database" -ForegroundColor Yellow
    }
}