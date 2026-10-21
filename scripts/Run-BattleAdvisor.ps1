#requires -Version 7.4
[CmdletBinding()]
param(
    [string] $Profile = '',
    [ValidateRange(1, 2)] [int] $NativeWorker = 1,
    [ValidateRange(2, 3)] [int] $Horizon = 3,
    [switch] $SkipBuild
)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
$gameRoot = Join-Path $workspace $(if ($NativeWorker -eq 1) { '.sandbox-game' } else { '.sandbox-game-worker2' })
$executable = Join-Path $gameRoot 'MonsterTrain2.exe'
$profilesRoot = [IO.Path]::GetFullPath((Join-Path $workspace '.probe-runs')).TrimEnd('\')
if (-not (Test-Path -LiteralPath $executable)) { throw "Missing isolated game: $executable" }
if (Get-CimInstance Win32_Process -Filter "Name='MonsterTrain2.exe'" | Where-Object ExecutablePath -EQ $executable) {
    throw "Worker $NativeWorker is already running. Close it or select the other worker."
}
if ($Profile) {
    $Profile = (Resolve-Path -LiteralPath $Profile).Path.TrimEnd('\')
    if ((Split-Path $Profile -Parent) -ine $profilesRoot) { throw 'Profile must be a direct child of this workspace .probe-runs directory.' }
} else {
    $Profile = Join-Path $profilesRoot ('advisor-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
    Copy-Item -LiteralPath (Join-Path $workspace '.probe-baseline') -Destination $Profile -Recurse
}
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/Probe/Probe.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE -ne 0) { throw 'Advisor build failed.' }
}
foreach ($assembly in @('Probe', 'Model', 'FixtureArchive')) {
    Copy-Item -LiteralPath (Join-Path $workspace "src/Probe/bin/Release/netstandard2.1/$assembly.dll") -Destination (Join-Path $gameRoot 'BepInEx/plugins') -Force
}
$start = [Diagnostics.ProcessStartInfo]::new($executable)
$start.WorkingDirectory = $gameRoot
$start.UseShellExecute = $false
$start.ArgumentList.Add('-logFile')
$start.ArgumentList.Add((Join-Path $Profile 'unity-scenario.log'))
# An interactive launch must never inherit a prior calibration/autoplay setup.
foreach ($name in @($start.Environment.Keys)) {
    if ($name.StartsWith('MT2_PROBE_') -or $name.StartsWith('MT2_ADVISOR_')) { $null = $start.Environment.Remove($name) }
}
$settings = @{
    MT2_PROBE_DATA_DIR = $Profile; MT2_PROBE_SCENARIO = 'advisor'
    MT2_PROBE_PLANNER_HORIZON = "$Horizon"
    MT2_PROBE_BRANCH_CARD_MASKS = '1'; MT2_PROBE_PHYSICAL_SPAWNPOINTS = '1'
    MT2_PROBE_SETTLE_CARD_ANIMATIONS = '1'
}
foreach ($entry in $settings.GetEnumerator()) { $start.Environment[$entry.Key] = $entry.Value }
# This is the interactive game window the user controls; no probe autoplay.
$process = [Diagnostics.Process]::Start($start)
[pscustomobject]@{ ProcessId = $process.Id; Profile = $Profile; Worker = $NativeWorker; Horizon = $Horizon; Controls = 'F8: show/hide; panel: refresh/pause' } | ConvertTo-Json -Compress
