#requires -Version 7.4
[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateRange(1, 10)][int] $Rank,
    [Parameter(Mandatory)][string] $InputProfile,
    [ValidateRange(2, 8)][int] $StopAfterRing = 8,
    [ValidateRange(1, 2)][int] $NativeWorker = 1,
    [ValidateRange(60, 3600)][int] $TimeoutSeconds = 1200,
    [switch] $SkipBuild
)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
$profilesRoot = [IO.Path]::GetFullPath((Join-Path $workspace '.probe-runs')).TrimEnd('\')
$InputProfile = (Resolve-Path -LiteralPath $InputProfile).Path.TrimEnd('\')
if ((Split-Path $InputProfile -Parent) -ine $profilesRoot) { throw 'InputProfile must be a direct child of .probe-runs.' }
function Read-Input([string] $Profile, [int] $ExpectedDistance) {
    $save = Get-Content -LiteralPath (Join-Path $Profile 'saves/save-singlePlayer.json') -Raw | ConvertFrom-Json
    if ($save.gameSequence -ne 6 -or $save.wasDevCheatUsed -or $save.startingConditions.ascensionLevel -ne $Rank -or
        $save.startingConditions.seed -ne 424242 -or $save.runState.distance -ne $ExpectedDistance -or
        $save.completedBattles.Count -ne $ExpectedDistance -or @($save.completedBattles | Where-Object bossHPRemaining -GT 0).Count) {
        throw 'Default campaign requires the declared original rank/seed, BattleIntro and every preceding native victory.'
    }
    return $save
}
$initial = Get-Content -LiteralPath (Join-Path $InputProfile 'saves/save-singlePlayer.json') -Raw | ConvertFrom-Json
$distance = [int]$initial.runState.distance
$null = Read-Input $InputProfile $distance
if ($distance -lt 1 -or $distance -ge $StopAfterRing) { throw 'Default campaign must start at an unfinished progressed ring within StopAfterRing.' }
$run = Join-Path $profilesRoot ('default-campaign-' + (Get-Date -Format yyyyMMdd-HHmmss) + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
$null = New-Item -ItemType Directory -Path $run
$journal = Join-Path $run 'accepted-rings.jsonl'
Write-Output ('DEFAULT-CAMPAIGN-START rank=' + $Rank + ' ring=' + ($distance + 1) + ' journal=' + $journal)
$nextInput = $InputProfile
$build = -not $SkipBuild
while ($distance -lt $StopAfterRing) {
    # Each ring must independently pass actual-default advice and a fresh
    # native frozen replay before original rewards/map progression can run.
    $pair = $null
    & (Join-Path $PSScriptRoot 'Run-NativeCovenantPlayerPairs.ps1') -Rank $Rank -Battle Campaign -BattleDistance $distance `
        -InputProfile $nextInput -DefaultAdvice -NativeWorker $NativeWorker -TimeoutSeconds $TimeoutSeconds -SkipBuild:(-not $build) |
        ForEach-Object {
            $line = $_.ToString()
            if ($line.StartsWith('{')) {
                $record = $line | ConvertFrom-Json
                if ($record.Passed -eq $true) { $pair = $record }
            }
            Write-Output $line
        }
    $build = $false
    if ($null -eq $pair -or -not $pair.DefaultAdvice -or $pair.Rank -ne $Rank -or $pair.BattleDistance -ne $distance) {
        throw "Missing accepted default pair for ring $($distance + 1); journal: $journal"
    }
    $checkpoint = Join-Path $run ('ring' + ($distance + 1) + '-input.zip')
    & (Join-Path $PSScriptRoot 'Save-CampaignCheckpoint.ps1') -Profile $nextInput -Archive $checkpoint
    $restored = & (Join-Path $PSScriptRoot 'Save-CampaignCheckpoint.ps1') -Archive $checkpoint -Restore | ConvertFrom-Json
    if ($restored.Ring -ne ($distance + 1) -or $restored.FilesVerified -lt 1) { throw 'Native checkpoint restoration failed.' }
    $accepted = [pscustomobject]@{ Schema = 1; Kind = 'pair-accepted'; Rank = $Rank; Ring = $distance + 1;
        Pair = $pair; InputArchive = $checkpoint; RestoredPayloads = $restored.FilesVerified }
    [IO.File]::AppendAllText($journal, ($accepted | ConvertTo-Json -Depth 8 -Compress) + [Environment]::NewLine)
    $progress = $null
    $progressOptions = @{ InputProfile = $pair.ReplayProfile; StopBeforeRing = $distance + 2;
        NativeWorker = $NativeWorker; TimeoutSeconds = $TimeoutSeconds; SkipBuild = $true }
    if ($distance -eq 7) {
        # Keep a complete final replay and native EndGame settlement in the
        # same profile for the existing completed-run archive verifier.
        $progressOptions.InputProfile = $nextInput
        $progressOptions.BattleReplay = Join-Path $pair.ReplayProfile 'full-battle.mt2f'
    }
    & (Join-Path $PSScriptRoot 'Run-CampaignProgress.ps1') @progressOptions | ForEach-Object {
            $line = $_.ToString()
            if ($line.StartsWith('{')) { $progress = $line | ConvertFrom-Json }
            Write-Output $line
        }
    if ($null -eq $progress -or -not $progress.NativePassed -or $progress.ExitCode -ne 0 -or -not $progress.OriginalFilesUnchanged) {
        throw "Original campaign continuation failed after ring $($distance + 1)."
    }
    $distance++
    if (-not $progress.NativeCampaignWon) { $null = Read-Input $progress.Profile $distance }
    $entry = [pscustomobject]@{ Schema = 1; Kind = 'progression'; Rank = $Rank; Ring = $distance; Progress = $progress }
    [IO.File]::AppendAllText($journal, ($entry | ConvertTo-Json -Depth 8 -Compress) + [Environment]::NewLine)
    Write-Output ('DEFAULT-CAMPAIGN-RING-ACCEPTED ' + $distance + ' journal=' + $journal)
    $nextInput = $progress.Profile
    if ($progress.NativeCampaignWon) { break }
}
$completedArchive = $null
if ($progress.NativeCampaignWon -and $progress.NativeVictoryType -eq 'Standard') {
    $completedArchive = Join-Path $run 'completed-standard.zip'
    & (Join-Path $PSScriptRoot 'Save-CampaignCheckpoint.ps1') -Profile $nextInput -Archive $completedArchive -CompletedRun
    & (Join-Path $PSScriptRoot 'Save-CampaignCheckpoint.ps1') -Archive $completedArchive -Restore
}
[pscustomobject]@{ Schema = 1; Rank = $Rank; LastAcceptedRing = $distance; NextInputProfile = $nextInput;
    NativeCampaignWon = $progress.NativeCampaignWon; NativeVictoryType = $progress.NativeVictoryType;
    CompletedArchive = $completedArchive; Journal = $journal } | ConvertTo-Json -Compress
