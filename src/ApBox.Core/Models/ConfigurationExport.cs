namespace ApBox.Core.Models;

/// <summary>
/// Complete system configuration export data structure
/// </summary>
public class ConfigurationExport
{
    public string ExportVersion { get; set; } = "1.0";
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
    public SystemInfo SystemInfo { get; set; } = new();
    public List<ReaderConfiguration> Readers { get; set; } = new();
    public FeedbackConfiguration FeedbackConfiguration { get; set; } = new();
}

/// <summary>
/// System information included in export
/// </summary>
public class SystemInfo
{
    public string ApBoxVersion { get; set; } = GetApBoxVersion();
    public string Framework { get; set; } = GetFrameworkVersion();
    public string Platform { get; set; } = GetPlatformInfo();
    public string MachineName { get; set; } = Environment.MachineName;
    public string OSVersion { get; set; } = Environment.OSVersion.ToString();
    public int ProcessorCount { get; set; } = Environment.ProcessorCount;
    public string WorkingDirectory { get; set; } = Environment.CurrentDirectory;
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    
    private static string GetApBoxVersion()
    {
        try
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version?.ToString() ?? "1.0.0";
        }
        catch
        {
            return "1.0.0";
        }
    }
    
    private static string GetFrameworkVersion()
    {
        try
        {
            return System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
        }
        catch
        {
            return ".NET 8";
        }
    }
    
    private static string GetPlatformInfo()
    {
        try
        {
            return $"{Environment.OSVersion.Platform} ({System.Runtime.InteropServices.RuntimeInformation.OSDescription})";
        }
        catch
        {
            return Environment.OSVersion.Platform.ToString();
        }
    }
}


/// <summary>
/// Configuration import validation result
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    
    public void AddError(string error) => Errors.Add(error);
    public void AddWarning(string warning) => Warnings.Add(warning);
}