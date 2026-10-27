#requires -Version 7.4
[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('Banished','Pyreborne','LunaCoven','Underlegion','LazarusLeague','Hellhorned','Awoken','Stygian','Umbra','Remnant','Wurmkin')][string] $PrimaryClan,
    [Parameter(Mandatory)][ValidateSet('Banished','Pyreborne','LunaCoven','Underlegion','LazarusLeague','Hellhorned','Awoken','Stygian','Umbra','Remnant','Wurmkin')][string] $SecondaryClan,
    [Parameter(Mandatory)][ValidateRange(1, 2147483647)][int] $Seed,
    [ValidateRange(0, 10)][int] $Rank = 0,
    [ValidateRange(100, 30000)][int] $TimeLimitMs = 3000,
    [string] $InputProfile = '',
    [string] $NewRunFromProfile = '',
    [ValidateRange(1, 4)][int] $NativeWorker = 2,
    [ValidateRange(60, 3600)][int] $TimeoutSeconds = 1200,
    [string] $NativeBuildRoot = '',
    [string] $ModelChecker = '',
    [switch] $CompleteDefeats,
    [switch] $SkipBuild
)
$ErrorActionPreference = 'Stop'
if ($PrimaryClan -ieq $SecondaryClan) { throw 'Starting clans must differ.' }
if ($InputProfile -and $NewRunFromProfile) { throw 'Choose either an unfinished InputProfile or an idle NewRunFromProfile.' }
if ($NativeBuildRoot -and -not $SkipBuild) { throw 'NativeBuildRoot requires prebuilt native binaries with SkipBuild.' }
if ($NativeBuildRoot) { $NativeBuildRoot = (Resolve-Path -LiteralPath $NativeBuildRoot).Path }
$workspace = Split-Path $PSScriptRoot -Parent
$profilesRoot = [IO.Path]::GetFullPath((Join-Path $workspace '.probe-runs')).TrimEnd('\')
if (-not $SkipBuild) {
    foreach ($project in @('Probe','Advisor','ModelChecks')) {
        dotnet build (Join-Path $workspace "src/$project/$project.csproj") -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw "$project build failed." }
    }
}
$checker = if ($ModelChecker) { (Resolve-Path -LiteralPath $ModelChecker).Path }
    else { Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll' }
if (-not (Test-Path -LiteralPath $checker -PathType Leaf) -or (Split-Path $checker -Leaf) -ine 'ModelChecks.dll') {
    throw 'Expected an existing ModelChecks.dll.'
}
function Invoke-Native([string] $Script, [hashtable] $Parameters) {
    if ($NativeBuildRoot) { $Parameters.NativeBuildRoot = $NativeBuildRoot }
    $result = $null
    & (Join-Path $PSScriptRoot $Script) @Parameters | ForEach-Object {
        $line = $_.ToString()
        if ($line.StartsWith('{')) {
            $result = $line | ConvertFrom-Json
            Write-Host "NATIVE-RESULT script=$Script passed=$($result.NativePassed) won=$($result.NativeWon) profile=$($result.Profile)"
        } else {
            if ($Script -eq 'Run-FullBattleProbe.ps1' -and $line -match '^FULL-BATTLE-RUN .* profile=(.+)$') {
                $receipt.SourceProfile = $Matches[1]
                Record @{ Kind='source-started'; Ring=$receipt.FailureRing; Profile=$receipt.SourceProfile; InputProfile=$receipt.NextInputProfile }
            }
            Write-Output $line
        }
    } | Out-Host
    if ($null -eq $result) { throw "Missing native result from $Script." }
    return $result
}
function Read-Checkpoint([string] $Profile, [int] $Distance) {
    $resolved = (Resolve-Path -LiteralPath $Profile).Path.TrimEnd('\')
    if ((Split-Path $resolved -Parent) -ine $profilesRoot) { throw 'Input must be an isolated probe profile.' }
    $save = Get-Content -LiteralPath (Join-Path $resolved 'saves/save-singlePlayer.json') -Raw | ConvertFrom-Json
    if ($save.gameSequence -ne 6 -or $save.wasDevCheatUsed -or $save.startingConditions.seed -ne $Seed -or
        $save.startingConditions.ascensionLevel -ne $Rank -or $save.runState.distance -ne $Distance -or
        $save.completedBattles.Count -ne $Distance -or @($save.completedBattles | Where-Object bossHPRemaining -GT 0).Count -or
        (($save.completedBattles.battleIndex | Sort-Object) -join ',') -cne ((@(for ($i=0; $i -lt $Distance; $i++) { $i })) -join ',')) {
        throw 'Checkpoint changed seed, rank, sequence or complete preceding victory history.'
    }
    return $save
}
$resumeDistance = 0
if ($InputProfile) {
    $InputProfile = (Resolve-Path -LiteralPath $InputProfile).Path.TrimEnd('\')
    $resumeDistance = [int](Get-Content -LiteralPath (Join-Path $InputProfile 'saves/save-singlePlayer.json') -Raw | ConvertFrom-Json).runState.distance
    if ($resumeDistance -lt 0 -or $resumeDistance -gt 7) { throw 'Resume requires an unfinished ordinary campaign checkpoint.' }
    $initial = Read-Checkpoint $InputProfile $resumeDistance
}
$run = Join-Path $profilesRoot ('random-campaign-' + (Get-Date -Format yyyyMMdd-HHmmss) + '-' + [Guid]::NewGuid().ToString('N').Substring(0,8))
$null = New-Item -ItemType Directory -Path $run
$journal = Join-Path $run 'accepted-rings.jsonl'
$receiptPath = Join-Path $run 'campaign-result.json'
function Record([object] $Entry) {
    [IO.File]::AppendAllText($journal, ($Entry | ConvertTo-Json -Depth 10 -Compress) + [Environment]::NewLine)
}
$receipt = [ordered]@{ Schema=1; PrimaryClan=$PrimaryClan; SecondaryClan=$SecondaryClan; Seed=$Seed; Rank=$Rank
    WholeBattle=$true; SearchWorkers=2; TimeLimitMs=$TimeLimitMs; Journal=$journal; InputProfile=$InputProfile; NativeBuildRoot=$NativeBuildRoot
    NewRunFromProfile=$NewRunFromProfile; StartingRing=$resumeDistance+1; LastAcceptedRing=$resumeDistance; NativeCampaignWon=$false; NativeVictoryType=$null
    CompleteDefeats=[bool]$CompleteDefeats; DefeatSettled=$false; SettlementProfile=$null
    Outcome='Running'; FailureRing=$null; Failure=$null; SourceProfile=$null; NextInputProfile=$InputProfile
    ModelChecker=$checker; ModelCheckerSHA256=(Get-FileHash -LiteralPath $checker).Hash }
Record ([pscustomobject]$receipt)
Write-Host "RANDOM-CAMPAIGN-START $PrimaryClan/$SecondaryClan seed=$Seed rank=$Rank budgetMs=$TimeLimitMs journal=$journal"
try {
    if (-not $InputProfile) {
        $openingParameters = @{ StopBeforeRing=1; Seed=$Seed; PrimaryClan=$PrimaryClan
            SecondaryClan=$SecondaryClan; StartingCovenantLevel=$Rank; NativeWorker=$NativeWorker; TimeoutSeconds=$TimeoutSeconds; SkipBuild=$true }
        if ($NewRunFromProfile) { $openingParameters.NewRunFromProfile = $NewRunFromProfile }
        $opening = Invoke-Native 'Run-CampaignProgress.ps1' $openingParameters
        if (-not $opening.NativePassed -or -not $opening.OriginalFilesUnchanged) { throw 'Native campaign opening failed.' }
        $InputProfile = $opening.Profile
        $initial = Read-Checkpoint $InputProfile 0
    }
    $mainId = $initial.startingConditions.mainClassInfo.classId
    $subId = $initial.startingConditions.subclassInfo.classId
    for ($distance=$resumeDistance; $distance -lt 8; $distance++) {
        $receipt.FailureRing = $distance+1
        $save = Read-Checkpoint $InputProfile $distance
        if ($save.startingConditions.mainClassInfo.classId -cne $mainId -or $save.startingConditions.subclassInfo.classId -cne $subId) {
            throw 'Native progression changed either starting clan.'
        }
        $receipt.NextInputProfile = $InputProfile
        $inputPath = Join-Path $InputProfile 'saves/save-singlePlayer.json'
        $inputHash = (Get-FileHash -LiteralPath $inputPath -Algorithm SHA256).Hash
        $source = Invoke-Native 'Run-FullBattleProbe.ps1' @{ InputProfile=$InputProfile; BattleDistance=$distance; Seed=$Seed
            PlayerAdvisor=$true; RollingAdvice=$true; PlannerWholeBattle=$true; PlannerSearchWorkers=2; PlannerTimeLimitMs=$TimeLimitMs
            PhysicalSpawnPoints=$true; BranchCardMasks=$true; SettleDeathDissolves=$true; SettleCardAnimations=$true
            NativeWorker=$NativeWorker; TimeoutSeconds=$TimeoutSeconds; SkipBuild=$true }
        $receipt.SourceProfile = $source.Profile
        $trace = Join-Path $source.Profile 'full-battle.mt2f'
        $accepted = Get-Content -LiteralPath (Join-Path $source.Profile 'native-accepted-summary.json') -Raw | ConvertFrom-Json
        if (-not $accepted.NativePassed -or $accepted.ExitCode -ne 0 -or -not $accepted.OriginalFilesUnchanged -or
            $accepted.CaptureFailures -ne 0 -or $accepted.Mismatches -ne 0 -or $accepted.Unsupported -ne 0 -or $accepted.Pending -ne 0 -or
            (Get-FileHash -LiteralPath $trace -Algorithm SHA256).Hash -ine $accepted.TraceSha256) { throw 'Source native acceptance failed.' }
        $loaded = Get-Content -LiteralPath (Join-Path $source.Profile 'native-checkpoint-input.json') -Raw | ConvertFrom-Json
        if ($loaded.SHA256 -ine $inputHash) { throw 'Native source loaded another campaign checkpoint.' }
        if ((Get-FileHash -LiteralPath $inputPath -Algorithm SHA256).Hash -cne $inputHash) { throw 'Original isolated input changed.' }
        if (-not $accepted.NativeWon) {
            dotnet $checker --random-campaign-source-only $trace $Seed $Rank $distance $PrimaryClan $SecondaryClan $TimeLimitMs
            if ($LASTEXITCODE -ne 0) { throw 'Complete defeat failed independent model/UI acceptance.' }
            $receipt.Outcome='Defeat'
            Record @{ Kind='native-defeat'; Ring=$distance+1; Source=$accepted; InputProfile=$InputProfile; InputSaveSHA256=$inputHash }
            if ($CompleteDefeats) {
                $settlement = Invoke-Native 'Run-CampaignProgress.ps1' @{ InputProfile=$InputProfile; BattleReplay=$trace; Seed=$Seed
                    CompleteDefeat=$true; StopBeforeRing=$distance+2; NativeWorker=$NativeWorker; TimeoutSeconds=$TimeoutSeconds; SkipBuild=$true }
                if (-not $settlement.NativePassed -or -not $settlement.CompleteDefeat -or -not $settlement.BattleReplayPassed -or
                    -not $settlement.PlayerReplay -or $settlement.NativeCampaignWon -or -not $settlement.OriginalFilesUnchanged) {
                    throw 'Native defeat did not settle normally.'
                }
                $replay = Join-Path $settlement.Profile 'full-battle.mt2f'
                dotnet $checker --random-campaign-defeat-pair-only $trace $replay $Seed $Rank $distance $PrimaryClan $SecondaryClan $TimeLimitMs
                if ($LASTEXITCODE -ne 0) { throw 'Independent frozen defeat pair check failed.' }
                $receipt.DefeatSettled=$true; $receipt.SettlementProfile=$settlement.Profile
                $receipt.NextInputProfile=$settlement.Profile
                Record @{ Kind='settled-native-defeat'; Ring=$distance+1; SourceProfile=$source.Profile; ReplayProfile=$settlement.Profile
                    SourceSha256=$accepted.TraceSha256; ReplaySha256=(Get-FileHash -LiteralPath $replay -Algorithm SHA256).Hash
                    InputProfile=$InputProfile; InputSaveSHA256=$inputHash; Settlement=$settlement }
            }
            break
        }
        # The second native process both freezes every winning input and makes
        # normal reward/map choices. No third process repeats the same battle.
        $progress = Invoke-Native 'Run-CampaignProgress.ps1' @{ InputProfile=$InputProfile; BattleReplay=$trace; Seed=$Seed
            StopBeforeRing=$distance+2; NativeWorker=$NativeWorker; TimeoutSeconds=$TimeoutSeconds; SkipBuild=$true }
        if (-not $progress.NativePassed -or -not $progress.BattleReplayPassed -or -not $progress.PlayerReplay -or
            -not $progress.OriginalFilesUnchanged) { throw 'Frozen native battle and campaign progression failed.' }
        $replay = Join-Path $progress.Profile 'full-battle.mt2f'
        dotnet $checker --random-campaign-pair-only $trace $replay $Seed $Rank $distance $PrimaryClan $SecondaryClan $TimeLimitMs
        if ($LASTEXITCODE -ne 0) { throw 'Independent random campaign pair check failed.' }
        if ((Get-FileHash -LiteralPath $inputPath -Algorithm SHA256).Hash -cne $inputHash) { throw 'Original isolated input changed.' }
        $pair = @{ Kind='pair-accepted'; Ring=$distance+1; SourceProfile=$source.Profile; ReplayProfile=$progress.Profile
            SourceSha256=$accepted.TraceSha256; ReplaySha256=(Get-FileHash -LiteralPath $replay -Algorithm SHA256).Hash
            InputProfile=$InputProfile; InputSaveSHA256=$inputHash; Progress=$progress }
        Record $pair
        $receipt.LastAcceptedRing=$distance+1
        $receipt.NextInputProfile=$progress.Profile
        if ($progress.NativeCampaignWon) {
            if ($distance -ne 7 -or $progress.NativeVictoryType -ne 'Standard') { throw 'Unexpected campaign settlement.' }
            $archive = Join-Path $run 'completed-standard.zip'
            & (Join-Path $PSScriptRoot 'Save-CampaignCheckpoint.ps1') -Profile $progress.Profile -Archive $archive -CompletedRun | Out-Host
            & (Join-Path $PSScriptRoot 'Save-CampaignCheckpoint.ps1') -Archive $archive -Restore | Out-Host
            $receipt.NativeCampaignWon=$true; $receipt.NativeVictoryType=$progress.NativeVictoryType
            $receipt.Outcome='Victory'; $receipt.FailureRing=$null
            Record @{ Kind='campaign-victory'; Archive=$archive; Progress=$progress }
            break
        }
        $InputProfile = $progress.Profile
        $null = Read-Checkpoint $InputProfile ($distance+1)
        Write-Host "RANDOM-CAMPAIGN-RING-ACCEPTED $($distance+1) $PrimaryClan/$SecondaryClan"
    }
} catch {
    $receipt.Outcome='Blocked'; $receipt.Failure=$_.Exception.Message
    Record @{ Kind='blocked'; Ring=$receipt.FailureRing; Reason=$receipt.Failure; InputProfile=$receipt.NextInputProfile }
    throw
} finally {
    $receipt | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $receiptPath -Encoding utf8
    $receipt | ConvertTo-Json -Depth 8 -Compress | Write-Output
}
