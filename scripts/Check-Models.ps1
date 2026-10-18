#requires -Version 7.4
param(
    [string[]]$Fixture = @(),
    [ValidateRange(1, 32)][int]$Workers = 1,
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
$workspace = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$fixtureNames = @('full-battle-steward-once.mt2f', 'full-battle-no-cards.mt2f', 'full-battle-units-and-junk.mt2f',
    'full-battle-unplayed-card-pyre-damage.mt2f',
    'full-battle-unplayed-card-effects.mt2f', 'full-battle-train-heal.mt2f', 'full-battle-status-upgrade.mt2f',
    'full-battle-overheal.mt2f', 'full-battle-corruption.mt2f', 'full-battle-corruption-unit-trigger.mt2f',
    'full-battle-corruption-carver-trigger.mt2f', 'full-battle-corruption-keeper-trigger.mt2f',
    'full-battle-corruption-relic-trigger.mt2f', 'full-battle-permanent-corruption.mt2f',
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
    'full-battle-original-fused-pyreshard.mt2f',
    'full-battle-original-part-factory.mt2f',
    'full-battle-original-big-damage-many-morsels.mt2f',
    'full-battle-original-early-discard-energy.mt2f',
    'full-battle-original-cheaper-spell-cost.mt2f',
    'full-battle-original-first-card-cost.mt2f',
    'full-battle-original-rebalance-valor.mt2f',
    'full-battle-original-nefarious-overflow.mt2f', 'full-battle-original-uncontrolled-group.mt2f',
    'full-battle-original-copy-first-spell-played.mt2f',
    'full-battle-original-copy-first-consume-played.mt2f',
    'full-battle-original-copy-consumed-card.mt2f',
    'full-battle-original-monster-spell-targeting.mt2f',
    'full-battle-original-damage-on-floor-transition.mt2f',
    'full-battle-original-pyregel-on-ascend.mt2f',
    'full-battle-original-funguy-deck-scaling.mt2f',
    'full-battle-original-card-draw-on-heal.mt2f',
    'full-battle-original-broken-rail.mt2f',
    'full-battle-original-rage-on-burnout-stack-loss.mt2f',
    'full-battle-original-pyre-revenge.mt2f',
    'full-battle-original-extract-for-ember-and-echos.mt2f',
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
    'full-battle-original-two-charges-on-card-consumed.mt2f',
    'full-battle-original-chance-for-spell-weakness.mt2f',
    'full-battle-original-chance-to-cleanse.mt2f',
    'full-battle-original-extra-feed-trigger.mt2f',
    'full-battle-original-hephs-blueprints.mt2f',
    'full-battle-original-summon-with-avarice.mt2f',
    'full-battle-original-card-draw-on-card-play.mt2f',
    'full-battle-original-carriage-jack.mt2f',
    'full-battle-original-first-room-attachment.mt2f',
    'full-battle-original-start-room-attachment.mt2f', 'full-battle-original-start-room-attachment-floor2.mt2f',
    'full-battle-original-start-room-attachment-floor3.mt2f',
    'full-battle-original-room-entry-status-relics.mt2f',
    'full-battle-original-moon-phase-energy-relic.mt2f',
    'full-battle-original-moon-phase-heal-relic.mt2f',
    'full-battle-moon-heal-on-heal.mt2f',
    'full-battle-original-moon-phase-status-relic.mt2f',
    'full-battle-original-card-attack-relic.mt2f',
    'full-battle-original-upgrade-count-relics.mt2f',
    'full-battle-original-healing-power-relic.mt2f',
    'full-battle-original-start-combat-energy-relic.mt2f',
    'full-battle-original-start-combat-hoard-relic.mt2f',
    'full-battle-original-forge-conservation-relic.mt2f',
    'full-battle-original-opening-corruption-relics.mt2f',
    'full-battle-original-spell-play-threshold-relics.mt2f',
    'full-battle-original-shroom-decay-relic.mt2f',
    'full-battle-original-conduit-relics.mt2f',
    'full-battle-original-equipment-growth-relics.mt2f',
    'full-battle-original-health-choice-relic.mt2f',
    'full-battle-original-after-draw-generation-relics.mt2f',
    'full-battle-original-add-snack-on-kill.mt2f',
    'full-battle-original-final-ending-artifact.mt2f', 'full-battle-original-balatro-event-yorick.mt2f',
    'full-battle-original-balatro-event-cavendish.mt2f',
    'full-battle-original-garfield-box-random-card.mt2f',
    'full-battle-original-unit-resource-relics.mt2f',
    'full-battle-original-opening-draw-relics.mt2f',
    'full-battle-original-turn-draw-relics.mt2f',
    'full-battle-original-relic-count-attack.mt2f', 'full-battle-original-more-spikes.mt2f',
    'full-battle-original-stealth-slay.mt2f',
    'full-battle-original-unit-phase-upgrade-relics.mt2f',
    'full-battle-original-add-stats-to-solo-units.mt2f',
    'full-battle-original-front-room-health-upgrade.mt2f',
    'full-battle-original-champion-start-turn-heal.mt2f',
    'full-battle-original-nims-oath.mt2f',
    'full-battle-original-judges-scales.mt2f',
    'full-battle-original-no-upgrade-cost.mt2f',
    'full-battle-original-first-imp-bonus.mt2f',
    'full-battle-original-moon-phase-rooms-gain-magic-power.mt2f',
    'full-battle-original-mothers-remnant3.mt2f',
    'full-battle-original-balatro-event-triboulet.mt2f',
    'full-battle-original-cooldown-on-damage-dh.mt2f',
    'full-battle-original-deck-turn-draw.mt2f',
    'full-battle-original-card-play-damage.mt2f',
    'full-battle-original-consume-damage.mt2f',
    'full-battle-original-eaten-damage.mt2f',
    'full-battle-original-more-capacity.mt2f',
    'full-battle-original-burnout-death-damage.mt2f',
    'full-battle-original-heal-damage.mt2f',
    'full-battle-original-kill-attack.mt2f',
    'full-battle-original-chance-to-silence.mt2f',
    'full-battle-original-freeze-reduce-cost.mt2f',
    'full-battle-original-random-freeze.mt2f',
    'full-battle-original-random-hand-upgrade.mt2f',
    'full-battle-original-start-turn-propagation.mt2f',
    'full-battle-original-mooncycle-fae.mt2f',
    'full-battle-original-random-draw-cost.mt2f',
    'full-battle-original-suppress-exhaust.mt2f',
    'full-battle-original-expensive-card-upgrade.mt2f',
    'full-battle-original-wolos-bloodletter.mt2f',
    'full-battle-original-card-type-sequence.mt2f',
    'full-battle-original-whelp-cooldown.mt2f',
    'full-battle-original-spawn-retreat.mt2f',
    'full-battle-original-gros-michel.mt2f',
    'full-battle-original-bone-dog.mt2f',
    'full-battle-original-decay-death.mt2f',
    'full-battle-original-opening-morsels.mt2f',
    'full-battle-card-backed-feeding.mt2f',
    'full-battle-forced-feeding.mt2f',
    'full-battle-soldier-of-fortune.mt2f',
    'full-battle-restoration-detonation.mt2f',
    'full-battle-run-card-generation.mt2f',
    'full-battle-armament-drive.mt2f',
    'full-battle-preserve.mt2f',
    'full-battle-mass-production.mt2f',
    'full-battle-stardust-invocation.mt2f',
    'full-battle-adaptive-mutation.mt2f',
    'full-battle-mixalot.mt2f',
    'full-battle-small-box.mt2f',
    'full-battle-magnetic-relay.mt2f',
    'full-battle-transcend-imp.mt2f',
    'full-battle-fused-weaponry.mt2f',
    'full-battle-eclipse-chamber.mt2f',
    'full-battle-original-room-cards.mt2f',
    'full-battle-original-forge-cards.mt2f',
    'full-battle-original-status-cards.mt2f',
    'full-battle-original-deck-spell.mt2f',
    'full-battle-goldmance.mt2f',
    'full-battle-engine-upgrade.mt2f',
    'full-battle-stewards-quarters.mt2f',
    'full-battle-zone-of-silence.mt2f',
    'full-battle-memorial.mt2f',
    'full-battle-planetarium.mt2f',
    'full-battle-grow-room.mt2f',
    'full-battle-mushy-room.mt2f',
    'full-battle-shroud-spike-feeding.mt2f',
    'full-battle-original-opening-deck-spawn.mt2f',
    'full-battle-original-perkeo.mt2f',
    'full-battle-original-corrupted-angels.mt2f',
    'full-battle-corrupted-angels-cost-first.mt2f',
    'full-battle-corrupted-angels-cost-last.mt2f',
    'full-battle-original-propagate-on-status-effect-applied.mt2f',
    'full-battle-original-pyre-health-card-cost.mt2f',
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
    'full-battle-original-tinkermaster-mix.mt2f', 'full-battle-original-progress-pacemaker-mix.mt2f',
    'full-battle-original-card-type-cost-relic.mt2f',
    'full-battle-original-purge-to-exhaust.mt2f',
    'full-battle-original-conserve-ember.mt2f',
    'full-battle-card-type-cost-discount-first.mt2f', 'full-battle-card-type-cost-upgrade-first.mt2f',
    'full-battle-damage-by-units-killed.mt2f',
    'full-battle-damage-by-units-killed-physical.mt2f',
    'full-battle-hand-upgrades.mt2f', 'full-battle-targeted-hand-upgrades.mt2f', 'full-battle-permanent-consume-hand-upgrades.mt2f',
    'full-battle-permanent-consume-over-temporary.mt2f',
    'full-battle-choose-discard-others.mt2f',
    'full-battle-choose-discard-others-policy.mt2f',
    'full-battle-choose-discard-refine.mt2f',
    'full-battle-choose-discard-persistent.mt2f',
    'full-battle-original-smelt-ember.mt2f', 'full-battle-original-smelt-ember-zero.mt2f',
    'full-battle-original-smelt-ember-bulk.mt2f',
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
    'full-battle-tower-corruption-callbacks.mt2f',
    'full-battle-null-unit-ability.mt2f',
    'full-battle-removed-status-upgrade-scaling.mt2f',
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
    'campaign-relic-calibration.mt2f',
    'campaign-draft-calibration.mt2f',
    'campaign-post-battle-calibration.mt2f',
    'campaign-battle-resource-calibration.mt2f',
    'campaign-purge-gold-calibration.mt2f',
    'campaign-deck-copy-calibration.mt2f',
    'campaign-draft-enhancer-count-calibration.mt2f',
    'null-relic-equivalence-calibration.mt2f',
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
if ($Workers -gt 1) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release
        if ($LASTEXITCODE -ne 0) { throw 'Independent model checker build failed.' }
    }
    $binaryRoot = Join-Path $workspace 'src\ModelChecks\bin\Release\net8.0'
    $binaryHashes = [ordered]@{}
    foreach ($binary in @('ModelChecks.dll', 'Model.dll', 'FixtureArchive.dll')) {
        $binaryHashes[$binary] = (Get-FileHash -LiteralPath (Join-Path $binaryRoot $binary) -Algorithm SHA256).Hash
    }
    $runDirectory = Join-Path $workspace ('.probe-runs\model-checks-' +
        (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
    $null = New-Item -ItemType Directory -Path $runDirectory
    $workerCount = [Math]::Min($Workers, $selectedFixtureNames.Count)
    $workerStates = [Collections.Generic.List[object]]::new()
    $startedUtc = [DateTime]::UtcNow.ToString('o')
    $summaryPath = Join-Path $runDirectory 'summary.json'
    $runError = $null
    $cleanupErrors = [Collections.Generic.List[string]]::new()
    function Save-ParallelSummary([string]$Status) {
        $incomplete = @($workerStates | Where-Object Status -ne 'Passed')
        [ordered]@{
            Status = $Status
            StartedUtc = $startedUtc
            UpdatedUtc = [DateTime]::UtcNow.ToString('o')
            RequestedWorkers = $Workers
            FixtureCount = $selectedFixtureNames.Count
            BinaryHashes = $binaryHashes
            Error = $runError
            CleanupErrors = @($cleanupErrors)
            Workers = @($workerStates | Select-Object Number, Status, ProcessId, ExitCode,
                Fixtures, InputPath, OutputPath, ErrorPath, ResultPath)
            # A failed shard may have partial passes. Rerun its complete selection
            # because general checks precede its focused checks.
            RerunFixtures = @($incomplete | ForEach-Object Fixtures)
        } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $summaryPath -Encoding utf8
    }
    function Complete-ParallelWorker($Worker) {
        $Worker.Process.WaitForExit()
        $Worker.ExitCode = $Worker.Process.ExitCode
        $Worker.Status = 'Failed'
        if (Test-Path -LiteralPath $Worker.ResultPath) {
            $result = Get-Content -LiteralPath $Worker.ResultPath -Raw | ConvertFrom-Json
            if ($Worker.ExitCode -eq 0 -and $result.ExitCode -eq 0 -and
                (@($result.Fixtures) -join "`n") -ceq ($Worker.Fixtures -join "`n")) {
                $Worker.Status = 'Passed'
            }
        }
    }
    for ($workerIndex = 0; $workerIndex -lt $workerCount; $workerIndex++) {
        $workerFixtures = @(for ($fixtureIndex = $workerIndex; $fixtureIndex -lt $selectedFixtureNames.Count; $fixtureIndex += $workerCount) {
            $selectedFixtureNames[$fixtureIndex]
        })
        $prefix = Join-Path $runDirectory ('worker-{0:D2}' -f ($workerIndex + 1))
        $worker = [pscustomobject]@{
            Number = $workerIndex + 1; Status = 'Pending'; ProcessId = $null; ExitCode = $null
            Fixtures = $workerFixtures; InputPath = "$prefix-input.json"
            OutputPath = "$prefix.stdout.log"; ErrorPath = "$prefix.stderr.log"
            ResultPath = "$prefix-result.json"; Process = $null
        }
        [ordered]@{ ScriptPath = $PSCommandPath; Fixtures = $workerFixtures; ResultPath = $worker.ResultPath } |
            ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $worker.InputPath -Encoding utf8
        $workerStates.Add($worker)
    }
    Save-ParallelSummary 'Running'
    Write-Host "MODEL-CHECKS-PARALLEL START: $workerCount workers, $($fixtures.Count) fixtures; $runDirectory"
    try {
        foreach ($worker in $workerStates) {
            # EncodedCommand avoids native command-line quoting of fixture arrays
            # and workspace paths. Only this literal JSON path enters the script.
            $inputLiteral = "'" + $worker.InputPath.Replace("'", "''") + "'"
            $workerCommand = @'
$ErrorActionPreference = 'Stop'
$inputData = Get-Content -LiteralPath __INPUT_PATH__ -Raw | ConvertFrom-Json
$started = [DateTime]::UtcNow.ToString('o')
$exitCode = 1
$errorText = $null
try {
    & $inputData.ScriptPath -Fixture ([string[]]$inputData.Fixtures) -SkipBuild
    $exitCode = 0
} catch {
    $errorText = $_.ToString()
    [Console]::Error.WriteLine($_.ToString())
    [Console]::Error.WriteLine($_.ScriptStackTrace)
} finally {
    [ordered]@{
        ExitCode = $exitCode; StartedUtc = $started; CompletedUtc = [DateTime]::UtcNow.ToString('o')
        Fixtures = @($inputData.Fixtures); Error = $errorText
    } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $inputData.ResultPath -Encoding utf8
}
exit $exitCode
'@
            $workerCommand = $workerCommand.Replace('__INPUT_PATH__', $inputLiteral)
            $encodedCommand = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($workerCommand))
            $process = Start-Process -FilePath (Join-Path $PSHOME 'pwsh.exe') -WorkingDirectory $workspace `
                -ArgumentList @('-NoProfile', '-NonInteractive', '-OutputFormat', 'Text', '-EncodedCommand', $encodedCommand) `
                -WindowStyle Hidden -RedirectStandardOutput $worker.OutputPath `
                -RedirectStandardError $worker.ErrorPath -PassThru
            $worker.Process = $process
            $worker.ProcessId = $process.Id
            $worker.Status = 'Running'
            Save-ParallelSummary 'Running'
        }
        $progressTimer = [Diagnostics.Stopwatch]::StartNew()
        while (@($workerStates | Where-Object Status -eq 'Running').Count -gt 0) {
            foreach ($worker in @($workerStates | Where-Object Status -eq 'Running')) {
                if (-not $worker.Process.HasExited) { continue }
                Complete-ParallelWorker $worker
                Write-Host "MODEL-CHECKS-WORKER $($worker.Number): $($worker.Status), exit=$($worker.ExitCode), fixtures=$($worker.Fixtures.Count)"
                Save-ParallelSummary 'Running'
            }
            if ($progressTimer.Elapsed.TotalSeconds -ge 15) {
                $passed = @($workerStates | Where-Object Status -eq 'Passed')
                $passedCount = @($passed | ForEach-Object Fixtures).Count
                Write-Host "MODEL-CHECKS-PARALLEL PROGRESS: $($passed.Count)/$workerCount workers passed, $passedCount/$($fixtures.Count) fixtures complete; $summaryPath"
                $progressTimer.Restart()
            }
            if (@($workerStates | Where-Object Status -eq 'Running').Count -gt 0) { Start-Sleep -Milliseconds 250 }
        }
        try {
            foreach ($binary in $binaryHashes.Keys) {
                if ((Get-FileHash -LiteralPath (Join-Path $binaryRoot $binary) -Algorithm SHA256).Hash -ne $binaryHashes[$binary]) {
                    throw "Checker binary changed during parallel regression: $binary"
                }
            }
        } catch {
            # Missing or unreadable binaries invalidate the run just like a hash
            # mismatch; every shard must then remain in the rerun selection.
            foreach ($worker in $workerStates) { $worker.Status = 'Invalidated' }
            throw
        }
        $failed = @($workerStates | Where-Object Status -ne 'Passed')
        if ($failed.Count -gt 0) {
            throw "Parallel model checks failed in workers $($failed.Number -join ', '); see logs and RerunFixtures in $summaryPath"
        }
    } catch {
        $runError = $_.ToString()
        throw
    } finally {
        # On interruption or launch failure, terminate only children started by
        # this invocation; completed worker exit records remain on disk.
        foreach ($worker in $workerStates) {
            if ($null -ne $worker.Process) {
                try {
                    if (-not $worker.Process.HasExited) {
                        $worker.Process.Kill($true)
                        $worker.Process.WaitForExit()
                        $worker.Status = 'Interrupted'
                        $worker.ExitCode = $worker.Process.ExitCode
                    } elseif ($worker.Status -in @('Pending', 'Running')) {
                        Complete-ParallelWorker $worker
                    }
                } catch {
                    $worker.Status = 'CleanupFailed'
                    $cleanupErrors.Add("Worker $($worker.Number) cleanup: $($_.ToString())")
                } finally {
                    try {
                        $worker.Process.Dispose()
                    } catch {
                        $worker.Status = 'CleanupFailed'
                        $cleanupErrors.Add("Worker $($worker.Number) dispose: $($_.ToString())")
                    }
                }
            }
        }
        if ($cleanupErrors.Count -gt 0 -and $null -eq $runError) {
            $runError = $cleanupErrors -join [Environment]::NewLine
        }
        try {
            if ($null -ne $runError -or @($workerStates | Where-Object Status -ne 'Passed').Count -gt 0) {
                Save-ParallelSummary 'Failed'
            } else {
                Save-ParallelSummary 'Passed'
            }
        } catch {
            # Keep the original failure or interruption and still report a
            # secondary summary-write error without masking it.
            $cleanupErrors.Add("Final summary: $($_.ToString())")
        }
        foreach ($cleanupError in $cleanupErrors) { Write-Warning $cleanupError -WarningAction Continue }
    }
    if ($cleanupErrors.Count -gt 0) { throw ($cleanupErrors -join [Environment]::NewLine) }
    Write-Host "MODEL-CHECKS-PARALLEL PASS: $($fixtures.Count) fixtures, $workerCount workers, all terminal exit codes zero; $summaryPath"
    return
}
$focusedBumpFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -in @('full-battle-companion-bump.mt2f', 'full-battle-outer-boss-bump.mt2f',
        'full-battle-original-damage-on-floor-transition.mt2f', 'full-battle-original-pyregel-on-ascend.mt2f')
})
$focusedTeamSpawnStatusFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-team-spawn-status.mt2f'
})
# These isolated relic fixtures have dedicated native checks; their search checks run below.
$generalFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -notin @('full-battle-companion-bump.mt2f', 'full-battle-outer-boss-bump.mt2f',
        'full-battle-original-damage-on-floor-transition.mt2f', 'full-battle-original-pyregel-on-ascend.mt2f',
        'full-battle-original-team-spawn-status.mt2f', 'full-battle-egg-transformation.mt2f',
        'full-battle-corruption-unit-trigger.mt2f',
        'full-battle-original-kinhost-vessel.mt2f',
        'full-battle-original-kinhost-vessel-delayed.mt2f',
        'full-battle-relic-card-cast-trigger-upgrades.mt2f',
        'full-battle-relic-card-cast-trigger-unit-upgrades.mt2f',
        'full-battle-original-cheaper-spell-cost.mt2f',
        'full-battle-original-first-card-cost.mt2f',
        'full-battle-original-rebalance-valor.mt2f',
        'full-battle-original-first-imp-bonus.mt2f',
        'full-battle-original-moon-phase-rooms-gain-magic-power.mt2f',
        'full-battle-original-uncontrolled-group.mt2f')
})
$buildArguments = @()
if ($SkipBuild) { $buildArguments += '--no-build' }
if ($generalFixtures.Count -gt 0) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release @buildArguments -- @generalFixtures
} else {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release @buildArguments
}
if ($LASTEXITCODE -ne 0) { throw 'Independent model checks failed.' }

dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --relic-first-card-cost-pure
if ($LASTEXITCODE -ne 0) { throw 'First-card cost pure checks failed.' }
dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --original-rebalance-valor-pure
if ($LASTEXITCODE -ne 0) { throw 'RebalanceValor pure checks failed.' }

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

$firstRoomAttachmentFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-first-room-attachment.mt2f'
})
foreach ($fixturePath in $firstRoomAttachmentFixtures) {
    foreach ($mode in @('--original-first-room-attachment-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "First-room attachment relic checks failed ($mode): $fixturePath" }
    }
}

$startRoomAttachmentFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -in @('full-battle-original-start-room-attachment.mt2f', 'full-battle-original-start-room-attachment-floor2.mt2f', 'full-battle-original-start-room-attachment-floor3.mt2f')
})
foreach ($fixturePath in $startRoomAttachmentFixtures) {
    foreach ($mode in @('--original-start-room-attachment-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Start-room attachment relic checks failed ($mode): $fixturePath" }
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

$corruptionKeeperFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-corruption-keeper-trigger.mt2f'
})
foreach ($fixturePath in $corruptionKeeperFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --corruption-keeper-trigger-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Corruption Keeper trigger checks failed: $fixturePath" }
}

$corruptionRelicFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-corruption-relic-trigger.mt2f'
})
foreach ($fixturePath in $corruptionRelicFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --corruption-relic-trigger-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Corruption relic trigger checks failed: $fixturePath" }
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
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -in @(
    'full-battle-original-tinkermaster-mix.mt2f', 'full-battle-original-progress-pacemaker-mix.mt2f') })) {
    foreach ($mode in @('--original-mix-relic-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Mix relic checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-upgrade-count-relics.mt2f' })) {
    foreach ($mode in @('--original-upgrade-count-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original upgrade-count relic checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-forge-conservation-relic.mt2f' })) {
    foreach ($mode in @('--original-forge-conservation-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original forge conservation checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-shroom-decay-relic.mt2f' })) {
    foreach ($mode in @('--original-combat-trigger-relics-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Shroom Decay checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-conduit-relics.mt2f' })) {
    foreach ($mode in @('--original-conduit-relics-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Conduit relic checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-equipment-growth-relics.mt2f' })) {
    foreach ($mode in @('--original-equipment-growth-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original equipment growth relic checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-health-choice-relic.mt2f' })) {
    foreach ($mode in @('--original-health-choice-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original health-choice relic checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-unit-phase-upgrade-relics.mt2f' })) {
    foreach ($mode in @('--original-unit-phase-upgrades-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original unit phase upgrade checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-add-stats-to-solo-units.mt2f' })) {
    foreach ($mode in @('--original-solo-unit-upgrade-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original solo-unit upgrade checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-front-room-health-upgrade.mt2f' })) {
    foreach ($mode in @('--original-front-room-health-upgrade-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original front-room health upgrade checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-champion-start-turn-heal.mt2f' })) {
    foreach ($mode in @('--original-champion-start-turn-heal-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original champion start-turn heal checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-nims-oath.mt2f' })) {
    foreach ($mode in @('--original-nims-oath-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Nims Oath checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-judges-scales.mt2f' })) {
    foreach ($mode in @('--original-judges-scales-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Judges Scales checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-first-imp-bonus.mt2f' })) {
    foreach ($mode in @('--original-first-imp-bonus-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original FirstImpBonus checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-moon-phase-rooms-gain-magic-power.mt2f' })) {
    foreach ($mode in @('--original-moon-phase-hand-upgrade-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original moon-phase hand-upgrade checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-no-upgrade-cost.mt2f' })) {
    foreach ($mode in @('--original-no-upgrade-card-cost-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original no-upgrade card-cost checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-pyre-health-card-cost.mt2f' })) {
    foreach ($mode in @('--original-pyre-health-card-cost-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Pyre health card-cost checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-relic-count-attack.mt2f' })) {
    foreach ($mode in @('--original-relic-count-attack-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original relic-count attack checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-more-spikes.mt2f' })) {
    foreach ($mode in @('--original-more-spikes-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original MoreSpikes checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-stealth-slay.mt2f' })) {
    foreach ($mode in @('--original-stealth-slay-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original stealth-on-slay checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-turn-draw-relics.mt2f' })) {
    foreach ($mode in @('--original-turn-draw-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original turn draw checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-opening-draw-relics.mt2f' })) {
    foreach ($mode in @('--original-opening-draw-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original opening draw checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-unit-resource-relics.mt2f' })) {
    foreach ($mode in @('--original-unit-resources-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original unit resource checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-after-draw-generation-relics.mt2f' })) {
    foreach ($mode in @('--original-after-draw-generation-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original after-draw generation checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-add-snack-on-kill.mt2f' })) {
    foreach ($mode in @('--original-add-snack-on-kill-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original AddSnackOnKill checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-final-ending-artifact.mt2f' })) {
    foreach ($mode in @('--original-final-ending-artifact-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original FinalEndingArtifact checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-balatro-event-yorick.mt2f' })) {
    foreach ($mode in @('--original-balatro-event-yorick-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original BalatroEventYorick checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-balatro-event-cavendish.mt2f' })) {
    foreach ($mode in @('--original-balatro-event-cavendish-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original BalatroEventCavendish checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-two-charges-on-card-consumed.mt2f' })) {
    foreach ($mode in @('--original-two-charges-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original 2ChargesOnCardConsumed checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-chance-for-spell-weakness.mt2f' })) {
    foreach ($mode in @('--original-chance-for-spell-weakness-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original ChanceForSpellWeakness checks failed ($mode): $fixturePath" }
    }
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --original-chance-for-spell-weakness-model-only
    if ($LASTEXITCODE -ne 0) { throw "Original ChanceForSpellWeakness focused model checks failed: $fixturePath" }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-chance-to-cleanse.mt2f' })) {
    foreach ($mode in @('--original-chance-to-cleanse-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original ChanceToCleanse checks failed ($mode): $fixturePath" }
    }
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --original-chance-to-cleanse-model-only
    if ($LASTEXITCODE -ne 0) { throw "Original ChanceToCleanse focused model checks failed: $fixturePath" }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-extra-feed-trigger.mt2f' })) {
    foreach ($mode in @('--original-extra-feed-trigger-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original ExtraFeedTrigger checks failed ($mode): $fixturePath" }
    }
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --original-extra-feed-trigger-model-only
    if ($LASTEXITCODE -ne 0) { throw "Original ExtraFeedTrigger focused model checks failed: $fixturePath" }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-hephs-blueprints.mt2f' })) {
    foreach ($mode in @('--original-hephs-blueprints-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original HephsBlueprints checks failed ($mode): $fixturePath" }
    }
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --original-hephs-blueprints-model-only
    if ($LASTEXITCODE -ne 0) { throw "Original HephsBlueprints focused model checks failed: $fixturePath" }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-summon-with-avarice.mt2f' })) {
    foreach ($mode in @('--original-summon-with-avarice-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original SummonWithAvarice checks failed ($mode): $fixturePath" }
    }
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --original-summon-with-avarice-model-only
    if ($LASTEXITCODE -ne 0) { throw "Original SummonWithAvarice focused model checks failed: $fixturePath" }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-garfield-box-random-card.mt2f' })) {
    foreach ($mode in @('--original-garfield-box-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original GarfieldBox checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-part-factory.mt2f' })) {
    foreach ($mode in @('--original-part-factory-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original PartFactory checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-big-damage-many-morsels.mt2f' })) {
    foreach ($mode in @('--original-big-damage-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "BigDamageFromManyMorsels checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-early-discard-energy.mt2f' })) {
    foreach ($mode in @('--original-early-discard-energy-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "EmberOnEarlyDiscard checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-cheaper-spell-cost.mt2f' })) {
    foreach ($mode in @('--original-cheaper-spell-cost-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "ReduceSpellCostOfCheaper checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-first-card-cost.mt2f' })) {
    foreach ($mode in @('--original-first-card-cost-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "ReduceFirstSpellCostPerDeckSize checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-rebalance-valor.mt2f' })) {
    foreach ($mode in @('--original-rebalance-valor-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "RebalanceValor checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-nefarious-overflow.mt2f' })) {
    foreach ($mode in @('--original-nefarious-overflow-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "NefariousOverflow checks failed ($mode): $fixturePath" }
    }
}
dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --original-uncontrolled-group-pure
if ($LASTEXITCODE -ne 0) { throw 'Original UncontrolledGroup pure checks failed.' }
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-uncontrolled-group.mt2f' })) {
    foreach ($mode in @('--original-uncontrolled-group-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original UncontrolledGroup checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-copy-first-spell-played.mt2f' })) {
    foreach ($mode in @('--original-copy-first-spell-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "CopyFirstSpellPlayed checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-copy-first-consume-played.mt2f' })) {
    foreach ($mode in @('--original-copy-first-consume-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "CopyFirstConsumePlayed checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-copy-consumed-card.mt2f' })) {
    foreach ($mode in @('--original-copy-consumed-card-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "CopyConsumedCard checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-monster-spell-targeting.mt2f' })) {
    foreach ($mode in @('--original-monster-spell-targeting-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "SingleToTeamMonsterSpellTargeting checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-funguy-deck-scaling.mt2f' })) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --original-funguy-deck-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Original Funguy deck-scaling checks failed: $fixturePath" }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-card-draw-on-heal.mt2f' })) {
    foreach ($mode in @('--original-card-draw-on-heal-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original CardDrawOnHeal checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-broken-rail.mt2f' })) {
    foreach ($mode in @('--original-broken-rail-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original BrokenRail checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-rage-on-burnout-stack-loss.mt2f' })) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --original-rage-on-burnout-stack-loss-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Original RageOnBurnoutStackLoss checks failed: $fixturePath" }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-pyre-revenge.mt2f' })) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --original-pyre-revenge-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Original PyreRevengeAddPyregel checks failed: $fixturePath" }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-extract-for-ember-and-echos.mt2f' })) {
    foreach ($mode in @('--original-extract-for-ember-and-echos-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original ExtractForEmberAndEchos checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-spell-play-threshold-relics.mt2f' })) {
    foreach ($mode in @('--original-spell-play-thresholds-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original spell threshold checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-opening-corruption-relics.mt2f' })) {
    foreach ($mode in @('--original-opening-corruption-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original opening corruption checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -in @(
    'full-battle-original-healing-power-relic.mt2f', 'full-battle-original-start-combat-energy-relic.mt2f',
    'full-battle-original-start-combat-hoard-relic.mt2f') })) {
    $focusedMode = if ((Split-Path -Leaf $fixturePath) -eq 'full-battle-original-healing-power-relic.mt2f') {
        '--original-healing-power-only'
    } elseif ((Split-Path -Leaf $fixturePath) -eq 'full-battle-original-start-combat-hoard-relic.mt2f') {
        '--original-start-combat-hoard-only'
    } else { '--original-start-combat-energy-only' }
    foreach ($mode in @($focusedMode, '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original healing/start-combat relic checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-card-type-cost-relic.mt2f' })) {
    foreach ($mode in @('--original-card-type-cost-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original card-type cost relic checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-purge-to-exhaust.mt2f' })) {
    foreach ($mode in @('--original-purge-to-exhaust-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original PurgeToExhaust checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-conserve-ember.mt2f' })) {
    foreach ($mode in @('--original-conserve-ember-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original ConserveEmber checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -in @(
    'full-battle-card-type-cost-discount-first.mt2f', 'full-battle-card-type-cost-upgrade-first.mt2f') })) {
    foreach ($mode in @('--card-type-cost-order-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Card-type cost upgrade-order checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -in @(
    'full-battle-original-smelt-ember.mt2f', 'full-battle-original-smelt-ember-zero.mt2f',
    'full-battle-original-smelt-ember-bulk.mt2f') })) {
    foreach ($mode in @('--original-smelt-ember-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original smelt Ember checks failed ($mode): $fixturePath" }
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
    (Split-Path -Leaf $_) -in @('full-battle-bump.mt2f', 'full-battle-companion-bump.mt2f', 'full-battle-outer-boss-bump.mt2f',
        'full-battle-original-damage-on-floor-transition.mt2f', 'full-battle-original-pyregel-on-ascend.mt2f')
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
$originalFloorTransitionFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-damage-on-floor-transition.mt2f'
})
foreach ($fixturePath in $originalFloorTransitionFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --battle-search-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Original floor-transition damage relic battle search failed: $fixturePath" }
}

$originalPyregelFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-pyregel-on-ascend.mt2f'
})
foreach ($fixturePath in $originalPyregelFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --pyregel-on-ascend-pure
    if ($LASTEXITCODE -ne 0) { throw 'Original PyregelOnAscend_DH pure model checks failed.' }
    foreach ($mode in @('--pyregel-on-ascend-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original PyregelOnAscend_DH checks failed ($mode): $fixturePath" }
    }
}

$originalMoonPhaseHealFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -in @('full-battle-original-moon-phase-heal-relic.mt2f', 'full-battle-moon-heal-on-heal.mt2f')
})
foreach ($fixturePath in $originalMoonPhaseHealFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --battle-search-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Original moon-phase healing relic search checks failed: $fixturePath" }
}
$originalMoonPhaseStatusFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-moon-phase-status-relic.mt2f'
})
foreach ($fixturePath in $originalMoonPhaseStatusFixtures) {
    foreach ($mode in @('--original-moon-phase-status-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original moon-phase status relic checks failed ($mode): $fixturePath" }
    }
}
$originalPropagateStatusFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-propagate-on-status-effect-applied.mt2f'
})
foreach ($fixturePath in $originalPropagateStatusFixtures) {
    foreach ($mode in @('--original-propagate-on-status-effect-applied-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original status-propagation relic checks failed ($mode): $fixturePath" }
    }
}
$originalMothersRemnant3Fixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-mothers-remnant3.mt2f'
})
foreach ($fixturePath in $originalMothersRemnant3Fixtures) {
    foreach ($mode in @('--original-mothers-remnant3-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original MothersRemnant3 checks failed ($mode): $fixturePath" }
    }
}
$originalTribouletFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-balatro-event-triboulet.mt2f'
})
foreach ($fixturePath in $originalTribouletFixtures) {
    foreach ($mode in @('--original-triboulet-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Triboulet checks failed ($mode): $fixturePath" }
    }
}
$originalCooldownOnDamageFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-cooldown-on-damage-dh.mt2f'
})
foreach ($fixturePath in $originalCooldownOnDamageFixtures) {
    foreach ($mode in @('--original-cooldown-on-damage-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original CooldownOnDamage checks failed ($mode): $fixturePath" }
    }
}
$originalDeckTurnDrawFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-deck-turn-draw.mt2f'
})
foreach ($fixturePath in $originalDeckTurnDrawFixtures) {
    foreach ($mode in @('--original-deck-turn-draw-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original DrawCardPerDeckSize checks failed ($mode): $fixturePath" }
    }
}
$originalCardPlayDamageFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-card-play-damage.mt2f'
})
foreach ($fixturePath in $originalCardPlayDamageFixtures) {
    foreach ($mode in @('--original-card-play-damage-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original card-play damage checks failed ($mode): $fixturePath" }
    }
}
$originalConsumeDamageFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-consume-damage.mt2f'
})
foreach ($fixturePath in $originalConsumeDamageFixtures) {
    foreach ($mode in @('--original-consume-damage-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Consume damage checks failed ($mode): $fixturePath" }
    }
}
$originalEatenDamageFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-eaten-damage.mt2f'
})
foreach ($fixturePath in $originalEatenDamageFixtures) {
    foreach ($mode in @('--original-eaten-damage-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original OnEaten damage checks failed ($mode): $fixturePath" }
    }
}
$originalMoreCapacityFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-more-capacity.mt2f'
})
foreach ($fixturePath in $originalMoreCapacityFixtures) {
    foreach ($mode in @('--original-more-capacity-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original MoreCapacity checks failed ($mode): $fixturePath" }
    }
}
$originalBurnoutDamageFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-burnout-death-damage.mt2f'
})
foreach ($fixturePath in $originalBurnoutDamageFixtures) {
    foreach ($mode in @('--original-burnout-damage-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original burnout/death damage checks failed ($mode): $fixturePath" }
    }
}
$originalHealDamageFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-heal-damage.mt2f'
})
foreach ($fixturePath in $originalHealDamageFixtures) {
    foreach ($mode in @('--original-heal-damage-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original heal damage checks failed ($mode): $fixturePath" }
    }
}
$originalKillAttackFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-kill-attack.mt2f'
})
foreach ($fixturePath in $originalKillAttackFixtures) {
    foreach ($mode in @('--original-kill-attack-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original kill-attack checks failed ($mode): $fixturePath" }
    }
}
$originalSpawnSilenceFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-chance-to-silence.mt2f'
})
foreach ($fixturePath in $originalSpawnSilenceFixtures) {
    foreach ($mode in @('--original-spawn-silence-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original ChanceToSilence checks failed ($mode): $fixturePath" }
    }
}
$originalFreezeCostFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-freeze-reduce-cost.mt2f'
})
foreach ($fixturePath in $originalFreezeCostFixtures) {
    foreach ($mode in @('--original-freeze-cost-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original FreezeReduceCost checks failed ($mode): $fixturePath" }
    }
}
$originalRandomFreezeFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-random-freeze.mt2f'
})
foreach ($fixturePath in $originalRandomFreezeFixtures) {
    foreach ($mode in @('--original-random-freeze-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original FreezeRandomEndOfTurn checks failed ($mode): $fixturePath" }
    }
}
$originalRandomHandUpgradeFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-random-hand-upgrade.mt2f'
})
foreach ($fixturePath in $originalRandomHandUpgradeFixtures) {
    foreach ($mode in @('--original-random-hand-upgrade-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original ReduceRandomCardCostOnUnitPlay checks failed ($mode): $fixturePath" }
    }
}
$originalStartTurnPropagationFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-start-turn-propagation.mt2f'
})
foreach ($fixturePath in $originalStartTurnPropagationFixtures) {
    foreach ($mode in @('--original-start-turn-propagation-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original StartOfTurnPropagate checks failed ($mode): $fixturePath" }
    }
}
$originalMooncycleFaeFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-mooncycle-fae.mt2f'
})
foreach ($fixturePath in $originalMooncycleFaeFixtures) {
    foreach ($mode in @('--original-mooncycle-fae-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original MooncycleFaeAddMagicPower checks failed ($mode): $fixturePath" }
    }
}
$originalRandomDrawCostFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-random-draw-cost.mt2f'
})
foreach ($fixturePath in $originalRandomDrawCostFixtures) {
    foreach ($mode in @('--original-random-draw-cost-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original RandomCardCostOnDraw checks failed ($mode): $fixturePath" }
    }
}
$originalSuppressExhaustFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-suppress-exhaust.mt2f'
})
foreach ($fixturePath in $originalSuppressExhaustFixtures) {
    foreach ($mode in @('--original-suppress-exhaust-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original ChanceToNotConsume checks failed ($mode): $fixturePath" }
    }
}
$originalExpensiveCardUpgradeFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-expensive-card-upgrade.mt2f'
})
foreach ($fixturePath in $originalExpensiveCardUpgradeFixtures) {
    foreach ($mode in @('--original-expensive-card-upgrade-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original expensive-card Permafrost checks failed ($mode): $fixturePath" }
    }
}
$originalWolosBloodletterFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-wolos-bloodletter.mt2f'
})
foreach ($fixturePath in $originalWolosBloodletterFixtures) {
    foreach ($mode in @('--original-wolos-bloodletter-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Wolos Bloodletter checks failed ($mode): $fixturePath" }
    }
}
$originalCardTypeSequenceFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-card-type-sequence.mt2f'
})
foreach ($fixturePath in $originalCardTypeSequenceFixtures) {
    foreach ($mode in @('--original-card-type-sequence-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original three-card-type sequence checks failed ($mode): $fixturePath" }
    }
}
$originalWhelpCooldownFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-whelp-cooldown.mt2f'
})
foreach ($fixturePath in $originalWhelpCooldownFixtures) {
    foreach ($mode in @('--original-whelp-cooldown-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Whelp cooldown checks failed ($mode): $fixturePath" }
    }
}
$originalSpawnRetreatFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-spawn-retreat.mt2f'
})
foreach ($fixturePath in $originalSpawnRetreatFixtures) {
    foreach ($mode in @('--original-spawn-retreat-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original spawn retreat checks failed ($mode): $fixturePath" }
    }
}
$originalPerkeoFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-perkeo.mt2f' })
$originalCorruptedAngelsFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -in @('full-battle-original-corrupted-angels.mt2f', 'full-battle-corrupted-angels-cost-first.mt2f', 'full-battle-corrupted-angels-cost-last.mt2f') })
foreach ($fixturePath in $originalCorruptedAngelsFixtures) {
    foreach ($mode in @('--original-corrupted-angels-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Corrupted Angels checks failed ($mode): $fixturePath" }
    }
}
$extendedMechanicFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -in @('full-battle-tower-corruption-callbacks.mt2f', 'full-battle-null-unit-ability.mt2f', 'full-battle-removed-status-upgrade-scaling.mt2f')
})
foreach ($fixturePath in $extendedMechanicFixtures) {
    $mechanicMode = switch (Split-Path -Leaf $fixturePath) {
        'full-battle-tower-corruption-callbacks.mt2f' { '--corruption-keeper-trigger-only' }
        'full-battle-null-unit-ability.mt2f' { '--unit-ability-native-only' }
        'full-battle-removed-status-upgrade-scaling.mt2f' { '--unit-upgrade-scaling-only' }
    }
    foreach ($mode in @($mechanicMode, '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Extended mechanic checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in $originalPerkeoFixtures) {
    foreach ($mode in @('--original-perkeo-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Perkeo checks failed ($mode): $fixturePath" }
    }
}
$originalOpeningDeckSpawnFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-opening-deck-spawn.mt2f'
})
foreach ($fixturePath in $originalOpeningDeckSpawnFixtures) {
    foreach ($mode in @('--original-opening-deck-spawn-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original opening deck-spawn checks failed ($mode): $fixturePath" }
    }
}
$originalOpeningMorselsFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-opening-morsels.mt2f'
})
foreach ($fixturePath in $originalOpeningMorselsFixtures) {
    foreach ($mode in @('--original-opening-morsels-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original opening Morsels checks failed ($mode): $fixturePath" }
    }
}
$feedingFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-card-backed-feeding.mt2f' })
foreach ($fixturePath in $feedingFixtures) {
    foreach ($mode in @('--card-backed-feeding-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Card-backed feeding checks failed ($mode): $fixturePath" }
    }
}
$forcedFeedingFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -in @('full-battle-forced-feeding.mt2f', 'full-battle-shroud-spike-feeding.mt2f') })
foreach ($fixturePath in $forcedFeedingFixtures) {
    foreach ($mode in @('--forced-feeding-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Forced feeding checks failed ($mode): $fixturePath" }
    }
}
$soldierFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-soldier-of-fortune.mt2f' })
foreach ($fixturePath in $soldierFixtures) {
    foreach ($mode in @('--soldier-of-fortune-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Soldier of Fortune checks failed ($mode): $fixturePath" }
    }
}
$mushyRoomFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-mushy-room.mt2f' })
foreach ($fixturePath in $mushyRoomFixtures) {
    foreach ($mode in @('--mushy-room-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Mushy Room checks failed ($mode): $fixturePath" }
    }
}
$growRoomFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-grow-room.mt2f' })
foreach ($fixturePath in $growRoomFixtures) {
    foreach ($mode in @('--grow-room-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Grow Room checks failed ($mode): $fixturePath" }
    }
}
$planetariumFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-planetarium.mt2f' })
foreach ($fixturePath in $planetariumFixtures) {
    foreach ($mode in @('--planetarium-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Planetarium checks failed ($mode): $fixturePath" }
    }
}
$memorialFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-memorial.mt2f' })
foreach ($fixturePath in $memorialFixtures) {
    foreach ($mode in @('--memorial-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Memorial checks failed ($mode): $fixturePath" }
    }
}
$silenceRoomFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-zone-of-silence.mt2f' })
foreach ($fixturePath in $silenceRoomFixtures) {
    foreach ($mode in @('--zone-of-silence-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Zone of Silence checks failed ($mode): $fixturePath" }
    }
}
$quartersFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-stewards-quarters.mt2f' })
foreach ($fixturePath in $quartersFixtures) {
    foreach ($mode in @('--stewards-quarters-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Stewards Quarters checks failed ($mode): $fixturePath" }
    }
}
$engineUpgradeFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-engine-upgrade.mt2f' })
foreach ($fixturePath in $engineUpgradeFixtures) {
    foreach ($mode in @('--engine-upgrade-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Engine Upgrade checks failed ($mode): $fixturePath" }
    }
}
$goldmanceFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-goldmance.mt2f' })
foreach ($fixturePath in $goldmanceFixtures) {
    foreach ($mode in @('--goldmance-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Goldmance checks failed ($mode): $fixturePath" }
    }
}
$originalDeckSpellFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-deck-spell.mt2f' })
foreach ($fixturePath in $originalDeckSpellFixtures) {
    foreach ($mode in @('--original-deck-spell-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original deck-spell checks failed ($mode): $fixturePath" }
    }
}
$originalStatusCardsFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-status-cards.mt2f' })
foreach ($fixturePath in $originalStatusCardsFixtures) {
    foreach ($mode in @('--original-status-cards-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original status-card checks failed ($mode): $fixturePath" }
    }
}
$originalForgeCardsFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-forge-cards.mt2f' })
foreach ($fixturePath in $originalForgeCardsFixtures) {
    foreach ($mode in @('--original-forge-cards-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original forge-card checks failed ($mode): $fixturePath" }
    }
}
$originalRoomCardsFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-room-cards.mt2f' })
foreach ($fixturePath in $originalRoomCardsFixtures) {
    foreach ($mode in @('--original-room-cards-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original room-card checks failed ($mode): $fixturePath" }
    }
}
$eclipseFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-eclipse-chamber.mt2f' })
foreach ($fixturePath in $eclipseFixtures) {
    foreach ($mode in @('--eclipse-chamber-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Eclipse Chamber checks failed ($mode): $fixturePath" }
    }
}
$transcendImpFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-transcend-imp.mt2f' })
foreach ($fixturePath in $transcendImpFixtures) {
    foreach ($mode in @('--transcend-imp-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Transcend Imp checks failed ($mode): $fixturePath" }
    }
}
$magneticRelayFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-magnetic-relay.mt2f' })
foreach ($fixturePath in $magneticRelayFixtures) {
    foreach ($mode in @('--magnetic-relay-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Magnetic Relay checks failed ($mode): $fixturePath" }
    }
}
$equipmentMergeFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-fused-weaponry.mt2f' })
foreach ($fixturePath in $equipmentMergeFixtures) {
    foreach ($mode in @('--fused-weaponry-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Fused Weaponry checks failed ($mode): $fixturePath" }
    }
}
$smallBoxFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-small-box.mt2f' })
foreach ($fixturePath in $smallBoxFixtures) {
    foreach ($mode in @('--small-box-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Small Box checks failed ($mode): $fixturePath" }
    }
}
$mixalotFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-mixalot.mt2f' })
foreach ($fixturePath in $mixalotFixtures) {
    foreach ($mode in @('--mixalot-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Mixalot checks failed ($mode): $fixturePath" }
    }
}
$adaptiveFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-adaptive-mutation.mt2f' })
foreach ($fixturePath in $adaptiveFixtures) {
    foreach ($mode in @('--adaptive-mutation-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Adaptive Mutation checks failed ($mode): $fixturePath" }
    }
}
$duplicateFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -in @('full-battle-mass-production.mt2f', 'full-battle-stardust-invocation.mt2f') })
foreach ($fixturePath in $duplicateFixtures) {
    foreach ($mode in @('--card-duplicate-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Hand duplication checks failed ($mode): $fixturePath" }
    }
}
$preserveFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-preserve.mt2f' })
foreach ($fixturePath in $preserveFixtures) {
    foreach ($mode in @('--preserve-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Preserve checks failed ($mode): $fixturePath" }
    }
}
$armamentFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-armament-drive.mt2f' })
foreach ($fixturePath in $armamentFixtures) {
    foreach ($mode in @('--armament-drive-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Armament Drive checks failed ($mode): $fixturePath" }
    }
}
$runGenerationFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-run-card-generation.mt2f' })
foreach ($fixturePath in $runGenerationFixtures) {
    foreach ($mode in @('--run-card-generation-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Run-card generation checks failed ($mode): $fixturePath" }
    }
}
$restorationFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-restoration-detonation.mt2f' })
foreach ($fixturePath in $restorationFixtures) {
    foreach ($mode in @('--restoration-detonation-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Restoration Detonation checks failed ($mode): $fixturePath" }
    }
}
$originalDecayDeathFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-decay-death.mt2f'
})
foreach ($fixturePath in $originalDecayDeathFixtures) {
    foreach ($mode in @('--original-decay-death-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Decay-death checks failed ($mode): $fixturePath" }
    }
}
$originalBoneDogFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-bone-dog.mt2f'
})
foreach ($fixturePath in $originalBoneDogFixtures) {
    foreach ($mode in @('--original-bone-dog-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Bone Dog checks failed ($mode): $fixturePath" }
    }
}
$originalGrosMichelFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-gros-michel.mt2f'
})
foreach ($fixturePath in $originalGrosMichelFixtures) {
    foreach ($mode in @('--original-gros-michel-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Gros Michel checks failed ($mode): $fixturePath" }
    }
}
