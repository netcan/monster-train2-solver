#requires -Version 7.4
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $CompletedArchive,
    [Parameter(Mandatory)][string] $OpeningArchive,
    [string[]] $ProgressArchive = @()
)
$ErrorActionPreference = 'Stop'
# Restore verifies every checkpoint member hash before reading native evidence.
$previous = & (Join-Path $PSScriptRoot 'Save-CampaignCheckpoint.ps1') -Archive $CompletedArchive -Restore | ConvertFrom-Json
$opening = & (Join-Path $PSScriptRoot 'Save-CampaignCheckpoint.ps1') -Archive $OpeningArchive -Restore | ConvertFrom-Json
if ($previous.StateKind -cne 'CompletedNativeRun' -or $opening.Ring -ne 1) {
    throw 'Expected a completed native campaign and a new first-battle checkpoint.'
}
$openingInput = Get-Content (Join-Path $opening.Profile 'campaign-input.json') -Raw | ConvertFrom-Json
$summary = Get-Content (Join-Path $opening.Profile 'campaign-summary.json') -Raw | ConvertFrom-Json
$save = Get-Content (Join-Path $opening.Profile 'saves/save-singlePlayer.json') -Raw | ConvertFrom-Json
$audit = @(Get-Content (Join-Path $opening.Profile 'campaign-audit.jsonl') | ForEach-Object { $_ | ConvertFrom-Json })
if (-not $openingInput.NewRunFromProfile -or $openingInput.StopBeforeRing -ne 1 -or
    (Get-FileHash (Join-Path $previous.Profile 'saves/save-singlePlayer.json')).Hash -ine $openingInput.InputSaveSHA256 -or
    (Get-FileHash (Join-Path $previous.Profile 'metagameSave.json')).Hash -ine $openingInput.InputMetagameSHA256 -or
    -not $summary.NativePassed -or $summary.ExitCode -ne 0 -or -not $summary.InputProfileUnchanged -or
    -not $summary.OriginalFilesUnchanged -or $summary.NativeCampaignWon -or $summary.BattlesWon -ne 0 -or
    (Get-FileHash (Join-Path $opening.Profile 'saves/save-singlePlayer.json')).Hash -ine $summary.FinalSaveSHA256 -or
    $save.gameSequence -ne 6 -or $save.runState.distance -ne 0 -or $save.completedBattles.Count -ne 0 -or
    $save.wasDevCheatUsed -or $save.startingConditions.seed -ne $openingInput.Seed -or
    $audit[0].kind -cne 'start' -or $audit[0].data.resume -ne $false -or $audit[-1].kind -cne 'complete' -or
    @($audit | Where-Object kind -EQ 'resume').Count -ne 0) {
    throw 'New-run native evidence or source-profile binding changed.'
}
$newRun = @($audit | Where-Object kind -EQ 'new-run')
if ($newRun.Count -ne 1 -or $newRun[0].data.seed -ne $openingInput.Seed) { throw 'Missing native new-run input.' }
$old = Get-Content (Join-Path $previous.Profile 'metagameSave.json') -Raw | ConvertFrom-Json
$next = Get-Content (Join-Path $opening.Profile 'metagameSave.json') -Raw | ConvertFrom-Json
$tracked = @{}
foreach ($row in $next.trackedValues) {
    if ($tracked.ContainsKey([int]$row.trackedValue)) { throw 'Duplicate native tracked-value identity.' }
    $tracked[[int]$row.trackedValue] = [long]$row.value
}
foreach ($row in $old.trackedValues) {
    if (-not $tracked.ContainsKey([int]$row.trackedValue) -or $tracked[[int]$row.trackedValue] -lt [long]$row.value) {
        throw "Cumulative native progression was reset: $($row.trackedValue)"
    }
}
foreach ($name in @('unlockedPyreCharacterIds', 'unlockedFeatures', 'completedDialogues')) {
    foreach ($value in $old.$name) {
        if ($value -notin $next.$name) { throw "Native metagame entry was lost: $name" }
    }
}
if ($next.storyProgressionNumCompleted -lt $old.storyProgressionNumCompleted) { throw 'Native story progression moved backwards.' }
$progressRows = @()
$priorProfile = $opening.Profile
$priorSave = $save
$priorMeta = $next
foreach ($path in $ProgressArchive) {
    $checkpoint = & (Join-Path $PSScriptRoot 'Save-CampaignCheckpoint.ps1') -Archive $path -Restore | ConvertFrom-Json
    $input = Get-Content (Join-Path $checkpoint.Profile 'campaign-input.json') -Raw | ConvertFrom-Json
    $result = Get-Content (Join-Path $checkpoint.Profile 'campaign-summary.json') -Raw | ConvertFrom-Json
    $currentSave = Get-Content (Join-Path $checkpoint.Profile 'saves/save-singlePlayer.json') -Raw | ConvertFrom-Json
    $currentMeta = Get-Content (Join-Path $checkpoint.Profile 'metagameSave.json') -Raw | ConvertFrom-Json
    $distance = [int]$currentSave.runState.distance
    if ($distance -ne [int]$priorSave.runState.distance + 1 -or $checkpoint.Ring -ne $distance + 1 -or
        $input.Seed -ne $openingInput.Seed -or $currentSave.startingConditions.seed -ne $openingInput.Seed -or
        $currentSave.startingConditions.ascensionLevel -ne $priorSave.startingConditions.ascensionLevel -or
        $currentSave.startingConditions.mainClassInfo.classId -cne $priorSave.startingConditions.mainClassInfo.classId -or
        $currentSave.startingConditions.subclassInfo.classId -cne $priorSave.startingConditions.subclassInfo.classId -or
        $currentSave.gameSequence -ne 6 -or $currentSave.wasDevCheatUsed -or
        $currentSave.completedBattles.Count -ne $distance -or
        @($currentSave.completedBattles | Where-Object bossHPRemaining -GT 0).Count -or
        (($currentSave.completedBattles.battleIndex | Sort-Object) -join ',') -cne ((0..($distance - 1)) -join ',') -or
        $input.StopBeforeRing -ne $distance + 1 -or $input.NewRunFromProfile -or
        $input.InputSaveSHA256 -ine (Get-FileHash (Join-Path $priorProfile 'saves/save-singlePlayer.json')).Hash -or
        $input.InputMetagameSHA256 -ine (Get-FileHash (Join-Path $priorProfile 'metagameSave.json')).Hash -or
        -not $result.NativePassed -or $result.ExitCode -ne 0 -or -not $result.InputProfileUnchanged -or
        -not $result.OriginalFilesUnchanged -or -not $result.PlayerReplay -or -not $result.BattleReplayPassed -or
        $result.StoryProgressionOnly -or $result.NativeCampaignWon -or $result.BattlesWon -ne 1 -or
        $result.FinalSaveSHA256 -ine (Get-FileHash (Join-Path $checkpoint.Profile 'saves/save-singlePlayer.json')).Hash) {
        throw 'Progress archive does not continue the immediately preceding native metagame checkpoint.'
    }
    $currentTracked = @{}
    foreach ($row in $currentMeta.trackedValues) {
        if ($currentTracked.ContainsKey([int]$row.trackedValue)) { throw 'Duplicate progress tracked-value identity.' }
        $currentTracked[[int]$row.trackedValue] = [long]$row.value
    }
    $changes = @()
    foreach ($row in $priorMeta.trackedValues) {
        $key = [int]$row.trackedValue
        if (-not $currentTracked.ContainsKey($key) -or $currentTracked[$key] -lt [long]$row.value) {
            throw "Cumulative native battle progression was reset: $key"
        }
        if ($currentTracked[$key] -gt [long]$row.value) {
            $changes += [ordered]@{ TrackedValue = $key; Before = [long]$row.value; After = $currentTracked[$key] }
        }
    }
    foreach ($name in @('unlockedPyreCharacterIds', 'unlockedFeatures', 'completedDialogues')) {
        foreach ($value in $priorMeta.$name) {
            if ($value -notin $currentMeta.$name) { throw "Progress metagame entry was lost: $name" }
        }
    }
    if ($currentMeta.storyProgressionNumCompleted -lt $priorMeta.storyProgressionNumCompleted) {
        throw 'Progress story moved backwards.'
    }
    $progressRows += [ordered]@{ CompletedBattles = $distance; Changes = $changes
        ArchiveSHA256 = (Get-FileHash $path).Hash }
    $priorProfile = $checkpoint.Profile; $priorSave = $currentSave; $priorMeta = $currentMeta
}
[ordered]@{
    Evidence = 'NativeNewRunAndUnchangedSourceProfile'; BattleSimulations = 0
    Seed = $openingInput.Seed; PreviousTrackedValuesRetained = $old.trackedValues.Count
    DialogueSkipInputs = @($audit | Where-Object kind -EQ 'skip-character-dialogue').Count
    CompletedDialoguesBefore = $old.completedDialogues.Count; CompletedDialoguesAfter = $next.completedDialogues.Count
    OpeningArchiveSHA256 = (Get-FileHash $OpeningArchive).Hash
    BattleProgress = $progressRows
    TrueFinalVictoryAccepted = $false
} | ConvertTo-Json -Depth 8
