#requires -Version 7.4
param(
    [string] $GameDirectory = 'F:/SteamLibrary/steamapps/common/Monster Train 2',
    [string] $PayloadDirectory = 'src/Advisor/bin/Release/netstandard2.1',
    [ValidatePattern('^[a-zA-Z0-9-]+$')] [string] $Label = 'startup'
)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
$PayloadDirectory = if ([IO.Path]::IsPathRooted($PayloadDirectory)) { $PayloadDirectory } else { Join-Path $workspace $PayloadDirectory }
$gameRoot = Join-Path $workspace '.sandbox-game-worker2'
$gameExecutable = Join-Path $gameRoot 'MonsterTrain2.exe'
if (@(Get-CimInstance Win32_Process -Filter "Name='MonsterTrain2.exe'" | Where-Object {
    $_.ExecutablePath -and [IO.Path]::GetFullPath($_.ExecutablePath) -ieq [IO.Path]::GetFullPath($gameExecutable)
}).Count) {
    throw 'Isolated UI worker is already running.'
}
$token = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
$profile = Join-Path $workspace ('.probe-runs/player-ui-' + $Label + '-' + $token)
Copy-Item -LiteralPath (Join-Path $workspace '.probe-baseline') -Destination $profile -Recurse
$plugins = Join-Path $gameRoot 'BepInEx/plugins'
if (@(Get-ChildItem -LiteralPath $plugins -Recurse -File -Filter 'MonsterTrain2Poju.Advisor.dll').Count) {
    throw 'Existing player plugin in isolated UI worker.'
}
$temporaryPlugin = Join-Path $plugins ('PlayerUiCheck-' + $token)
New-Item -ItemType Directory -Path $temporaryPlugin | Out-Null
foreach ($name in @('Probe', 'Model', 'FixtureArchive')) {
    Copy-Item -LiteralPath (Join-Path $workspace "src/Probe/bin/Release/netstandard2.1/$name.dll") -Destination $plugins -Force
}
foreach ($name in @('MonsterTrain2Poju.Advisor.dll', 'Model.dll', 'FixtureArchive.dll')) {
    Copy-Item -LiteralPath (Join-Path $PayloadDirectory $name) -Destination $temporaryPlugin
}
$originalGame = [IO.Path]::GetFullPath($GameDirectory)
Copy-Item -LiteralPath "$originalGame/BepInEx/plugins/Patty_RelicPicker_MOD.dll" -Destination $temporaryPlugin
Copy-Item -LiteralPath "$originalGame/BepInEx/plugins/ConfigurationManager/ConfigurationManager.dll" -Destination $temporaryPlugin
$configPath = Join-Path $gameRoot 'BepInEx/config/netcan.monstertrain2.advisor.cfg'
$configBackup = Join-Path $profile 'previous-player.cfg'
$hadConfig = Test-Path -LiteralPath $configPath
if ($hadConfig) { Copy-Item -LiteralPath $configPath -Destination $configBackup }
Copy-Item -LiteralPath "$originalGame/BepInEx/config/netcan.monstertrain2.advisor.cfg" -Destination $configPath -Force
$loaderConfig = Join-Path $gameRoot 'BepInEx/config/BepInEx.cfg'
$loaderBackup = Join-Path $profile 'previous-BepInEx.cfg'
Copy-Item -LiteralPath $loaderConfig -Destination $loaderBackup
Copy-Item -LiteralPath "$originalGame/BepInEx/config/BepInEx.cfg" -Destination $loaderConfig -Force
$environment = @{ MT2_PROBE_DATA_DIR = $profile; MT2_PROBE_SCENARIO = 'player-ui' }
$logPath = Join-Path $profile 'unity-ui.log'
$process = $null
try {
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
    [pscustomobject]@{ Passed = $true; Profile = $profile; PlayerOnlyUi = $true; Autoplay = $false; RealSaveAccess = $false } | ConvertTo-Json -Compress
} finally {
    if ($process -and -not $process.HasExited) {
        [void]$process.CloseMainWindow()
        if (-not $process.WaitForExit(8000)) { Stop-Process -Id $process.Id }
    }
    $resolvedTemporary = [IO.Path]::GetFullPath($temporaryPlugin)
    $expectedParent = [IO.Path]::GetFullPath($plugins).TrimEnd('\')
    if ((Split-Path $resolvedTemporary -Parent) -cne $expectedParent -or (Split-Path $resolvedTemporary -Leaf) -notlike 'PlayerUiCheck-*') {
        throw 'Unexpected temporary plugin cleanup target.'
    }
    Remove-Item -LiteralPath $resolvedTemporary -Recurse
    if ($hadConfig) { Copy-Item -LiteralPath $configBackup -Destination $configPath -Force }
    else { Remove-Item -LiteralPath $configPath }
    Copy-Item -LiteralPath $loaderBackup -Destination $loaderConfig -Force
}
