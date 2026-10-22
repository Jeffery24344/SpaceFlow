# SpaceFlow — Intelligent Temporary Workspace for Low-Spec PCs

**Tagline:** More Room When You Need It.

> SpaceFlow is a Windows desktop application that intelligently provides temporary workspace using local storage (and optionally cloud) when the system experiences high memory pressure.  
> It does **NOT** fake RAM. It never claims SSD/HDD or cloud storage equals physical RAM.

## Philosophy

> The computer should use only the resources it needs, only when it needs them, and release them when the workload is finished.

## Current Status: Version 0.2 — Smart Local Flow

### Implemented

| Feature | Status |
|---------|--------|
| Modern WPF dashboard | ✓ |
| Real-time RAM / CPU / Disk monitoring | ✓ |
| Memory pressure detection (configurable thresholds) | ✓ |
| Local temporary workspace (create / size / release) | ✓ |
| Automatic cleanup (only SpaceFlow data) | ✓ |
| Safety margins (never fill the drive) | ✓ |
| Application detection (top memory consumers) | ✓ |
| Process preference controls (Prioritize / Ignore / Automatic) | ✓ |
| SSD / HDD / NVMe detection (WMI best-effort) | ✓ |
| Intelligent workspace sizing | ✓ |
| Performance analyzer + automatic rollback | ✓ |
| Health score display | ✓ |

### Still planned

- Cloud Flow (v0.3)
- Full Hybrid decision engine (v0.4)
- Windows Service background mode
- Settings window / persistence
- Notifications
- Installer

## Easiest way to get a runnable build (no Visual Studio needed)

### Option A — Download from GitHub Actions (recommended)

1. Open **Actions** on this repository: https://github.com/Jeffery24344/SpaceFlow/actions  
2. Click the latest **Build SpaceFlow** run (or press **Run workflow** under *workflow_dispatch*).  
3. When it finishes (green check), open the run → **Artifacts** → download **SpaceFlow-Windows-x64**.  
4. Unzip the folder and double-click **SpaceFlow.UI.exe**.

The app is self-contained (no extra .NET install required on the PC that runs it).

### Option B — Build on your PC with free tools only

1. Install **.NET 8 SDK**: https://dotnet.microsoft.com/download/dotnet/8.0 (Windows x64)  
2. Download or clone this repo.  
3. Double-click **run.bat**  
   *or* in Command Prompt / PowerShell:

```bash
dotnet restore
dotnet run --project SpaceFlow.UI
```

Optional: VS Code + **C# Dev Kit** extension if you want an editor.

## Project Structure

```
SpaceFlow/
├── SpaceFlow.sln
├── SpaceFlow.Core/               # Models, interfaces, settings
├── SpaceFlow.SystemMonitor/      # RAM, CPU, Disk, Apps, Performance
├── SpaceFlow.Workspace/          # Temporary workspace manager
├── SpaceFlow.UI/                 # WPF Dashboard
└── SpaceFlow.Service/            # (Future) Background service
```

## Key Safety Rules (enforced in code)

- Never present storage as RAM  
- Never fill the entire drive (configurable safety reserve)  
- Only delete SpaceFlow-created temporary data (marker-file protected)  
- Prefer faster storage when detected  
- If intervention makes performance worse → automatically reduce or stop  
- Responsiveness > maximum memory utilization  

## Version Roadmap

| Version | Focus |
|---------|-------|
| 0.1     | Prototype (UI + basic monitor + workspace) |
| **0.2** | **Smart Local Flow (current)** |
| 0.3     | Cloud Flow |
| 0.4     | Hybrid decision engine |
| 1.0     | Production release |

---

Built with safety, transparency, and real technical limitations in mind.
