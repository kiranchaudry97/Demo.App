param(
    [string]$AvdName = "DemoApp_AVD",
    [string]$ApiLevel = "33",
    [string]$SystemImage = "system-images;android-33;google_apis;x86_64"
)

function Find-AndroidSdkRoot {
    if ($env:ANDROID_SDK_ROOT) { return $env:ANDROID_SDK_ROOT }
    if ($env:ANDROID_HOME) { return $env:ANDROID_HOME }
    $possible = "$env:LOCALAPPDATA\Android\Sdk"
    if (Test-Path $possible) { return $possible }
    return $null
}

$sdkRoot = Find-AndroidSdkRoot
if (-not $sdkRoot) {
    Write-Error "Android SDK not found. Set ANDROID_SDK_ROOT or ANDROID_HOME, or install Android SDK and command-line tools."
    exit 1
}

Write-Host "Android SDK Root: $sdkRoot"

function CmdExists($name) {
    $which = Get-Command $name -ErrorAction SilentlyContinue
    return $which -ne $null
}

if (-not (CmdExists sdkmanager)) {
    $sdkManagerPath = Join-Path $sdkRoot "cmdline-tools\latest\bin\sdkmanager.bat"
    if (-not (Test-Path $sdkManagerPath)) {
        $sdkManagerPath = Join-Path $sdkRoot "tools\bin\sdkmanager.bat"
    }
    if (Test-Path $sdkManagerPath) { $env:PATH = "$($sdkRoot)\cmdline-tools\latest\bin;$($sdkRoot)\tools\bin;$env:PATH" }
}

if (-not (CmdExists avdmanager)) {
    $avdManagerPath = Join-Path $sdkRoot "cmdline-tools\latest\bin\avdmanager.bat"
    if (-not (Test-Path $avdManagerPath)) {
        $avdManagerPath = Join-Path $sdkRoot "tools\bin\avdmanager.bat"
    }
    if (Test-Path $avdManagerPath) { $env:PATH = "$($sdkRoot)\cmdline-tools\latest\bin;$($sdkRoot)\tools\bin;$env:PATH" }
}

if (-not (CmdExists emulator)) {
    $emulatorPath = Join-Path $sdkRoot "emulator"
    if (Test-Path $emulatorPath) { $env:PATH = "$emulatorPath;$env:PATH" }
}

if (-not (CmdExists adb)) {
    $platformTools = Join-Path $sdkRoot "platform-tools"
    if (Test-Path $platformTools) { $env:PATH = "$platformTools;$env:PATH" }
}

if (-not (CmdExists sdkmanager -or (Test-Path (Join-Path $sdkRoot "tools\bin\sdkmanager.bat")))) {
    Write-Warning "sdkmanager not found in PATH. Please install Android command-line tools and ensure sdkmanager is available."
}

Write-Host "Installing SDK components (platform-tools, emulator, system image) -- this may take a while..."
& sdkmanager --install "platform-tools" "emulator" "platforms;android-$ApiLevel" "$SystemImage" 2>&1 | ForEach-Object { Write-Host $_ }

Write-Host "Creating AVD '$AvdName' (system image: $SystemImage)"
# avdmanager may prompt; pass 'no' to skip editing the hardware profile if prompted
echo no | avdmanager create avd -n $AvdName -k "$SystemImage" -d "pixel" 2>&1 | ForEach-Object { Write-Host $_ }

Write-Host "Starting emulator '$AvdName'"
Start-Process -FilePath emulator -ArgumentList "-avd $AvdName -netdelay none -netspeed full" -NoNewWindow

Write-Host "Waiting for device to be online..."
for ($i = 0; $i -lt 120; $i++) {
    Start-Sleep -Seconds 2
    $devices = (& adb devices) -join "`n"
    if ($devices -match "device`$" ) {
        Write-Host "Emulator is online."
        break
    }
    Write-Host -NoNewline "."
}

Write-Host "Done. Use 'adb logcat' to view logs or run the MAUI app from Visual Studio."
