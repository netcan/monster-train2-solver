#requires -Version 7.4
[CmdletBinding()]
param(
    [ValidateRange(1, 9)] [int] $StopBeforeRing = 5,
    [int] $Seed = 424242,
    [string] $InputProfile = '',
    [string[]] $ExcludeUpgradeTrait = @(),
    [ValidateRange(1, 2)] [int] $NativeWorker = 1,
    [ValidateRange(60, 7200)] [int] $TimeoutSeconds = 1800,
    [switch] $SkipBuild
)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
$profilesRoot = [IO.Path]::GetFullPath((Join-Path $workspace '.probe-runs')).TrimEnd('\')
$gameRoot = Join-Path $workspace $(if ($NativeWorker -eq 1) { '.sandbox-game' } else { '.sandbox-game-worker2' })
$executable = Join-Path $gameRoot 'MonsterTrain2.exe'
if (Get-CimInstance Win32_Process -Filter "Name='MonsterTrain2.exe'" | Where-Object ExecutablePath -EQ $executable) {
    throw "Worker $NativeWorker is already running."
}
$source = Join-Path $workspace '.probe-baseline'
if ($InputProfile) {
    $source = (Resolve-Path -LiteralPath $InputProfile).Path.TrimEnd('\')
    if ((Split-Path $source -Parent) -ine $profilesRoot) { throw 'InputProfile must be a direct child of .probe-runs.' }
    if (-not (Test-Path -LiteralPath (Join-Path $source 'saves/save-singlePlayer.json'))) { throw 'InputProfile has no native save.' }
}
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/Probe/Probe.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE -ne 0) { throw 'Campaign driver build failed.' }
}
$originalRoot = Join-Path ([Environment]::GetFolderPath('UserProfile')) 'AppData/LocalLow/Shiny Shoe/MonsterTrain2'
function Get-OriginalHashes {
    @(Get-ChildItem -LiteralPath $originalRoot -File -Recurse | Where-Object {
        $_.Name -match '^(save-|metagameSave|preferences|prefs)' -or $_.Directory.Name -eq 'saves'
    } | Sort-Object FullName | ForEach-Object { $_.FullName + '=' + (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash })
}
$originalBefore = Get-OriginalHashes
$profile = Join-Path $profilesRoot ('campaign-ring' + $StopBeforeRing + '-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
$null = New-Item -ItemType Directory -Path $profile
foreach ($item in Get-ChildItem -LiteralPath $source) {
    if (-not $InputProfile) { Copy-Item -LiteralPath $item.FullName -Destination $profile -Recurse; continue }
    if ($item.Name -in @('saves', 'metagameSave.json', 'preferences.json', 'preferences-local.json', 'prefs.json') -or
        ($item.PSIsContainer -and $item.Name -in @('metagame', 'preferences'))) {
        Copy-Item -LiteralPath $item.FullName -Destination $profile -Recurse
    }
}
# Preserve a chain of native inputs without copying large battle traces or logs.
if (Test-Path -LiteralPath (Join-Path $source 'campaign-audit.jsonl')) {
    Copy-Item -LiteralPath (Join-Path $source 'campaign-audit.jsonl') -Destination (Join-Path $profile 'campaign-parent-audit.jsonl')
}
$savePath = Join-Path $profile 'saves/save-singlePlayer.json'
[pscustomobject]@{ InputProfile = $source; InputSaveSHA256 = (Get-FileHash -LiteralPath $savePath -Algorithm SHA256).Hash;
    StopBeforeRing = $StopBeforeRing; Seed = $Seed; NativeWorker = $NativeWorker; ExcludeUpgradeTrait = $ExcludeUpgradeTrait } |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $profile 'campaign-input.json')
foreach ($assembly in @('Probe', 'Model', 'FixtureArchive')) {
    Copy-Item -LiteralPath (Join-Path $workspace "src/Probe/bin/Release/netstandard2.1/$assembly.dll") -Destination (Join-Path $gameRoot 'BepInEx/plugins') -Force
}
$start = [Diagnostics.ProcessStartInfo]::new($executable)
$start.WorkingDirectory = $gameRoot
$start.UseShellExecute = $false
$start.WindowStyle = [Diagnostics.ProcessWindowStyle]::Hidden
$start.ArgumentList.Add('-logFile')
$start.ArgumentList.Add((Join-Path $profile 'unity-scenario.log'))
foreach ($name in @($start.Environment.Keys)) {
    if ($name.StartsWith('MT2_PROBE_') -or $name.StartsWith('MT2_ADVISOR_')) { $null = $start.Environment.Remove($name) }
}
$settings = @{
    MT2_PROBE_DATA_DIR = $profile; MT2_PROBE_SCENARIO = 'campaign'; MT2_PROBE_SEED = "$Seed"
    MT2_PROBE_CAMPAIGN_STOP_RING = "$StopBeforeRing"; MT2_PROBE_SETTLE_CARD_ANIMATIONS = '1'
    MT2_PROBE_CAMPAIGN_EXCLUDED_UPGRADE_TRAITS = ($ExcludeUpgradeTrait -join ';')
    MT2_PROBE_BRANCH_CARD_MASKS = '1'; MT2_PROBE_PHYSICAL_SPAWNPOINTS = '1'; MT2_PROBE_GAME_SPEED = 'Instant'
}
if ($InputProfile) { $settings.MT2_PROBE_RESUME_RUN = '1' }
foreach ($entry in $settings.GetEnumerator()) { $start.Environment[$entry.Key] = $entry.Value }
$process = [Diagnostics.Process]::Start($start)
Write-Host "CAMPAIGN-RUN Profile=$profile ProcessId=$($process.Id)"
$deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
while (-not $process.WaitForExit(1000)) {
    if ([DateTime]::UtcNow -gt $deadline) { $process.Kill(); throw "Campaign timed out: $profile" }
}
$originalAfter = Get-OriginalHashes
$unchanged = (($originalBefore -join "`n") -ceq ($originalAfter -join "`n"))
$audit = @(Get-Content -LiteralPath (Join-Path $profile 'campaign-audit.jsonl') | ForEach-Object { $_ | ConvertFrom-Json })
$passed = $process.ExitCode -eq 0 -and $audit[-1].kind -eq 'complete' -and $unchanged
$summary = [pscustomobject]@{ Profile = $profile; NativePassed = $passed; ExitCode = $process.ExitCode;
    OriginalFilesUnchanged = $unchanged; Events = $audit.Count; BattlesWon = @($audit | Where-Object { $_.kind -eq 'battle-outcome' -and $_.data.won }).Count;
    FinalSaveSHA256 = (Get-FileHash -LiteralPath $savePath -Algorithm SHA256).Hash }
$summary | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $profile 'campaign-summary.json')
$summary | ConvertTo-Json -Compress
if (-not $passed) { throw "Native campaign did not complete: $profile" }
