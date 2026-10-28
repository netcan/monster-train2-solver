#requires -Version 7.4
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $BaselineProfile,
    [Parameter(Mandatory)][string] $AlternativeProfile,
    [ValidateRange(1, 2)][int] $DraftChoiceRank = 1
)
$ErrorActionPreference = 'Stop'
function Read-Profile([string] $Path) {
    $path = (Resolve-Path -LiteralPath $Path).Path
    $inputRecord = Get-Content -LiteralPath (Join-Path $path 'campaign-input.json') -Raw | ConvertFrom-Json
    $summary = Get-Content -LiteralPath (Join-Path $path 'campaign-summary.json') -Raw | ConvertFrom-Json
    if (-not $summary.NativePassed -or -not $summary.InputProfileUnchanged -or -not $summary.OriginalFilesUnchanged -or
        -not $summary.BattleReplayPassed -or -not $summary.PlayerReplay -or $summary.BattlesWon -ne 1 -or
        (Get-FileHash -LiteralPath (Join-Path $path 'saves/save-singlePlayer.json') -Algorithm SHA256).Hash -ine $summary.FinalSaveSHA256) {
        throw "Profile failed native checkpoint acceptance: $path"
    }
    $events = @(Get-Content -LiteralPath (Join-Path $path 'campaign-audit.jsonl') | ForEach-Object { $_ | ConvertFrom-Json })
    return [pscustomobject]@{ Input = $inputRecord; Drafts = @($events | Where-Object kind -eq 'draft');
        Screen = @($events | Where-Object kind -eq 'screen')[-1].data }
}
$baseline = Read-Profile $BaselineProfile
$alternative = Read-Profile $AlternativeProfile
if ($baseline.Input.InputSaveSHA256 -cne $alternative.Input.InputSaveSHA256 -or
    $baseline.Input.BattleReplaySHA256 -cne $alternative.Input.BattleReplaySHA256 -or
    $alternative.Input.DraftChoiceRank -ne $DraftChoiceRank -or
    $baseline.Drafts.Count -eq 0 -or $baseline.Drafts.Count -ne $alternative.Drafts.Count) {
    throw 'Profiles do not share the original replay and requested draft rank.'
}
$changed = 0
for ($index = 0; $index -lt $baseline.Drafts.Count; $index++) {
    $old = $baseline.Drafts[$index].data
    $new = $alternative.Drafts[$index].data
    if ($new.rank -ne $DraftChoiceRank -or
        ($old.offers.id -join ',') -cne ($new.offers.id -join ',') -or
        @($new.offers | Where-Object id -CEQ $new.selected.id).Count -ne 1) {
        throw "Draft offers or selection differ unexpectedly at index $index."
    }
    if ($old.selected.id -cne $new.selected.id) { $changed++ }
}
$oldDeck = @($baseline.Screen.deck | ForEach-Object { $_.id + ':' + ($_.upgrades -join ',') } | Sort-Object)
$newDeck = @($alternative.Screen.deck | ForEach-Object { $_.id + ':' + ($_.upgrades -join ',') } | Sort-Object)
if ($changed -eq 0 -or $oldDeck.Count -ne $newDeck.Count -or -not (Compare-Object $oldDeck $newDeck)) {
    throw 'Alternative draft did not produce a different observed deck.'
}
Write-Output "CAMPAIGN-DRAFT-CHOICE PASS: $changed changed selections in $($baseline.Drafts.Count) shared native drafts; replay/checkpoint integrity verified."
