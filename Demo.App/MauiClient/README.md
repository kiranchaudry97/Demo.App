This is a minimal MAUI client GUI that calls the Demo.App API endpoints.

Quick start (local dev):

1. Start the web API and MAUI client (attempts to start both):
   - From the repo root run: `./run-dev.ps1` (PowerShell)
   - The script will start `Demo.App` on https://localhost:7187 and attempt to run the MAUI project.

2. If automatic MAUI launch fails: open `MauiClient/MauiClient.csproj` in Visual Studio and run on an emulator.

Notes:
- The MAUI project references `Demo.SharedModels` to reuse models.
- For the Android emulator, use base address `https://10.0.2.2:7187/` to reach the host machine.
- If HTTPS certificate errors occur on the emulator, either trust the dev certificate on the emulator or change the API to HTTP for local testing.
