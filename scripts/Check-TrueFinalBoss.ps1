#requires -Version 7.4
[CmdletBinding()]
param(
    [ValidateSet('Birth', 'Spawn', 'ScheduledSpawn', 'Health', 'LifeLink', 'HealthUpdates', 'World', 'Damage', 'AllTargetDamage', 'SpellDamage', 'PyreBaneTargets', 'Controllers', 'Phases', 'ExhaustedPhases', 'Continuation', 'Passive', 'UnitTurns', 'PostCombat', 'Train', 'Preview', 'Battle', 'Allied', 'MultiAllied', 'Dormant', 'DormantTrample', 'Relentless', 'Lethal', 'Death')][string[]] $Case = @('Birth', 'Spawn', 'ScheduledSpawn', 'Health', 'LifeLink', 'HealthUpdates', 'World', 'Damage', 'AllTargetDamage', 'SpellDamage', 'PyreBaneTargets', 'Controllers', 'Phases', 'ExhaustedPhases', 'Continuation', 'Passive', 'UnitTurns', 'PostCombat', 'Train', 'Preview', 'Battle', 'Allied', 'MultiAllied', 'Dormant', 'DormantTrample', 'Relentless', 'Lethal', 'Death'),
    [switch] $SkipBuild
)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE -ne 0) { throw 'True-final-boss checker build failed.' }
}
$checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
$manifest = Import-Csv -LiteralPath (Join-Path $workspace 'tests/fixtures/manifest.tsv') -Delimiter "`t"
function Invoke-TrueFinalBossArchive([string] $archive, [string] $mode) {
    $path = Join-Path $workspace "tests/fixtures/$archive"
    $rows = @($manifest | Where-Object archive -CEQ $archive)
    if ($rows.Count -ne 1 -or (Get-Item -LiteralPath $path).Length -ne [long]$rows[0].archive_bytes -or
        (Get-FileHash -LiteralPath $path).Hash -ine $rows[0].archive_sha256) { throw "True-final-boss archive metadata differs: $archive" }
    dotnet $checker $mode $path
    if ($LASTEXITCODE -ne 0) { throw "True-final-boss focused check failed: $archive" }
}
foreach ($item in $Case) {
    if ($item -eq 'Death') {
        foreach ($target in @('dominion', 'savagery', 'entropybot', 'entropymid', 'entropytop')) {
            Invoke-TrueFinalBossArchive "true-final-boss-death-$target.mt2f" '--true-final-boss-death-only'
        }
        continue
    }
    $archive = if ($item -eq 'Birth') { 'true-final-boss-birth-calibration.mt2f' }
    elseif ($item -eq 'Spawn') { 'true-final-boss-spawn-calibration.mt2f' } elseif ($item -eq 'Health') {
        'true-final-boss-linked-health-calibration.mt2f'
    } elseif ($item -eq 'LifeLink') { 'true-final-boss-life-link-calibration.mt2f' }
    elseif ($item -eq 'HealthUpdates') { 'true-final-boss-health-updates-calibration.mt2f' }
    elseif ($item -eq 'World') { 'true-final-boss-live-world-observation.mt2f' }
    elseif ($item -eq 'AllTargetDamage') { 'true-final-boss-all-target-damage-calibration.mt2f' }
    elseif ($item -eq 'SpellDamage') { 'true-final-boss-spell-damage-calibration.mt2f' }
    elseif ($item -eq 'PyreBaneTargets') { 'true-final-boss-pyrebane-targets-calibration.mt2f' }
    elseif ($item -eq 'Controllers') { 'true-final-boss-controller-calibration.mt2f' }
    elseif ($item -eq 'ScheduledSpawn') { 'true-final-boss-scheduled-spawn-calibration.mt2f' }
    elseif ($item -eq 'Continuation') { 'true-final-boss-continuation-calibration.mt2f' }
    elseif ($item -eq 'Passive') { 'true-final-boss-passive-calibration.mt2f' }
    elseif ($item -eq 'UnitTurns') { 'true-final-boss-unit-turn-calibration.mt2f' }
    elseif ($item -in @('PostCombat', 'Train')) { 'true-final-boss-post-combat-calibration.mt2f' }
    elseif ($item -eq 'Preview') { 'true-final-boss-preview-calibration.mt2f' }
    elseif ($item -eq 'Battle') { 'true-final-boss-full-battle-calibration.mt2f' }
    elseif ($item -eq 'Allied') { 'true-final-boss-allied-battle-calibration.mt2f' }
    elseif ($item -eq 'MultiAllied') { 'true-final-boss-multi-allied-calibration.mt2f' }
    elseif ($item -eq 'Dormant') { 'true-final-boss-dormant-calibration.mt2f' }
    elseif ($item -eq 'DormantTrample') { 'true-final-boss-dormant-trample-calibration.mt2f' }
    elseif ($item -eq 'Relentless') { 'true-final-boss-relentless-calibration.mt2f' }
    elseif ($item -eq 'Lethal') { 'true-final-boss-lethal-calibration.mt2f' }
    elseif ($item -eq 'Phases') { 'true-final-boss-phase-calibration.mt2f' }
    elseif ($item -eq 'ExhaustedPhases') { 'true-final-boss-exhausted-phase-calibration.mt2f' }
    else { 'true-final-boss-damage-calibration.mt2f' }
    $mode = if ($item -eq 'Birth') { '--true-final-boss-birth-only' }
    elseif ($item -eq 'Spawn') { '--true-final-boss-spawn-only' } elseif ($item -eq 'Health') {
        '--true-final-boss-health-only'
    } elseif ($item -eq 'LifeLink') { '--true-final-boss-life-link-only' }
    elseif ($item -eq 'HealthUpdates') { '--true-final-boss-health-updates-only' }
    elseif ($item -eq 'World') { '--true-final-boss-world-only' }
    elseif ($item -eq 'PyreBaneTargets') { '--pyrebane-targets-only' }
    elseif ($item -eq 'Controllers') { '--true-final-boss-controllers-only' }
    elseif ($item -eq 'ScheduledSpawn') { '--true-final-boss-scheduled-only' }
    elseif ($item -eq 'Continuation') { '--true-final-boss-continuation-only' }
    elseif ($item -eq 'Passive') { '--true-final-boss-passive-only' }
    elseif ($item -eq 'UnitTurns') { '--true-final-boss-unit-turns-only' }
    elseif ($item -eq 'PostCombat') { '--true-final-boss-post-combat-only' }
    elseif ($item -eq 'Train') { '--true-final-boss-train-only' }
    elseif ($item -eq 'Preview') { '--true-final-boss-preview-only' }
    elseif ($item -eq 'Battle') { '--true-final-boss-battle-only' }
    elseif ($item -eq 'Allied') { '--true-final-boss-allied-only' }
    elseif ($item -eq 'MultiAllied') { '--true-final-boss-multi-allied-only' }
    elseif ($item -in @('Dormant', 'DormantTrample')) { '--true-final-boss-dormant-only' }
    elseif ($item -eq 'Relentless') { '--true-final-boss-relentless-only' }
    elseif ($item -eq 'Lethal') { '--true-final-boss-lethal-only' }
    elseif ($item -in @('Phases', 'ExhaustedPhases')) { '--true-final-boss-phases-only' }
    else { '--true-final-boss-damage-only' }
    Invoke-TrueFinalBossArchive $archive $mode
}
Write-Output "TRUE-FINAL-BOSS-FOCUSED PASS cases=$($Case -join ','); no full regression executed."
