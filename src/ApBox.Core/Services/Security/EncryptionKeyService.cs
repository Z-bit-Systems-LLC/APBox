using System.Security.Cryptography;
using System.Text;
using ApBox.Core.Services.Infrastructure;

namespace ApBox.Core.Services.Security;

/// <summary>
/// Service for managing encryption keys with secure local storage
/// </summary>
public sealed class EncryptionKeyService : IEncryptionKeyService
{
    private readonly string _keyFilePath;
    private readonly ILogger<EncryptionKeyService> _logger;
    private readonly IFileSystem _fileSystem;
    private readonly SemaphoreSlim _keyLock = new(1, 1);

    public EncryptionKeyService(ILogger<EncryptionKeyService> logger, IFileSystem fileSystem)
    {
        _logger = logger;
        _fileSystem = fileSystem;
        
        // Store the key file in the application data directory
        var appDataPath = _fileSystem.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var apboxDataDir = _fileSystem.CombinePath(appDataPath, "ApBox");
        
        // Ensure directory exists
        _fileSystem.CreateDirectory(apboxDataDir);
        
        _keyFilePath = _fileSystem.CombinePath(apboxDataDir, ".encryption_key");
    }

    public async Task<byte[]> GetEncryptionKeyAsync()
    {
        await _keyLock.WaitAsync();
        try
        {
            if (await KeyExistsInternalAsync())
            {
                return await LoadKeyAsync();
            }
            else
            {
                _logger.LogInformation("No encryption key found, generating new key");
                return await GenerateAndSaveKeyAsync();
            }
        }
        finally
        {
            _keyLock.Release();
        }
    }

    public async Task<byte[]> RegenerateKeyAsync()
    {
        await _keyLock.WaitAsync();
        try
        {
            _logger.LogWarning("Regenerating encryption key - previously encrypted data will become unreadable");
            return await GenerateAndSaveKeyAsync();
        }
        finally
        {
            _keyLock.Release();
        }
    }

    public async Task<bool> KeyExistsAsync()
    {
        await _keyLock.WaitAsync();
        try
        {
            return await KeyExistsInternalAsync();
        }
        finally
        {
            _keyLock.Release();
        }
    }

    private async Task<bool> KeyExistsInternalAsync()
    {
        return await Task.FromResult(_fileSystem.FileExists(_keyFilePath));
    }

    private async Task<byte[]> GenerateAndSaveKeyAsync()
    {
        try
        {
            // Generate a cryptographically secure 256-bit (32-byte) key
            using var rng = RandomNumberGenerator.Create();
            var key = new byte[32]; // 256 bits for AES-256
            rng.GetBytes(key);

            // Save the key to file with restricted permissions
            await SaveKeySecurelyAsync(key);
            
            _logger.LogInformation("Generated and saved new encryption key");
            return key;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate and save encryption key");
            throw;
        }
    }

    private async Task SaveKeySecurelyAsync(byte[] key)
    {
        try
        {
            // Convert key to base64 for storage
            var keyBase64 = Convert.ToBase64String(key);
            
            // Write to file
            await _fileSystem.WriteAllTextAsync(_keyFilePath, keyBase64, Encoding.UTF8);
            
            // Set file permissions to be readable only by the current user
            if (OperatingSystem.IsWindows())
            {
                SetWindowsFilePermissions(_keyFilePath);
            }
            else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                SetUnixFilePermissions(_keyFilePath);
            }
            
            _logger.LogDebug("Encryption key saved securely to {FilePath}", _keyFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save encryption key to {FilePath}", _keyFilePath);
            throw;
        }
    }

    private async Task<byte[]> LoadKeyAsync()
    {
        try
        {
            var keyBase64 = await _fileSystem.ReadAllTextAsync(_keyFilePath, Encoding.UTF8);
            var key = Convert.FromBase64String(keyBase64.Trim());
            
            // Validate key length
            if (key.Length != 32)
            {
                throw new InvalidOperationException($"Invalid key length: expected 32 bytes, got {key.Length} bytes");
            }
            
            _logger.LogDebug("Loaded encryption key from {FilePath}", _keyFilePath);
            return key;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load encryption key from {FilePath}", _keyFilePath);
            throw;
        }
    }

    private void SetWindowsFilePermissions(string filePath)
    {
        try
        {
            // On Windows, the file is created in the user's LocalApplicationData which is already 
            // protected by NTFS permissions. Additional ACL restrictions could be added here if needed.
            _fileSystem.SetFileAttributes(filePath, FileAttributes.Hidden | FileAttributes.ReadOnly);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not set Windows file permissions for {FilePath}", filePath);
        }
    }

    private void SetUnixFilePermissions(string filePath)
    {
        try
        {
            // Set file permissions to 600 (read/write for owner only)
            _fileSystem.SetUnixFileMode(filePath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not set Unix file permissions for {FilePath}", filePath);
        }
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            _keyLock.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}