namespace ApBox.Core.Services.Infrastructure;

/// <summary>
/// Abstraction for file system operations to enable testing
/// </summary>
public interface IFileSystem
{
    /// <summary>
    /// Checks if a file exists at the specified path
    /// </summary>
    /// <param name="path">The file path to check</param>
    /// <returns>True if the file exists, false otherwise</returns>
    bool FileExists(string path);
    
    /// <summary>
    /// Reads all text from a file asynchronously
    /// </summary>
    /// <param name="path">The file path to read from</param>
    /// <param name="encoding">The text encoding to use</param>
    /// <returns>The contents of the file</returns>
    Task<string> ReadAllTextAsync(string path, System.Text.Encoding encoding);
    
    /// <summary>
    /// Writes text to a file asynchronously
    /// </summary>
    /// <param name="path">The file path to write to</param>
    /// <param name="contents">The text content to write</param>
    /// <param name="encoding">The text encoding to use</param>
    Task WriteAllTextAsync(string path, string contents, System.Text.Encoding encoding);
    
    /// <summary>
    /// Creates a directory if it doesn't exist
    /// </summary>
    /// <param name="path">The directory path to create</param>
    void CreateDirectory(string path);
    
    /// <summary>
    /// Sets file permissions on Windows
    /// </summary>
    /// <param name="path">The file path</param>
    /// <param name="attributes">The file attributes to set</param>
    void SetFileAttributes(string path, FileAttributes attributes);
    
    /// <summary>
    /// Sets Unix file permissions
    /// </summary>
    /// <param name="path">The file path</param>
    /// <param name="mode">The Unix file mode</param>
    void SetUnixFileMode(string path, UnixFileMode mode);
    
    /// <summary>
    /// Gets the special folder path
    /// </summary>
    /// <param name="folder">The special folder</param>
    /// <returns>The folder path</returns>
    string GetFolderPath(Environment.SpecialFolder folder);
    
    /// <summary>
    /// Combines path components
    /// </summary>
    /// <param name="paths">The path components to combine</param>
    /// <returns>The combined path</returns>
    string CombinePath(params string[] paths);
}