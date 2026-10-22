using SpaceFlow.Core.Interfaces;
using SpaceFlow.Core.Models;

namespace SpaceFlow.Workspace;

public sealed class WorkspaceManager : IWorkspaceManager
{
    private readonly AppSettings _settings;
    private readonly object _lock = new();
    private WorkspaceInfo _current = new();
    private const string WorkspaceFolderName = "SpaceFlow_TempWorkspace";
    private const string MarkerFileName = ".spaceflow_workspace";

    public event EventHandler<WorkspaceInfo>? WorkspaceChanged;
    public WorkspaceInfo CurrentWorkspace { get { lock (_lock) return Clone(_current); } }

    public WorkspaceManager(AppSettings settings)
    {
        _settings = settings;
        DiscoverExistingWorkspace();
    }

    public long GetSafeAvailableBytes()
    {
        try
        {
            string root = GetWorkspaceRoot();
            var drive = new DriveInfo(Path.GetPathRoot(root) ?? "C:\\");
            if (!drive.IsReady) return 0;
            long free = drive.AvailableFreeSpace;
            long safe = Math.Max(0, free - _settings.MinFreeDiskSpaceBytes);
            return Math.Min(safe, _settings.MaxWorkspaceSizeBytes);
        }
        catch { return 0; }
    }

    public long CalculateRecommendedSize(double ramPressurePercent, StorageType storageType)
    {
        long safeMax = GetSafeAvailableBytes();
        if (safeMax <= 0) return 0;
        double fraction = _settings.Aggressiveness / 10.0;
        if (ramPressurePercent >= 95) fraction = Math.Min(1.0, fraction + 0.25);
        else if (ramPressurePercent >= 90) fraction = Math.Min(1.0, fraction + 0.15);
        if (storageType == StorageType.NvmeSsd) fraction = Math.Min(1.0, fraction + 0.1);
        else if (storageType == StorageType.HDD) fraction *= 0.6;
        return Math.Min((long)(_settings.MaxWorkspaceSizeBytes * fraction), safeMax);
    }

    public async Task<bool> PrepareWorkspaceAsync(long requestedBytes, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                long safeMax = GetSafeAvailableBytes();
                if (safeMax <= 0) return false;
                long target = Math.Min(requestedBytes, safeMax);
                if (target < 64 * 1024 * 1024) return false;
                string root = GetWorkspaceRoot();
                Directory.CreateDirectory(root);
                string marker = Path.Combine(root, MarkerFileName);
                if (!File.Exists(marker))
                    File.WriteAllText(marker, $"SpaceFlow Temporary Workspace\nCreated: {DateTime.UtcNow:O}");
                var storageType = DetectStorageType(root);
                lock (_lock)
                {
                    _current = new WorkspaceInfo
                    {
                        IsActive = true, RootPath = root, AllocatedBytes = target,
                        UsedBytes = GetDirectorySize(root), MaxAllowedBytes = safeMax,
                        DriveLetter = _settings.PreferredDrive, StorageType = storageType
                    };
                }
                RaiseChanged();
                return true;
            }
            catch { return false; }
        }, ct);
    }

    public async Task ReduceWorkspaceAsync(double factor = 0.5, CancellationToken ct = default)
    {
        await Task.Run(() =>
        {
            lock (_lock)
            {
                if (!_current.IsActive) return;
                long newSize = (long)(_current.AllocatedBytes * Math.Clamp(factor, 0.1, 0.9));
                if (newSize < 64 * 1024 * 1024) { _ = ReleaseWorkspaceAsync(false, ct); return; }
                _current.AllocatedBytes = newSize;
            }
            RaiseChanged();
        }, ct);
    }

    public async Task ReleaseWorkspaceAsync(bool forceFullCleanup = false, CancellationToken ct = default)
    {
        await Task.Run(() =>
        {
            try
            {
                string root = GetWorkspaceRoot();
                if (Directory.Exists(root))
                {
                    string marker = Path.Combine(root, MarkerFileName);
                    if (File.Exists(marker) || forceFullCleanup)
                    {
                        try { Directory.Delete(root, recursive: true); } catch { }
                    }
                }
                lock (_lock)
                {
                    _current = new WorkspaceInfo
                    {
                        IsActive = false, RootPath = null, AllocatedBytes = 0, UsedBytes = 0,
                        MaxAllowedBytes = GetSafeAvailableBytes(), DriveLetter = _settings.PreferredDrive
                    };
                }
                RaiseChanged();
            }
            catch { }
        }, ct);
    }

    private void DiscoverExistingWorkspace()
    {
        try
        {
            string root = GetWorkspaceRoot();
            string marker = Path.Combine(root, MarkerFileName);
            if (Directory.Exists(root) && File.Exists(marker))
            {
                lock (_lock)
                {
                    long size = GetDirectorySize(root);
                    _current = new WorkspaceInfo
                    {
                        IsActive = true, RootPath = root, AllocatedBytes = size, UsedBytes = size,
                        MaxAllowedBytes = GetSafeAvailableBytes(), DriveLetter = _settings.PreferredDrive,
                        StorageType = DetectStorageType(root)
                    };
                }
            }
        }
        catch { }
    }

    private string GetWorkspaceRoot()
    {
        string drive = _settings.PreferredDrive.TrimEnd(':', '\\') + ":\\";
        return Path.Combine(drive, WorkspaceFolderName);
    }

    private static long GetDirectorySize(string path)
    {
        try { return new DirectoryInfo(path).EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length); }
        catch { return 0; }
    }

    private static StorageType DetectStorageType(string path)
    {
        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher(
                "SELECT MediaType, Model, InterfaceType FROM Win32_DiskDrive");
            foreach (var obj in searcher.Get())
            {
                string model = (obj["Model"]?.ToString() ?? "").ToUpperInvariant();
                string iface = (obj["InterfaceType"]?.ToString() ?? "").ToUpperInvariant();
                if (model.Contains("NVME") || iface.Contains("NVME") || model.Contains("NVM EXPRESS"))
                    return StorageType.NvmeSsd;
                if (model.Contains("SSD") || model.Contains("SOLID STATE"))
                    return StorageType.SataSsd;
            }
            return StorageType.Unknown;
        }
        catch { return StorageType.Unknown; }
    }

    private void RaiseChanged() => WorkspaceChanged?.Invoke(this, CurrentWorkspace);

    private static WorkspaceInfo Clone(WorkspaceInfo src) => new()
    {
        IsActive = src.IsActive, RootPath = src.RootPath, AllocatedBytes = src.AllocatedBytes,
        UsedBytes = src.UsedBytes, MaxAllowedBytes = src.MaxAllowedBytes,
        DriveLetter = src.DriveLetter, StorageType = src.StorageType
    };
}
