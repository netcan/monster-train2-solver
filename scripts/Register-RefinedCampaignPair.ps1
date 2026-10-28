#requires -Version 7.4
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string[]] $PriorJournal,
    [Parameter(Mandatory)][string] $SourceProfile,
    [Parameter(Mandatory)][string] $ReplayProfile,
    [Parameter(Mandatory)][string] $Prediction,
    [Parameter(Mandatory)][string] $OutputDirectory,
    [string] $FixtureToolsRoot = 'src/FixtureTools/bin/Release/net8.0'
)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
$source = (Resolve-Path -LiteralPath $SourceProfile).Path
$replay = (Resolve-Path -LiteralPath $ReplayProfile).Path
$predictionPath = (Resolve-Path -LiteralPath $Prediction).Path
$output = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $output) { throw "Output already exists: $output" }
$run = Get-Content -LiteralPath $PriorJournal[0] -TotalCount 1 | ConvertFrom-Json
$checkpoint = Get-Content -LiteralPath (Join-Path $source 'native-checkpoint-input.json') -Raw | ConvertFrom-Json
$progress = Get-Content -LiteralPath (Join-Path $replay 'campaign-summary.json') -Raw | ConvertFrom-Json
$save = Get-Content -LiteralPath (Join-Path $checkpoint.InputProfile 'saves/save-singlePlayer.json') -Raw | ConvertFrom-Json
$distance = [int]$save.runState.distance
if ($distance -lt 0 -or $distance -gt 7 -or -not $progress.NativePassed -or
    -not $progress.BattleReplayPassed -or -not $progress.PlayerReplay -or
    -not $progress.OriginalFilesUnchanged -or $progress.BattlesWon -ne 1) {
    throw 'Expected a completed independent winning replay and an ordinary campaign checkpoint.'
}
$sourceTrace = Join-Path $source 'full-battle.mt2f'
$replayTrace = Join-Path $replay 'full-battle.mt2f'
$toolsRoot = (Resolve-Path -LiteralPath $FixtureToolsRoot).Path
$fixtureTools = Join-Path $toolsRoot 'FixtureTools.dll'
$checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
Add-Type -Path (Join-Path $workspace 'src/FixtureArchive/bin/Release/net8.0/FixtureArchive.dll')
$document = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read($predictionPath)
try { $inputCount = $document.RootElement.GetProperty('RollingDecisions').GetArrayLength() }
finally { $document.Dispose() }
foreach ($toolRoot in @($toolsRoot, (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0'))) {
    $toolModel = (Get-FileHash -LiteralPath (Join-Path $toolRoot 'Model.dll')).Hash
    if ($toolModel -ine (Get-FileHash -LiteralPath (Join-Path $source 'player-plugin/Model.dll')).Hash) {
        throw "Verification model differs from the native source: $toolRoot"
    }
}
New-Item -ItemType Directory -Path $output | Out-Null
$bindings = @()
foreach ($entry in @(@($sourceTrace, 'source-native.log'), @($replayTrace, 'replay-native.log'))) {
    $log = Join-Path $output $entry[1]
    & dotnet $fixtureTools verify-offline-native $predictionPath $entry[0] | Tee-Object -FilePath $log
    if ($LASTEXITCODE -ne 0) { throw "Native prediction binding failed: $($entry[0])" }
    $bindings += [ordered]@{ ArchiveSHA256=(Get-FileHash -LiteralPath $entry[0]).Hash
        Log=$log; LogSHA256=(Get-FileHash -LiteralPath $log).Hash }
}
$pairLog = Join-Path $output 'refined-pair.log'
& dotnet $checker --random-refined-campaign-pair-only $sourceTrace $replayTrace $run.Seed $run.Rank $distance $run.PrimaryClan $run.SecondaryClan $inputCount | Tee-Object -FilePath $pairLog
if ($LASTEXITCODE -ne 0) { throw 'Independent refined campaign pair verification failed.' }
$pair = [ordered]@{ Kind='refined-pair-accepted'; Ring=$distance+1
    InputProfile=$checkpoint.InputProfile; InputSaveSHA256=$checkpoint.SHA256
    SourceProfile=$source; SourceSha256=(Get-FileHash -LiteralPath $sourceTrace).Hash
    ReplayProfile=$replay; ReplaySha256=(Get-FileHash -LiteralPath $replayTrace).Hash
    Prediction=$predictionPath; PredictionSHA256=(Get-FileHash -LiteralPath $predictionPath).Hash
    PairCheckLog=$pairLog; PairCheckLogSHA256=(Get-FileHash -LiteralPath $pairLog).Hash
    NativeBindings=$bindings; Progress=$progress }
$journal = Join-Path $output 'accepted-rings.jsonl'
@(($run | ConvertTo-Json -Depth 10 -Compress), ($pair | ConvertTo-Json -Depth 10 -Compress)) |
    Set-Content -LiteralPath $journal -Encoding utf8
& (Join-Path $PSScriptRoot 'Check-RandomCampaignChain.ps1') -Journal @($PriorJournal + $journal) -ThroughRing ($distance+1) -AllowRefinedRoutes
Write-Host "REFINED-CAMPAIGN-RECEIPT PASS: $journal"
