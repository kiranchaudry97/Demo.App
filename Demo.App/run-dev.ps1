param(
    [int]$ApiPort = 5500
)

Write-Host "Starting Demo.App (web API) on http://localhost:$ApiPort ..."

$apiProj = Join-Path -Path (Split-Path -Parent $PSScriptRoot) -ChildPath "Demo.App\Demo.App.csproj"
if (Test-Path $apiProj) {
    Start-Process -NoNewWindow -FilePath "dotnet" -ArgumentList "run --project `"$apiProj`" --urls http://localhost:$ApiPort" -WorkingDirectory (Split-Path $apiProj)
    Start-Sleep -Seconds 2
    Write-Host "Web API started (check output)."
} else {
    Write-Error "Could not find web project at $apiProj"
}

Write-Host "Attempting to start MAUI client (may require Visual Studio / emulator)."
$mauiProj = Join-Path -Path (Split-Path -Parent $PSScriptRoot) -ChildPath "MauiClient\MauiClient.csproj"
if (Test-Path $mauiProj) {
    try {
        $startInfo = @{ FilePath = 'dotnet'; ArgumentList = "run --project `"$mauiProj`""; WorkingDirectory = (Split-Path $mauiProj); NoNewWindow = $true; }
        # Set API_BASE env var for the MAUI process
        $env:API_BASE = "http://10.0.2.2:$ApiPort/"
        Start-Process @startInfo
    }
    catch {
        Write-Host "Automatic MAUI launch failed: $($_.Exception.Message)"
        Write-Host "Open the MAUI project in Visual Studio and run it in the emulator."
    }
} else {
    Write-Host "MAUI project not found at $mauiProj. Open MauiClient in Visual Studio to run the client."
}

Write-Host "If you see HTTPS certificate errors on the emulator, either trust the dev certificate on the emulator or change the API to use HTTP for local testing."
