@echo off
echo SpaceFlow - Building and running...
echo.
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET SDK not found.
    echo Download it free from: https://dotnet.microsoft.com/download/dotnet/8.0
    echo Install the Windows x64 SDK, then run this file again.
    pause
    exit /b 1
)
dotnet restore
if errorlevel 1 (
    echo Restore failed.
    pause
    exit /b 1
)
dotnet run --project SpaceFlow.UI
pause
