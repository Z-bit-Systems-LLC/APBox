namespace ApBox.Core.Services.Security;

/// <summary>
/// Service for managing encryption keys with secure storage
/// </summary>
public interface IEncryptionKeyService : IDisposable
{
    /// <summary>
    /// Gets the encryption key for the current machine. 
    /// Generates a new key if one doesn't exist.
    /// </summary>
    /// <returns>A 32-byte encryption key for AES-256</returns>
    Task<byte[]> GetEncryptionKeyAsync();
    
    /// <summary>
    /// Regenerates the encryption key. Warning: This will make previously encrypted data unreadable.
    /// </summary>
    /// <returns>The new 32-byte encryption key</returns>
    Task<byte[]> RegenerateKeyAsync();
    
    /// <summary>
    /// Checks if an encryption key exists for this machine
    /// </summary>
    /// <returns>True if a key exists, false otherwise</returns>
    Task<bool> KeyExistsAsync();
}