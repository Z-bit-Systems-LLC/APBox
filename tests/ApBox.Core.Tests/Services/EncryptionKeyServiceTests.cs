using Microsoft.Extensions.Logging;
using Moq;
using ApBox.Core.Services.Security;
using ApBox.Core.Services.Infrastructure;
using System.Text;

namespace ApBox.Core.Tests.Services;

[TestFixture]
public class EncryptionKeyServiceTests
{
    private EncryptionKeyService _keyService;
    private Mock<ILogger<EncryptionKeyService>> _mockLogger;
    private Mock<IFileSystem> _mockFileSystem;

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<EncryptionKeyService>>();
        _mockFileSystem = new Mock<IFileSystem>();
        
        // Setup file system mocks
        _mockFileSystem.Setup(x => x.GetFolderPath(Environment.SpecialFolder.CommonApplicationData))
                      .Returns(@"C:\ProgramData");
        _mockFileSystem.Setup(x => x.CombinePath(It.IsAny<string[]>()))
                      .Returns((string[] paths) => paths.Length > 0 ? Path.Combine(paths) : string.Empty);
        
        _keyService = new EncryptionKeyService(_mockLogger.Object, _mockFileSystem.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _keyService.Dispose();
    }

    [Test]
    public async Task GetEncryptionKeyAsync_WhenNoKeyExists_GeneratesAndReturnsNewKey()
    {
        // Arrange
        _mockFileSystem.Setup(x => x.FileExists(It.IsAny<string>())).Returns(false);
        
        // Act
        var key = await _keyService.GetEncryptionKeyAsync();

        // Assert
        Assert.That(key, Is.Not.Null);
        Assert.That(key.Length, Is.EqualTo(32)); // 256 bits for AES-256
        
        // Verify that file was written
        _mockFileSystem.Verify(x => x.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), Encoding.UTF8), Times.Once);
        _mockFileSystem.Verify(x => x.CreateDirectory(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task GetEncryptionKeyAsync_WhenKeyExists_ReturnsExistingKey()
    {
        // Arrange
        var existingKeyBase64 = Convert.ToBase64String(new byte[32]); // Mock existing key
        _mockFileSystem.Setup(x => x.FileExists(It.IsAny<string>())).Returns(true);
        _mockFileSystem.Setup(x => x.ReadAllTextAsync(It.IsAny<string>(), Encoding.UTF8))
                      .ReturnsAsync(existingKeyBase64);

        // Act
        var key = await _keyService.GetEncryptionKeyAsync();

        // Assert
        Assert.That(key, Is.Not.Null);
        Assert.That(key.Length, Is.EqualTo(32));
        
        // Verify file was read but not written
        _mockFileSystem.Verify(x => x.ReadAllTextAsync(It.IsAny<string>(), Encoding.UTF8), Times.Once);
        _mockFileSystem.Verify(x => x.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), Encoding.UTF8), Times.Never);
    }

    [Test]
    public async Task RegenerateKeyAsync_ReplacesExistingKey()
    {
        // Act
        var newKey = await _keyService.RegenerateKeyAsync();

        // Assert
        Assert.That(newKey, Is.Not.Null);
        Assert.That(newKey.Length, Is.EqualTo(32));
        
        // Verify that file was written
        _mockFileSystem.Verify(x => x.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), Encoding.UTF8), Times.Once);
    }

    [Test]
    public async Task KeyExistsAsync_WhenNoKey_ReturnsFalse()
    {
        // Arrange
        _mockFileSystem.Setup(x => x.FileExists(It.IsAny<string>())).Returns(false);

        // Act & Assert
        Assert.That(await _keyService.KeyExistsAsync(), Is.False);
    }

    [Test]
    public async Task KeyExistsAsync_WhenKeyExists_ReturnsTrue()
    {
        // Arrange
        _mockFileSystem.Setup(x => x.FileExists(It.IsAny<string>())).Returns(true);

        // Act & Assert
        Assert.That(await _keyService.KeyExistsAsync(), Is.True);
    }

    [Test]
    public async Task GetEncryptionKeyAsync_MultipleCallsConcurrent_ReturnsSameKey()
    {
        // Arrange
        var existingKeyBase64 = Convert.ToBase64String(new byte[32]);
        _mockFileSystem.Setup(x => x.FileExists(It.IsAny<string>())).Returns(true);
        _mockFileSystem.Setup(x => x.ReadAllTextAsync(It.IsAny<string>(), Encoding.UTF8))
                      .ReturnsAsync(existingKeyBase64);

        // Act
        var tasks = new List<Task<byte[]>>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_keyService.GetEncryptionKeyAsync());
        }
        
        var keys = await Task.WhenAll(tasks);

        // Assert
        var firstKey = keys[0];
        foreach (var key in keys)
        {
            Assert.That(key, Is.EqualTo(firstKey));
        }
        
        // Verify file was read (could be multiple times due to concurrency, but at least once)
        _mockFileSystem.Verify(x => x.ReadAllTextAsync(It.IsAny<string>(), Encoding.UTF8), Times.AtLeastOnce);
    }

    [Test]
    public void GetEncryptionKeyAsync_InvalidKeyLength_ThrowsException()
    {
        // Arrange
        var invalidKeyBase64 = Convert.ToBase64String(new byte[16]); // Invalid length (should be 32)
        _mockFileSystem.Setup(x => x.FileExists(It.IsAny<string>())).Returns(true);
        _mockFileSystem.Setup(x => x.ReadAllTextAsync(It.IsAny<string>(), Encoding.UTF8))
                      .ReturnsAsync(invalidKeyBase64);

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(async () => 
            await _keyService.GetEncryptionKeyAsync());
    }

    [Test]
    public void GetEncryptionKeyAsync_FileSystemException_ThrowsException()
    {
        // Arrange
        _mockFileSystem.Setup(x => x.FileExists(It.IsAny<string>())).Returns(true);
        _mockFileSystem.Setup(x => x.ReadAllTextAsync(It.IsAny<string>(), Encoding.UTF8))
                      .ThrowsAsync(new IOException("File access denied"));

        // Act & Assert
        Assert.ThrowsAsync<IOException>(async () => 
            await _keyService.GetEncryptionKeyAsync());
    }
}