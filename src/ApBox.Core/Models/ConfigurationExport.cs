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
    public string ApBoxVersion { get; set; } = "1.0.0";
    public string Framework { get; set; } = ".NET 8";
    public string Platform { get; set; } = Environment.OSVersion.Platform.ToString();
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