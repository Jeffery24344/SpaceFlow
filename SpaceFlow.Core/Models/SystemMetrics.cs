namespace SpaceFlow.Core.Models;

/// <summary>
/// Snapshot of current system resource metrics.
/// </summary>
public class SystemMetrics
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // RAM
    public long TotalPhysicalMemoryBytes { get; set; }
    public long AvailablePhysicalMemoryBytes { get; set; }
    public long UsedPhysicalMemoryBytes => TotalPhysicalMemoryBytes - AvailablePhysicalMemoryBytes;
    public double RamUsagePercent => TotalPhysicalMemoryBytes > 0
        ? (double)UsedPhysicalMemoryBytes / TotalPhysicalMemoryBytes * 100.0
        : 0;

    // CPU
    public double CpuUsagePercent { get; set; }

    // Disk (selected workspace drive)
    public string? WorkspaceDriveLetter { get; set; }
    public long TotalDiskBytes { get; set; }
    public long AvailableDiskBytes { get; set; }
    public long UsedDiskBytes => TotalDiskBytes - AvailableDiskBytes;
    public double DiskUsagePercent => TotalDiskBytes > 0
        ? (double)UsedDiskBytes / TotalDiskBytes * 100.0
        : 0;

    // Memory pressure classification
    public MemoryPressureLevel PressureLevel { get; set; } = MemoryPressureLevel.Normal;

    // Human-readable helpers
    public string RamDisplay => $"{FormatBytes(UsedPhysicalMemoryBytes)} / {FormatBytes(TotalPhysicalMemoryBytes)}";
    public string DiskDisplay => $"{FormatBytes(AvailableDiskBytes)} free";

    public static string FormatBytes(long bytes)
    {
        string[] suffixes = ["B", "KB", "MB", "GB", "TB"];
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < suffixes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {suffixes[order]}";
    }
}

public enum MemoryPressureLevel
{
    Normal,      // 0–70%
    Monitoring,  // 70–85%
    Preparing,   // 85–92%
    High         // 92–100%
}
