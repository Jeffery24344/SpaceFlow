namespace SpaceFlow.Core.Models;

/// <summary>
/// Used by the Performance Analyzer to decide whether SpaceFlow intervention
/// is helping or hurting overall responsiveness.
/// </summary>
public class PerformanceSnapshot
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public double RamUsagePercent { get; set; }
    public double CpuUsagePercent { get; set; }
    public double DiskQueueLength { get; set; }      // Approximate I/O pressure
    public long WorkspaceUsedBytes { get; set; }
    public bool WorkspaceActive { get; set; }

    /// <summary>
    /// Simple health score (higher = better). Used for rollback decisions.
    /// </summary>
    public double HealthScore
    {
        get
        {
            // Heavily penalize extreme disk activity when workspace is active
            double diskPenalty = WorkspaceActive && DiskQueueLength > 5 ? DiskQueueLength * 8 : 0;
            double ramPenalty = RamUsagePercent > 95 ? 30 : RamUsagePercent * 0.3;
            double cpuPenalty = CpuUsagePercent > 90 ? 20 : 0;

            return Math.Max(0, 100 - ramPenalty - cpuPenalty - diskPenalty);
        }
    }
}
