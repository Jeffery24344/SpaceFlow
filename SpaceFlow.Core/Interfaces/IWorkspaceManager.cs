using SpaceFlow.Core.Models;

namespace SpaceFlow.Core.Interfaces;

public interface IWorkspaceManager
{
    WorkspaceInfo CurrentWorkspace { get; }

    /// <summary>
    /// Prepare / expand temporary workspace based on current pressure and settings.
    /// Returns true if workspace is ready.
    /// </summary>
    Task<bool> PrepareWorkspaceAsync(long requestedBytes, CancellationToken ct = default);

    /// <summary>
    /// Intelligent recommended size based on pressure, aggressiveness and storage type.
    /// </summary>
    long CalculateRecommendedSize(double ramPressurePercent, StorageType storageType);

    /// <summary>
    /// Reduce allocated workspace size (used by performance rollback).
    /// </summary>
    Task ReduceWorkspaceAsync(double factor = 0.5, CancellationToken ct = default);

    /// <summary>
    /// Reduce or fully release the temporary workspace.
    /// Only deletes SpaceFlow-created temporary data.
    /// </summary>
    Task ReleaseWorkspaceAsync(bool forceFullCleanup = false, CancellationToken ct = default);

    /// <summary>
    /// Returns how much space we can safely allocate on the preferred drive
    /// while respecting the safety reserve.
    /// </summary>
    long GetSafeAvailableBytes();

    event EventHandler<WorkspaceInfo>? WorkspaceChanged;
}
