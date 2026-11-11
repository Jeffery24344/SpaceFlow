using SpaceFlow.Core.Interfaces;
using SpaceFlow.Core.Models;

namespace SpaceFlow.SystemMonitor;

public sealed class PerformanceAnalyzer : IPerformanceAnalyzer
{
    private readonly AppSettings _settings;
    private readonly Queue<PerformanceSnapshot> _history = new();
    private readonly object _lock = new();

    private double _baselineHealth = 70;

    public bool ShouldRollback { get; private set; }
    public WorkspaceAction RecommendedAction { get; private set; } = WorkspaceAction.Hold;
    public double CurrentHealthScore { get; private set; } = 70;
    public double BaselineHealthScore => _baselineHealth;

    public PerformanceAnalyzer(AppSettings settings)
    {
        _settings = settings;
    }

    public void Record(PerformanceSnapshot snapshot)
    {
        lock (_lock)
        {
            _history.Enqueue(snapshot);

            int maxSamples = Math.Max(5, _settings.PerformanceHistorySeconds * 1000 / Math.Max(500, _settings.MonitorIntervalMs));
            while (_history.Count > maxSamples)
                _history.Dequeue();

            CurrentHealthScore = snapshot.HealthScore;

            if (!snapshot.WorkspaceActive)
            {
                _baselineHealth = (_baselineHealth * 0.85) + (snapshot.HealthScore * 0.15);
                ShouldRollback = false;
                RecommendedAction = WorkspaceAction.Hold;
                return;
            }

            double drop = _baselineHealth - snapshot.HealthScore;

            if (_settings.EnablePerformanceRollback && drop >= _settings.RollbackHealthDropThreshold)
            {
                ShouldRollback = true;
                RecommendedAction = drop >= _settings.RollbackHealthDropThreshold * 1.8
                    ? WorkspaceAction.Release
                    : WorkspaceAction.Reduce;
            }
            else if (snapshot.RamUsagePercent >= _settings.HighPressureThresholdPercent
                     && snapshot.DiskQueueLength < 3
                     && snapshot.HealthScore >= _baselineHealth - 5)
            {
                RecommendedAction = WorkspaceAction.Expand;
                ShouldRollback = false;
            }
            else
            {
                RecommendedAction = WorkspaceAction.Hold;
                ShouldRollback = false;
            }
        }
    }
}
