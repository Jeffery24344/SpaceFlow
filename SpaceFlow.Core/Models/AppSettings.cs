namespace SpaceFlow.Core.Models;

/// <summary>
/// User-configurable settings for SpaceFlow.
/// All thresholds are configurable — never hard-coded in production logic.
/// </summary>
public class AppSettings
{
    // General
    public bool StartWithWindows { get; set; } = false;
    public bool EnableSmartMode { get; set; } = true;
    public bool ShowNotifications { get; set; } = true;
    public bool AutomaticCleanup { get; set; } = true;

    // Workspace
    public long MaxWorkspaceSizeBytes { get; set; } = 16L * 1024 * 1024 * 1024; // 16 GB default
    public long MinFreeDiskSpaceBytes { get; set; } = 10L * 1024 * 1024 * 1024;  // 10 GB safety reserve
    public string PreferredDrive { get; set; } = "C";
    public double ActivationThresholdPercent { get; set; } = 85.0;   // Start preparing
    public double HighPressureThresholdPercent { get; set; } = 92.0; // Fully active
    public double ReleaseThresholdPercent { get; set; } = 70.0;      // Release when below this

    // Performance & Rollback (V0.2)
    public int Aggressiveness { get; set; } = 5; // 1–10 scale
    public int MonitorIntervalMs { get; set; } = 2000;
    public bool EnablePerformanceRollback { get; set; } = true;
    public double RollbackHealthDropThreshold { get; set; } = 15.0; // If health drops by this much → reduce workspace
    public int PerformanceHistorySeconds { get; set; } = 30;

    // Application Detection (V0.2)
    public int TopProcessesToTrack { get; set; } = 8;
    public long MinProcessMemoryBytes { get; set; } = 100L * 1024 * 1024; // Ignore tiny processes
    public Dictionary<string, ProcessPriority> ProcessPreferences { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    // Cloud (placeholders for later)
    public bool EnableCloudFlow { get; set; } = false;
    public long MaxCloudStorageBytes { get; set; } = 5L * 1024 * 1024 * 1024;
    public long MaxBandwidthBytesPerSecond { get; set; } = 10L * 1024 * 1024; // 10 MB/s

    // Current mode
    public FlowMode CurrentMode { get; set; } = FlowMode.Local;
}
