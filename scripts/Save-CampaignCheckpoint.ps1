#requires -Version 7.4
[CmdletBinding(DefaultParameterSetName = 'Export')]
param(
    [Parameter(Mandatory, ParameterSetName = 'Export')] [string] $Profile,
    [Parameter(Mandatory)] [string] $Archive,
    [Parameter(ParameterSetName = 'Export')] [switch] $CompletedRun,
    [Parameter(Mandatory, ParameterSetName = 'Restore')] [switch] $Restore
)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
$profilesRoot = [IO.Path]::GetFullPath((Join-Path $workspace '.probe-runs')).TrimEnd('\')
$Archive = [IO.Path]::GetFullPath($Archive)
function Get-SHA256([byte[]] $Bytes) { [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($Bytes)) }
if ($Restore) {
    $zip = [IO.Compression.ZipFile]::OpenRead($Archive)
    try {
        $entry = $zip.GetEntry('checkpoint-manifest.json')
        if ($null -eq $entry) { throw 'Archive has no checkpoint manifest.' }
        $reader = [IO.StreamReader]::new($entry.Open())
        try { $manifest = $reader.ReadToEnd() | ConvertFrom-Json } finally { $reader.Dispose() }
        if ($manifest.Schema -ne 1) { throw 'Unknown campaign checkpoint schema.' }
        $files = @{}
        foreach ($file in $manifest.Files) {
            if ($file.Path -notmatch '^(saves/[A-Za-z0-9._-]+|[A-Za-z0-9._-]+)$') { throw 'Unsafe checkpoint member path.' }
            $entry = $zip.GetEntry($file.Path)
            if ($null -eq $entry -or $entry.Length -ne $file.Bytes) { throw "Missing or altered checkpoint member: $($file.Path)" }
            $stream = $entry.Open(); $buffer = [IO.MemoryStream]::new()
            try { $stream.CopyTo($buffer); $bytes = $buffer.ToArray() } finally { $stream.Dispose(); $buffer.Dispose() }
            if ((Get-SHA256 $bytes) -cne $file.SHA256) { throw "Checkpoint hash mismatch: $($file.Path)" }
            $files[$file.Path] = $bytes
        }
        $prefix = if ($manifest.StateKind -eq 'CompletedNativeRun') { 'restored-completed-campaign-' } else { 'restored-campaign-ring' + $manifest.Ring + '-' }
        $destination = Join-Path $profilesRoot ($prefix + [Guid]::NewGuid().ToString('N').Substring(0, 8))
        $null = New-Item -ItemType Directory -Path (Join-Path $destination 'saves')
        foreach ($path in $files.Keys) { [IO.File]::WriteAllBytes((Join-Path $destination $path), $files[$path]) }
        [IO.File]::WriteAllText((Join-Path $destination 'checkpoint-manifest.json'), ($manifest | ConvertTo-Json -Depth 8))
        [pscustomobject]@{ Profile = $destination; Ring = $manifest.Ring; Seed = $manifest.Seed; StateKind = $manifest.StateKind;
            FilesVerified = $files.Count } | ConvertTo-Json -Compress
    } finally { $zip.Dispose() }
    return
}
$Profile = (Resolve-Path -LiteralPath $Profile).Path.TrimEnd('\')
if ((Split-Path $Profile -Parent) -ine $profilesRoot) { throw 'Profile must be a direct child of .probe-runs.' }
$savePath = Join-Path $Profile 'saves/save-singlePlayer.json'
if ($CompletedRun) {
    $summary = Get-Content -LiteralPath (Join-Path $Profile 'campaign-summary.json') -Raw | ConvertFrom-Json
    $audit = @(Get-Content -LiteralPath (Join-Path $Profile 'campaign-audit.jsonl') | ForEach-Object { $_ | ConvertFrom-Json })
    $outcome = @($audit | Where-Object kind -EQ 'run-outcome')
    if (-not $summary.NativePassed -or -not $summary.OriginalFilesUnchanged -or -not $summary.BattleReplayPassed -or
        -not $summary.NativeCampaignWon -or $summary.NativeVictoryType -ne 'Standard' -or
        $outcome.Count -ne 1 -or -not $outcome[0].data.won -or -not $outcome[0].data.nativeVictory -or $outcome[0].data.cheat -or
        $outcome[0].data.battlesWon -ne 8 -or $audit[-1].kind -ne 'complete' -or
        @($outcome[0].data.battles | Where-Object { -not $_.won }).Count -gt 0 -or
        (($outcome[0].data.battles.battleIndex | Sort-Object) -join ',') -ne '0,1,2,3,4,5,6,7') {
        throw 'Expected a completed unmodified native eight-battle Standard victory and full replay/choice history.'
    }
    # Native FinishRun may replace both slots. Use the serialized native data
    # retained at EndGame, before reset, without changing the game's save flow.
    $savePath = Join-Path $Profile 'completed-save-singlePlayer.json'
}
$save = Get-Content -LiteralPath $savePath -Raw | ConvertFrom-Json
$distance = [int]$save.runState.distance
if ($save.wasDevCheatUsed -or $distance -lt 0 -or ($CompletedRun -and $distance -ne 8) -or
    (-not $CompletedRun -and $save.gameSequence -ne 6)) { throw 'Expected an unmodified progressed native save at the requested boundary.' }
$completed = @($save.completedBattles | Sort-Object battleIndex)
if ($completed.Count -ne $distance -or @($completed | Where-Object bossHPRemaining -GT 0).Count -gt 0 -or
    (($completed.battleIndex -join ',') -ne ((@(for ($i=0; $i -lt $distance; $i++) { $i })) -join ','))) { throw 'The native save does not contain every preceding battle victory.' }
$members = [ordered]@{}
foreach ($name in @('saves/save-singlePlayer.json', 'metagameSave.json', 'preferences.json', 'preferences-local.json')) {
    $members[$name] = [IO.File]::ReadAllBytes((Join-Path $Profile $name))
}
if ($CompletedRun) {
    $members['completed-save-singlePlayer.json'] = [IO.File]::ReadAllBytes($savePath)
    $members['campaign-summary.json'] = [IO.File]::ReadAllBytes((Join-Path $Profile 'campaign-summary.json'))
    $replayProfile = $Profile
    $replayPath = Join-Path $replayProfile 'full-battle.mt2f'
    $inheritedReplay = -not (Test-Path -LiteralPath $replayPath)
    if ($inheritedReplay) {
        # A native post-battle resume can finish rewards without replaying combat.
        # Resolve only its immediate, hash-verified input; retain that process's
        # actual summary, including any failed overall campaign continuation.
        $input = Get-Content -LiteralPath (Join-Path $Profile 'campaign-input.json') -Raw | ConvertFrom-Json
        $replayProfile = [IO.Path]::GetFullPath($input.InputProfile).TrimEnd('\')
        if ((Split-Path $replayProfile -Parent) -ine $profilesRoot -or $replayProfile -ieq $Profile) {
            throw 'Inherited final-battle replay must come from the immediate isolated input.'
        }
        $parentSave = Join-Path $replayProfile 'saves/save-singlePlayer.json'
        $parentSummary = Get-Content -LiteralPath (Join-Path $replayProfile 'campaign-summary.json') -Raw | ConvertFrom-Json
        $parentState = Get-Content -LiteralPath $parentSave -Raw | ConvertFrom-Json
        if ((Get-FileHash -LiteralPath $parentSave -Algorithm SHA256).Hash -ine $input.InputSaveSHA256 -or
            -not $parentSummary.OriginalFilesUnchanged -or -not $parentSummary.BattleReplayPassed -or
            $parentState.gameSequence -ne 2 -or $parentState.runState.distance -ne 8 -or $parentState.wasDevCheatUsed -or
            $parentState.completedBattles.Count -ne 8 -or @($parentState.completedBattles | Where-Object bossHPRemaining -GT 0).Count) {
            throw 'Inherited replay input is not an unchanged native eight-win post-battle checkpoint.'
        }
        $replayPath = Join-Path $replayProfile 'full-battle.mt2f'
    }
    Add-Type -Path (Join-Path $workspace 'src/FixtureArchive/bin/Release/net8.0/FixtureArchive.dll')
    $replayDocument = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read($replayPath)
    try { $replay = $replayDocument.RootElement.ToObjectGraph() } finally { $replayDocument.Dispose() }
    if ($replay.NativeWon -ne $true -or $replay.TerminalEffectsSettled -ne $true -or
        $null -eq $replay.CaptureFailures -or $replay.CaptureFailures -ne 0 -or $replay.Mismatches -ne 0 -or
        $replay.Unsupported -ne 0 -or $replay.Pending -ne 0) {
        throw 'Completed campaign requires a settled native winning final-battle replay with zero failure counters.'
    }
    if ($inheritedReplay -and ($replay.NativeRunInput.Distance -ne 7 -or $replay.NativeRunInput.WasDevCheatUsed -ne $false -or
        $replay.RollingDecisions.Count -eq 0 -or @($replay.RollingDecisions | Where-Object {
            $_.Mode -ne 'replay' -or $_.Nodes -ne 0 -or $_.Difference -or $null -eq $_.Actual }).Count)) {
        throw 'Inherited final-battle replay lacks the complete original Ring8 native input chain.'
    }
    $members['campaign-battle-replay.mt2f'] = [IO.File]::ReadAllBytes($replayPath)
    $members['completed-battle-replay-source.json'] = [Text.Encoding]::UTF8.GetBytes(([pscustomobject]@{
        Inherited = $inheritedReplay; SourceProfile = Split-Path $replayProfile -Leaf;
        ArchiveSHA256 = Get-SHA256 $members['campaign-battle-replay.mt2f'];
        SourceInputSaveSHA256 = $(if ($inheritedReplay) { $input.InputSaveSHA256 } else { $null })
    } | ConvertTo-Json))
}
# Bundle the retained native choice history and each predecessor's input hash.
$cursor = $Profile; $visited = @{}; $depth = 0
while ((Split-Path $cursor -Parent) -ieq $profilesRoot -and (Test-Path -LiteralPath $cursor) -and -not $visited.ContainsKey($cursor)) {
    $visited[$cursor] = $true
    foreach ($name in @('campaign-audit.jsonl', 'campaign-parent-audit.jsonl', 'campaign-input.json', 'native-checkpoint-input.json', 'campaign-battle-replay.mt2f', 'native-story-progression.json')) {
        $path = Join-Path $cursor $name
        $memberName = if ($depth -eq 0) { $name } else { "history-$depth-$name" }
        if (Test-Path -LiteralPath $path) { $members[$memberName] = [IO.File]::ReadAllBytes($path) }
    }
    foreach ($history in Get-ChildItem -LiteralPath $cursor -File -Filter 'history-*') {
        $members["history-$depth-$($history.Name)"] = [IO.File]::ReadAllBytes($history.FullName)
    }
    $inputPath = Join-Path $cursor 'native-checkpoint-input.json'
    if (-not (Test-Path -LiteralPath $inputPath)) { $inputPath = Join-Path $cursor 'campaign-input.json' }
    if (-not (Test-Path -LiteralPath $inputPath)) { break }
    $cursor = (Get-Content -LiteralPath $inputPath -Raw | ConvertFrom-Json).InputProfile
    if (-not $cursor) { break }
    $depth++
}
if (@($members.Keys | Where-Object { $_ -like '*campaign-audit.jsonl' }).Count -eq 0) { throw 'Missing native campaign choice history.' }
$manifest = [ordered]@{ Schema = 1; Ring = $distance + 1; Seed = $save.startingConditions.seed;
    CompletedBattles = $completed.Count; DeckCards = $save.deckState.Count; Relics = $save.blessings.Count;
    Files = @($members.Keys | ForEach-Object { [pscustomobject]@{ Path = $_; Bytes = $members[$_].Length; SHA256 = Get-SHA256 $members[$_] } }) }
if ($CompletedRun) { $manifest.StateKind = 'CompletedNativeRun'; $manifest.Ring = 8; $manifest.NativeVictoryType = $summary.NativeVictoryType }
$members['checkpoint-manifest.json'] = [Text.Encoding]::UTF8.GetBytes(($manifest | ConvertTo-Json -Depth 8))
$zip = [IO.Compression.ZipFile]::Open($Archive, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($name in $members.Keys) {
        $stream = $zip.CreateEntry($name, [IO.Compression.CompressionLevel]::Optimal).Open()
        try { $stream.Write($members[$name]) } finally { $stream.Dispose() }
    }
} finally { $zip.Dispose() }
[pscustomobject]@{ Archive = $Archive; SHA256 = (Get-FileHash -LiteralPath $Archive -Algorithm SHA256).Hash;
    Ring = $manifest.Ring; CompletedBattles = $completed.Count; Files = $members.Count } | ConvertTo-Json -Compress
