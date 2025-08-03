using System.Text;

namespace ApBox.Core.Services.Infrastructure;

/// <summary>
/// Default implementation of IFileSystem that delegates to the actual file system
/// </summary>
public class FileSystem : IFileSystem
{
    public bool FileExists(string path) => File.Exists(path);

    public async Task<string> ReadAllTextAsync(string path, Encoding encoding) => 
        await File.ReadAllTextAsync(path, encoding);

    public async Task WriteAllTextAsync(string path, string contents, Encoding encoding) => 
        await File.WriteAllTextAsync(path, contents, encoding);

    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    public void SetFileAttributes(string path, FileAttributes attributes)
    {
        var fileInfo = new FileInfo(path);
        fileInfo.Attributes = attributes;
    }

    public void SetUnixFileMode(string path, UnixFileMode mode)
    {
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            File.SetUnixFileMode(path, mode);
        }
    }

    public string GetFolderPath(Environment.SpecialFolder folder) => 
        Environment.GetFolderPath(folder);

    public string CombinePath(params string[] paths) => Path.Combine(paths);
}