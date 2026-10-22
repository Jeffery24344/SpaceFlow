using SpaceFlow.Core.Models;

namespace SpaceFlow.Core.Interfaces;

public interface IApplicationMonitor
{
    event EventHandler<IReadOnlyList<ProcessInfo>>? TopProcessesUpdated;

    IReadOnlyList<ProcessInfo> CurrentTopProcesses { get; }

    void Start(int intervalMs = 3000);
    void Stop();
    bool IsRunning { get; }

    void SetProcessPreference(string processName, ProcessPriority priority);
    ProcessPriority GetProcessPreference(string processName);
}
