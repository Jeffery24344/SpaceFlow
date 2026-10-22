using System.Windows;
using System.Windows.Media;
using SpaceFlow.Core.Interfaces;
using SpaceFlow.Core.Models;
using SpaceFlow.SystemMonitor;
using SpaceFlow.Workspace;

namespace SpaceFlow.UI;

public partial class MainWindow : Window
{
    private readonly AppSettings _settings;
    private readonly SystemMonitorService _monitor;
    private readonly ApplicationMonitorService _appMonitor;
    private readonly PerformanceAnalyzer _perfAnalyzer;
    private readonly WorkspaceManager _workspace;
    private bool _isRunning;
    private bool _workspaceActive;

    public MainWindow()
    {
        InitializeComponent();
        _settings = new AppSettings
        {
            PreferredDrive = "C",
            MaxWorkspaceSizeBytes = 16L * 1024 * 1024 * 1024,
            MinFreeDiskSpaceBytes = 10L * 1024 * 1024 * 1024,
            ActivationThresholdPercent = 85,
            HighPressureThresholdPercent = 92,
            ReleaseThresholdPercent = 70,
            MonitorIntervalMs = 2000,
            EnableSmartMode = true,
            EnablePerformanceRollback = true,
            Aggressiveness = 5,
            CurrentMode = FlowMode.Local
        };
        _monitor = new SystemMonitorService(_settings);
        _appMonitor = new ApplicationMonitorService(_settings);
        _perfAnalyzer = new PerformanceAnalyzer(_settings);
        _workspace = new WorkspaceManager(_settings);
        _monitor.MetricsUpdated += OnMetricsUpdated;
        _appMonitor.TopProcessesUpdated += OnTopProcessesUpdated;
        _workspace.WorkspaceChanged += OnWorkspaceChanged;
        UpdateStatus(false);
        ModeText.Text = "LOCAL";
    }

    private void BtnStart_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning) return;
        _monitor.Start(_settings.MonitorIntervalMs);
        _appMonitor.Start(3000);
        _isRunning = true;
        BtnStart.IsEnabled = false;
        BtnStop.IsEnabled = true;
        UpdateStatus(true);
    }

    private void BtnStop_Click(object sender, RoutedEventArgs e)
    {
        if (!_isRunning) return;
        _monitor.Stop();
        _appMonitor.Stop();
        _ = _workspace.ReleaseWorkspaceAsync(forceFullCleanup: true);
        _isRunning = false;
        _workspaceActive = false;
        BtnStart.IsEnabled = true;
        BtnStop.IsEnabled = false;
        UpdateStatus(false);
        WorkspaceValueText.Text = "0 / 0 GB";
        WorkspaceBar.Value = 0;
        WorkspaceStatusText.Text = "Inactive";
        ProcessList.ItemsSource = null;
        HealthText.Text = "Health: —";
    }

    private void BtnLocal_Click(object sender, RoutedEventArgs e)
    {
        _settings.CurrentMode = FlowMode.Local;
        ModeText.Text = "LOCAL";
    }

    private void BtnPrioritize_Click(object sender, RoutedEventArgs e)
    {
        if (ProcessList.SelectedItem is ProcessInfo p)
        {
            _appMonitor.SetProcessPreference(p.ProcessName, ProcessPriority.Prioritize);
            p.Preference = ProcessPriority.Prioritize;
            ProcessList.Items.Refresh();
        }
    }

    private void BtnIgnore_Click(object sender, RoutedEventArgs e)
    {
        if (ProcessList.SelectedItem is ProcessInfo p)
        {
            _appMonitor.SetProcessPreference(p.ProcessName, ProcessPriority.Ignore);
            p.Preference = ProcessPriority.Ignore;
            ProcessList.Items.Refresh();
        }
    }

    private void BtnResetPref_Click(object sender, RoutedEventArgs e)
    {
        if (ProcessList.SelectedItem is ProcessInfo p)
        {
            _appMonitor.SetProcessPreference(p.ProcessName, ProcessPriority.Automatic);
            p.Preference = ProcessPriority.Automatic;
            ProcessList.Items.Refresh();
        }
    }

    private async void OnMetricsUpdated(object? sender, SystemMetrics metrics)
    {
        await Dispatcher.InvokeAsync(async () =>
        {
            RamValueText.Text = metrics.RamDisplay;
            RamBar.Value = metrics.RamUsagePercent;
            RamPercentText.Text = $"{metrics.RamUsagePercent:0.0}%";
            CpuText.Text = $"CPU: {metrics.CpuUsagePercent:0.0}%";
            PressureText.Text = metrics.PressureLevel.ToString().ToUpperInvariant();
            PressureText.Foreground = metrics.PressureLevel switch
            {
                MemoryPressureLevel.High => (Brush)FindResource("Danger"),
                MemoryPressureLevel.Preparing => (Brush)FindResource("Warning"),
                MemoryPressureLevel.Monitoring => (Brush)FindResource("Accent"),
                _ => (Brush)FindResource("Success")
            };
            DiskText.Text = $"Drive: {metrics.WorkspaceDriveLetter ?? _settings.PreferredDrive}  •  Free: {SystemMetrics.FormatBytes(metrics.AvailableDiskBytes)}";
            var ws = _workspace.CurrentWorkspace;
            StorageTypeText.Text = $"Type: {ws.StorageType}";
            var snap = new PerformanceSnapshot
            {
                RamUsagePercent = metrics.RamUsagePercent,
                CpuUsagePercent = metrics.CpuUsagePercent,
                DiskQueueLength = 0,
                WorkspaceUsedBytes = ws.UsedBytes,
                WorkspaceActive = ws.IsActive
            };
            _perfAnalyzer.Record(snap);
            HealthText.Text = $"Health: {_perfAnalyzer.CurrentHealthScore:0}  (base {_perfAnalyzer.BaselineHealthScore:0})";
            if (_isRunning && _settings.EnableSmartMode)
                await HandleSmartLogicAsync(metrics, ws);
        });
    }

    private async Task HandleSmartLogicAsync(SystemMetrics metrics, WorkspaceInfo ws)
    {
        if (_settings.EnablePerformanceRollback && _workspaceActive)
        {
            switch (_perfAnalyzer.RecommendedAction)
            {
                case WorkspaceAction.Release:
                    await _workspace.ReleaseWorkspaceAsync();
                    _workspaceActive = false;
                    return;
                case WorkspaceAction.Reduce:
                    await _workspace.ReduceWorkspaceAsync(0.5);
                    return;
            }
        }
        var top = _appMonitor.CurrentTopProcesses;
        if (top.Count > 0 && top.All(p => p.Preference == ProcessPriority.Ignore)) return;
        if (metrics.PressureLevel is MemoryPressureLevel.High or MemoryPressureLevel.Preparing)
        {
            long recommended = _workspace.CalculateRecommendedSize(metrics.RamUsagePercent, ws.StorageType);
            if (!_workspaceActive)
            {
                if (await _workspace.PrepareWorkspaceAsync(recommended))
                    _workspaceActive = true;
            }
            else if (_perfAnalyzer.RecommendedAction == WorkspaceAction.Expand)
            {
                long bigger = Math.Min(recommended, (long)(ws.AllocatedBytes * 1.25));
                await _workspace.PrepareWorkspaceAsync(bigger);
            }
        }
        else if (metrics.PressureLevel == MemoryPressureLevel.Normal && _workspaceActive)
        {
            if (metrics.RamUsagePercent < _settings.ReleaseThresholdPercent)
            {
                await _workspace.ReleaseWorkspaceAsync();
                _workspaceActive = false;
            }
        }
    }

    private void OnTopProcessesUpdated(object? sender, IReadOnlyList<ProcessInfo> processes)
    {
        Dispatcher.Invoke(() => ProcessList.ItemsSource = processes);
    }

    private void OnWorkspaceChanged(object? sender, WorkspaceInfo info)
    {
        Dispatcher.Invoke(() =>
        {
            if (info.IsActive)
            {
                WorkspaceValueText.Text = $"{info.DisplayUsed} / {info.DisplayAllocated}";
                WorkspaceBar.Value = info.UsagePercent;
                WorkspaceStatusText.Text = $"Active on {info.DriveLetter}: ({info.StorageType})";
                StorageTypeText.Text = $"Type: {info.StorageType}";
            }
            else
            {
                WorkspaceValueText.Text = "0 / 0 GB";
                WorkspaceBar.Value = 0;
                WorkspaceStatusText.Text = "Inactive";
            }
        });
    }

    private void UpdateStatus(bool running)
    {
        if (running)
        {
            StatusDot.Fill = (Brush)FindResource("Success");
            StatusText.Text = "ACTIVE";
            StatusText.Foreground = (Brush)FindResource("Success");
        }
        else
        {
            StatusDot.Fill = (Brush)FindResource("TextSecondary");
            StatusText.Text = "IDLE";
            StatusText.Foreground = (Brush)FindResource("TextSecondary");
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _monitor.Stop();
        _appMonitor.Stop();
        _ = _workspace.ReleaseWorkspaceAsync(forceFullCleanup: true);
        base.OnClosed(e);
    }
}
