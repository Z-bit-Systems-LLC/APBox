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

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public string[] GetFiles(string path, string searchPattern) => Directory.GetFiles(path, searchPattern);

    public string GetFileNameWithoutExtension(string path) => Path.GetFileNameWithoutExtension(path);

    public string GetFileName(string path) => Path.GetFileName(path);

    public string? GetDirectoryName(string path) => Path.GetDirectoryName(path);

    public async Task<string[]> ReadAllLinesAsync(string path) => await File.ReadAllLinesAsync(path);

    public void AppendAllLines(string path, IEnumerable<string> contents) => File.AppendAllLines(path, contents);

    public void DeleteFile(string path) => File.Delete(path);

    public void DeleteDirectory(string path, bool recursive) => Directory.Delete(path, recursive);
}