#requires -Version 7.4
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Serial', 'Parallel')]
    [string] $Mode,
    [switch] $Replay
)

$ErrorActionPreference = 'Stop'
$workspace = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$baseline = Join-Path $workspace '.probe-baseline'
$profileParent = Join-Path $workspace '.probe-runs'
$gameRoots = @(
    (Join-Path $workspace '.sandbox-game'),
    (Join-Path $workspace '.sandbox-game-worker2')
)
$originalLogRoot = Join-Path ([Environment]::GetFolderPath('UserProfile')) `
    'AppData\LocalLow\Shiny Shoe\MonsterTrain2'
$runId = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
$workload = if ($Replay) { 'replay' } else { 'direct' }
$jobs = @()

dotnet build (Join-Path $workspace 'src\Probe\Probe.csproj') -c Release
if ($LASTEXITCODE -ne 0) {
    throw 'Probe build failed.'
}
foreach ($gameRoot in $gameRoots) {
    $pluginDir = Join-Path $gameRoot 'BepInEx\plugins'
    Copy-Item -LiteralPath (Join-Path $workspace 'src\Probe\bin\Release\netstandard2.1\Probe.dll') `
        -Destination $pluginDir -Force
    Copy-Item -LiteralPath (Join-Path $workspace 'src\Model\bin\Release\netstandard2.1\Model.dll') `
        -Destination $pluginDir -Force
}

function Get-OriginalLogSignature {
    $entries = Get-ChildItem -LiteralPath $originalLogRoot -Filter 'logfile*.log' -File -Force |
        Sort-Object Name | ForEach-Object {
            $_.Name + ':' + $_.LastWriteTimeUtc.Ticks + ':' +
                (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
        }
    return [string]::Join('|', [string[]] @($entries))
}

function Get-LogFingerprint([string] $logPath, [string] $kind) {
    if (-not (Test-Path -LiteralPath $logPath)) {
        return ''
    }
    $passLabel = if ($Replay) { 'branched' } else { 'direct' }
    $line = (Select-String -LiteralPath $logPath -Pattern "SMOKE-$kind $passLabel-2 " |
        Select-Object -Last 1).Line
    if ([string]::IsNullOrEmpty($line)) {
        return ''
    }
    $value = $line -replace '^.*?SMOKE-(SNAPSHOT|RNG) (direct|branched)-2 ', ''
    $value = $value -replace 'NonDeterministic=\{[^}]*\} ?', ''
    if ($kind -eq 'RNG') {
        $value = $value -replace 'Chatter=\{[^}]*\} ?', ''
    }
    $bytes = [Text.Encoding]::UTF8.GetBytes($value)
    return [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes))
}

function Start-Worker($job) {
    $environment = @{
        MT2_PROBE_DATA_DIR = $job.Profile
        MT2_PROBE_SCENARIO = 'native-replay'
        MT2_PROBE_DEPTH = '2'
        MT2_PROBE_TARGET_TURN = '1'
        MT2_PROBE_SOURCE_PLAY_TURNS = ''
        MT2_PROBE_DIRECT_BRANCH = $(if ($Replay) { '0' } else { '1' })
        MT2_PROBE_FAST_REPLAY = '0'
        MT2_PROBE_NO_TIMEOUT = '1'
        MT2_PROBE_BRANCH_ANY_UNIT = '0'
    }
    $job.Timer = [Diagnostics.Stopwatch]::StartNew()
    $job.Process = Start-Process -FilePath (Join-Path $job.GameRoot 'MonsterTrain2.exe') `
        -WorkingDirectory $job.GameRoot -ArgumentList @('-logFile', $job.LogPath) `
        -Environment $environment -WindowStyle Hidden -PassThru
}

function Wait-Workers($activeJobs) {
    $waitTimer = [Diagnostics.Stopwatch]::StartNew()
    while ($true) {
        $running = 0
        foreach ($job in $activeJobs) {
            $job.Process.Refresh()
            if ($job.Process.HasExited) {
                if (-not $job.TimerStopped) {
                    $job.Timer.Stop()
                    $job.TimerStopped = $true
                }
                continue
            }
            $running++
            $job.PeakWorkingSetMB = [Math]::Max($job.PeakWorkingSetMB,
                [Math]::Round($job.Process.WorkingSet64 / 1MB, 1))
            $job.CpuSeconds = [Math]::Round($job.Process.TotalProcessorTime.TotalSeconds, 1)
        }
        if ($running -eq 0) {
            break
        }
        if ($waitTimer.Elapsed.TotalSeconds -gt 180) {
            foreach ($job in $activeJobs) {
                if (-not $job.Process.HasExited) {
                    $job.Process.Kill()
                }
            }
            throw 'A benchmark worker exceeded 180 seconds.'
        }
        Start-Sleep -Milliseconds 250
    }
    foreach ($job in $activeJobs) {
        $job.Process.WaitForExit()
    }
}

foreach ($index in 0..1) {
    $gameRoot = $gameRoots[$index]
    if (-not (Test-Path -LiteralPath (Join-Path $gameRoot 'BepInEx\plugins\Probe.dll'))) {
        throw "Probe DLL is missing from $gameRoot"
    }
    $profile = Join-Path $profileParent "benchmark-$workload-$($Mode.ToLowerInvariant())-$runId-$index"
    if (Test-Path -LiteralPath $profile) {
        throw "Profile already exists: $profile"
    }
    Copy-Item -LiteralPath $baseline -Destination $profile -Recurse
    $jobs += [pscustomobject]@{
        GameRoot = $gameRoot
        Profile = $profile
        LogPath = (Join-Path $profile 'unity-scenario.log')
        Process = $null
        Timer = $null
        TimerStopped = $false
        PeakWorkingSetMB = 0
        CpuSeconds = 0
    }
}

$originalLogsBefore = Get-OriginalLogSignature
$overall = [Diagnostics.Stopwatch]::StartNew()
try {
    if ($Mode -eq 'Serial') {
        foreach ($job in $jobs) {
            Start-Worker $job
            Wait-Workers @($job)
        }
    } else {
        foreach ($job in $jobs) {
            Start-Worker $job
        }
        Wait-Workers $jobs
    }
} finally {
    $overall.Stop()
    foreach ($job in $jobs) {
        if ($null -ne $job.Process -and -not $job.Process.HasExited) {
            $job.Process.Kill()
            $job.Process.WaitForExit()
        }
    }
}

$workerResults = @($jobs | ForEach-Object {
    $pass = (Test-Path -LiteralPath $_.LogPath) -and
        [bool] (Select-String -LiteralPath $_.LogPath -Pattern 'DEPTH-PASS')
    [pscustomobject]@{
        Profile = $_.Profile
        Pid = $_.Process.Id
        ExitCode = $_.Process.ExitCode
        Passed = $pass
        WallSeconds = [Math]::Round($_.Timer.Elapsed.TotalSeconds, 2)
        CpuSeconds = $_.CpuSeconds
        PeakWorkingSetMB = $_.PeakWorkingSetMB
        StateHash = Get-LogFingerprint $_.LogPath 'SNAPSHOT'
        GameplayRngHash = Get-LogFingerprint $_.LogPath 'RNG'
    }
})
$result = [pscustomobject]@{
    Mode = $Mode
    Workload = $workload
    RunId = $runId
    TotalWallSeconds = [Math]::Round($overall.Elapsed.TotalSeconds, 2)
    OriginalGameLogsUnchanged = ((Get-OriginalLogSignature) -ceq $originalLogsBefore)
    OutcomesEqual = ($workerResults[0].Passed -and $workerResults[1].Passed -and
        $workerResults[0].StateHash.Length -gt 0 -and $workerResults[0].GameplayRngHash.Length -gt 0 -and
        $workerResults[0].StateHash -ceq $workerResults[1].StateHash -and
        $workerResults[0].GameplayRngHash -ceq $workerResults[1].GameplayRngHash)
    Workers = $workerResults
}
$resultPath = Join-Path $workspace "results\benchmark-$workload-$($Mode.ToLowerInvariant())-$runId.json"
$result | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $resultPath -Encoding utf8
$result | ConvertTo-Json -Depth 5
