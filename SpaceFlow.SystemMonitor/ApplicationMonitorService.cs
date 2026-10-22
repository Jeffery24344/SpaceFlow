using System.Diagnostics;
using SpaceFlow.Core.Interfaces;
using SpaceFlow.Core.Models;

namespace SpaceFlow.SystemMonitor;

/// <summary>
/// Detects applications that are consuming significant memory / CPU.
/// Uses only documented Process APIs.
/// </summary>
public sealed class ApplicationMonitorService : IApplicationMonitor, IDisposable
{
    private readonly AppSettings _settings;
    private readonly object _lock = new();
    private CancellationTokenSource? _cts;
    private Task? _task;
    private List<ProcessInfo> _current = new();

    public event EventHandler<IReadOnlyList<ProcessInfo>>? TopProcessesUpdated;
    public IReadOnlyList<ProcessInfo> CurrentTopProcesses
    {
        get { lock (_lock) return _current.ToList(); }
    }
    public bool IsRunning { get; private set; }

    public ApplicationMonitorService(AppSettings settings)
    {
        _settings = settings;
    }

    public void Start(int intervalMs = 3000)
    {
        if (IsRunning) return;
        _cts = new CancellationTokenSource();
        IsRunning = true;
        _task = Task.Run(() => Loop(intervalMs, _cts.Token));
    }

    public void Stop()
    {
        if (!IsRunning) return;
        _cts?.Cancel();
        try { _task?.Wait(1500); } catch { }
        _cts?.Dispose();
        _cts = null;
        _task = null;
        IsRunning = false;
    }

    public void SetProcessPreference(string processName, ProcessPriority priority)
    {
        _settings.ProcessPreferences[processName] = priority;
    }

    public ProcessPriority GetProcessPreference(string processName)
    {
        return _settings.ProcessPreferences.TryGetValue(processName, out var p)
            ? p
            : ProcessPriority.Automatic;
    }

    private async Task Loop(int intervalMs, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var list = CollectTopProcesses();
                lock (_lock) { _current = list; }
                TopProcessesUpdated?.Invoke(this, list);
            }
            catch { /* keep running */ }

            try { await Task.Delay(intervalMs, ct); }
            catch (OperationCanceledException) { break; }
        }
    }

    private List<ProcessInfo> CollectTopProcesses()
    {
        var result = new List<ProcessInfo>();

        Process[] processes;
        try
        {
            processes = Process.GetProcesses();
        }
        catch
        {
            return result;
        }

        foreach (var p in processes)
        {
            try
            {
                // Skip system / idle processes that we should never touch
                if (p.Id == 0 || p.Id == 4) continue;
                if (string.IsNullOrWhiteSpace(p.ProcessName)) continue;

                long workingSet = 0;
                try { workingSet = p.WorkingSet64; } catch { continue; }

                if (workingSet < _settings.MinProcessMemoryBytes) continue;

                string? title = null;
                try { title = p.MainWindowTitle; } catch { }

                var info = new ProcessInfo
                {
                    ProcessId = p.Id,
                    ProcessName = p.ProcessName,
                    MainWindowTitle = string.IsNullOrWhiteSpace(title) ? null : title,
                    WorkingSetBytes = workingSet,
                    PrivateMemoryBytes = SafePrivateMemory(p),
                    Preference = GetProcessPreference(p.ProcessName)
                };

                result.Add(info);
            }
            catch
            {
                // Access denied or process exited — ignore
            }
            finally
            {
                try { p.Dispose(); } catch { }
            }
        }

        // Sort by memory descending and take top N
        return result
            .OrderByDescending(x => x.WorkingSetBytes)
            .Take(_settings.TopProcessesToTrack)
            .ToList();
    }

    private static long SafePrivateMemory(Process p)
    {
        try { return p.PrivateMemorySize64; }
        catch { return 0; }
    }

    public void Dispose() => Stop();
}
