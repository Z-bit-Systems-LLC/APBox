using System.Security.Cryptography;
using System.Text;

namespace ApBox.Core.Services.Security;

/// <summary>
/// Service for encrypting/decrypting sensitive data using AES encryption
/// </summary>
public class DataEncryptionService : IDataEncryptionService
{
    private readonly byte[] _key;
    private readonly ILogger<DataEncryptionService> _logger;

    public DataEncryptionService(ILogger<DataEncryptionService> logger)
    {
        _logger = logger;
        // In production, this key should come from secure configuration
        // For now, using a hardcoded key for demonstration
        var keyString = "ApBoxEncryptionKey123456789012345"; // Must be exactly 32 bytes for AES-256
        _key = Encoding.UTF8.GetBytes(keyString).Take(32).ToArray(); // Ensure exactly 32 bytes
    }

    public string EncryptData(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        try
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            using var msEncrypt = new MemoryStream();
            using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
            using (var swEncrypt = new StreamWriter(csEncrypt))
            {
                swEncrypt.Write(plainText);
            }

            var iv = aes.IV;
            var encrypted = msEncrypt.ToArray();
            var result = new byte[iv.Length + encrypted.Length];
            Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
            Buffer.BlockCopy(encrypted, 0, result, iv.Length, encrypted.Length);

            return Convert.ToBase64String(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt data");
            throw;
        }
    }

    public string DecryptData(string encryptedData)
    {
        if (string.IsNullOrEmpty(encryptedData))
            return string.Empty;

        try
        {
            var fullCipher = Convert.FromBase64String(encryptedData);

            using var aes = Aes.Create();
            aes.Key = _key;

            var iv = new byte[aes.BlockSize / 8];
            var cipher = new byte[fullCipher.Length - iv.Length];

            Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, cipher.Length);

            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            using var msDecrypt = new MemoryStream(cipher);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);
            
            return srDecrypt.ReadToEnd();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt data");
            throw;
        }
    }
}