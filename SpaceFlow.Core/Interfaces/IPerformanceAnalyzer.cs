using SpaceFlow.Core.Models;

namespace SpaceFlow.Core.Interfaces;

public interface IPerformanceAnalyzer
{
    /// <summary>
    /// Record a new snapshot. Called by the orchestrator on every metrics update.
    /// </summary>
    void Record(PerformanceSnapshot snapshot);

    /// <summary>
    /// Returns true if recent intervention appears to be making things worse.
    /// </summary>
    bool ShouldRollback { get; }

    /// <summary>
    /// Suggested action: Reduce, Hold, or Expand workspace.
    /// </summary>
    WorkspaceAction RecommendedAction { get; }

    double CurrentHealthScore { get; }
    double BaselineHealthScore { get; }
}

public enum WorkspaceAction
{
    Hold,
    Expand,
    Reduce,
    Release
}
