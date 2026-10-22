namespace SpaceFlow.Core.Models;

/// <summary>
/// Information about a running process that is consuming significant resources.
/// </summary>
public class ProcessInfo
{
    public int ProcessId { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public string? MainWindowTitle { get; set; }
    public long WorkingSetBytes { get; set; }          // Physical memory used
    public long PrivateMemoryBytes { get; set; }
    public double CpuPercent { get; set; }
    public ProcessPriority Preference { get; set; } = ProcessPriority.Automatic;

    public string DisplayMemory => SystemMetrics.FormatBytes(WorkingSetBytes);
    public string DisplayName => string.IsNullOrWhiteSpace(MainWindowTitle)
        ? ProcessName
        : $"{ProcessName} — {MainWindowTitle}";
}

public enum ProcessPriority
{
    Automatic,      // SpaceFlow decides
    Prioritize,     // Prefer protecting this app
    Ignore,         // Never intervene for this app
    ForceLocal,     // Always use Local Flow for this app
    AllowCloud,     // Future
    NeverCloud      // Future
}
