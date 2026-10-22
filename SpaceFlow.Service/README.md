# SpaceFlow.Service (Future)

This project will host the Windows background service for SpaceFlow in later versions.

For Version 0.1 the monitoring and workspace logic runs inside the UI process for simplicity.

Planned responsibilities:
- Run as a Windows Service
- Continue monitoring even when the UI is closed
- Communicate with the UI via named pipes or gRPC
- Automatic recovery and logging
