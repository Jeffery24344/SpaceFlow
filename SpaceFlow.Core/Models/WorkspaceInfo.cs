namespace SpaceFlow.Core.Models;

/// <summary>
/// Information about the current temporary workspace.
/// IMPORTANT: This is NEVER presented as "extra RAM".
/// </summary>
public class WorkspaceInfo
{
    public bool IsActive { get; set; }
    public string? RootPath { get; set; }
    public long AllocatedBytes { get; set; }
    public long UsedBytes { get; set; }
    public long MaxAllowedBytes { get; set; }
    public string DriveLetter { get; set; } = "C";
    public StorageType StorageType { get; set; } = StorageType.Unknown;

    public string DisplayAllocated => SystemMetrics.FormatBytes(AllocatedBytes);
    public string DisplayUsed => SystemMetrics.FormatBytes(UsedBytes);
    public string DisplayMax => SystemMetrics.FormatBytes(MaxAllowedBytes);

    public double UsagePercent => AllocatedBytes > 0
        ? (double)UsedBytes / AllocatedBytes * 100.0
        : 0;
}

public enum StorageType
{
    Unknown,
    HDD,
    SataSsd,
    NvmeSsd
}

public enum FlowMode
{
    Off,
    Local,
    Cloud,      // Future
    Hybrid      // Future / preferred
}
