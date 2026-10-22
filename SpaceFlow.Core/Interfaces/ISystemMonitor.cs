using SpaceFlow.Core.Models;

namespace SpaceFlow.Core.Interfaces;

public interface ISystemMonitor
{
    /// <summary>
    /// Raised whenever new metrics are available.
    /// </summary>
    event EventHandler<SystemMetrics>? MetricsUpdated;

    SystemMetrics CurrentMetrics { get; }

    void Start(int intervalMs = 2000);
    void Stop();
    bool IsRunning { get; }
}
