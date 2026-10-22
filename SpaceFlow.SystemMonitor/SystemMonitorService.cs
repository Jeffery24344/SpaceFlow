using System.Diagnostics;
using System.Runtime.InteropServices;
using SpaceFlow.Core.Interfaces;
using SpaceFlow.Core.Models;

namespace SpaceFlow.SystemMonitor;

/// <summary>
/// Monitors RAM, CPU, and disk using documented Windows APIs / Performance Counters.
/// Does NOT use undocumented hacks.
/// </summary>
public sealed class SystemMonitorService : ISystemMonitor, IDisposable
{
    private readonly AppSettings _settings;
    private readonly object _lock = new();
    private CancellationTokenSource? _cts;
    private Task? _monitorTask;
    private PerformanceCounter? _cpuCounter;

    public event EventHandler<SystemMetrics>? MetricsUpdated;
    public SystemMetrics CurrentMetrics { get; private set; } = new();
    public bool IsRunning { get; private set; }

    public SystemMonitorService(AppSettings settings)
    {
        _settings = settings;
    }

    public void Start(int intervalMs = 2000)
    {
        if (IsRunning) return;

        try
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _cpuCounter.NextValue(); // first call always returns 0
        }
        catch
        {
            // Performance counter may not be available in some environments
            _cpuCounter = null;
        }

        _cts = new CancellationTokenSource();
        IsRunning = true;
        _monitorTask = Task.Run(() => MonitorLoop(intervalMs, _cts.Token));
    }

    public void Stop()
    {
        if (!IsRunning) return;

        _cts?.Cancel();
        try { _monitorTask?.Wait(2000); } catch { /* ignore */ }
        _cts?.Dispose();
        _cts = null;
        _monitorTask = null;
        IsRunning = false;

        _cpuCounter?.Dispose();
        _cpuCounter = null;
    }

    private async Task MonitorLoop(int intervalMs, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var metrics = CollectMetrics();
                lock (_lock)
                {
                    CurrentMetrics = metrics;
                }
                MetricsUpdated?.Invoke(this, metrics);
            }
            catch (Exception)
            {
                // Keep running even if one sample fails
            }

            try
            {
                await Task.Delay(intervalMs, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private SystemMetrics CollectMetrics()
    {
        var metrics = new SystemMetrics
        {
            Timestamp = DateTime.UtcNow
        };

        // --- Physical RAM ---
        try
        {
            var memStatus = new MEMORYSTATUSEX();
            if (GlobalMemoryStatusEx(memStatus))
            {
                metrics.TotalPhysicalMemoryBytes = (long)memStatus.ullTotalPhys;
                metrics.AvailablePhysicalMemoryBytes = (long)memStatus.ullAvailPhys;
            }
        }
        catch { /* keep defaults */ }

        // --- CPU ---
        try
        {
            if (_cpuCounter != null)
            {
                metrics.CpuUsagePercent = Math.Clamp(_cpuCounter.NextValue(), 0, 100);
            }
        }
        catch { /* keep 0 */ }

        // --- Disk (preferred drive) ---
        try
        {
            string drive = _settings.PreferredDrive.TrimEnd(':') + ":\\";
            var driveInfo = new DriveInfo(drive);
            if (driveInfo.IsReady)
            {
                metrics.WorkspaceDriveLetter = driveInfo.Name;
                metrics.TotalDiskBytes = driveInfo.TotalSize;
                metrics.AvailableDiskBytes = driveInfo.AvailableFreeSpace;
            }
        }
        catch { /* keep defaults */ }

        // --- Memory Pressure classification ---
        metrics.PressureLevel = ClassifyPressure(metrics.RamUsagePercent);

        return metrics;
    }

    private MemoryPressureLevel ClassifyPressure(double ramPercent)
    {
        // Thresholds come from settings — never hard-coded permanently
        if (ramPercent >= _settings.HighPressureThresholdPercent)
            return MemoryPressureLevel.High;
        if (ramPercent >= _settings.ActivationThresholdPercent)
            return MemoryPressureLevel.Preparing;
        if (ramPercent >= 70.0) // soft monitoring band
            return MemoryPressureLevel.Monitoring;

        return MemoryPressureLevel.Normal;
    }

    public void Dispose()
    {
        Stop();
    }

    #region Native Windows API (documented)

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class MEMORYSTATUSEX
    {
        public uint dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

    #endregion
}
