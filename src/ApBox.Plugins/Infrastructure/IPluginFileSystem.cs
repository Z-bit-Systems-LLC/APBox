namespace ApBox.Plugins.Infrastructure;

/// <summary>
/// Abstraction for file system operations needed by plugin system
/// </summary>
public interface IPluginFileSystem
{
    /// <summary>
    /// Checks if a directory exists
    /// </summary>
    /// <param name="path">The directory path to check</param>
    /// <returns>True if the directory exists, false otherwise</returns>
    bool DirectoryExists(string path);
    
    /// <summary>
    /// Gets all files in a directory matching a pattern
    /// </summary>
    /// <param name="path">The directory path</param>
    /// <param name="searchPattern">The search pattern (e.g., "*.dll")</param>
    /// <returns>Array of file paths</returns>
    string[] GetFiles(string path, string searchPattern);
    
    /// <summary>
    /// Gets the filename from a path
    /// </summary>
    /// <param name="path">The file path</param>
    /// <returns>The filename</returns>
    string GetFileName(string path);
    
    /// <summary>
    /// Combines path components
    /// </summary>
    /// <param name="paths">The path components to combine</param>
    /// <returns>The combined path</returns>
    string CombinePath(params string[] paths);
    
    /// <summary>
    /// Creates a directory if it doesn't exist
    /// </summary>
    /// <param name="path">The directory path to create</param>
    void CreateDirectory(string path);
    
    /// <summary>
    /// Deletes a directory recursively
    /// </summary>
    /// <param name="path">The directory path to delete</param>
    /// <param name="recursive">Whether to delete recursively</param>
    void DeleteDirectory(string path, bool recursive);
    
    /// <summary>
    /// Gets temporary path
    /// </summary>
    /// <returns>The temporary directory path</returns>
    string GetTempPath();
}