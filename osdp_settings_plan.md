# Plan: Add OSDP Settings to Readers

## Overview
Extend the reader configuration system to include OSDP-specific settings, enabling proper communication with OSDP devices.

## Current State Analysis

### Existing Structure
1. **ReaderConfiguration Model** currently contains:
   - ReaderId (Guid)
   - ReaderName (string)
   - Address (byte) - OSDP address defaulting to 1
   - IsEnabled (bool)
   - CreatedAt/UpdatedAt timestamps

2. **OSDP Infrastructure** already exists with:
   - `IOsdpDevice` interface for device communication
   - `IOsdpCommunicationManager` for managing multiple devices
   - `OsdpDeviceConfiguration` class with comprehensive OSDP settings
   - Mock implementations for testing

3. **UI**: The ReadersConfiguration component only allows editing the reader name currently

## Implementation Steps

### 1. Extend ReaderConfiguration Model
Add OSDP properties to `ApBox.Core.Models.ReaderConfiguration`:
- `SerialPort` - Serial port name (e.g., "COM3", "/dev/ttyUSB0")
- `BaudRate` - Serial communication speed (default: 9600)
- `SecurityMode` - Enum: ClearText, Install, Secure (default: ClearText)
- `SecureChannelKey` - 16-byte key for secure communication (byte[]) - system managed
- `PollIntervalSeconds` - Time between device polls (default: 1)

Add SecurityMode enum:
```csharp
public enum OsdpSecurityMode
{
    ClearText = 0,  // No security
    Install = 1,    // Use default key to install random key
    Secure = 2      // Use installed random key
}
```

### 2. Database Migration
Update existing migration file `001_initial_schema.sql` to include OSDP settings:
```sql
-- Update the reader_configurations table creation to include OSDP columns
CREATE TABLE IF NOT EXISTS reader_configurations (
    reader_id TEXT PRIMARY KEY,
    reader_name TEXT NOT NULL,
    address INTEGER NOT NULL DEFAULT 1,
    serial_port TEXT,
    baud_rate INTEGER DEFAULT 9600,
    security_mode INTEGER DEFAULT 0, -- 0=ClearText, 1=Install, 2=Secure
    secure_channel_key TEXT, -- Encrypted, system managed
    poll_interval_seconds INTEGER DEFAULT 1,
    is_enabled BOOLEAN NOT NULL DEFAULT 1,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);
```

### 3. Update Data Layer
**ReaderConfigurationEntity**:
- Add new properties matching the model
- Handle SecureChannelKey as string (base64) in entity

**ReaderConfigurationRepository**:
- Update all SQL queries to include new columns
- Add mapping for SecureChannelKey (byte[] ↔ base64 string conversion)
- Update GetAll, Get, Create, and Update methods

### 4. Service Layer Integration
**Security Key Management**:
```csharp
public class OsdpSecurityService
{
    // Default OSDP secure channel key (well-known for installation)
    private static readonly byte[] DefaultSecureChannelKey = new byte[] 
    { 
        0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37,
        0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F
    };
    
    public byte[] GetSecurityKey(OsdpSecurityMode mode, byte[]? storedKey)
    {
        return mode switch
        {
            OsdpSecurityMode.ClearText => null,
            OsdpSecurityMode.Install => DefaultSecureChannelKey,
            OsdpSecurityMode.Secure => storedKey ?? throw new InvalidOperationException("No secure key stored"),
            _ => null
        };
    }
    
    public byte[] GenerateRandomKey()
    {
        var key = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(key);
        }
        return key;
    }
}
```

**Create Mapping Helper**:
```csharp
public static OsdpDeviceConfiguration ToOsdpConfiguration(this ReaderConfiguration reader, IOsdpSecurityService securityService)
{
    return new OsdpDeviceConfiguration
    {
        Address = reader.Address,
        ConnectionString = reader.SerialPort,
        BaudRate = reader.BaudRate,
        UseSecureChannel = reader.SecurityMode != OsdpSecurityMode.ClearText,
        SecureChannelKey = securityService.GetSecurityKey(reader.SecurityMode, reader.SecureChannelKey),
        PollInterval = TimeSpan.FromSeconds(reader.PollIntervalSeconds)
    };
}
```

**Update ReaderService**:
- Register readers with `OsdpCommunicationManager` on startup
- Handle security mode transitions:
  - Install mode: Connect with default key, generate and set random key, update to Secure mode
  - Clear Text: Disable secure channel
  - Secure: Use stored random key
- Implement connection testing functionality

### 5. UI Enhancements
Update `ReadersConfiguration.razor` component:

**Serial Connection Settings**:
- Serial port dropdown (auto-detect available ports)
- Baud rate selector (9600, 19200, 38400, 57600, 115200)
- Refresh ports button

**Security Settings Section**:
- Security Mode selector with two options:
  - "Clear Text" - No security
  - "Install Secure Channel" - Install random key using default key
- Status indicator showing current mode:
  - 🔓 Clear Text (no security)
  - 🔧 Install Mode (ready to install)
  - 🔒 Secure (key installed)
- Installation status/progress when in Install mode

**Advanced Settings Section**:
- Poll interval slider/input (1-60 seconds)
- Connection test button
- Status indicator (Connected/Disconnected)

### 6. Security Implementation
**Key Storage Encryption with TPM Support**:
```csharp
public interface IKeyEncryptionService
{
    string EncryptKey(byte[] key);
    byte[] DecryptKey(string encryptedKey);
    bool IsTpmAvailable();
    string GetEncryptionMethod();
}

// TPM-based encryption (highest security)
public class TpmKeyEncryptionService : IKeyEncryptionService
{
    private readonly ILogger<TpmKeyEncryptionService> _logger;
    private readonly TpmHandle _keyHandle;
    private readonly bool _tpmAvailable;
    
    public TpmKeyEncryptionService(ILogger<TpmKeyEncryptionService> logger)
    {
        _logger = logger;
        _tpmAvailable = InitializeTpm();
    }
    
    private bool InitializeTpm()
    {
        try
        {
            // Check if TPM 2.0 is available
            using (var tpm = TSS.Net.Tpm2Device.CreateDefaultTpm())
            {
                tpm.Connect();
                
                // Create persistent key for OSDP key encryption
                var keyTemplate = new TpmPublic(
                    TpmAlgId.Sha256,
                    ObjectAttr.Decrypt | ObjectAttr.UserWithAuth | ObjectAttr.FixedParent | ObjectAttr.FixedTPM,
                    new byte[] { }, // No auth required
                    new RsaParms(new SymDefObject(), new SchemeOaep(TpmAlgId.Sha256), 2048, 0),
                    new Tpm2bPublicKeyRsa()
                );
                
                // Store key in TPM NV storage
                _keyHandle = tpm.CreatePrimary(TpmHandle.RhOwner, new SensitiveCreate(), keyTemplate);
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("TPM not available: {Message}", ex.Message);
            return false;
        }
    }
    
    public string EncryptKey(byte[] key)
    {
        if (!_tpmAvailable) throw new InvalidOperationException("TPM not available");
        
        using (var tpm = TSS.Net.Tpm2Device.CreateDefaultTpm())
        {
            tpm.Connect();
            var encrypted = tpm.RsaEncrypt(_keyHandle, key, new SchemeOaep(TpmAlgId.Sha256), null);
            return Convert.ToBase64String(encrypted);
        }
    }
    
    public byte[] DecryptKey(string encryptedKey)
    {
        if (!_tpmAvailable) throw new InvalidOperationException("TPM not available");
        
        using (var tpm = TSS.Net.Tpm2Device.CreateDefaultTpm())
        {
            tpm.Connect();
            var encrypted = Convert.FromBase64String(encryptedKey);
            var decrypted = tpm.RsaDecrypt(_keyHandle, encrypted, new SchemeOaep(TpmAlgId.Sha256), null);
            return decrypted;
        }
    }
    
    public bool IsTpmAvailable() => _tpmAvailable;
    public string GetEncryptionMethod() => "TPM 2.0";
}

// DPAPI-based encryption (Windows)
public class DpapiKeyEncryptionService : IKeyEncryptionService
{
    private readonly IDataProtector _protector;
    
    public DpapiKeyEncryptionService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("ApBox.OsdpKeys");
    }
    
    public string EncryptKey(byte[] key)
    {
        if (key == null || key.Length == 0) return null;
        
        var protectedData = _protector.Protect(key);
        return Convert.ToBase64String(protectedData);
    }
    
    public byte[] DecryptKey(string encryptedKey)
    {
        if (string.IsNullOrEmpty(encryptedKey)) return null;
        
        var protectedData = Convert.FromBase64String(encryptedKey);
        return _protector.Unprotect(protectedData);
    }
    
    public bool IsTpmAvailable() => false;
    public string GetEncryptionMethod() => "DPAPI";
}
```

// Machine Key encryption (fallback for Linux/Docker)
public class MachineKeyEncryptionService : IKeyEncryptionService
{
    private readonly byte[] _machineKey;
    
    public MachineKeyEncryptionService(IConfiguration config)
    {
        var keyString = config["Security:MachineKey"];
        if (string.IsNullOrEmpty(keyString))
        {
            throw new InvalidOperationException("Machine key not configured");
        }
        _machineKey = Convert.FromBase64String(keyString);
    }
    
    public string EncryptKey(byte[] key)
    {
        using var aes = Aes.Create();
        aes.Key = _machineKey;
        aes.GenerateIV();
        
        using var encryptor = aes.CreateEncryptor();
        var encrypted = encryptor.TransformFinalBlock(key, 0, key.Length);
        
        var result = new byte[aes.IV.Length + encrypted.Length];
        aes.IV.CopyTo(result, 0);
        encrypted.CopyTo(result, aes.IV.Length);
        
        return Convert.ToBase64String(result);
    }
    
    public byte[] DecryptKey(string encryptedKey)
    {
        var data = Convert.FromBase64String(encryptedKey);
        
        using var aes = Aes.Create();
        aes.Key = _machineKey;
        
        var iv = new byte[aes.BlockSize / 8];
        var encrypted = new byte[data.Length - iv.Length];
        Array.Copy(data, 0, iv, 0, iv.Length);
        Array.Copy(data, iv.Length, encrypted, 0, encrypted.Length);
        
        aes.IV = iv;
        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);
    }
    
    public bool IsTpmAvailable() => false;
    public string GetEncryptionMethod() => "AES-256";
}
```

**Key Encryption Service Factory**:
```csharp
public class KeyEncryptionServiceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<KeyEncryptionServiceFactory> _logger;
    
    public KeyEncryptionServiceFactory(IServiceProvider serviceProvider, ILogger<KeyEncryptionServiceFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    public IKeyEncryptionService CreateKeyEncryptionService()
    {
        // Priority order: TPM > DPAPI > Machine Key
        
        // 1. Try TPM first (most secure)
        try
        {
            var tpmService = _serviceProvider.GetService<TpmKeyEncryptionService>();
            if (tpmService != null && tpmService.IsTpmAvailable())
            {
                _logger.LogInformation("Using TPM 2.0 for key encryption");
                return tpmService;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug("TPM not available: {Message}", ex.Message);
        }
        
        // 2. Try DPAPI (Windows)
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                var dpapiService = _serviceProvider.GetService<DpapiKeyEncryptionService>();
                if (dpapiService != null)
                {
                    _logger.LogInformation("Using DPAPI for key encryption");
                    return dpapiService;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("DPAPI not available: {Message}", ex.Message);
            }
        }
        
        // 3. Fall back to machine key (cross-platform)
        _logger.LogInformation("Using AES-256 machine key for key encryption");
        return _serviceProvider.GetRequiredService<MachineKeyEncryptionService>();
    }
}
```

**Service Registration**:
```csharp
// In Program.cs or ServiceCollectionExtensions
services.AddSingleton<TpmKeyEncryptionService>();
services.AddSingleton<DpapiKeyEncryptionService>();
services.AddSingleton<MachineKeyEncryptionService>();
services.AddSingleton<KeyEncryptionServiceFactory>();
services.AddScoped<IKeyEncryptionService>(provider => 
    provider.GetRequiredService<KeyEncryptionServiceFactory>().CreateKeyEncryptionService());

// Add TPM package: Microsoft.TSS.NET
```

### 7. Multi-Device Serial Port Management
**Baud Rate Consistency Validation**:
```csharp
public interface ISerialPortValidator
{
    Task<ValidationResult> ValidateReaderConfiguration(ReaderConfiguration reader);
    Task<IEnumerable<ReaderConfiguration>> GetReadersOnSamePort(string serialPort);
}

public class SerialPortValidator : ISerialPortValidator
{
    private readonly IReaderConfigurationService _readerService;
    
    public SerialPortValidator(IReaderConfigurationService readerService)
    {
        _readerService = readerService;
    }
    
    public async Task<ValidationResult> ValidateReaderConfiguration(ReaderConfiguration reader)
    {
        // Check if other readers are using the same serial port
        var readersOnPort = await GetReadersOnSamePort(reader.SerialPort);
        var otherReaders = readersOnPort.Where(r => r.ReaderId != reader.ReaderId);
        
        if (otherReaders.Any())
        {
            // Check if all have the same baud rate
            var existingBaudRate = otherReaders.First().BaudRate;
            if (otherReaders.All(r => r.BaudRate == existingBaudRate))
            {
                if (reader.BaudRate != existingBaudRate)
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        Message = $"All readers on {reader.SerialPort} must use the same baud rate ({existingBaudRate}). " +
                                  $"There are {otherReaders.Count()} other reader(s) on this port."
                    };
                }
            }
        }
        
        return new ValidationResult { IsValid = true };
    }
    
    public async Task<IEnumerable<ReaderConfiguration>> GetReadersOnSamePort(string serialPort)
    {
        var allReaders = await _readerService.GetAllReadersAsync();
        return allReaders.Where(r => r.SerialPort == serialPort && r.IsEnabled);
    }
}
```

**UI Enhancements for Baud Rate**:
- When selecting a serial port that's already in use:
  - Auto-populate baud rate from existing readers
  - Show warning: "X readers already on this port using Y baud rate"
  - Disable baud rate selector if port is shared
  - Show list of readers sharing the port

### 8. Validation & Testing
**Validation Rules**:
- SerialPort: Valid COM port (Windows) or /dev/tty* (Linux)
- BaudRate: Standard values (9600, 19200, 38400, 57600, 115200)
- **Baud Rate Consistency**: All readers on same serial port must use same baud rate
- SecurityMode: Can only select ClearText or Install (not Secure directly)
- PollInterval: Between 1 and 60 seconds

**Testing**:
- Unit tests for model mapping
- Integration tests for repository changes
- UI component tests for new fields
- End-to-end connection tests

## Security Workflow
1. **Clear Text Mode**: No encryption, no secure channel
2. **Install Mode**: 
   - User selects "Install Secure Channel"
   - System connects using default OSDP key
   - System generates random 16-byte key
   - Key is encrypted using best available method (TPM > DPAPI > Machine Key)
   - System installs key on reader
   - Mode automatically changes to "Secure"
3. **Secure Mode**: 
   - System decrypts stored key for use
   - Secure channel active with installed key
   - User cannot directly select this mode

## Key Encryption Priority
1. **TPM 2.0** (if available) - Hardware-based security
2. **DPAPI** (Windows) - OS-level protection
3. **AES-256** (cross-platform) - Machine key encryption

## Multi-Reader Serial Port Considerations
- OSDP supports multiple devices on one serial port (RS-485 multi-drop)
- All readers on the same port MUST use the same baud rate
- UI enforces baud rate consistency automatically
- Clear visual indication when ports are shared

## Benefits
- Full OSDP configuration without manual key management
- Secure key storage using platform encryption
- Automated key installation process
- Proper separation between business and communication layers
- User-friendly three-state security model

## Next Steps
1. Start with model and database changes
2. Update repository and service layers
3. Enhance UI with new configuration options
4. Add comprehensive testing
5. Document OSDP configuration in user guide