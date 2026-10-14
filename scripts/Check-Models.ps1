#requires -Version 7.4
param([string[]]$Fixture = @())

$ErrorActionPreference = 'Stop'
$workspace = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$fixtureNames = @('full-battle-steward-once.mt2f', 'full-battle-no-cards.mt2f', 'full-battle-units-and-junk.mt2f',
    'full-battle-unplayed-card-pyre-damage.mt2f',
    'full-battle-unplayed-card-effects.mt2f', 'full-battle-train-heal.mt2f', 'full-battle-status-upgrade.mt2f',
    'full-battle-overheal.mt2f', 'full-battle-corruption.mt2f', 'full-battle-corruption-unit-trigger.mt2f',
    'full-battle-corruption-carver-trigger.mt2f', 'full-battle-permanent-corruption.mt2f',
    'full-battle-soul-siphon.mt2f', 'full-battle-moon-phase-card.mt2f', 'full-battle-moon-phase-advance.mt2f',
    'full-battle-moon-phase-callbacks.mt2f',
    'full-battle-conditional-no-player-status.mt2f',
    'full-battle-conditional-pyre-damage.mt2f',
    'full-battle-self-attack-status.mt2f',
    'full-battle-units-spells-and-junk.mt2f', 'full-battle-relic-card-modifiers.mt2f', 'full-battle-original-relic-batch.mt2f',
    'full-battle-covenant-character-filter.mt2f',
    'full-battle-titan-skin.mt2f',
    'full-battle-all-units-multistrike-mutator.mt2f',
    'full-battle-original-unit-relics.mt2f',
    'full-battle-original-trigger-relics.mt2f', 'full-battle-original-trigger-relic-deaths.mt2f',
    'full-battle-original-status-relics.mt2f', 'full-battle-original-burnout-stacks.mt2f',
    'full-battle-original-sting-relics.mt2f',
    'full-battle-original-deployable-relic.mt2f',
    'full-battle-original-magnitude-relics.mt2f',
    'full-battle-original-lifecycle-relics.mt2f',
    'full-battle-original-decay-relics.mt2f',
    'full-battle-original-resource-relics.mt2f', 'full-battle-original-forge-points-relic.mt2f', 'full-battle-pyre-resource-refresh.mt2f',
    'full-battle-original-floor-capacity-relics.mt2f',
    'full-battle-original-endless-return.mt2f',
    'full-battle-original-card-play-magic-power.mt2f',
    'full-battle-original-card-draw-on-card-play.mt2f',
    'full-battle-original-room-entry-status-relics.mt2f',
    'full-battle-original-moon-phase-energy-relic.mt2f',
    'full-battle-original-card-attack-relic.mt2f',
    'full-battle-original-unit-upgrade-trigger-relic.mt2f',
    'full-battle-relic-card-status-upgrades.mt2f', 'full-battle-relic-card-negative-unhealed.mt2f',
    'full-battle-generation-relic-card-status-upgrades.mt2f',
    'full-battle-relic-card-piercing-upgrades.mt2f', 'full-battle-relic-card-self-purge-upgrades.mt2f',
    'full-battle-relic-card-exhaust-upgrades.mt2f',
    'full-battle-relic-card-ephemeral-upgrades.mt2f',
    'full-battle-relic-extra-tough-tombs.mt2f',
    'full-battle-relic-card-ability-upgrades.mt2f',
    'full-battle-relic-card-trigger-upgrades.mt2f',
    'full-battle-relic-card-cast-trigger-upgrades.mt2f',
    'full-battle-relic-card-cast-trigger-unit-upgrades.mt2f',
    'full-battle-relic-card-replacement-upgrades.mt2f',
    'full-battle-conditional-relic-card-upgrades.mt2f', 'full-battle-statistics.mt2f', 'full-battle-numeric-upgrades.mt2f',
    'full-battle-dynamic-upgrades.mt2f', 'full-battle-sacrifice-effects.mt2f', 'full-battle-card-effect-kill.mt2f', 'full-battle-card-effect-kill-subtype.mt2f', 'full-battle-null-forwarding-cleave.mt2f', 'full-battle-null-unit-triggers.mt2f', 'full-battle-null-targetless-scourges.mt2f', 'full-battle-original-trample-basher.mt2f', 'full-battle-original-spell-shield.mt2f', 'full-battle-choose-discard.mt2f', 'full-battle-dragon-hoard.mt2f', 'full-battle-remove-all-status-effects.mt2f', 'full-battle-card-recursion.mt2f', 'full-battle-card-recursion-choice.mt2f', 'full-battle-card-recursion-choose-to-hand.mt2f', 'full-battle-card-recursion-filter-choice.mt2f', 'full-battle-card-recursion-discard-choice.mt2f', 'full-battle-card-recursion-upgrade.mt2f', 'full-battle-card-recursion-upgrade-hand.mt2f', 'full-battle-card-recursion-upgrade-choice.mt2f', 'full-battle-card-recursion-upgrade-filter-choice.mt2f', 'full-battle-card-recursion-upgrade-replacement-choice.mt2f', 'full-battle-sacrifice-upgrades.mt2f',
    'full-battle-set-health.mt2f',
    'full-battle-transfer-attack.mt2f',
    'full-battle-transfer-attack-physical.mt2f',
    'full-battle-transfer-attack-ability.mt2f',
    'full-battle-brain-transfer.mt2f',
    'full-battle-frost-channeling.mt2f',
    'full-battle-mix.mt2f', 'full-battle-mix-alternate.mt2f', 'full-battle-mix-01.mt2f', 'full-battle-mix-10.mt2f',
    'full-battle-mix-unstable.mt2f', 'full-battle-mix-unstable-explosion.mt2f',
    'full-battle-damage-by-units-killed.mt2f',
    'full-battle-damage-by-units-killed-physical.mt2f',
    'full-battle-hand-upgrades.mt2f', 'full-battle-targeted-hand-upgrades.mt2f', 'full-battle-permanent-consume-hand-upgrades.mt2f',
    'full-battle-permanent-consume-over-temporary.mt2f',
    'full-battle-choose-discard-others.mt2f',
    'full-battle-choose-discard-others-policy.mt2f',
    'full-battle-choose-discard-refine.mt2f',
    'full-battle-choose-discard-persistent.mt2f',
    'full-battle-healing.mt2f', 'full-battle-healing-triggers.mt2f', 'full-battle-room-spells.mt2f',
    'full-battle-damage-per-hp.mt2f', 'full-battle-damage-per-hp-last-attacker.mt2f',
    'full-battle-damage-per-hp-ability.mt2f', 'full-battle-damage-overflow.mt2f',
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
    'full-battle-typed-draw.mt2f',
    'full-battle-hand-removal.mt2f', 'full-battle-hand-removal-lethal.mt2f', 'full-battle-random-discard.mt2f',
    'full-battle-generation.mt2f', 'full-battle-generation-permanent-consume.mt2f', 'full-battle-generation-lethal.mt2f',
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
    'full-battle-purify.mt2f',
    'full-battle-purify-queues.mt2f',
    'full-battle-incant.mt2f',
    'full-battle-incant-thresholds.mt2f',
    'full-battle-ability-incant.mt2f', 'full-battle-room-selection-preview.mt2f', 'full-battle-incant-relic.mt2f',
    'full-battle-incant-relics-combined.mt2f',
    'full-battle-spawn-status-relics.mt2f',
    'full-battle-spawn-status-relics-clones.mt2f',
    'full-battle-spawn-status-relics-clones-ungated-capture.mt2f',
    'full-battle-spawn-status-relics-mixed-cardless-order.mt2f',
    'full-battle-original-stealth-waxers.mt2f',
    'full-battle-original-team-spawn-status.mt2f',
    'full-battle-spawn-status-relics-tuned-gear.mt2f',
    'full-battle-spawn-status-relics-boss-mute.mt2f',
    'full-battle-spawn-status-relics-pyre.mt2f',
    'full-battle-spawn-status-relics-egg-hatch.mt2f',
    'full-battle-egg-transformation.mt2f',
    'full-battle-original-kinhost-vessel.mt2f',
    'full-battle-original-kinhost-vessel-delayed.mt2f',
    'full-battle-original-mageblade-magnitude.mt2f',
    'full-battle-cardless-relic-upgrades.mt2f',
    'full-battle-energy-effects.mt2f',
    'full-battle-energy-effects-lethal.mt2f',
    'full-battle-x-cost.mt2f',
    'full-battle-x-cost-lethal.mt2f',
    'full-battle-bonus-draw.mt2f',
    'full-battle-bonus-draw-lethal.mt2f',
    'full-battle-room-capacity.mt2f',
    'full-battle-room-capacity-lethal.mt2f',
    'full-battle-room-capacity-attachment.mt2f',
    'full-battle-direct-unit-upgrades.mt2f',
    'full-battle-equipment.mt2f',
    'full-battle-equipment-exhausted.mt2f',
    'full-battle-equipment-overflow.mt2f',
    'full-battle-equipment-triggers.mt2f',
    'full-battle-equipment-buff.mt2f',
    'full-battle-trigger-mutation.mt2f', 'full-battle-detached-bonus-draw.mt2f',
    'full-battle-conditional-triggers.mt2f',
    'full-battle-trigger-repeats.mt2f',
    'full-battle-companion-boss.mt2f', 'full-battle-companion-boss-actions.mt2f',
    'full-battle-sentry.mt2f', 'full-battle-sentry-lethal.mt2f',
    'full-battle-ability-cooldown.mt2f', 'full-battle-ability-cache.mt2f',
    'full-battle-ability-activation.mt2f', 'full-battle-ability-activation-x.mt2f',
    'full-battle-ability-activation-lethal.mt2f',
    'full-battle-unit-ability-teleport.mt2f',
    'full-battle-spawn-or-status.mt2f',
    'full-battle-ability-lifecycle.mt2f',
    'full-battle-ability-effects.mt2f',
    'full-battle-equipment-abilities.mt2f',
    'full-battle-horde-statuses.mt2f',
    'full-battle-horde-merge.mt2f',
    'full-battle-harvest-triggers.mt2f',
    'full-battle-horde-removal.mt2f',
    'full-battle-horde-death.mt2f',
    'full-battle-horde-upgrades.mt2f',
    'full-battle-dying-horde-upgrades.mt2f',
    'full-battle-rally-triggers.mt2f',
    'full-battle-rally-lethal.mt2f',
    'full-battle-unit-identities.mt2f',
    'full-battle-unit-identities-lethal.mt2f',
    'full-battle-mageblade.mt2f',
    'full-battle-multi-summon.mt2f',
    'full-battle-multi-summon-zero.mt2f',
    'full-battle-multi-summon-upgrade.mt2f',
    'full-battle-multi-summon-upgrade-unique.mt2f',
    'full-battle-multi-summon-upgrade-restricted.mt2f',
    'full-battle-multi-summon-fresh.mt2f',
    'full-battle-multi-summon-fresh-deaths.mt2f',
    'full-battle-multi-summon-additional.mt2f',
    'full-battle-multi-summon-additional-fresh.mt2f',
    'full-battle-multi-summon-pool-additional.mt2f',
    'full-battle-multi-summon-pool-additional-fresh.mt2f',
    'full-battle-multi-summon-pool-singleton-additional.mt2f',
    'full-battle-multi-summon-pool.mt2f',
    'full-battle-multi-summon-pool-no-primary.mt2f',
    'full-battle-multi-summon-pool-missing-fresh.mt2f',
    'full-battle-multi-summon-pool-additional-missing-fresh.mt2f',
    'full-battle-multi-summon-pool-no-primary-missing-fresh.mt2f',
    'full-battle-multi-summon-pool-no-primary-missing-fresh-deaths.mt2f',
    'full-battle-spawn-points.mt2f',
    'full-battle-physical-spawn-points.mt2f',
    'full-battle-physical-spawn-points-no-cards.mt2f',
    'full-battle-triggered-summons.mt2f', 'full-battle-triggered-summons-fresh.mt2f',
    'full-battle-triggered-summons-equipment.mt2f', 'full-battle-triggered-summons-equipment-fresh.mt2f',
    'full-battle-triggered-summons-equipment-death.mt2f', 'full-battle-triggered-summons-equipment-death-fresh.mt2f',
    'full-battle-triggered-summons-equipment-owned.mt2f', 'full-battle-triggered-summons-equipment-owned-fresh.mt2f',
    'full-battle-triggered-summons-equipment-owned-death.mt2f', 'full-battle-triggered-summons-equipment-owned-death-fresh.mt2f',
    'full-battle-triggered-summons-equipment-owned-revival.mt2f', 'full-battle-triggered-summons-equipment-owned-revival-fresh.mt2f',
    'full-battle-triggered-summons-death.mt2f', 'full-battle-triggered-summons-death-fresh.mt2f',
    'full-battle-revival.mt2f', 'full-battle-bump.mt2f', 'full-battle-companion-bump.mt2f', 'full-battle-outer-boss-bump.mt2f', 'full-battle-unit-clone.mt2f', 'full-battle-unit-copy.mt2f',
    'full-battle-hero-copy.mt2f',
    'full-battle-spawn-enchant.mt2f',
    'full-battle-persistent-enchantment.mt2f',
    'full-battle-persistent-enchantment-deaths.mt2f',
    'full-battle-persistent-enchantment-revivals.mt2f',
    'full-battle-persistent-enchantment-random.mt2f',
    'full-battle-persistent-enchantment-random-deaths.mt2f',
    'full-battle-persistent-enchantment-random-revivals.mt2f',
    'full-battle-persistent-enchantment-upgrades.mt2f',
    'full-battle-persistent-enchantment-random-upgrades.mt2f',
    'full-battle-persistent-enchantment-summons.mt2f',
    'full-battle-persistent-enchantment-summons-fresh.mt2f',
    'full-battle-persistent-enchantment-random-summons.mt2f',
    'full-battle-persistent-enchantment-random-summons-fresh.mt2f',
    'full-battle-branch-card-masks.mt2f', 'full-battle-branch-card-masks-hand-upgrades.mt2f',
    'full-battle-branch-card-masks-generation.mt2f', 'full-battle-branch-card-masks-clone-upgrade-refresh.mt2f',
    'full-battle-branch-card-masks-bonus-draw.mt2f', 'full-battle-branch-card-masks-equipment.mt2f',
    'card-modifier-calibration.mt2f', 'rng-calibration.mt2f', 'gold-reward-calibration.mt2f',
    'standby-routing-calibration.mt2f', 'ui-rng-isolation-calibration.mt2f',
    'statistic-query-calibration.mt2f', 'statistic-overflow-calibration.mt2f',
    'statistic-zero-increment-calibration.mt2f', 'horde-stat-calibration.mt2f', 'enchantment-lifecycle-calibration.mt2f',
    'enchantment-combat-calibration.mt2f', 'enchantment-world-calibration.mt2f', 'enchantment-source-order-calibration.mt2f',
    'character-removal-calibration.mt2f',
    'enchantment-summon-calibration.mt2f', 'enchantment-summon-fresh-calibration.mt2f',
    'enchantment-summon-random-calibration.mt2f', 'enchantment-summon-random-fresh-calibration.mt2f',
    'preview-reference-calibration.mt2f',
    'preview-birth-calibration.mt2f', 'preview-birth-fresh-calibration.mt2f',
    'preview-birth-random-calibration.mt2f', 'preview-birth-random-fresh-calibration.mt2f',
    'physical-plane-calibration.mt2f', 'card-upgrade-mask-calibration.mt2f',
    'card-modifier-overflow-calibration.mt2f', 'card-trait-composition-calibration.mt2f',
    'card-status-composition-calibration.mt2f', 'card-owned-mask-calibration.mt2f',
    'card-upgrade-lifecycle-calibration.mt2f', 'relic-card-upgrade-calibration.mt2f')
$selectedFixtureNames = $fixtureNames
if ($Fixture.Count -gt 0) {
    $selectedFixtureNames = @($Fixture | ForEach-Object { Split-Path -Leaf $_ })
    $unknown = @($selectedFixtureNames | Where-Object { $_ -notin $fixtureNames })
    if ($unknown.Count -gt 0) { throw "Unknown curated fixture: $($unknown -join ', ')" }
    if (@($selectedFixtureNames | Select-Object -Unique).Count -ne $selectedFixtureNames.Count) {
        throw 'Duplicate fixture selections are not allowed.'
    }
}
$fixtures = @($selectedFixtureNames | ForEach-Object { Join-Path $workspace ('tests\fixtures\' + $_) })
$manifest = Import-Csv -LiteralPath (Join-Path $workspace 'tests\fixtures\manifest.tsv') -Delimiter "`t"
if ($manifest.Count -ne $fixtureNames.Count) { throw 'Fixture manifest inventory differs from the curated regression list.' }
$manifestByName = @{}
foreach ($entry in $manifest) {
    if ($manifestByName.ContainsKey($entry.archive)) { throw "Duplicate fixture manifest entry: $($entry.archive)" }
    if ($entry.archive -notin $fixtureNames) { throw "Unexpected fixture manifest entry: $($entry.archive)" }
    $manifestByName[$entry.archive] = $entry
}
foreach ($fixturePath in $fixtures) {
    $name = Split-Path -Leaf $fixturePath
    if (-not $manifestByName.ContainsKey($name)) { throw "Fixture missing from manifest: $name" }
    $entry = $manifestByName[$name]
    if ((Get-Item -LiteralPath $fixturePath).Length -ne [long]$entry.archive_bytes -or
        (Get-FileHash -LiteralPath $fixturePath -Algorithm SHA256).Hash -ne $entry.archive_sha256) {
        throw "Fixture archive integrity check failed: $name"
    }
}
if ($Fixture.Count -gt 0) {
    Write-Host "FIXTURE-INTEGRITY PASS: $($fixtures.Count) selected of $($fixtureNames.Count) curated archives match the SHA-256 manifest."
} else {
    Write-Host "FIXTURE-INTEGRITY PASS: $($fixtures.Count) binary archives match the curated inventory and SHA-256 manifest."
}
$focusedBumpFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -in @('full-battle-companion-bump.mt2f', 'full-battle-outer-boss-bump.mt2f')
})
$focusedTeamSpawnStatusFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-team-spawn-status.mt2f'
})
$generalFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -notin @('full-battle-companion-bump.mt2f', 'full-battle-outer-boss-bump.mt2f',
        'full-battle-original-team-spawn-status.mt2f', 'full-battle-egg-transformation.mt2f',
        'full-battle-corruption-unit-trigger.mt2f',
        'full-battle-original-kinhost-vessel.mt2f',
        'full-battle-original-kinhost-vessel-delayed.mt2f',
        'full-battle-relic-card-cast-trigger-upgrades.mt2f',
        'full-battle-relic-card-cast-trigger-unit-upgrades.mt2f')
})
if ($generalFixtures.Count -gt 0) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release -- @generalFixtures
} else {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release
}
if ($LASTEXITCODE -ne 0) { throw 'Independent model checks failed.' }

foreach ($fixturePath in $focusedTeamSpawnStatusFixtures) {
    foreach ($mode in @('--room-combat-only', '--native-actions-only', '--unit-clone-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Team-wide spawn status checks failed ($mode): $fixturePath" }
    }
}

$focusedSpawnStatusFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -in @('full-battle-spawn-status-relics-boss-mute.mt2f',
        'full-battle-spawn-status-relics-pyre.mt2f', 'full-battle-spawn-status-relics-egg-hatch.mt2f')
})
foreach ($fixturePath in $focusedSpawnStatusFixtures) {
    foreach ($mode in @('--relic-spawn-status-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Spawn-status relic checks failed ($mode): $fixturePath" }
    }
}

foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-egg-transformation.mt2f' })) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --egg-transformation-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Egg transformation checks failed: $fixturePath" }
}

foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-kinhost-vessel.mt2f' })) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --original-egg-card-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Original Egg card checks failed: $fixturePath" }
}

foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-kinhost-vessel-delayed.mt2f' })) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --original-egg-card-delayed-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Delayed original Egg card checks failed: $fixturePath" }
}

$relicCardCastTriggerFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -in @('full-battle-relic-card-cast-trigger-upgrades.mt2f',
        'full-battle-relic-card-cast-trigger-unit-upgrades.mt2f')
})
foreach ($fixturePath in $relicCardCastTriggerFixtures) {
    foreach ($mode in @('--relic-card-modifiers-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Relic card cast-trigger checks failed ($mode): $fixturePath" }
    }
}

$cardDrawOnCardPlayFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-card-draw-on-card-play.mt2f'
})
foreach ($fixturePath in $cardDrawOnCardPlayFixtures) {
    foreach ($mode in @('--original-card-draw-on-card-play-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Card-draw-on-play relic checks failed ($mode): $fixturePath" }
    }
}

$unplayedCardTriggerFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-unplayed-card-pyre-damage.mt2f'
})
foreach ($fixturePath in $unplayedCardTriggerFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --unplayed-card-trigger-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Unplayed-card trigger checks failed: $fixturePath" }
}

$unplayedCardEffectsFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-unplayed-card-effects.mt2f'
})
foreach ($fixturePath in $unplayedCardEffectsFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --unplayed-card-effects-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Unplayed-card effect checks failed: $fixturePath" }
}

$removeAllStatusFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-remove-all-status-effects.mt2f'
})
foreach ($fixturePath in $removeAllStatusFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --remove-all-status-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Remove-all-status checks failed: $fixturePath" }
}

$trainHealFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-train-heal.mt2f'
})
foreach ($fixturePath in $trainHealFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --train-heal-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Train-heal checks failed: $fixturePath" }
}

$overhealFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-overheal.mt2f'
})
foreach ($fixturePath in $overhealFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --overheal-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Overheal checks failed: $fixturePath" }
}

$corruptionFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-corruption.mt2f'
})
foreach ($fixturePath in $corruptionFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --room-corruption-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Room-corruption checks failed: $fixturePath" }
}

$corruptionUnitFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-corruption-unit-trigger.mt2f'
})
foreach ($fixturePath in $corruptionUnitFixtures) {
    foreach ($mode in @('--room-combat-only', '--corruption-unit-trigger-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Corruption unit-trigger checks failed ($mode): $fixturePath" }
    }
}

$corruptionCarverFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-corruption-carver-trigger.mt2f'
})
foreach ($fixturePath in $corruptionCarverFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --corruption-carver-trigger-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Corruption Carver trigger checks failed: $fixturePath" }
}

$permanentCorruptionFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-permanent-corruption.mt2f'
})
foreach ($fixturePath in $permanentCorruptionFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --permanent-corruption-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Permanent-corruption checks failed: $fixturePath" }
}

$moonPhaseCardFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-moon-phase-card.mt2f'
})
foreach ($fixturePath in $moonPhaseCardFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --moon-phase-transition-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Moon-phase transition checks failed: $fixturePath" }
}

$moonPhaseAdvanceFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-moon-phase-advance.mt2f'
})
foreach ($fixturePath in $moonPhaseAdvanceFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --moon-phase-advance-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Moon-phase advance checks failed: $fixturePath" }
}

$moonPhaseCallbackFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-moon-phase-callbacks.mt2f'
})
foreach ($fixturePath in $moonPhaseCallbackFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --moon-phase-callbacks-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Moon-phase callback checks failed: $fixturePath" }
}

$conditionalNoPlayerFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-conditional-no-player-status.mt2f'
})
foreach ($fixturePath in $conditionalNoPlayerFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --no-player-units-status-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Conditional no-player status checks failed: $fixturePath" }
}

$conditionalPyreDamageFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-conditional-pyre-damage.mt2f'
})
foreach ($fixturePath in $conditionalPyreDamageFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --conditional-pyre-damage-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Conditional Pyre damage checks failed: $fixturePath" }
}

$selfAttackStatusFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-self-attack-status.mt2f'
})
foreach ($fixturePath in $selfAttackStatusFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --self-attack-status-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Self-attack status checks failed: $fixturePath" }
}

$scorchFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-soul-siphon.mt2f'
})
foreach ($fixturePath in $scorchFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --scorch-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Scorch checks failed: $fixturePath" }
}

$statusUpgradeFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-status-upgrade.mt2f'
})
foreach ($fixturePath in $statusUpgradeFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --status-upgrade-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Status-upgrade checks failed: $fixturePath" }
}

$recursionChoiceFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -in @('full-battle-card-recursion-choice.mt2f', 'full-battle-card-recursion-choose-to-hand.mt2f', 'full-battle-card-recursion-filter-choice.mt2f', 'full-battle-card-recursion-discard-choice.mt2f')
})
foreach ($fixturePath in $recursionChoiceFixtures) {
    $selectionMode = switch (Split-Path -Leaf $fixturePath) {
        'full-battle-card-recursion-choose-to-hand.mt2f' { '--card-recursion-hand-choice-only' }
        'full-battle-card-recursion-filter-choice.mt2f' { '--card-recursion-filter-choice-only' }
        'full-battle-card-recursion-discard-choice.mt2f' { '--card-recursion-discard-only' }
        default { '--card-recursion-choice-only' }
    }
    foreach ($mode in @($selectionMode, '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Chosen card recursion checks failed ($mode): $fixturePath" }
    }
}

$recursionUpgradeFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -in @('full-battle-card-recursion-upgrade.mt2f', 'full-battle-card-recursion-upgrade-hand.mt2f', 'full-battle-card-recursion-upgrade-choice.mt2f', 'full-battle-card-recursion-upgrade-filter-choice.mt2f', 'full-battle-card-recursion-upgrade-replacement-choice.mt2f')
})
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-choose-discard.mt2f' })) {
    foreach ($mode in @('--choose-discard-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Choose-discard checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-choose-discard-others.mt2f' })) {
    foreach ($mode in @('--choose-discard-others-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Choose-discard others checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-choose-discard-others-policy.mt2f' })) {
    foreach ($mode in @('--choose-discard-others-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Choose-discard others policy checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-choose-discard-refine.mt2f' })) {
    foreach ($mode in @('--choose-discard-others-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Choose-discard Refine checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-choose-discard-persistent.mt2f' })) {
    foreach ($mode in @('--choose-discard-others-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Choose-discard Persistent checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-set-health.mt2f' })) {
    foreach ($mode in @('--native-actions-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "SetHealth checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -in @('full-battle-mix.mt2f', 'full-battle-mix-alternate.mt2f',
        'full-battle-mix-01.mt2f', 'full-battle-mix-10.mt2f', 'full-battle-mix-unstable.mt2f',
        'full-battle-mix-unstable-explosion.mt2f')
})) {
    foreach ($mode in @('--mix-catalog-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Mix checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-mix-unstable-explosion.mt2f' })) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --unstable-explosion-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Unstable explosion witness failed: $fixturePath" }
}
foreach ($fixturePath in @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -in @('full-battle-damage-by-units-killed.mt2f',
        'full-battle-damage-by-units-killed-physical.mt2f')
})) {
    foreach ($mode in @('--damage-by-units-killed-only', '--native-actions-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Damage-by-units-killed checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in $recursionUpgradeFixtures) {
    foreach ($mode in @('--card-recursion-upgrade-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Card recursion upgrade checks failed ($mode): $fixturePath" }
    }
}

$covenantFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-covenant-character-filter.mt2f' })
foreach ($fixturePath in $covenantFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --covenant-character-filter-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Covenant character-filter checks failed: $fixturePath" }
}

$bumpFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -in @('full-battle-bump.mt2f', 'full-battle-companion-bump.mt2f', 'full-battle-outer-boss-bump.mt2f')
})
foreach ($fixturePath in $bumpFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --bump-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Bump scenario checks failed: $fixturePath" }
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --native-actions-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Native action checks failed: $fixturePath" }
}
foreach ($fixturePath in $focusedBumpFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --room-combat-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Room combat checks failed: $fixturePath" }
}
