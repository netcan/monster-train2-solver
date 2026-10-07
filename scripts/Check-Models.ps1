#requires -Version 7.4
$ErrorActionPreference = 'Stop'
$workspace = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$fixtures = @('full-battle-steward-once.mt2f', 'full-battle-no-cards.mt2f', 'full-battle-units-and-junk.mt2f',
    'full-battle-units-spells-and-junk.mt2f', 'full-battle-statistics.mt2f', 'full-battle-numeric-upgrades.mt2f',
    'full-battle-dynamic-upgrades.mt2f', 'full-battle-sacrifice-upgrades.mt2f',
    'full-battle-hand-upgrades.mt2f', 'full-battle-targeted-hand-upgrades.mt2f',
    'full-battle-healing.mt2f', 'full-battle-healing-triggers.mt2f', 'full-battle-room-spells.mt2f',
    'full-battle-terminal-spells.mt2f',
    'full-battle-post-kill-spells.mt2f',
    'full-battle-random-spells.mt2f',
    'full-battle-random-status.mt2f',
    'full-battle-cross-room-spells.mt2f', 'full-battle-cross-room-targets.mt2f',
    'full-battle-attack-buffs.mt2f',
    'full-battle-max-health-spells.mt2f', 'full-battle-max-health-lethal.mt2f',
    'full-battle-numeric-ranges-ui-isolated.mt2f', 'full-battle-numeric-ranges-lethal-ui-isolated.mt2f',
    'full-battle-target-filters.mt2f',
    'full-battle-drawing-ui-isolated.mt2f',
    'full-battle-hand-removal.mt2f', 'full-battle-hand-removal-lethal.mt2f',
    'full-battle-generation.mt2f', 'full-battle-generation-lethal.mt2f',
    'full-battle-shared-piles.mt2f', 'full-battle-shared-piles-lethal.mt2f',
    'full-battle-damage-scaling.mt2f',
    'full-battle-dynamic-statistics.mt2f',
    'full-battle-status-scaling.mt2f',
    'full-battle-unit-upgrade-scaling.mt2f',
    'full-battle-unit-trigger-upgrades.mt2f',
    'full-battle-spawn-triggers.mt2f', 'full-battle-spawn-triggers-lethal.mt2f',
    'full-battle-unit-turn-begin.mt2f',
    'full-battle-team-turn-begin.mt2f',
    'full-battle-pre-hand-discard.mt2f', 'full-battle-pre-hand-discard-lethal.mt2f',
    'full-battle-clone-upgrade-refresh.mt2f',
    'full-battle-pre-combat.mt2f',
    'full-battle-statistic-cache.mt2f',
    'full-battle-triggered-healing.mt2f',
    'full-battle-post-combat-healing.mt2f',
    'full-battle-triggered-damage.mt2f',
    'full-battle-damage-death-queue.mt2f',
    'full-battle-terminal-death-damage.mt2f',
    'full-battle-hit-kill.mt2f',
    'full-battle-dying-upgrades.mt2f',
    'full-battle-attack-triggers.mt2f',
    'full-battle-triggered-status.mt2f',
    'full-battle-triggered-status-instant.mt2f',
    'full-battle-status-registry.mt2f',
    'full-battle-native-binary.mt2f',
    'full-battle-status-callbacks.mt2f',
    'full-battle-status-callback-actions.mt2f',
    'full-battle-energy-effects.mt2f',
    'full-battle-energy-effects-lethal.mt2f',
    'full-battle-x-cost.mt2f',
    'full-battle-x-cost-lethal.mt2f',
    'full-battle-bonus-draw.mt2f',
    'full-battle-bonus-draw-lethal.mt2f',
    'full-battle-room-capacity.mt2f',
    'full-battle-room-capacity-lethal.mt2f',
    'full-battle-direct-unit-upgrades.mt2f',
    'full-battle-equipment.mt2f',
    'full-battle-equipment-exhausted.mt2f',
    'full-battle-equipment-overflow.mt2f',
    'full-battle-equipment-triggers.mt2f',
    'full-battle-trigger-mutation.mt2f',
    'card-modifier-calibration.mt2f', 'rng-calibration.mt2f', 'gold-reward-calibration.mt2f',
    'standby-routing-calibration.mt2f', 'ui-rng-isolation-calibration.mt2f',
    'statistic-query-calibration.mt2f', 'statistic-overflow-calibration.mt2f',
    'statistic-zero-increment-calibration.mt2f') |
    ForEach-Object { Join-Path $workspace ('tests\fixtures\' + $_) }
$manifest = Import-Csv -LiteralPath (Join-Path $workspace 'tests\fixtures\manifest.tsv') -Delimiter "`t"
if ($manifest.Count -ne $fixtures.Count) { throw 'Fixture manifest inventory differs from the curated regression list.' }
$manifestByName = @{}
foreach ($entry in $manifest) {
    if ($manifestByName.ContainsKey($entry.archive)) { throw "Duplicate fixture manifest entry: $($entry.archive)" }
    $manifestByName[$entry.archive] = $entry
}
foreach ($fixture in $fixtures) {
    $name = Split-Path -Leaf $fixture
    if (-not $manifestByName.ContainsKey($name)) { throw "Fixture missing from manifest: $name" }
    $entry = $manifestByName[$name]
    if ((Get-Item -LiteralPath $fixture).Length -ne [long]$entry.archive_bytes -or
        (Get-FileHash -LiteralPath $fixture -Algorithm SHA256).Hash -ne $entry.archive_sha256) {
        throw "Fixture archive integrity check failed: $name"
    }
}
Write-Host "FIXTURE-INTEGRITY PASS: $($fixtures.Count) binary archives match the curated inventory and SHA-256 manifest."
dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release -- @fixtures
if ($LASTEXITCODE -ne 0) { throw 'Independent model checks failed.' }
