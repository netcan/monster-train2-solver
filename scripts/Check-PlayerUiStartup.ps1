#requires -Version 7.4
param(
    [string] $GameDirectory = 'F:/SteamLibrary/steamapps/common/Monster Train 2',
    [string] $PayloadDirectory = 'src/Advisor/bin/Release/netstandard2.1',
    [string] $ProbeDirectory = 'src/Probe/bin/Release/netstandard2.1',
    [ValidatePattern('^[a-zA-Z0-9-]+$')] [string] $Label = 'startup',
    [ValidateRange(2, 4)] [int] $NativeWorker = 2
)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
$PayloadDirectory = if ([IO.Path]::IsPathRooted($PayloadDirectory)) { $PayloadDirectory } else { Join-Path $workspace $PayloadDirectory }
$ProbeDirectory = if ([IO.Path]::IsPathRooted($ProbeDirectory)) { $ProbeDirectory } else { Join-Path $workspace $ProbeDirectory }
$gameRoot = Join-Path $workspace ".sandbox-game-worker$NativeWorker"
$gameExecutable = Join-Path $gameRoot 'MonsterTrain2.exe'
$nativeDll = Join-Path $gameRoot 'MonsterTrain2_Data/Managed/Assembly-CSharp.dll'
$plugins = Join-Path $gameRoot 'BepInEx/plugins'
if (-not (Test-Path -LiteralPath $gameExecutable -PathType Leaf) -or
    -not (Test-Path -LiteralPath (Join-Path $gameRoot 'BepInEx/core/BepInEx.dll') -PathType Leaf)) {
    throw "Native worker $NativeWorker is not initialized. Use scripts/Initialize-NativeWorker.ps1."
}
if ($NativeWorker -eq 3) {
    $nativePe = [System.Reflection.PortableExecutable.PEReader]::new([IO.File]::OpenRead($nativeDll))
    try {
        $metadata = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($nativePe)
        $module = $metadata.GetGuid($metadata.GetModuleDefinition().Mvid)
    } finally { $nativePe.Dispose() }
    if ($module -ne [Guid]'bf93f060-beda-46b1-b808-95ebf1e9a99d') {
        throw "Native worker 3 requires the pinned 2.3.1 module. Found $module."
    }
}
$sharedDlls = @('Model.dll', 'FixtureArchive.dll')
foreach ($name in $sharedDlls) {
    if ((Get-FileHash -LiteralPath (Join-Path $probeDirectory $name)).Hash -cne
        (Get-FileHash -LiteralPath (Join-Path $PayloadDirectory $name)).Hash) {
        throw "Probe and the player payload have different $name binaries."
    }
}
$originalGame = [IO.Path]::GetFullPath($GameDirectory)
$token = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
$runs = Join-Path $workspace '.probe-runs'
$profile = Join-Path $runs ('player-ui-' + $Label + '-' + $token)
$temporaryPlugin = Join-Path $plugins ('PlayerUiCheck-' + $token)
$configPath = Join-Path $gameRoot 'BepInEx/config/netcan.monstertrain2.advisor.cfg'
$loaderConfig = Join-Path $gameRoot 'BepInEx/config/BepInEx.cfg'
$backups = [ordered]@{}
$environment = @{ MT2_PROBE_DATA_DIR = $profile; MT2_PROBE_SCENARIO = 'player-ui' }
$logPath = Join-Path $profile 'unity-ui.log'
$process = $null
New-Item -ItemType Directory -Path $runs -Force | Out-Null
$lease = [IO.File]::Open((Join-Path $runs "native-worker-$NativeWorker.lock"), [IO.FileMode]::OpenOrCreate,
    [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
try {
    if (@(Get-CimInstance Win32_Process -Filter "Name='MonsterTrain2.exe'" | Where-Object {
        $_.ExecutablePath -and [IO.Path]::GetFullPath($_.ExecutablePath) -ieq $gameExecutable
    }).Count) {
        throw "Isolated UI worker $NativeWorker is already running."
    }
    $probeDlls = @('Probe.dll') + $sharedDlls
    $existing = @(Get-ChildItem -LiteralPath $plugins -Recurse -File | Where-Object {
        $_.Name -ieq 'MonsterTrain2Poju.Advisor.dll' -or ($_.Name -in $probeDlls -and $_.DirectoryName -ine $plugins)
    })
    if ($existing.Count) { throw "Existing advisor or duplicate Probe dependency in isolated worker $NativeWorker." }

    Copy-Item -LiteralPath (Join-Path $workspace '.probe-baseline') -Destination $profile -Recurse
    $preferencesPath = Join-Path $profile 'preferences.json'
    if (Test-Path -LiteralPath $preferencesPath) {
        $preferences = Get-Content -LiteralPath $preferencesPath -Raw | ConvertFrom-Json
        if ($null -eq $preferences.audioSettings) { throw 'Probe profile is missing audio settings.' }
        $preferences.audioSettings.globalMuted = $true
        $preferences | ConvertTo-Json -Depth 32 -Compress | Set-Content -LiteralPath $preferencesPath -NoNewline -Encoding utf8
    }

    # Temporarily park the worker's dev copies so the loader sees one shared payload.
    $workerFiles = @($probeDlls | ForEach-Object { Join-Path $plugins $_ }) + @($configPath, $loaderConfig)
    foreach ($path in $workerFiles) {
        $backup = Join-Path $profile ('previous-' + (Split-Path $path -Leaf))
        if (Test-Path -LiteralPath $path) {
            Move-Item -LiteralPath $path -Destination $backup
            $backups[$path] = $backup
        } else { $backups[$path] = $null }
    }
    New-Item -ItemType Directory -Path $temporaryPlugin | Out-Null
    Copy-Item -LiteralPath (Join-Path $probeDirectory 'Probe.dll') -Destination $temporaryPlugin
    foreach ($name in @('MonsterTrain2Poju.Advisor.dll') + $sharedDlls) {
        Copy-Item -LiteralPath (Join-Path $PayloadDirectory $name) -Destination $temporaryPlugin
    }
    Copy-Item -LiteralPath "$originalGame/BepInEx/plugins/Patty_RelicPicker_MOD.dll" -Destination $temporaryPlugin
    Copy-Item -LiteralPath "$originalGame/BepInEx/plugins/ConfigurationManager/ConfigurationManager.dll" -Destination $temporaryPlugin
    Copy-Item -LiteralPath "$originalGame/BepInEx/config/netcan.monstertrain2.advisor.cfg" -Destination $configPath
    Copy-Item -LiteralPath "$originalGame/BepInEx/config/BepInEx.cfg" -Destination $loaderConfig

    $process = Start-Process -FilePath $gameExecutable -WorkingDirectory $gameRoot `
        -ArgumentList @('-screen-fullscreen', '0', '-screen-width', '1280', '-screen-height', '800', '-logFile', $logPath) `
        -Environment $environment -WindowStyle Hidden -PassThru
    $deadline = [DateTime]::UtcNow.AddSeconds(28)
    do {
        $log = if (Test-Path -LiteralPath $logPath) { Get-Content -LiteralPath $logPath -Raw } else { '' }
        if ($log -match 'ADVISOR-UI ready') { Start-Sleep -Seconds 12; break }
        if ($process.HasExited) { break }
        Start-Sleep -Milliseconds 500
    } while ([DateTime]::UtcNow -lt $deadline)
    $log = Get-Content -LiteralPath $logPath -Raw
    $log -split "`n" | Where-Object { $_ -match 'POJU-|ADVISOR-|save-root|Unknown MT2|Error|Exception' -and $_.Length -lt 500 } | Select-Object -Last 20
    if ($log -notmatch 'isolated root: ' -or $log -notmatch 'probe update driver disabled' -or
        $log -notmatch 'POJU-READY' -or $log -notmatch 'POJU-UPDATE' -or $log -notmatch 'ADVISOR-UI ready' -or
        $log -match 'ADVISOR-FAIL|破局助手启动失败|POJU-HOST destroyed') { throw 'Player UI did not pass isolated bootstrap.' }
    Add-Type -AssemblyName System.Drawing
    Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class PojuUiCapture {
 [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
 [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr window, out RECT rect);
 [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr window, IntPtr context, uint flags);
}
'@
    $process.Refresh()
    $bounds = [PojuUiCapture+RECT]::new()
    $captured = $false
    if ($process.MainWindowHandle -ne [IntPtr]::Zero -and [PojuUiCapture]::GetWindowRect($process.MainWindowHandle, [ref]$bounds)) {
        $bitmap = [Drawing.Bitmap]::new(($bounds.Right - $bounds.Left), ($bounds.Bottom - $bounds.Top))
        $graphics = [Drawing.Graphics]::FromImage($bitmap)
        $context = $graphics.GetHdc()
        try { $captured = [PojuUiCapture]::PrintWindow($process.MainWindowHandle, $context, 2) }
        finally { $graphics.ReleaseHdc($context) }
        try { if ($captured) { $bitmap.Save((Join-Path $profile 'game-window.png'), [Drawing.Imaging.ImageFormat]::Png) } }
        finally { $graphics.Dispose(); $bitmap.Dispose() }
    }
    if (-not $captured) { throw 'Could not capture the isolated player window.' }
} finally {
    try {
        if ($process -and -not $process.HasExited) {
            [void]$process.CloseMainWindow()
            if (-not $process.WaitForExit(8000)) {
                Stop-Process -Id $process.Id
                $process.WaitForExit()
            }
        }
        foreach ($path in $backups.Keys) {
            if ($backups[$path]) { Move-Item -LiteralPath $backups[$path] -Destination $path -Force }
            elseif (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path }
        }
        if (Test-Path -LiteralPath $temporaryPlugin) {
            $resolvedTemporary = [IO.Path]::GetFullPath($temporaryPlugin)
            $expectedParent = [IO.Path]::GetFullPath($plugins).TrimEnd('\')
            if ((Split-Path $resolvedTemporary -Parent) -cne $expectedParent -or (Split-Path $resolvedTemporary -Leaf) -notlike 'PlayerUiCheck-*') {
                throw 'Unexpected temporary plugin cleanup target.'
            }
            Remove-Item -LiteralPath $resolvedTemporary -Recurse
        }
    } finally { $lease.Dispose() }
}
[pscustomobject]@{ Passed = $true; Profile = $profile; NativeWorker = $NativeWorker; PlayerOnlyUi = $true; Autoplay = $false; RealSaveAccess = $false } | ConvertTo-Json -Compress
