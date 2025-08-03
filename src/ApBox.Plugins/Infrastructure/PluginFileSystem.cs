namespace ApBox.Plugins.Infrastructure;

/// <summary>
/// Default implementation of IPluginFileSystem that delegates to the actual file system
/// </summary>
public class PluginFileSystem : IPluginFileSystem
{
    public bool DirectoryExists(string path) => Directory.Exists(path);

    public string[] GetFiles(string path, string searchPattern) => Directory.GetFiles(path, searchPattern);

    public string GetFileName(string path) => Path.GetFileName(path);

    public string CombinePath(params string[] paths) => Path.Combine(paths);

    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    public void DeleteDirectory(string path, bool recursive) => Directory.Delete(path, recursive);

    public string GetTempPath() => Path.GetTempPath();
}