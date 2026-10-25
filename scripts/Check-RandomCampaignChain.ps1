#requires -Version 7.4
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string[]] $Journal,
    [ValidateRange(1, 8)][int] $ThroughRing = 8
)
$ErrorActionPreference = 'Stop'
# Reuse completed per-battle checks. This verifies unchanged evidence and
# uninterrupted native checkpoints across resumed runners, without simulations.
$records = @{}
$settings = $null
foreach ($path in $Journal) {
    $entries = @(Get-Content -LiteralPath $path | ForEach-Object { $_ | ConvertFrom-Json })
    $run = $entries[0]
    if ($run.Schema -ne 1 -or -not $run.WholeBattle -or $run.SearchWorkers -ne 2) {
        throw "Unexpected random campaign settings: $path"
    }
    $key = @($run.Seed, $run.Rank, $run.PrimaryClan, $run.SecondaryClan, $run.TimeLimitMs) -join '|'
    if ($null -eq $settings) { $settings = $key; $firstRun = $run }
    if ($key -cne $settings) { throw 'Campaign journals have different seed, clans, Covenant or budget.' }
    foreach ($pair in $entries | Where-Object Kind -EQ 'pair-accepted') {
        $ring = [int]$pair.Ring
        if ($ring -lt 1 -or $ring -gt 8 -or $records.ContainsKey($ring)) {
            throw "Invalid or duplicate accepted ring: $ring"
        }
        $records[$ring] = $pair
    }
}
function Assert-Hash([string] $Path, [string] $Expected) {
    if (-not $Expected -or (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash -ine $Expected) {
        throw "Campaign evidence hash changed: $Path"
    }
}
$previous = $null
$rows = @()
foreach ($ring in 1..$ThroughRing) {
    if (-not $records.ContainsKey($ring)) { throw "Missing independent native pair for ring $ring." }
    $pair = $records[$ring]
    Assert-Hash (Join-Path $pair.SourceProfile 'full-battle.mt2f') $pair.SourceSha256
    Assert-Hash (Join-Path $pair.ReplayProfile 'full-battle.mt2f') $pair.ReplaySha256
    Assert-Hash (Join-Path $pair.InputProfile 'saves/save-singlePlayer.json') $pair.InputSaveSHA256
    $source = Get-Content -LiteralPath (Join-Path $pair.SourceProfile 'native-accepted-summary.json') -Raw | ConvertFrom-Json
    $loaded = Get-Content -LiteralPath (Join-Path $pair.SourceProfile 'native-checkpoint-input.json') -Raw | ConvertFrom-Json
    $progress = Get-Content -LiteralPath (Join-Path $pair.ReplayProfile 'campaign-summary.json') -Raw | ConvertFrom-Json
    if (-not $source.NativePassed -or -not $source.NativeWon -or $source.ExitCode -ne 0 -or
        -not $source.OriginalFilesUnchanged -or $source.CaptureFailures -ne 0 -or $source.Mismatches -ne 0 -or
        $source.Unsupported -ne 0 -or $source.Pending -ne 0 -or $source.TraceSha256 -ine $pair.SourceSha256 -or
        $loaded.SHA256 -ine $pair.InputSaveSHA256 -or -not $progress.NativePassed -or $progress.ExitCode -ne 0 -or
        -not $progress.OriginalFilesUnchanged -or -not $progress.PlayerReplay -or -not $progress.BattleReplayPassed -or
        $progress.BattlesWon -ne 1 -or $progress.StoryProgressionOnly -or
        $progress.FinalSaveSHA256 -ine $pair.Progress.FinalSaveSHA256) {
        throw "Native source/replay acceptance changed at ring $ring."
    }
    if ($previous -and
        ([IO.Path]::GetFullPath($pair.InputProfile).TrimEnd('\') -ine
            [IO.Path]::GetFullPath($previous.ReplayProfile).TrimEnd('\') -or
         $pair.InputSaveSHA256 -ine $previous.Progress.FinalSaveSHA256)) {
        throw "Native checkpoint continuity broken before ring $ring."
    }
    $save = Get-Content -LiteralPath (Join-Path $pair.InputProfile 'saves/save-singlePlayer.json') -Raw | ConvertFrom-Json
    if ($save.wasDevCheatUsed -or $save.gameSequence -ne 6 -or $save.startingConditions.seed -ne $firstRun.Seed -or
        $save.startingConditions.ascensionLevel -ne $firstRun.Rank -or $save.runState.distance -ne $ring - 1 -or
        $save.completedBattles.Count -ne $ring - 1 -or @($save.completedBattles | Where-Object bossHPRemaining -GT 0).Count -or
        (($save.completedBattles.battleIndex | Sort-Object) -join ',') -cne ((@(for ($i = 0; $i -lt $ring - 1; $i++) { $i })) -join ',')) {
        throw "Native checkpoint history differs at ring $ring."
    }
    $clans = @($save.startingConditions.mainClassInfo.classId, $save.startingConditions.subclassInfo.classId) -join '|'
    if ($ring -eq 1) { $initialClans = $clans }
    if ($clans -cne $initialClans) { throw "Native starting clans changed at ring $ring." }
    foreach ($name in @('MonsterTrain2Poju.Advisor.dll', 'Model.dll', 'FixtureArchive.dll')) {
        $payloadHash = (Get-FileHash -LiteralPath (Join-Path $pair.SourceProfile "player-plugin/$name")).Hash
        Assert-Hash (Join-Path $pair.ReplayProfile "player-plugin/$name") $payloadHash
    }
    Assert-Hash (Join-Path $pair.ReplayProfile 'saves/save-singlePlayer.json') $progress.FinalSaveSHA256
    if ($ring -lt 8) {
        if ($progress.NativeCampaignWon) { throw 'Premature campaign victory.' }
    } else {
        $outcomes = @(Get-Content -LiteralPath (Join-Path $pair.ReplayProfile 'campaign-audit.jsonl') |
            ForEach-Object { $_ | ConvertFrom-Json } | Where-Object kind -EQ 'run-outcome')
        if (-not $progress.NativeCampaignWon -or $progress.NativeVictoryType -cne 'Standard' -or
            $outcomes.Count -ne 1 -or -not $outcomes[0].data.won -or -not $outcomes[0].data.nativeVictory -or
            $outcomes[0].data.cheat -or $outcomes[0].data.battlesWon -ne 8 -or
            @($outcomes[0].data.battles | Where-Object { -not $_.won }).Count -or
            (($outcomes[0].data.battles.battleIndex | Sort-Object) -join ',') -cne '0,1,2,3,4,5,6,7') {
            throw 'Final ring lacks a native unmodified eight-battle Standard victory.'
        }
    }
    $rows += [ordered]@{ Ring = $ring; SourceSHA256 = $pair.SourceSha256; ReplaySHA256 = $pair.ReplaySha256
        Version = [Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $pair.SourceProfile 'player-plugin/MonsterTrain2Poju.Advisor.dll')).ProductVersion }
    $previous = $pair
}
[ordered]@{ Evidence = 'UnchangedIndependentPairReceiptsAndNativeCheckpointChain'; Simulations = 0
    Seed = $firstRun.Seed; Rank = $firstRun.Rank; PrimaryClan = $firstRun.PrimaryClan; SecondaryClan = $firstRun.SecondaryClan
    TimeLimitMs = $firstRun.TimeLimitMs; ThroughRing = $ThroughRing; CompleteStandardCampaign = $ThroughRing -eq 8
    Rings = $rows } | ConvertTo-Json -Depth 5
