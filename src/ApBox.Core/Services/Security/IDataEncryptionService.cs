namespace ApBox.Core.Services.Security;

/// <summary>
/// Service for encrypting/decrypting sensitive data like PINs
/// </summary>
public interface IDataEncryptionService
{
    /// <summary>
    /// Encrypts sensitive data using AES encryption
    /// </summary>
    /// <param name="plainText">The data to encrypt</param>
    /// <returns>Base64 encoded encrypted data</returns>
    string EncryptData(string plainText);
    
    /// <summary>
    /// Decrypts data that was encrypted with EncryptData
    /// </summary>
    /// <param name="encryptedData">Base64 encoded encrypted data</param>
    /// <returns>The original plain text</returns>
    string DecryptData(string encryptedData);
}