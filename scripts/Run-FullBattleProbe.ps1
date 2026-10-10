#requires -Version 7.4
[CmdletBinding()]
param(
    [ValidateSet('no-cards', 'steward-once', 'units-and-junk', 'units-spells-and-junk')]
    [string] $Policy = 'steward-once',
    [ValidateSet('Normal', 'Fast', 'Ultra', 'SuperUltra', 'Instant')]
    [string] $GameSpeed = 'Instant',
    [switch] $SpawnPoints,
    [switch] $PhysicalSpawnPoints,
    [switch] $Revival,
    [switch] $TriggeredSummons,
    [switch] $TriggeredSummonsFresh,
    [switch] $TriggeredSummonsDeath,
    [switch] $TriggeredSummonsEquipment,
    [switch] $TriggeredSummonsEquipmentOwned,
    [switch] $TriggeredSummonsRevival,
    [switch] $NumericUpgrades,
    [switch] $DynamicUpgrades,
    [switch] $SacrificeUpgrades,
    [switch] $HandUpgrades,
    [switch] $TargetedHandUpgrades,
    [switch] $Healing,
    [switch] $HealingTriggers,
    [switch] $RoomSpells,
    [switch] $TerminalSpells,
    [switch] $PostKillSpells,
    [switch] $RandomSpells,
    [switch] $RandomStatus,
    [switch] $CrossRoomSpells,
    [switch] $CrossRoomTargets,
    [switch] $AttackBuffs,
    [switch] $MaxHealthSpells,
    [switch] $MaxHealthLethal,
    [switch] $NumericRanges,
    [switch] $NumericRangesLethal,
    [switch] $TargetFilters,
    [switch] $Drawing,
    [switch] $HandRemoval,
    [switch] $HandRemovalLethal,
    [switch] $Generation,
    [switch] $GenerationLethal,
    [switch] $StatisticQueries,
    [switch] $StatisticOverflow,
    [switch] $DamageScaling,
    [switch] $DynamicStatistics,
    [switch] $StatusScaling,
    [switch] $UnitUpgradeScaling,
    [switch] $UnitTriggerUpgrades,
    [switch] $SpawnTriggers,
    [switch] $SpawnTriggersLethal,
    [switch] $UnitTurnBegin,
    [switch] $TeamTurnBegin,
    [switch] $PreHandDiscard,
    [switch] $PreHandDiscardLethal,
    [switch] $CloneUpgradeRefresh,
    [switch] $PreCombat,
    [switch] $PreCombatLethal,
    [switch] $TriggeredHealing,
    [switch] $PostCombatHealing,
    [switch] $TriggeredDamage,
    [switch] $DamageDeathQueue,
    [switch] $TerminalDeathDamage,
    [switch] $HitKill,
    [switch] $DyingUpgrades,
    [switch] $AttackTriggers,
    [switch] $TriggeredStatus,
    [switch] $StatusCallbacks,
    [switch] $StatusCallbackActions,
    [switch] $EnergyEffects,
    [switch] $EnergyEffectsLethal,
    [switch] $XCost,
    [switch] $XCostLethal,
    [switch] $RoomCapacity,
    [switch] $RoomCapacityLethal,
    [switch] $DirectUnitUpgrades,
    [switch] $Equipment,
    [switch] $EquipmentExhausted,
    [switch] $EquipmentOverflow,
    [switch] $EquipmentTriggers,
    [switch] $TriggerMutation,
    [switch] $DetachedBonusDraw,
    [switch] $ConditionalTriggers,
    [switch] $TriggerRepeats,
    [switch] $CompanionBoss,
    [switch] $AbilityEffects,
    [switch] $EquipmentAbilities,
    [switch] $HordeStats,
    [switch] $EnchantmentLifecycle,
    [switch] $EnchantmentCombat,
    [switch] $EnchantmentWorld,
    [switch] $HordeStatuses,
    [switch] $HordeMerge,
    [switch] $Bump,
    [switch] $UnitClone,
    [switch] $UnitCopy,
    [switch] $HeroCopy,
    [switch] $SpawnEnchant,
    [switch] $Purify,
    [switch] $PurifyQueues,
    [switch] $Incant,
    [switch] $IncantThresholds,
    [switch] $AbilityIncant,
    [switch] $PersistentEnchantments,
    [switch] $PersistentEnchantmentDeaths,
    [switch] $PersistentEnchantmentRevivals,
    [switch] $PersistentEnchantmentRandomPools,
    [switch] $PersistentEnchantmentUpgrades,
    [switch] $PersistentEnchantmentSummons,
    [switch] $PersistentEnchantmentSummonsFresh,
    [switch] $CharacterRemoval,
    [switch] $PreviewReferences,
    [switch] $RelicCatalog,
    [switch] $UpgradeMasks,
    [switch] $ModifierOverflow,
    [switch] $TraitComposition,
    [switch] $CardStatusComposition,
    [switch] $OwnedCardMasks,
    [switch] $CardUpgradeLifecycle,
    [switch] $BranchCardMasks,
    [switch] $RelicCardUpgrades,
    [switch] $BattleRelicUpgrades,
    [switch] $ConditionalBattleRelicUpgrades,
    [switch] $RelicCardStatusUpgrades,
    [switch] $RelicCardPiercingUpgrades,
    [switch] $RelicCardSelfPurgeUpgrades,
    [switch] $RelicCardAbilityUpgrades,
    [switch] $IncantRelic,
    [switch] $IncantRelics,
    [switch] $SpawnStatusRelics,
    [switch] $SpawnStatusRelicsClones,
    [switch] $SettleDeathDissolves,
    [switch] $SettleCardAnimations,
    [switch] $HarvestTriggers,
    [switch] $HordeRemoval,
    [switch] $HordeDeath,
    [switch] $HordeUpgrades,
    [switch] $DyingHordeUpgrades,
    [switch] $RallyTriggers,
    [switch] $LethalRally,
    [switch] $MultiSummon,
    [switch] $MultiSummonZero,
    [switch] $MultiSummonUpgrade,
    [switch] $MultiSummonUpgradeUnique,
    [switch] $MultiSummonUpgradeRestricted,
    [switch] $MultiSummonFresh,
    [switch] $MultiSummonFreshDeaths,
    [switch] $MultiSummonAdditional,
    [switch] $MultiSummonAdditionalFresh,
    [switch] $MultiSummonPool,
    [switch] $MultiSummonPoolAdditional,
    [switch] $MultiSummonPoolAdditionalFresh,
    [switch] $MultiSummonPoolSingletonAdditional,
    [switch] $MultiSummonPoolNoPrimary,
    [switch] $MultiSummonPoolMissingFresh,
    [switch] $MultiSummonPoolAdditionalMissingFresh,
    [switch] $MultiSummonPoolNoPrimaryMissingFresh,
    [switch] $MultiSummonPoolNoPrimaryMissingFreshDeaths,
    [switch] $AbilityLifecycle,
    [switch] $AbilityCooldown,
    [switch] $AbilityCache,
    [switch] $AbilityActivation,
    [switch] $AbilityActivationX,
    [switch] $AbilityActivationLethal,
    [switch] $Sentry,
    [switch] $SentryLethal,
    [switch] $BonusDraw,
    [switch] $BonusDrawLethal,
    [switch] $BinaryCapture = $true,
    [switch] $CaptureJson,
    [switch] $SkipBuild
)

$ErrorActionPreference = 'Stop'
if ($ConditionalBattleRelicUpgrades) { $BattleRelicUpgrades = $true }
if ($RelicCardStatusUpgrades) { $BattleRelicUpgrades = $true }
if ($RelicCardPiercingUpgrades) { $BattleRelicUpgrades = $true }
if ($RelicCardSelfPurgeUpgrades) { $BattleRelicUpgrades = $true }
if ($RelicCardAbilityUpgrades) { $BattleRelicUpgrades = $true }
if ($BattleRelicUpgrades -or $SpawnStatusRelicsClones) { $BranchCardMasks = $true }
if ($SpawnStatusRelicsClones) { $SpawnStatusRelics = $true; $UnitClone = $true }
if ($EnchantmentWorld) { $EnchantmentCombat = $true }
if ($PersistentEnchantmentRevivals) { $PersistentEnchantmentDeaths = $true }
if ($PersistentEnchantmentDeaths) { $PersistentEnchantments = $true }
if ($PersistentEnchantmentRandomPools) { $PersistentEnchantments = $true }
if ($PersistentEnchantmentSummonsFresh) { $PersistentEnchantmentSummons = $true }
if ($PersistentEnchantmentSummons) {
    if ($PersistentEnchantmentDeaths -or $PersistentEnchantmentUpgrades -or $TriggeredSummons -or $TriggeredSummonsFresh -or $TriggeredSummonsDeath -or $TriggeredSummonsEquipment -or $TriggeredSummonsEquipmentOwned -or $TriggeredSummonsRevival) {
        throw 'Choose aura child summons or source death/upgrades/standalone summons.'
    }
    $PersistentEnchantments = $true
}
if ($PersistentEnchantmentUpgrades) {
    if ($PersistentEnchantmentDeaths) { throw 'Choose aura child upgrades or source death/revival.' }
    $PersistentEnchantments = $true
}
if ($PersistentEnchantments) { $PhysicalSpawnPoints = $true }
if ($Revival -and $TriggeredSummonsRevival) { throw 'Choose standalone revival or triggered summon revival.' }
if ($TriggeredSummonsRevival) { $TriggeredSummonsEquipmentOwned = $true; $TriggeredSummonsDeath = $true }
if ($TriggerRepeats) { $ConditionalTriggers = $true }
if ($TriggeredSummonsEquipmentOwned) { $TriggeredSummonsEquipment = $true }
if ($TriggeredSummonsFresh -or $TriggeredSummonsDeath -or $TriggeredSummonsEquipment) { $TriggeredSummons = $true }
if ($TriggeredSummons -or $Revival -or $HordeMerge -or $Bump -or $UnitClone -or $UnitCopy -or $HeroCopy -or $SpawnEnchant) { $PhysicalSpawnPoints = $true }
if ($MultiSummonUpgradeUnique -or $MultiSummonUpgradeRestricted) { $MultiSummonUpgrade = $true }
if ($MultiSummonAdditionalFresh) { $MultiSummonAdditional = $true; $MultiSummonFresh = $true }
$missingFresh = $MultiSummonPoolMissingFresh -or $MultiSummonPoolAdditionalMissingFresh -or $MultiSummonPoolNoPrimaryMissingFresh -or $MultiSummonPoolNoPrimaryMissingFreshDeaths
if ($missingFresh) { $MultiSummonPool = $true; $MultiSummonFresh = $true }
if ($MultiSummonPoolNoPrimaryMissingFreshDeaths) { $MultiSummonFreshDeaths = $true }
if ($MultiSummonPoolAdditional -or $MultiSummonPoolAdditionalFresh -or $MultiSummonPoolSingletonAdditional -or $MultiSummonPoolNoPrimary) { $MultiSummonPool = $true }
if ($MultiSummonPoolAdditionalFresh) { $MultiSummonFresh = $true }
if ($MultiSummonFreshDeaths) { $MultiSummonFresh = $true }
if ($MultiSummonZero -or $MultiSummonUpgrade -or $MultiSummonFresh -or $MultiSummonAdditional -or $MultiSummonPool) { $MultiSummon = $true }
if ($EquipmentExhausted -or $EquipmentOverflow -or $EquipmentTriggers) { $Equipment = $true }
if ($StatusCallbackActions) { $StatusCallbacks = $true }
if ($StatusCallbacks) { $TriggeredStatus = $true }
if ($PurifyQueues) { $Purify = $true }
if ($IncantThresholds) { $Incant = $true }
if ($CaptureJson) { $BinaryCapture = $true }
$workspace = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$gameRoot = Join-Path $workspace '.sandbox-game'
$runId = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
$profile = Join-Path $workspace ('.probe-runs\full-battle-' + $Policy + '-' + $runId)
$originalLogRoot = Join-Path ([Environment]::GetFolderPath('UserProfile')) 'AppData\LocalLow\Shiny Shoe\MonsterTrain2'

function Get-OriginalSignature {
    $parts = Get-ChildItem -LiteralPath $originalLogRoot -File -Force -Recurse |
        Sort-Object FullName | ForEach-Object {
            $_.FullName + ':' + $_.LastWriteTimeUtc.Ticks + ':' +
                (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
        }
    return [string]::Join('|', [string[]] @($parts))
}

if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src\Probe\Probe.csproj') -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Probe build failed.' }
    if ($BinaryCapture) {
        dotnet build (Join-Path $workspace 'src\FixtureArchive\FixtureArchive.csproj') -c Release -f net8.0
        if ($LASTEXITCODE -ne 0) { throw 'Fixture inspection build failed.' }
    }
}
$pluginDir = Join-Path $gameRoot 'BepInEx\plugins'
Copy-Item -LiteralPath (Join-Path $workspace 'src\Probe\bin\Release\netstandard2.1\Probe.dll') -Destination $pluginDir -Force
Copy-Item -LiteralPath (Join-Path $workspace 'src\Model\bin\Release\netstandard2.1\Model.dll') -Destination $pluginDir -Force
Copy-Item -LiteralPath (Join-Path $workspace 'src\FixtureArchive\bin\Release\netstandard2.1\FixtureArchive.dll') -Destination $pluginDir -Force
Copy-Item -LiteralPath (Join-Path $workspace '.probe-baseline') -Destination $profile -Recurse
$environment = @{
    MT2_PROBE_DATA_DIR = $profile
    MT2_PROBE_SCENARIO = 'native-replay'
    MT2_PROBE_FULL_BATTLE = '1'
    MT2_PROBE_FULL_BATTLE_POLICY = $Policy
    MT2_PROBE_MODIFIERS = $(if ($RelicCardStatusUpgrades) { 'relic-card-status-upgrades' } elseif ($ConditionalBattleRelicUpgrades) { 'conditional-relic-card-upgrades' } elseif ($Revival) { 'revival' } elseif ($TriggeredSummonsEquipment) { 'triggered-summon-equipment' + $(if ($TriggeredSummonsEquipmentOwned) { '-owned' } else { '' }) + $(if ($TriggeredSummonsDeath) { '-death' } else { '' }) + $(if ($TriggeredSummonsFresh) { '-fresh' } else { '' }) } elseif ($TriggeredSummonsDeath -and $TriggeredSummonsFresh) { 'triggered-summon-death-fresh' } elseif ($TriggeredSummonsDeath) { 'triggered-summon-death' } elseif ($TriggeredSummonsFresh) { 'triggered-summon-fresh' } elseif ($TriggeredSummons) { 'triggered-summon' } elseif ($SpawnPoints) { 'spawn-points' } elseif ($MultiSummonPoolNoPrimaryMissingFreshDeaths) { 'multi-summon-pool-no-primary-missing-fresh-deaths' } elseif ($MultiSummonPoolNoPrimaryMissingFresh) { 'multi-summon-pool-no-primary-missing-fresh' } elseif ($MultiSummonPoolAdditionalMissingFresh) { 'multi-summon-pool-additional-missing-fresh' } elseif ($MultiSummonPoolMissingFresh) { 'multi-summon-pool-missing-fresh' } elseif ($MultiSummonPoolAdditionalFresh) { 'multi-summon-pool-additional-fresh' } elseif ($MultiSummonPoolSingletonAdditional) { 'multi-summon-pool-singleton-additional' } elseif ($MultiSummonPoolNoPrimary) { 'multi-summon-pool-no-primary' } elseif ($MultiSummonPoolAdditional) { 'multi-summon-pool-additional' } elseif ($MultiSummonPool) { 'multi-summon-pool' } elseif ($MultiSummonAdditionalFresh) { 'multi-summon-additional-fresh' } elseif ($MultiSummonAdditional) { 'multi-summon-additional' } elseif ($MultiSummonFreshDeaths) { 'multi-summon-fresh-deaths' } elseif ($MultiSummonFresh) { 'multi-summon-fresh' } elseif ($MultiSummonUpgradeRestricted) { 'multi-summon-upgrade-restricted' } elseif ($MultiSummonUpgradeUnique) { 'multi-summon-upgrade-unique' } elseif ($MultiSummonUpgrade) { 'multi-summon-upgrade' } elseif ($MultiSummonZero) { 'multi-summon-zero' } elseif ($MultiSummon) { 'multi-summon' } elseif ($LethalRally) { 'rally-lethal' } elseif ($RallyTriggers) { 'rally-triggers' } elseif ($DyingHordeUpgrades) { 'dying-horde-upgrades' } elseif ($HordeUpgrades) { 'horde-upgrades' } elseif ($HordeDeath) { 'horde-death' } elseif ($HordeRemoval) { 'horde-removal' } elseif ($HarvestTriggers) { 'harvest-triggers' } elseif ($HordeStatuses) { 'horde-statuses' } elseif ($EquipmentAbilities) { 'equipment-abilities' } elseif ($AbilityEffects) { 'ability-effects' } elseif ($AbilityLifecycle) { 'ability-lifecycle' } elseif ($AbilityActivationLethal) { 'ability-activation-lethal' } elseif ($AbilityActivationX) { 'ability-activation-x' } elseif ($AbilityActivation) { 'ability-activation' } elseif ($AbilityCache) { 'ability-cache' } elseif ($AbilityCooldown) { 'ability-cooldown' } elseif ($CompanionBoss) { 'companion-boss' } elseif ($ConditionalTriggers) { 'conditional-triggers' } elseif ($DetachedBonusDraw) { 'detached-bonus-draw' } elseif ($TriggerMutation) { 'trigger-mutation' } elseif ($EquipmentTriggers) { 'equipment-triggers' } elseif ($EquipmentOverflow) { 'equipment-overflow' } elseif ($EquipmentExhausted) { 'equipment-exhausted' } elseif ($Equipment) { 'equipment' } elseif ($DirectUnitUpgrades) { 'direct-unit-upgrades' } elseif ($RoomCapacityLethal) { 'room-capacity-lethal' } elseif ($RoomCapacity) { 'room-capacity' } elseif ($BonusDrawLethal) { 'bonus-draw-lethal' } elseif ($BonusDraw) { 'bonus-draw' } elseif ($XCostLethal) { 'x-cost-lethal' } elseif ($XCost) { 'x-cost' } elseif ($EnergyEffectsLethal) { 'energy-effects-lethal' } elseif ($EnergyEffects) { 'energy-effects' } elseif ($TriggeredStatus) { 'triggered-status' } elseif ($AttackTriggers) { 'attack-triggers' } elseif ($DyingUpgrades) { 'dying-upgrades' } elseif ($HitKill) { 'hit-kill' } elseif ($TerminalDeathDamage) { 'terminal-death-damage' } elseif ($DamageDeathQueue) { 'damage-death-queue' } elseif ($TriggeredDamage) { 'triggered-damage' } elseif ($PostCombatHealing) { 'post-combat-healing' } elseif ($TriggeredHealing) { 'triggered-healing' } elseif ($PreCombatLethal) { 'pre-combat-lethal' } elseif ($PreCombat) { 'pre-combat' } elseif ($CloneUpgradeRefresh) { 'clone-upgrade-refresh' } elseif ($PreHandDiscardLethal) { 'pre-hand-discard-lethal' } elseif ($PreHandDiscard) { 'pre-hand-discard' } elseif ($TeamTurnBegin) { 'team-turn-begin' } elseif ($UnitTurnBegin) { 'unit-turn-begin' } elseif ($SpawnTriggersLethal) { 'spawn-triggers-lethal' } elseif ($SpawnTriggers) { 'spawn-triggers' } elseif ($UnitTriggerUpgrades) { 'unit-trigger-upgrades' } elseif ($UnitUpgradeScaling) { 'unit-upgrade-scaling' } elseif ($StatusScaling) { 'status-scaling' } elseif ($DynamicStatistics) { 'dynamic-statistics' } elseif ($DamageScaling) { 'damage-scaling' } elseif ($GenerationLethal) { 'generation-lethal' } elseif ($Generation) { 'generation' } elseif ($HandRemovalLethal) { 'hand-removal-lethal' } elseif ($HandRemoval) { 'hand-removal' } elseif ($Drawing) { 'drawing' } elseif ($TargetFilters) { 'target-filters' } elseif ($NumericRangesLethal) { 'numeric-ranges-lethal' } elseif ($NumericRanges) { 'numeric-ranges' } elseif ($MaxHealthLethal) { 'max-health-lethal' } elseif ($MaxHealthSpells) { 'max-health-spells' } elseif ($AttackBuffs) { 'attack-buffs' } elseif ($CrossRoomTargets) { 'cross-room-targets' } elseif ($CrossRoomSpells) { 'cross-room-spells' } elseif ($RandomStatus) { 'random-status' } elseif ($RandomSpells) { 'random-spells' } elseif ($PostKillSpells) { 'post-kill-spells' } elseif ($TerminalSpells) { 'terminal-spells' } elseif ($RoomSpells) { 'room-spells' } elseif ($HealingTriggers) { 'healing-triggers' } elseif ($Healing) { 'healing' } elseif ($TargetedHandUpgrades) { 'targeted-hand-upgrades' } elseif ($HandUpgrades) { 'hand-upgrades' } elseif ($SacrificeUpgrades) { 'sacrifice-upgrades' } elseif ($DynamicUpgrades) { 'dynamic-upgrades' } elseif ($NumericUpgrades) { 'numeric-upgrades' } else { '' })
    MT2_PROBE_DIRECT_BRANCH = '1'
    MT2_PROBE_DEPTH = '100'
    MT2_PROBE_TARGET_TURN = '0'
    MT2_PROBE_SOURCE_PLAY_TURNS = ''
    MT2_PROBE_BRANCH_ANY_UNIT = '0'
    MT2_PROBE_FAST_REPLAY = '0'
    MT2_PROBE_GAME_SPEED = $GameSpeed
    MT2_PROBE_CHARACTER_REMOVAL = $(if ($CharacterRemoval) { '1' } else { '0' })
    MT2_PROBE_PREVIEW_REFERENCES = $(if ($PreviewReferences) { '1' } else { '0' })
    MT2_PROBE_RELIC_CATALOG = $(if ($RelicCatalog) { '1' } else { '0' })
    MT2_PROBE_UPGRADE_MASKS = $(if ($UpgradeMasks) { '1' } else { '0' })
    MT2_PROBE_MODIFIER_OVERFLOW = $(if ($ModifierOverflow) { '1' } else { '0' })
    MT2_PROBE_TRAIT_COMPOSITION = $(if ($TraitComposition) { '1' } else { '0' })
    MT2_PROBE_CARD_STATUS_COMPOSITION = $(if ($CardStatusComposition) { '1' } else { '0' })
    MT2_PROBE_OWNED_CARD_MASKS = $(if ($OwnedCardMasks) { '1' } else { '0' })
    MT2_PROBE_CARD_UPGRADE_LIFECYCLE = $(if ($CardUpgradeLifecycle) { '1' } else { '0' })
    MT2_PROBE_BRANCH_CARD_MASKS = $(if ($BranchCardMasks) { '1' } else { '0' })
    MT2_PROBE_RELIC_CARD_UPGRADES = $(if ($RelicCardUpgrades) { '1' } else { '0' })
    MT2_PROBE_BATTLE_RELIC_UPGRADES = $(if ($BattleRelicUpgrades) { '1' } else { '0' })
    MT2_PROBE_CONDITIONAL_BATTLE_RELIC_UPGRADES = $(if ($ConditionalBattleRelicUpgrades) { '1' } else { '0' })
    MT2_PROBE_RELIC_CARD_STATUS_UPGRADE = $(if ($RelicCardStatusUpgrades) { '1' } else { '0' })
    MT2_PROBE_RELIC_CARD_PIERCING_UPGRADE = $(if ($RelicCardPiercingUpgrades) { '1' } else { '0' })
    MT2_PROBE_RELIC_CARD_SELF_PURGE_UPGRADE = $(if ($RelicCardSelfPurgeUpgrades) { '1' } else { '0' })
    MT2_PROBE_RELIC_CARD_ABILITY_UPGRADE = $(if ($RelicCardAbilityUpgrades) { '1' } else { '0' })
    MT2_PROBE_GENERATION = $(if ($Generation) { '1' } else { '0' })
    MT2_PROBE_CARDLESS_RELIC_UPGRADES = $(if ($SpawnStatusRelicsClones) { '1' } else { '0' })
    MT2_PROBE_SETTLE_DEATH_DISSOLVES = $(if ($SettleDeathDissolves) { '1' } else { '0' })
    MT2_PROBE_SETTLE_CARD_ANIMATIONS = $(if ($SettleCardAnimations) { '1' } else { '0' })
    MT2_PROBE_STATUS_CALLBACKS = $(if ($HordeStatuses -or $StatusCallbacks) { '1' } else { '0' })
    MT2_PROBE_STATUS_CALLBACK_ACTIONS = $(if ($StatusCallbackActions) { '1' } else { '0' })
    MT2_PROBE_PHYSICAL_SPAWNPOINTS = $(if ($PhysicalSpawnPoints) { '1' } else { '0' })
    MT2_PROBE_BINARY_CAPTURE = $(if ($BinaryCapture) { '1' } else { '0' })
    MT2_PROBE_CAPTURE_JSON = $(if ($CaptureJson) { '1' } else { '0' })
    MT2_PROBE_NO_TIMEOUT = '1'
    MT2_PROBE_ISOLATE_PREVIEW_RNG = $(if ($TriggeredStatus -or $PersistentEnchantments) { '1' } else { '0' })
    MT2_PROBE_ISOLATE_UI_RNG = $(if ($CompanionBoss -or $ConditionalTriggers -or $DetachedBonusDraw -or $RoomCapacity -or $RoomCapacityLethal -or $BonusDraw -or $BonusDrawLethal -or $NumericRanges -or $NumericRangesLethal -or $Drawing -or $TriggeredHealing -or $PostCombatHealing -or $TriggeredDamage -or $DamageDeathQueue -or $TerminalDeathDamage -or $HitKill -or $DyingUpgrades -or $AttackTriggers -or $TriggeredStatus) { '1' } else { '0' })
    MT2_PROBE_STATISTIC_QUERIES = $(if ($StatisticQueries) { '1' } else { '0' })
    MT2_PROBE_STATISTIC_OVERFLOW = $(if ($StatisticOverflow) { '1' } else { '0' })
    MT2_PROBE_HORDE_STATS = $(if ($HordeStats) { '1' } else { '0' })
    MT2_PROBE_ENCHANTMENT_LIFECYCLE = $(if ($EnchantmentLifecycle) { '1' } else { '0' })
    MT2_PROBE_ENCHANTMENT_COMBAT = $(if ($EnchantmentCombat) { '1' } else { '0' })
    MT2_PROBE_ENCHANTMENT_WORLD = $(if ($EnchantmentWorld) { '1' } else { '0' })
    MT2_PROBE_TRIGGER_REPEATS = $(if ($TriggerRepeats) { '1' } else { '0' })
}
if ($TriggeredSummonsRevival) {
    $environment['MT2_PROBE_MODIFIERS'] = 'triggered-summon-equipment-owned-death-revival' + $(if ($TriggeredSummonsFresh) { '-fresh' } else { '' })
}
if ($HordeMerge) { $environment['MT2_PROBE_MODIFIERS'] = 'horde-merge' }
if ($Bump) { $environment['MT2_PROBE_MODIFIERS'] = 'bump' }
if ($UnitClone) { $environment['MT2_PROBE_MODIFIERS'] = 'unit-clone' }
if ($UnitCopy) { $environment['MT2_PROBE_MODIFIERS'] = 'unit-copy' }
if ($HeroCopy) { $environment['MT2_PROBE_MODIFIERS'] = 'hero-copy' }
if ($SpawnEnchant) { $environment['MT2_PROBE_MODIFIERS'] = 'spawn-enchant' }
if ($Purify) {
    $environment['MT2_PROBE_MODIFIERS'] = $(if ($PurifyQueues) { 'purify-queues' } else { 'purify' })
    $environment['MT2_PROBE_STATUS_CALLBACKS'] = '1'
}

if ($SpawnStatusRelics) {
    $environment['MT2_PROBE_MODIFIERS'] = $(if ($SpawnStatusRelicsClones) { 'spawn-status-relics-clones' } else { 'spawn-status-relics' })
    $environment['MT2_PROBE_STATUS_CALLBACKS'] = '1'
}
if ($RelicCardPiercingUpgrades) { $environment['MT2_PROBE_MODIFIERS'] = 'relic-card-piercing-upgrades' }
if ($RelicCardSelfPurgeUpgrades) { $environment['MT2_PROBE_MODIFIERS'] = 'relic-card-self-purge-upgrades' }
if ($RelicCardAbilityUpgrades) { $environment['MT2_PROBE_MODIFIERS'] = 'relic-card-ability-upgrades' }
if ($IncantRelics) { $IncantRelic = $true }
if ($IncantRelic) { $Incant = $true }
if ($Incant) {
    $environment['MT2_PROBE_MODIFIERS'] = $(if ($IncantRelics) { 'incant-relic-combined' } elseif ($IncantRelic) { 'incant-relic' } elseif ($IncantThresholds) { 'incant-thresholds' } else { 'incant' })
    $environment['MT2_PROBE_STATUS_CALLBACKS'] = '1'
}
if ($AbilityIncant) {
    $environment['MT2_PROBE_MODIFIERS'] = 'ability-incant'
    $environment['MT2_PROBE_STATUS_CALLBACKS'] = '1'
}
if ($PersistentEnchantments) {
    $environment['MT2_PROBE_MODIFIERS'] = 'persistent-enchantment' + $(if ($PersistentEnchantmentRandomPools) { '-random' } else { '' }) +
        $(if ($PersistentEnchantmentRevivals) { '-revivals' } elseif ($PersistentEnchantmentDeaths) { '-deaths' } elseif ($PersistentEnchantmentUpgrades) { '-upgrades' } elseif ($PersistentEnchantmentSummons) { '-summons' + $(if ($PersistentEnchantmentSummonsFresh) { '-fresh' } else { '' }) } else { '' })
}
if ($Sentry -or $SentryLethal) {
    $environment['MT2_PROBE_MODIFIERS'] = $(if ($SentryLethal) { 'sentry-lethal' } else { 'sentry' })
    $environment['MT2_PROBE_ISOLATE_UI_RNG'] = '1'
}
$originalBefore = Get-OriginalSignature
$unityLog = Join-Path $profile 'unity-scenario.log'
$nativeTimer = [System.Diagnostics.Stopwatch]::StartNew()
$process = Start-Process -FilePath (Join-Path $gameRoot 'MonsterTrain2.exe') `
    -WorkingDirectory $gameRoot -ArgumentList @('-logFile', $unityLog) `
    -Environment $environment -WindowStyle Hidden -PassThru
Write-Output "FULL-BATTLE-RUN pid=$($process.Id) policy=$Policy profile=$profile"
if (-not $process.WaitForExit(600000)) {
    $process.Kill()
    $process.WaitForExit()
    throw "Full battle timed out; profile: $profile"
}
$nativeTimer.Stop()
Write-Output "FULL-BATTLE-TIMING speed=$GameSpeed elapsedSeconds=$($nativeTimer.Elapsed.TotalSeconds.ToString('F2', [Globalization.CultureInfo]::InvariantCulture))"
$tracePath = Join-Path $profile $(if ($BinaryCapture) { 'full-battle.mt2f' } else { 'full-battle.json' })
if (-not (Test-Path -LiteralPath $tracePath)) { throw "Missing battle trace (native exit code $($process.ExitCode)); inspect $unityLog" }
$inspectionTimer = [System.Diagnostics.Stopwatch]::StartNew()
if ($BinaryCapture) {
    Add-Type -Path (Join-Path $workspace 'src\FixtureArchive\bin\Release\net8.0\FixtureArchive.dll')
    $archive = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read($tracePath)
    $trace = $archive.RootElement.ToObjectGraph()
    $archive.Dispose()
} else {
    $trace = Get-Content -LiteralPath $tracePath -Raw | ConvertFrom-Json
}
$inspectionTimer.Stop()
Write-Output "FULL-BATTLE-INSPECTION elapsedSeconds=$($inspectionTimer.Elapsed.TotalSeconds.ToString('F3', [Globalization.CultureInfo]::InvariantCulture))"
$nativePassed = [bool] (Select-String -LiteralPath $unityLog -Pattern 'DEPTH-PASS' -Quiet)
$terminalSettled = $trace.TerminalCaptureBoundary -eq 'AfterStopCombatLoop' -and $trace.TerminalEffectsSettled -eq $true
$originalUnchanged = (Get-OriginalSignature) -ceq $originalBefore
if ($RelicCatalog) {
    Add-Type -Path (Join-Path $workspace 'src\FixtureArchive\bin\Release\net8.0\FixtureArchive.dll')
    $catalogArchive = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read((Join-Path $profile 'relic-catalog.mt2f'))
    try {
        $catalog = $catalogArchive.RootElement
        $catalogRecords = @($catalog.GetProperty('Relics').EnumerateArray())
        if ($catalog.GetProperty('Schema').GetInt32() -ne 4 -or
            $catalog.GetProperty('GameModuleMvid').GetString() -ne $trace.GameModuleMvid -or
            $catalog.GetProperty('FrameBefore').GetInt32() -ne $catalog.GetProperty('FrameAfter').GetInt32() -or
            -not $catalog.GetProperty('RngBefore').ContentEquals($catalog.GetProperty('RngAfter')) -or
            $catalogRecords.Count -eq 0 -or
            @($catalogRecords | ForEach-Object { $_.GetProperty('Collection').GetString() } | Sort-Object -Unique).Count -ne 8) {
            throw 'Relic definition catalog is incomplete or changed native RNG/frame.'
        }
        Write-Output "NATIVE-RELIC-CATALOG PASS: $($catalogRecords.Count) definitions from eight original collections; unchanged gameplay/test RNG and frame."
    } finally { $catalogArchive.Dispose() }
}
if ($TraitComposition) {
    Add-Type -Path (Join-Path $workspace 'src\FixtureArchive\bin\Release\net8.0\FixtureArchive.dll')
    $traitArchive = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read((Join-Path $profile 'card-trait-composition-calibration.mt2f'))
    try {
        $traitData = $traitArchive.RootElement
        if ($traitData.GetProperty('Schema').GetInt32() -ne 1 -or
            -not $traitData.GetProperty('ContextUnchanged').GetBoolean() -or
            $traitData.GetProperty('GameModuleMvid').GetString() -ne $trace.GameModuleMvid -or
            $traitData.GetProperty('FrameBefore').GetInt32() -ne $traitData.GetProperty('FrameAfter').GetInt32() -or
            -not $traitData.GetProperty('RngBefore').ContentEquals($traitData.GetProperty('RngAfter')) -or
            $traitData.GetProperty('Rows').GetArrayLength() -ne 2134) { throw 'Incomplete or mutating trait composition calibration.' }
        Write-Output 'NATIVE-TRAIT-COMPOSITION-CAPTURE PASS: 2134 original/owned/controlled refresh observations; unchanged context/RNG/frame.'
    } finally { $traitArchive.Dispose() }
}
if ($CardStatusComposition) {
    Add-Type -Path (Join-Path $workspace 'src\FixtureArchive\bin\Release\net8.0\FixtureArchive.dll')
    $statusArchive = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read((Join-Path $profile 'card-status-composition-calibration.mt2f'))
    try {
        $statusData = $statusArchive.RootElement
        if ($statusData.GetProperty('Schema').GetInt32() -ne 1 -or
            -not $statusData.GetProperty('ContextUnchanged').GetBoolean() -or
            $statusData.GetProperty('GameModuleMvid').GetString() -ne $trace.GameModuleMvid -or
            $statusData.GetProperty('FrameBefore').GetInt32() -ne $statusData.GetProperty('FrameAfter').GetInt32() -or
            -not $statusData.GetProperty('RngBefore').ContentEquals($statusData.GetProperty('RngAfter')) -or
            $statusData.GetProperty('Rows').GetArrayLength() -ne 1039) { throw 'Incomplete or mutating card status composition calibration.' }
        Write-Output 'NATIVE-CARD-STATUS-COMPOSITION-CAPTURE PASS: 1039 original-owned/controlled status queries; unchanged context/RNG/frame.'
    } finally { $statusArchive.Dispose() }
}
if ($RelicCardUpgrades) {
    Add-Type -Path (Join-Path $workspace 'src\FixtureArchive\bin\Release\net8.0\FixtureArchive.dll')
    $relicUpgradeArchive = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read((Join-Path $profile 'relic-card-upgrade-calibration.mt2f'))
    try {
        $relicUpgradeData = $relicUpgradeArchive.RootElement
        if ($relicUpgradeData.GetProperty('Schema').GetInt32() -ne 1 -or
            -not $relicUpgradeData.GetProperty('ContextUnchanged').GetBoolean() -or
            $relicUpgradeData.GetProperty('GameModuleMvid').GetString() -ne $trace.GameModuleMvid -or
            $relicUpgradeData.GetProperty('FrameBefore').GetInt32() -ne $relicUpgradeData.GetProperty('FrameAfter').GetInt32() -or
            -not $relicUpgradeData.GetProperty('RngBefore').ContentEquals($relicUpgradeData.GetProperty('RngAfter')) -or
            $relicUpgradeData.GetProperty('Rows').GetArrayLength() -ne 432 -or
            $relicUpgradeData.GetProperty('OriginalRelicAssets').GetArrayLength() -ne 11 -or
            $relicUpgradeData.GetProperty('OwnedCardCount').GetInt32() -ne 15) { throw 'Incomplete or mutating relic card upgrade calibration.' }
        Write-Output 'NATIVE-RELIC-CARD-UPGRADE-CAPTURE PASS: 432 original-effect operations,11 original relics,15 owned cards; unchanged context/RNG/frame.'
    } finally { $relicUpgradeArchive.Dispose() }
}
if ($BattleRelicUpgrades) {
    $modifierRows = @($trace.RelicCardModifiers)
    $rejectedDispatches = if ($RelicCardStatusUpgrades -or $RelicCardPiercingUpgrades -or $RelicCardAbilityUpgrades) {
        @($modifierRows | ForEach-Object Dispatches | Where-Object { -not $_.Returned -and -not $_.UpgradeAdded }).Count
    } else {
        @($modifierRows | ForEach-Object Dispatches | Where-Object { $_.Returned -and -not $_.UpgradeAdded }).Count
    }
    if (-not $trace.RelicCardModifiersEnabled -or $modifierRows.Count -eq 0 -or
        @($modifierRows | Where-Object { $null -eq $_.Actual -or $_.Difference }).Count -gt 0 -or
        @($modifierRows | Where-Object { -not $_.ResetTemporary }).Count -eq 0 -or
        $rejectedDispatches -eq 0 -or
        @($modifierRows | ForEach-Object Dispatches | Where-Object UpgradeAdded).Count -eq 0) {
        throw 'Incomplete native relic manager card modifier acceptance.'
    }
    if ($RelicCardStatusUpgrades -or $RelicCardPiercingUpgrades -or $RelicCardSelfPurgeUpgrades -or $RelicCardAbilityUpgrades) {
        Write-Output "NATIVE-RELIC-CARD-UPGRADE-CAPTURE PASS: $($modifierRows.Count) actual manager calls, temporary upgrade payload, reset and failed-eligibility paths."
    } else {
        Write-Output "NATIVE-RELIC-CARD-MODIFIER-CAPTURE PASS: $($modifierRows.Count) actual manager calls, original eligibility, reset/repeat/unique rejection and notifications."
    }
}
if ($ConditionalBattleRelicUpgrades -and $trace.ModifierScenario -ne 'conditional-relic-card-upgrades') {
    throw 'Conditional relic card upgrade scenario was not recorded.'
}
if ($RelicCardStatusUpgrades -and $trace.ModifierScenario -ne 'relic-card-status-upgrades') {
    throw 'Relic card status upgrade scenario was not recorded.'
}
if ($RelicCardPiercingUpgrades -and $trace.ModifierScenario -ne 'relic-card-piercing-upgrades') {
    throw 'Relic card piercing upgrade scenario was not recorded.'
}
if ($RelicCardSelfPurgeUpgrades -and $trace.ModifierScenario -ne 'relic-card-self-purge-upgrades') {
    throw 'Relic card self-purge upgrade scenario was not recorded.'
}
if ($RelicCardAbilityUpgrades -and $trace.ModifierScenario -ne 'relic-card-ability-upgrades') {
    throw 'Relic card ability upgrade scenario was not recorded.'
}
if ($BranchCardMasks) {
    $branchRows = @($trace.BranchCardMasks)
    if (-not $trace.BranchCardMasksEnabled -or $branchRows.Count -eq 0 -or
        @($branchRows | Where-Object { @($_.Views).Count -ne @($_.Context.CardRegistry).Count -or
            @($_.Context.CardRegistry | Where-Object { $null -eq $_.MaskDescriptor -or $_.MaskDescriptor.Definition.DataId -ne $_.DataId }).Count -gt 0 }).Count -gt 0) {
        throw 'Branch card mask oracle or owned descriptors are incomplete.'
    }
    Write-Output "NATIVE-BRANCH-CARD-MASK-CAPTURE PASS: $($branchRows.Count) complete decision contexts with independent native views."
}
if ($OwnedCardMasks) {
    Add-Type -Path (Join-Path $workspace 'src\FixtureArchive\bin\Release\net8.0\FixtureArchive.dll')
    $ownedArchive = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read((Join-Path $profile 'card-owned-mask-calibration.mt2f'))
    try {
        $ownedData = $ownedArchive.RootElement
        if ($ownedData.GetProperty('Schema').GetInt32() -ne 1 -or
            -not $ownedData.GetProperty('ContextUnchanged').GetBoolean() -or
            $ownedData.GetProperty('GameModuleMvid').GetString() -ne $trace.GameModuleMvid -or
            $ownedData.GetProperty('FrameBefore').GetInt32() -ne $ownedData.GetProperty('FrameAfter').GetInt32() -or
            -not $ownedData.GetProperty('RngBefore').ContentEquals($ownedData.GetProperty('RngAfter')) -or
            $ownedData.GetProperty('Rows').GetArrayLength() -ne 416 -or
            $ownedData.GetProperty('Masks').GetArrayLength() -ne 62 -or
            -not $ownedData.GetProperty('ParameterTraitPresent').GetBoolean()) { throw 'Incomplete or mutating owned card mask calibration.' }
        Write-Output 'NATIVE-OWNED-CARD-MASK-CAPTURE PASS: 416 owned/controlled carried views and 62 original masks; unchanged context/RNG/frame.'
    } finally { $ownedArchive.Dispose() }
}
if ($CardUpgradeLifecycle) {
    Add-Type -Path (Join-Path $workspace 'src\FixtureArchive\bin\Release\net8.0\FixtureArchive.dll')
    $lifecycleArchive = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read((Join-Path $profile 'card-upgrade-lifecycle-calibration.mt2f'))
    try {
        $lifecycleData = $lifecycleArchive.RootElement
        if ($lifecycleData.GetProperty('Schema').GetInt32() -ne 1 -or
            -not $lifecycleData.GetProperty('ContextUnchanged').GetBoolean() -or
            $lifecycleData.GetProperty('GameModuleMvid').GetString() -ne $trace.GameModuleMvid -or
            $lifecycleData.GetProperty('FrameBefore').GetInt32() -ne $lifecycleData.GetProperty('FrameAfter').GetInt32() -or
            -not $lifecycleData.GetProperty('RngBefore').ContentEquals($lifecycleData.GetProperty('RngAfter')) -or
            $lifecycleData.GetProperty('OriginalRelicAssets').GetArrayLength() -ne 11 -or
            $lifecycleData.GetProperty('Rows').GetArrayLength() -ne 212) { throw 'Incomplete or mutating card upgrade lifecycle calibration.' }
        Write-Output 'NATIVE-CARD-UPGRADE-LIFECYCLE-CAPTURE PASS: 212 native operations across 11 original relic payloads and controlled lifecycle/magic power sequences; unchanged context/RNG/frame.'
    } finally { $lifecycleArchive.Dispose() }
}
if ($ModifierOverflow) {
    Add-Type -Path (Join-Path $workspace 'src\FixtureArchive\bin\Release\net8.0\FixtureArchive.dll')
    $overflowArchive = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read((Join-Path $profile 'card-modifier-overflow-calibration.mt2f'))
    try {
        $overflowData = $overflowArchive.RootElement
        if (-not $overflowData.GetProperty('ContextUnchanged').GetBoolean() -or
            $overflowData.GetProperty('GameModuleMvid').GetString() -ne $trace.GameModuleMvid -or
            $overflowData.GetProperty('FrameBefore').GetInt32() -ne $overflowData.GetProperty('FrameAfter').GetInt32() -or
            -not $overflowData.GetProperty('RngBefore').ContentEquals($overflowData.GetProperty('RngAfter')) -or
            $overflowData.GetProperty('Samples').GetArrayLength() -ne 2240) { throw 'Incomplete or mutating modifier overflow calibration.' }
        Write-Output 'NATIVE-MODIFIER-OVERFLOW-CAPTURE PASS: 2240 original numeric queries; unchanged context/RNG/frame.'
    } finally { $overflowArchive.Dispose() }
}
if ($UpgradeMasks) {
    Add-Type -Path (Join-Path $workspace 'src\FixtureArchive\bin\Release\net8.0\FixtureArchive.dll')
    $maskArchive = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read((Join-Path $profile 'card-upgrade-mask-calibration.mt2f'))
    try {
        $maskCalibration = $maskArchive.RootElement
        if ($maskCalibration.GetProperty('Schema').GetInt32() -ne 2 -or
            $maskCalibration.GetProperty('GameModuleMvid').GetString() -ne $trace.GameModuleMvid -or
            -not $maskCalibration.GetProperty('ContextUnchanged').GetBoolean() -or
            $maskCalibration.GetProperty('FrameBefore').GetInt32() -ne $maskCalibration.GetProperty('FrameAfter').GetInt32() -or
            -not $maskCalibration.GetProperty('RngBefore').ContentEquals($maskCalibration.GetProperty('RngAfter')) -or
            $maskCalibration.GetProperty('Rows').GetArrayLength() -eq 0 -or
            $maskCalibration.GetProperty('CardData').GetArrayLength() -eq 0 -or
            $maskCalibration.GetProperty('CardStates').GetArrayLength() -eq 0 -or
            $maskCalibration.GetProperty('Characters').GetArrayLength() -eq 0 -or
            $maskCalibration.GetProperty('EdgeCases').GetArrayLength() -lt 50) {
            throw 'Card upgrade mask calibration is incomplete or changed native state.'
        }
        Write-Output "NATIVE-UPGRADE-MASK-CAPTURE PASS: $($maskCalibration.GetProperty('Rows').GetArrayLength()) original masks, $($maskCalibration.GetProperty('CardData').GetArrayLength()) original card definitions, owned cards and live characters; unchanged native context/RNG/frame."
    } finally { $maskArchive.Dispose() }
}
if ($PersistentEnchantments) {
    $persistentScenario = $environment['MT2_PROBE_MODIFIERS']
    $persistentFrames = @($trace.Actions | ForEach-Object { $_.Actual.Spawn.Train.Context.Enchantments })
    $persistentRules = @($persistentFrames | ForEach-Object { $_.Rooms.Units.Triggers.Effects.Enchantment } | Where-Object { $null -ne $_ -and $_.Bound })
    if ($trace.Schema -ne $(if ($SettleCardAnimations) { 106 } elseif ($SettleDeathDissolves) { 105 } else { 104 }) -or $trace.ModifierScenario -ne $persistentScenario -or
        -not (Select-String -LiteralPath $unityLog -Pattern 'PERSISTENT-ENCHANTMENT-PREPARED' -Quiet) -or
        $persistentFrames.Count -ne @($trace.Actions).Count -or @($trace.Turns).Count -lt 3 -or
        $persistentRules.Count -lt 2 -or @($persistentRules | Where-Object { @($_.State.PrimaryTargets).Count -gt 0 }).Count -eq 0) {
        throw 'Persistent enchantment scenario did not retain bound, nonempty aura state through the paid multi-turn battle.'
    }
    if ($PersistentEnchantmentDeaths -and @($persistentFrames | ForEach-Object { $_.RetainedUnits } | Where-Object {
        $_.Unit.Health -eq 0 -and $_.Unit.DeathState.IsDestroyed -and
        @($_.Unit.Triggers.Effects.Enchantment | Where-Object { $null -ne $_ -and -not $_.Bound -and @($_.State.PrimaryTargets).Count -gt 0 }).Count -gt 0
    }).Count -eq 0) { throw 'Persistent enchantment death scenario did not destroy and release an aura source with retained effect maps.' }
    if ($PersistentEnchantmentRevivals -and (@($trace.Revivals).Count -lt 4 -or @($trace.RevivalOperations).Count -ne 0 -or
        @($trace.Revivals | Where-Object { $_.AutomaticQueueDeferrals -ne 0 -or -not $_.Completed -or $_.Error }).Count -gt 0)) {
        throw 'Persistent aura revival must include four natural complete revivals without setup operations or queue deferrals.'
    }
    if ($PersistentEnchantmentRandomPools) {
        $randomPoolIds = @($persistentRules | ForEach-Object { $_.State.CachedStatus.Id } | Sort-Object -Unique)
        $randomPreviews = @($trace.PreviewRngIsolation | Where-Object {
            ($_.BattleBefore.Values -join ',') -ne ($_.TestObserved.Values -join ',')
        })
        if (@($persistentRules | Where-Object { @($_.StatusPool).Count -ne 3 }).Count -gt 0 -or
            $randomPoolIds.Count -ne 3 -or $randomPreviews.Count -eq 0 -or
            @($trace.PreviewRngIsolation | Where-Object { -not $_.Completed -or
                ($_.BattleBefore.Values -join ',') -ne ($_.BattleAfter.Values -join ',') -or
                ($_.TestBefore.Values -join ',') -ne ($_.TestAfter.Values -join ',') }).Count -gt 0) {
            throw 'Random persistent auras require three selected pool entries and actual isolated preview RNG draws.'
        }
    }
}
$modifierActions = @($trace.Actions | Where-Object {
    $actionEntry = $_
    $playedCard = $actionEntry.Before.Spawn.Train.Context.Cards.Hand |
        Where-Object InstanceId -EQ $actionEntry.Action.CardInstanceId
    $definition = $actionEntry.Before.PlayRules.Cards | Where-Object DataId -EQ $playedCard.DataId
    @($definition.Effects | Where-Object { $_.Type -eq 'UnitUpgrade' -or $_.Type -eq 'HandUpgrade' }).Count -gt 0
})
$modifierCoverage = (-not ($DynamicUpgrades -or $SacrificeUpgrades -or $HandUpgrades -or $TargetedHandUpgrades)) -or $modifierActions.Count -gt 0
$healActions = @($trace.Actions | Where-Object {
    $entry = $_
    $card = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
    $definition = $entry.Before.PlayRules.Cards | Where-Object DataId -EQ $card.DataId
    @($definition.Effects | Where-Object Type -EQ 'Heal').Count -gt 0
})
$healingCoverage = -not ($Healing -or $HealingTriggers)
if ($Healing -or $HealingTriggers) {
    $healingCoverage = $healActions.Count -gt 0 -and @($healActions | Where-Object {
        $entry = $_
        $beforeUnit = $entry.Before.Spawn.Train.Rooms.Units | Where-Object Id -EQ $entry.Action.TargetUnitId
        $afterUnit = $entry.Actual.Spawn.Train.Rooms.Units | Where-Object Id -EQ $entry.Action.TargetUnitId
        $null -ne $afterUnit -and $afterUnit.Health -gt $beforeUnit.Health
    }).Count -gt 0
}
$onHealCoverage = -not $HealingTriggers
if ($HealingTriggers) {
    $onHealCoverage = @($healActions | Where-Object {
        $entry = $_
        $target = $entry.Before.Spawn.Train.Rooms.Units | Where-Object Id -EQ $entry.Action.TargetUnitId
        @($target.Statuses | Where-Object Id -EQ 'heal immunity').Count -gt 0 -and
            $entry.Actual.Spawn.Train.Context.Gold - $entry.Before.Spawn.Train.Context.Gold -eq 15
    }).Count -gt 0
}
$roomSpellCoverage = -not $RoomSpells
if ($RoomSpells) {
    $multiHeal = @($healActions | Where-Object {
        $entry = $_
        $beforeRoom = $entry.Before.Spawn.Train.Rooms | Where-Object RoomIndex -EQ $entry.Action.RoomIndex
        $afterRoom = $entry.Actual.Spawn.Train.Rooms | Where-Object RoomIndex -EQ $entry.Action.RoomIndex
        @($beforeRoom.Units | Where-Object {
            $beforeUnit = $_
            $afterUnit = $afterRoom.Units | Where-Object Id -EQ $beforeUnit.Id
            $beforeUnit.Team -eq 1 -and $null -ne $afterUnit -and $afterUnit.MaxHealth -eq $beforeUnit.MaxHealth + 4 -and
                $afterUnit.Health -eq $afterUnit.MaxHealth
        }).Count -ge 2
    }).Count -gt 0
    $multiDamage = @($healActions | Where-Object {
        $entry = $_
        $beforeRoom = $entry.Before.Spawn.Train.Rooms | Where-Object RoomIndex -EQ $entry.Action.RoomIndex
        $afterRoom = $entry.Actual.Spawn.Train.Rooms | Where-Object RoomIndex -EQ $entry.Action.RoomIndex
        @($beforeRoom.Units | Where-Object {
            $beforeUnit = $_
            $afterUnit = $afterRoom.Units | Where-Object Id -EQ $beforeUnit.Id
            $beforeUnit.Team -eq 0 -and ($null -eq $afterUnit -or $afterUnit.Health -lt $beforeUnit.Health)
        }).Count -ge 2
    }).Count -gt 0
    $roomSpellCoverage = $multiHeal -and $multiDamage
}
$terminalSpellCoverage = -not ($TerminalSpells -or $PostKillSpells) -or @($trace.Actions | Where-Object ActualOutcome -EQ 3).Count -gt 0
$postKillCoverage = -not $PostKillSpells
$randomCoverage = -not $RandomSpells -or @($trace.Actions | Where-Object {
    $entry = $_
    $card = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
    $rule = $entry.Before.PlayRules.Cards | Where-Object DataId -EQ $card.DataId
    @($rule.Effects | Where-Object Target -EQ 'RandomInRoom').Count -gt 0 -and
        (ConvertTo-Json -InputObject $entry.Before.Spawn.Train.Context.BattleRng -Compress) -cne
        (ConvertTo-Json -InputObject $entry.Actual.Spawn.Train.Context.BattleRng -Compress)
}).Count -gt 0
$randomStatusCoverage = -not $RandomStatus -or @($trace.Actions | Where-Object {
    $entry = $_
    $card = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
    $rule = $entry.Before.PlayRules.Cards | Where-Object DataId -EQ $card.DataId
    @($rule.Effects | Where-Object { $_.Type -eq 'AddStatus' -and $_.Statuses.Count -gt 1 }).Count -gt 0 -and
        $entry.ActualOutcome -eq 3 -and @($entry.Before.Spawn.Train.Rooms.Units | Where-Object {
            @($_.Statuses | Where-Object Id -EQ 'immune').Count -gt 0 -and -not $_.IsPyre
        }).Count -gt 0
}).Count -gt 0
$crossRoomCoverage = -not ($CrossRoomSpells -or $CrossRoomTargets) -or @($trace.Actions | Where-Object {
    $entry = $_
    (-not $CrossRoomSpells -or $entry.ActualOutcome -eq 3) -and @($entry.Before.Spawn.Train.Rooms | Where-Object {
        $oldRoom = $_
        $newRoom = $entry.Actual.Spawn.Train.Rooms | Where-Object RoomIndex -EQ $oldRoom.RoomIndex
        $oldRoom.RoomIndex -ne $entry.Action.RoomIndex -and @($oldRoom.Units | Where-Object {
            $old = $_
            $next = $newRoom.Units | Where-Object Id -EQ $old.Id
            $old.Team -eq 1 -and -not $old.IsPyre -and $null -ne $next -and
                $next.MaxHealth -eq $old.MaxHealth + 6 -and $next.Health -eq $next.MaxHealth
        }).Count -gt 0
    }).Count -gt 0
}).Count -gt 0
$attackCoverage = -not $AttackBuffs
if ($AttackBuffs) {
    $attackActions = @($trace.Actions | Where-Object {
        $entry = $_
        $card = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
        $rule = $entry.Before.PlayRules.Cards | Where-Object DataId -EQ $card.DataId
        $rule.Effects[0].Type -eq 'BuffAttack' -and $rule.Effects[0].Target -eq 'Tower'
    })
    $remoteAttack = $false
    $incapableAttack = $false
    $zeroAttack = $false
    foreach ($entry in $attackActions) {
        foreach ($room in $entry.Before.Spawn.Train.Rooms) {
            foreach ($old in $room.Units) {
                $next = $entry.Actual.Spawn.Train.Rooms.Units | Where-Object Id -EQ $old.Id
                if ($old.IsPyre -or $null -eq $next) { continue }
                if ($old.Team -eq 0 -and -not $old.CanAttack -and $next.Modifiers.DamageBuff -eq $old.Modifiers.DamageBuff) { $incapableAttack = $true }
                if ($old.CanAttack -and $old.BaseAttack -eq 0 -and $next.BaseAttack -gt 0) { $zeroAttack = $true }
                if ($room.RoomIndex -ne $entry.Action.RoomIndex -and $old.Team -eq 1 -and $old.CanAttack -and
                    ($next.Modifiers.DamageBuff - $old.Modifiers.DamageBuff) -in @(3, 4)) { $remoteAttack = $true }
            }
        }
    }
    $attackCoverage = $attackActions.Count -gt 0 -and $remoteAttack -and $incapableAttack -and $zeroAttack
}
$maxHealthCoverage = -not ($MaxHealthSpells -or $MaxHealthLethal)
if ($MaxHealthSpells -or $MaxHealthLethal) {
    $maxHealthActions = @($trace.Actions | Where-Object {
        $entry = $_
        $card = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
        $rule = $entry.Before.PlayRules.Cards | Where-Object DataId -EQ $card.DataId
        @($rule.Effects | Where-Object Type -EQ 'BuffHealth').Count -gt 0
    })
    $maxHealthRemote = $false
    $maxHealthDeath = $false
    $maxHealthSpawner = $false
    $maxHealthNoTrigger = $false
    foreach ($entry in $maxHealthActions) {
        foreach ($room in $entry.Before.Spawn.Train.Rooms) {
            foreach ($old in $room.Units) {
                $next = $entry.Actual.Spawn.Train.Rooms.Units | Where-Object Id -EQ $old.Id
                if ($old.IsPyre) { continue }
                if ($old.Team -eq 0 -and $null -eq $next) { $maxHealthDeath = $true }
                if ($old.Team -ne 1 -or $null -eq $next) { continue }
                if ($room.RoomIndex -ne $entry.Action.RoomIndex -and $next.MaxHealth -gt $old.MaxHealth) { $maxHealthRemote = $true }
                $oldCard = $entry.Before.Spawn.Train.Context.CardRegistry | Where-Object InstanceId -EQ $old.SpawnerCardId
                $nextCard = $entry.Actual.Spawn.Train.Context.CardRegistry | Where-Object InstanceId -EQ $old.SpawnerCardId
                if ($nextCard.Temporary.Offsets.Health - $oldCard.Temporary.Offsets.Health -eq $(if ($MaxHealthLethal) { 4 } else { 2 })) {
                    $maxHealthSpawner = $true
                }
                $maxHealthHealingTriggers = @($next.Triggers | Where-Object Kind -EQ 'OnHeal')
                if ($maxHealthHealingTriggers.Count -gt 0 -and @($maxHealthHealingTriggers | Where-Object HasTriggered -EQ $true).Count -eq 0) { $maxHealthNoTrigger = $true }
            }
        }
    }
    $maxHealthCoverage = $maxHealthActions.Count -gt 0 -and $maxHealthRemote -and $maxHealthDeath -and $maxHealthSpawner -and $maxHealthNoTrigger -and
        (-not $MaxHealthLethal -or @($maxHealthActions | Where-Object ActualOutcome -EQ 3).Count -gt 0)
}
if ($PostKillSpells) {
    $postKillCoverage = @($trace.Actions | Where-Object {
        $entry = $_
        if ($entry.ActualOutcome -ne 3) { return $false }
        $beforeRoom = $entry.Before.Spawn.Train.Rooms | Where-Object RoomIndex -EQ $entry.Action.RoomIndex
        $afterRoom = $entry.Actual.Spawn.Train.Rooms | Where-Object RoomIndex -EQ $entry.Action.RoomIndex
        $entry.Actual.Spawn.Train.Context.AllScenarioBossesDead -eq $true -and @($beforeRoom.Units | Where-Object {
            $old = $_
            $next = $afterRoom.Units | Where-Object Id -EQ $old.Id
            $card = $entry.Actual.Spawn.Train.Context.CardRegistry | Where-Object InstanceId -EQ $old.SpawnerCardId
            $old.Team -eq 1 -and $null -ne $next -and $next.MaxHealth -eq $old.MaxHealth + 4 -and
                $next.Health -eq $next.MaxHealth -and
                @($card.Permanent.Upgrades | Where-Object AssetKey -EQ 'PojuPostKillPermanent').Count -gt 0
        }).Count -ge 2
    }).Count -gt 0
}
if ($HandUpgrades -or $TargetedHandUpgrades) {
    $modifierCoverage = $modifierCoverage -and @($modifierActions | Where-Object {
        @($_.Actual.Spawn.Train.Context.CardInstances.Permanent.Upgrades | Where-Object AssetKey -Like 'PojuHand*').Count -gt 0
    }).Count -gt 0
} elseif ($SacrificeUpgrades) {
    $modifierCoverage = $modifierCoverage -and @($modifierActions | Where-Object {
        $modifierAction = $_
        @($modifierAction.Actual.Spawn.Train.Rooms.Units | Where-Object Id -EQ $modifierAction.Action.TargetUnitId).Count -eq 0
    }).Count -gt 0
} elseif ($DynamicUpgrades) {
    $modifierCoverage = $modifierCoverage -and @($modifierActions | Where-Object {
        @($_.Actual.Spawn.Train.Rooms.Units.Modifiers.Upgrades | Where-Object AssetKey -Like 'PojuProbe*').Count -gt 0
    }).Count -gt 0
}
$numericRangeCoverage = -not ($NumericRanges -or $NumericRangesLethal) -or (@($trace.NumericRanges | Where-Object Phase -EQ 'Cast').Count -gt 0 -and
    @($trace.NumericRanges | Where-Object Phase -EQ 'Test').Count -gt 0 -and @($trace.NumericRanges | Where-Object Phase -EQ 'Apply').Count -gt 0)
$targetFilterCoverage = -not $TargetFilters -or (@($trace.FilteredTargets | Where-Object Mode -EQ 'DropTargetCharacter').Count -gt 0 -and @($trace.FilteredTargets | Where-Object Mode -EQ 'Tower').Count -gt 0)
$drawCoverage = -not $Drawing -or (@($trace.CardCycles | Where-Object Kind -EQ 'SpellDraw').Count -gt 0 -and
    @($trace.NumericRanges | Where-Object Phase -EQ 'Cast').Count -gt 0 -and @($trace.NumericRanges | Where-Object Phase -EQ 'Test').Count -gt 0 -and
    @($trace.NumericRanges | Where-Object Phase -EQ 'Apply').Count -gt 0)
$handRemovalCoverage = -not ($HandRemoval -or $HandRemovalLethal) -or (@($trace.HandRemovals | Where-Object Mode -EQ 0).Count -gt 0 -and
    @($trace.HandRemovals | Where-Object Mode -EQ 1).Count -gt 0 -and @($trace.HandRemovals | Where-Object Difference -NE $null).Count -eq 0)
$generationCoverage = -not ($Generation -or $GenerationLethal) -or (@($trace.CardGenerations | ForEach-Object { $_.Rule.Destination } | Sort-Object -Unique).Count -eq 5 -and
    @($trace.CardGenerations | Where-Object { $_.Predicted.AddedCards.Count -gt 0 }).Count -gt 0 -and
    @($trace.CardGenerations | Where-Object Difference -NE $null).Count -eq 0)
if ($Generation -and -not $GenerationLethal) {
    $generationCoverage = $generationCoverage -and @($trace.CardGenerations | Where-Object Origin -EQ 'Unit').Count -gt 0
}
$scalingCoverage = -not ($DamageScaling -or $DynamicStatistics)
if ($DamageScaling -or $DynamicStatistics) {
    $samples = @($trace.DamageScaling)
    $scalingCoverage = $samples.Count -gt 0 -and @($samples | Where-Object DamageSourceCardId -GT 0).Count -gt 0 -and
        @($samples | Where-Object DamageSourceCardId -EQ 0).Count -gt 0 -and
        @($samples | Where-Object { $_.CaptureError -or $_.Difference -or $null -eq $_.After }).Count -eq 0
}
if ($DynamicStatistics) {
    $queryTypes = @($samples | ForEach-Object { $_.Trait.Query.Type } | Sort-Object -Unique)
    foreach ($queryType in @('Gold', 'TurnCount', 'MoonPhase', 'ForgePoints', 'DragonsHoardAmount', 'EnergyRemainingEndOfTurn', 'PyreHeartResurrection')) {
        $scalingCoverage = $scalingCoverage -and $queryTypes.Contains($queryType)
    }
    $scalingCoverage = $scalingCoverage -and
        @($samples | Where-Object { $_.Before.Statistics.EnergyRemainingEndOfTurn -gt 0 }).Count -gt 0 -and
        @($samples | Where-Object { $_.Before.Gold -ne $_.Before.Statistics.GoldStartOfThisTurn }).Count -gt 0 -and
        @($samples | ForEach-Object { $_.Before.QueryFrame.MoonPhase } | Sort-Object -Unique).Count -eq 2
}
$statusScalingCoverage = -not $StatusScaling
if ($StatusScaling) {
    $statusSamples = @($trace.StatusScaling)
    $applications = @($trace.StatusApplications)
    $statusScalingCoverage = $statusSamples.Count -ge 60 -and $applications.Count -gt 0 -and
        @($statusSamples | ForEach-Object { $_.Trait.Filter } | Sort-Object -Unique).Count -eq 3 -and
        @($statusSamples | Where-Object ActualBonus -LT 0).Count -gt 0 -and
        @($statusSamples | Where-Object ActualBonus -GT 0).Count -gt 0 -and
        @($statusSamples | Where-Object { $_.Trait.Filter -eq 1 -and $_.ActualBonus -gt 0 }).Count -gt 0 -and
        @($statusSamples | Where-Object { $_.Trait.OnlyWhenSourceZero -and $_.SourceStacks -eq 0 -and $_.ActualBonus -gt 0 }).Count -gt 0 -and
        @($statusSamples | Where-Object { $_.Trait.OnlyWhenSourceZero -and $_.SourceStacks -ne 0 -and $_.ActualBonus -eq 0 }).Count -gt 0 -and
        @($statusSamples | ForEach-Object { $_.Before.QueryFrame.MoonPhase } | Sort-Object -Unique).Count -eq 2 -and
        @($statusSamples | Where-Object { $_.CaptureError -or $_.Difference -or $null -eq $_.After }).Count -eq 0 -and
        @($applications | Where-Object { $_.CaptureError -or $_.Difference -or $null -eq $_.After }).Count -eq 0 -and
        @($applications | Where-Object { @($_.After.Units.Statuses | Where-Object Stacks -EQ 9999).Count -gt 0 }).Count -gt 0
}
$unitUpgradeScalingCoverage = -not $UnitUpgradeScaling
if ($UnitUpgradeScaling) {
    $upgradeSamples = @($trace.UnitUpgradeScaling)
    $unitUpgradeScalingCoverage = $trace.UnitUpgradeScalingCalibrationContextUnchanged -and
        @($upgradeSamples | Where-Object Origin -EQ 'Calibration').Count -eq 30 -and
        @($upgradeSamples | Where-Object Origin -EQ 'Live').Count -ge 35 -and
        @($upgradeSamples | ForEach-Object { $_.Trait.Stat } | Sort-Object -Unique).Count -eq 2 -and
        @($upgradeSamples | Where-Object TriggerKind -EQ 'OnSpawn').Count -gt 0 -and
        @($upgradeSamples | Where-Object TriggerKind -EQ 'OnHeal').Count -gt 0 -and
        @($upgradeSamples | Where-Object { $_.BeforeUpgrade.MagicPowerTraitScalingOnly }).Count -gt 0 -and
        @($upgradeSamples | Where-Object { $_.AfterUpgrade.Stats.Damage -lt $_.BeforeUpgrade.Stats.Damage }).Count -gt 0 -and
        @($upgradeSamples | Where-Object { $_.AfterUpgrade.Stats.Health -gt $_.BeforeUpgrade.Stats.Health }).Count -gt 0 -and
        @($upgradeSamples | Where-Object { $_.CaptureError -or $_.Difference -or $null -eq $_.After }).Count -eq 0
}
$unitTriggerUpgradeCoverage = -not $UnitTriggerUpgrades
if ($UnitTriggerUpgrades) {
    $triggerSamples = @($trace.UnitUpgradeScaling)
    $unitTriggerUpgradeCoverage = $triggerSamples.Count -ge 30 -and
        @($triggerSamples | Where-Object TriggerKind -EQ 'OnHeal').Count -gt 0 -and
        @($triggerSamples | Where-Object TriggerKind -EQ 'PostCombat').Count -gt 0 -and
        @($triggerSamples | Where-Object { $_.Trait.Restriction -eq 1 }).Count -gt 0 -and
        @($triggerSamples | Where-Object { $_.BeforeUpgrade.MagicPowerTraitScalingOnly }).Count -gt 0 -and
        @($triggerSamples | Where-Object { $_.Difference -or $_.CaptureError -or $null -eq $_.After }).Count -eq 0
}
$preHandDiscardCoverage = -not ($PreHandDiscard -or $PreHandDiscardLethal -or $CloneUpgradeRefresh)
if ($PreHandDiscard -or $PreHandDiscardLethal -or $CloneUpgradeRefresh) {
    $prePhases = @($trace.PreHandDiscards)
    $preHandDiscardCoverage = $prePhases.Count -gt 0 -and
        @($prePhases | Where-Object Team -EQ 1).Count -gt 0 -and @($prePhases | Where-Object Team -EQ 0).Count -gt 0 -and
        @($prePhases | Where-Object { $_.Difference -or $null -eq $_.Actual }).Count -eq 0 -and
        @($trace.UnitUpgradeScaling | Where-Object TriggerKind -EQ 'EndTurnPreHandDiscard').Count -ge 4 -and
        @($trace.UnitUpgradeScaling | Where-Object { $_.Difference -or $_.CaptureError -or $null -eq $_.After }).Count -eq 0 -and
        @($trace.CardGenerations | Where-Object { $_.Origin -eq 'Unit' -and $_.Rule.Destination -eq 'HandPile' }).Count -gt 0
}
$preCombatCoverage = -not ($PreCombat -or $PreCombatLethal -or $TriggeredHealing -or $TriggeredDamage -or $DamageDeathQueue -or $TerminalDeathDamage)
if ($PreCombat -or $PreCombatLethal -or $TriggeredHealing -or $TriggeredDamage -or $DamageDeathQueue -or $TerminalDeathDamage) {
    $prePhases = @($trace.PreCombats)
    $preCombatCoverage = $prePhases.Count -gt 0 -and
        @($prePhases | Where-Object Team -EQ 1).Count -gt 0 -and @($prePhases | Where-Object Team -EQ 0).Count -gt 0 -and
        @($prePhases | Where-Object { $_.Difference -or $null -eq $_.Actual }).Count -eq 0 -and
        @($trace.UnitUpgradeScaling | Where-Object TriggerKind -EQ 'PreCombat').Count -ge 4 -and
        @($trace.UnitUpgradeScaling | Where-Object { $_.Difference -or $_.CaptureError -or $null -eq $_.After }).Count -eq 0 -and
        @($trace.CardGenerations | Where-Object { $_.Origin -eq 'Unit' -and $_.Rule.Destination -eq 'HandPile' }).Count -gt 0
}
$triggeredHealingCoverage = -not $TriggeredHealing
if ($TriggeredHealing) {
    $heals = @($trace.TriggeredHeals)
    $triggeredHealingCoverage = $heals.Count -gt 0 -and
        @($heals | Where-Object { $_.Difference -or -not $_.Completed -or -not $_.Sampled }).Count -eq 0 -and
        @($heals | Where-Object { $_.Effect.Target -eq 'RandomInRoom' -and $null -ne $_.Effect.Range }).Count -gt 0 -and
        @($heals | Where-Object { $_.Effect.Target -eq 'Room' -and $_.Targets.Count -eq 0 -and $null -ne $_.Effect.Range }).Count -gt 0 -and
        @($heals | Where-Object { $_.Targets.Count -ge 2 }).Count -gt 0 -and
        @($heals.Requests | Where-Object { $_.AfterHealth -gt $_.Health }).Count -gt 0 -and
        @($heals.Requests | Where-Object Amount -LT 0).Count -gt 0 -and
        @($heals.Requests | Where-Object Amount -EQ 0).Count -gt 0 -and
        @($trace.UnitUpgradeScaling | Where-Object { $_.TriggerKind -eq 'OnHeal' -and $_.BeforeUpgrade.AssetKey -eq 'PojuOnHealDeferredMaxHealth' }).Count -gt 0
}
$postCombatHealingCoverage = -not $PostCombatHealing
if ($PostCombatHealing) {
    $postPhases = @($trace.UnitPostCombats)
    $postCombatHealingCoverage = $postPhases.Count -gt 0 -and
        @($postPhases | Where-Object { $_.Difference -or $null -eq $_.Actual }).Count -eq 0 -and
        @($trace.TriggeredHeals | Where-Object TriggerKind -EQ 'PostCombatHealing').Count -gt 0 -and
        @($postPhases | Where-Object { $_.CannotAttackOrHeal.Count -gt 0 }).Count -gt 0 -and
        @($trace.UnitUpgradeScaling | Where-Object TriggerKind -EQ 'PostCombat').Count -gt 0
}
$triggeredDamageCoverage = -not $TriggeredDamage
if ($TriggeredDamage) {
    $damageRecords = @($trace.TriggeredDamage)
    $applications = @($damageRecords | Where-Object Stage -EQ 'Application')
    $triggeredDamageCoverage = $applications.Count -gt 0 -and
        @($damageRecords | Where-Object { $_.Difference -or -not $_.Completed -or -not $_.Sampled }).Count -eq 0 -and
        @($applications | Where-Object { $_.Effect.Target -eq 'RandomInRoom' -and $null -ne $_.Effect.Range }).Count -gt 0 -and
        @($applications | Where-Object { $_.Targets.Count -eq 0 -and $null -ne $_.Effect.Range }).Count -gt 0 -and
        @($applications | Where-Object { $_.Targets.Count -ge 2 }).Count -gt 0 -and
        @($applications | Where-Object { $null -ne $_.StatusMultiplier -and $_.MultiplierStacks -gt 0 }).Count -gt 0 -and
        @($applications | Where-Object TriggerKind -EQ 'OnDeath').Count -gt 0 -and
        @($damageRecords | Where-Object { $_.Stage -eq 'Test' -and $_.Amount -lt 0 -and -not $_.TestPassed }).Count -gt 0 -and
        @($applications.Requests | Where-Object { -not $_.AttackerPreserved -or -not $_.DefaultDamage }).Count -eq 0
}
$damageDeathQueueCoverage = -not ($DamageDeathQueue -or $TerminalDeathDamage)
if ($DamageDeathQueue -or $TerminalDeathDamage) {
    $deathDamage = @($trace.TriggeredDamage | Where-Object { $_.Stage -eq 'Application' -and $_.TriggerKind -eq 'OnDeath' })
    $damageDeathQueueCoverage = $deathDamage.Count -gt 0 -and @($trace.PreCombats).Count -ge 2 -and
        @($deathDamage.Requests | Where-Object { @($_.Before.Modifiers.Upgrades | Where-Object AssetKey -EQ 'PojuOnHealBeforeDeathArmor').Count -gt 0 }).Count -gt 0 -and
        @($trace.UnitUpgradeScaling | Where-Object { $_.TriggerKind -eq 'OnHeal' -and $_.BeforeUpgrade.AssetKey -eq 'PojuOnHealBeforeDeathArmor' }).Count -gt 0
}
$cloneUpgradeRefreshCoverage = -not $CloneUpgradeRefresh
$triggeredStatusCoverage = -not $TriggeredStatus
if ($TriggeredStatus) {
    $status = @($trace.TriggeredStatuses)
    $previewRng = @($trace.PreviewRngIsolation)
    $triggeredStatusCoverage = $previewRng.Count -gt 0 -and
        @($previewRng | Where-Object { -not $_.Completed -or ($_.BattleBefore | ConvertTo-Json -Compress) -ne ($_.BattleAfter | ConvertTo-Json -Compress) -or ($_.TestBefore | ConvertTo-Json -Compress) -ne ($_.TestAfter | ConvertTo-Json -Compress) }).Count -eq 0 -and
        $status.Count -gt 10 -and
        @($status | Where-Object { -not $_.Completed -or $null -eq $_.Actual -or $null -eq $_.ActualUnits -or $_.Interactions.Count -gt 0 }).Count -eq 0 -and
        @($status | Where-Object { $_.Targets.Count -eq 0 }).Count -gt 0 -and
        @($status | Where-Object { $_.Targets.Count -gt 1 }).Count -gt 0 -and
        @($status | Where-Object { $_.Effect.Action.Statuses.Count -gt 1 }).Count -gt 0 -and
        @($status | Where-Object { $_.Effect.StatusScaling.MissingHealth -and $_.Effect.StatusScaling.MagicPower }).Count -gt 0 -and
        @($status | Where-Object { @($_.BeforeUnits | Where-Object Health -EQ 0).Count -gt 0 }).Count -gt 0 -and
        @($trace.UnitTurns | Where-Object { $null -eq $_.Actual -or $null -ne $_.Difference -or -not $_.Predicted.Supported }).Count -eq 0
}
$statusCallbackCoverage = -not $StatusCallbacks
if ($StatusCallbacks) {
    $callbackFires = @($trace.StatusCallbackFires)
    $statusCallbackCoverage = $callbackFires.Count -gt 20 -and $callbackFires.Count -eq @($trace.StatusCallbacks).Count -and
        @($callbackFires | Where-Object { -not $_.Completed -or $null -eq $_.Actual -or $null -eq $_.ActualUnit -or $_.Interactions.Count -gt 0 }).Count -eq 0 -and
        @($callbackFires | Where-Object { $_.Kind -eq 'OnSilenceLost' -and $_.GoldAfter -gt $_.GoldBefore }).Count -gt 0 -and
        @($callbackFires | Where-Object { $_.Kind -eq 'OnStatusEffectChanged' -and $_.ParamString -eq 'armor' -and $_.ParamInt2 -eq 0 -and $_.GoldAfter -gt $_.GoldBefore }).Count -gt 0 -and
        @($callbackFires | Where-Object { $_.BeforeUnit.Health -eq 0 }).Count -gt 0 -and
        @($callbackFires | Where-Object { $_.BeforeUnit.Team -eq 0 -or $_.BeforeUnit.Team -eq 'Enemy' }).Count -gt 0
    foreach ($callbackKind in 'OnStatusEffectChanged','OnArmorAdded','OnPyregelAdded','OnValiant','OnSilence','OnSilenceLost','OnNewStatusEffectAdded') {
        $statusCallbackCoverage = $statusCallbackCoverage -and @($callbackFires | Where-Object { $_.Kind -eq $callbackKind }).Count -gt 0
    }
}
$attackTriggerCoverage = -not $AttackTriggers
if ($AttackTriggers) {
    $attackFires = @($trace.AttackTriggers | Where-Object Stage -EQ 'Fire')
    $attackTriggerCoverage = $attackFires.Count -gt 10 -and
        @($attackFires | Where-Object Kind -EQ 'OnAttackingBeforeDamage').Count -gt 0 -and
        @($attackFires | Where-Object Kind -EQ 'OnAttacking').Count -gt 0 -and
        @($attackFires | Where-Object { $_.Health -eq 0 -and -not $_.DeadBoss }).Count -gt 0 -and
        @($trace.AttackTriggers | Where-Object { $_.Stage -eq 'Queue' -and $_.QueueRunning }).Count -gt 0 -and
        @($trace.AttackTriggers | Where-Object { -not $_.Completed -or $null -eq $_.GoldAfter -or $null -eq $_.AfterTriggered -or $_.OverrideTargetId -le 0 -or $_.ParamInt -ne 0 }).Count -eq 0 -and
        @($trace.UnitTurns | Where-Object { $null -eq $_.Actual -or $null -ne $_.Difference -or -not $_.Predicted.Supported }).Count -eq 0 -and
        @($trace.UnitTurns | Where-Object { $_.AttackedTargetIds.Count -gt 1 }).Count -gt 0
}
$dyingUpgradeCoverage = -not $DyingUpgrades
if ($DyingUpgrades) {
    $dying = @($trace.DyingUpgrades | Where-Object { @($_.BeforeUnits | Where-Object Health -EQ 0).Count -gt 0 })
    $dyingUpgradeCoverage = $dying.Count -ge 6 -and
        @($dying | Where-Object Kind -EQ 'OnKill').Count -gt 0 -and
        @($dying | Where-Object Kind -EQ 'OnHit').Count -gt 0 -and
        @($dying | Where-Object Kind -EQ 'OnDeath').Count -gt 0 -and
        @($dying | Where-Object { $_.Effect.Type -eq 'RemoveUnitUpgrade' }).Count -gt 0 -and
        @($dying | Where-Object { $_.Effect.Type -ne 'RemoveUnitUpgrade' -and $_.Effect.Upgrade.Stats.Health -lt 0 -and
            $_.ActualUnits[0].MaxHealth -lt $_.BeforeUnits[0].MaxHealth }).Count -gt 0 -and
        @($dying | Where-Object { $_.Effect.Upgrade.UnhealedHealth -lt 0 }).Count -gt 0 -and
        @($trace.DyingUpgrades | Where-Object { -not $_.Completed -or $null -eq $_.Actual -or $null -eq $_.ActualUnits -or $_.Interactions.Count -gt 0 }).Count -eq 0 -and
        @($trace.UnitTurns | Where-Object { $null -eq $_.Actual -or $null -ne $_.Difference -or -not $_.Predicted.Supported }).Count -eq 0
}
$hitKillCoverage = -not $HitKill
if ($HitKill) {
    $fires = @($trace.HitKills | Where-Object Stage -EQ 'Fire')
    $hitKillCoverage = $fires.Count -ge 10 -and
        @($fires | Where-Object { $_.Kind -eq 'OnHit' -and $_.Health -gt 0 -and $_.ParamInt -eq 0 -and $_.GoldAfter -gt $_.GoldBefore }).Count -gt 0 -and
        @($fires | Where-Object { $_.Kind -eq 'OnHit' -and $_.Health -eq 0 -and -not $_.DeadBoss -and $_.GoldAfter -gt $_.GoldBefore }).Count -gt 0 -and
        @($fires | Where-Object { $_.Kind -eq 'OnHit' -and $_.ParamInt -ge 2 }).Count -gt 0 -and
        @($fires | Where-Object { $_.Kind -eq 'OnKill' -and $_.DyingId -gt 0 -and $_.GoldAfter -gt $_.GoldBefore }).Count -gt 0 -and
        @($trace.HitKills | Where-Object { $_.Stage -eq 'Queue' -and $_.QueueRunning }).Count -gt 0 -and
        @($trace.UnitTurns | Where-Object { $_.AttackedTargetIds.Count -gt 1 }).Count -gt 0 -and
        @($trace.HitKills | Where-Object { -not $_.Completed -or $null -eq $_.GoldAfter -or $null -eq $_.AfterTriggered }).Count -eq 0 -and
        @($trace.UnitTurns | Where-Object { $null -eq $_.Actual -or $null -ne $_.Difference -or -not $_.Predicted.Supported }).Count -eq 0
}
$terminalDeathDamageCoverage = -not $TerminalDeathDamage
if ($TerminalDeathDamage) {
    $terminalRecords = @($trace.TerminalDeaths | Where-Object Terminal)
    $postClearDeaths = @($trace.TerminalDeaths | Where-Object { $_.Before.KillCamActivated -and $_.Team -eq 1 })
    $terminalDeathDamageCoverage = $terminalRecords.Count -gt 0 -and $postClearDeaths.Count -gt 0 -and @($trace.KillCams).Count -gt 0 -and
        @($trace.KillCams | Where-Object { -not $_.Completed -or $null -eq $_.Actual }).Count -eq 0 -and
        @($trace.TerminalDeaths | Where-Object { -not $_.Completed -or -not $_.FinishedDying -or $null -eq $_.Actual -or $_.Sacrifice }).Count -eq 0 -and
        @($terminalRecords | Where-Object { -not $_.Actual.KillCamActivated -or $_.Actual.CardInstances.Count -ne 0 }).Count -eq 0 -and
        @($trace.TriggeredDamage.Requests | Where-Object { $_.Context.KillCamActivated -and $_.Context.CardInstances.Count -eq 0 -and $_.AfterHealth -eq 0 }).Count -gt 0
}
if ($CloneUpgradeRefresh) {
    $cloneUpgradeRefreshCoverage = @($trace.CardGenerations | Where-Object { $_.Origin -eq 'Unit' -and $_.Rule.CopyModifiers }).Count -ge 2 -and
        @($trace.CardGenerations | Where-Object { $_.Difference -or $null -eq $_.Actual }).Count -eq 0
}
$teamTurnBeginCoverage = -not $TeamTurnBegin
if ($TeamTurnBegin) {
    $teamSamples = @($trace.UnitUpgradeScaling | Where-Object TriggerKind -EQ 'OnTeamTurnBegin')
    $teamPhases = @($trace.TeamTurnBegins)
    $teamTurnBeginCoverage = $teamSamples.Count -ge 6 -and $teamPhases.Count -gt 0 -and
        @($teamPhases | Where-Object Team -EQ 0).Count -gt 0 -and @($teamPhases | Where-Object Team -EQ 1).Count -gt 0 -and
        @($teamPhases | Where-Object { $_.Difference -or $null -eq $_.Actual }).Count -eq 0 -and
        @($trace.UnitTurns).Count -gt 0 -and @($trace.UnitTurns | Where-Object { $_.Difference -or $null -eq $_.Actual }).Count -eq 0 -and
        @($trace.UnitUpgradeScaling | Where-Object { $_.Difference -or $_.CaptureError -or $null -eq $_.After }).Count -eq 0
}
$unitTurnBeginCoverage = -not $UnitTurnBegin
if ($UnitTurnBegin) {
    $turnSamples = @($trace.UnitUpgradeScaling)
    $unitTurnBeginCoverage = $turnSamples.Count -ge 12 -and
        @($turnSamples | Where-Object TriggerKind -NE 'OnTurnBegin').Count -eq 0 -and
        @($turnSamples | Where-Object { $_.Trait.Restriction -eq 1 }).Count -gt 0 -and
        @($turnSamples | Where-Object { $_.Trait.Query.Type -eq 'TurnCount' -and $_.AfterUpgrade.Stats.Damage -gt $_.BeforeUpgrade.Stats.Damage }).Count -gt 0 -and
        @($turnSamples | Where-Object { $_.Difference -or $_.CaptureError -or $null -eq $_.After }).Count -eq 0 -and
        @($trace.UnitTurns).Count -gt 0 -and @($trace.UnitTurns | Where-Object { $_.Difference -or $null -eq $_.Actual }).Count -eq 0
}
$spawnTriggerCoverage = -not ($SpawnTriggers -or $SpawnTriggersLethal)
if ($SpawnTriggers -or $SpawnTriggersLethal) {
    $spawnSamples = @($trace.UnitUpgradeScaling)
    $spawnTriggerCoverage = @($spawnSamples | Where-Object TriggerKind -EQ 'OnSpawn').Count -ge 8 -and
        @($spawnSamples | Where-Object { $_.TriggerKind -eq 'OnSpawn' -and $_.Trait.Query.Type -eq 'AnyMonsterSpawned' -and $_.AfterUpgrade.Stats.Health -gt $_.BeforeUpgrade.Stats.Health }).Count -gt 0 -and
        @($spawnSamples | Where-Object { $_.Difference -or $_.CaptureError -or $null -eq $_.After }).Count -eq 0 -and
        @($trace.CardGenerations | Where-Object { $_.Origin -eq 'Unit' -and $_.SourceCardId -gt 0 -and $_.Rule.Count -gt 0 }).Count -gt 0
    if (-not $SpawnTriggersLethal) {
        $spawnTriggerCoverage = $spawnTriggerCoverage -and @($spawnSamples | Where-Object TriggerKind -EQ 'OnUnscaledSpawn').Count -gt 0
    }
}
$energyCoverage = $true
if ($EnergyEffects -or $EnergyEffectsLethal) {
    $energySamples = @($trace.EnergyEffects)
    $energyCoverage = $energySamples.Count -gt 0 -and @($energySamples.Effect.Type | Sort-Object -Unique).Count -eq 5 -and
        @($energySamples | Where-Object { -not $_.Completed -or -not $_.Sampled }).Count -eq 0 -and
        @($energySamples | Where-Object Amount -EQ 0).Count -gt 0 -and @($energySamples | Where-Object Amount -LT 0).Count -gt 0
    if ($EnergyEffectsLethal) {
        $energyCoverage = $energyCoverage -and @($trace.Actions | Where-Object { $_.Actual.Spawn.Train.Context.AllScenarioBossesDead -eq $true }).Count -eq 1
    }
}
$xCostCoverage = $true
if ($XCost -or $XCostLethal) {
    $xCasts = @($trace.Actions | Where-Object {
        $cardId = $_.Action.CardInstanceId
        $instance = $_.Before.Spawn.Train.Context.CardInstances | Where-Object InstanceId -EQ $cardId
        $definition = $_.Before.PlayRules.Cards | Where-Object DataId -EQ $instance.DataId
        $definition.CostType -eq 'ConsumeRemainingEnergy'
    })
    $xCostCoverage = $xCasts.Count -gt 0 -and @($xCasts | Where-Object { $_.Before.Energy -eq 0 }).Count -gt 0 -and
        @($xCasts | Where-Object { $_.Before.Energy -gt 0 }).Count -gt 0 -and
        @($trace.DamageScaling | Where-Object { $_.Difference -or $_.CaptureError -or $null -eq $_.After }).Count -eq 0 -and
        @($trace.DamageScaling.Trait.Query.Type | Sort-Object -Unique).Count -eq 2
    if ($XCostLethal) { $xCostCoverage = $xCostCoverage -and @($xCasts | Where-Object ActualOutcome -EQ 3).Count -eq 1 }
}
$bonusDrawCoverage = $true
if ($BonusDraw -or $BonusDrawLethal) {
    $bonusSamples = @($trace.BonusDrawEffects)
    $bonusDrawCoverage = $bonusSamples.Count -gt 0 -and @($bonusSamples | Where-Object { -not $_.Completed -or -not $_.Sampled -or $null -eq $_.After }).Count -eq 0 -and
        @($bonusSamples | Where-Object Amount -LT 0).Count -gt 0 -and @($bonusSamples | Where-Object Amount -EQ 0).Count -gt 0 -and
        @($bonusSamples | Where-Object { $_.Key -like 'unit:*' }).Count -gt 0 -and @($bonusSamples | Where-Object { $null -ne $_.Effect.Range }).Count -gt 0
    if ($BonusDrawLethal) { $bonusDrawCoverage = $bonusDrawCoverage -and @($trace.Actions | Where-Object ActualOutcome -EQ 3).Count -eq 1 }
}
$directUpgradeCoverage = -not $DirectUnitUpgrades -or (@($trace.DirectUnitUpgrades).Count -ge 21 -and @($trace.DirectUnitUpgrades | Where-Object { $null -eq $_.After -or $_.Difference -or $_.UnsupportedReason }).Count -eq 0)
$equipmentCoverage = -not $Equipment -or (@($trace.EquipmentOperations).Count -gt 3 -and
    @($trace.EquipmentOperations | Where-Object { -not $_.Completed -or $null -eq $_.After -or $_.Difference -or $_.UnsupportedReason }).Count -eq 0 -and
    @($trace.EquipmentOperations | Where-Object { $_.Remove -and $_.CardId -eq 0 }).Count -gt 0)
$capacityCoverage = $true
if ($RoomCapacity -or $RoomCapacityLethal) {
    $capacitySamples = @($trace.CapacityEffects)
    $capacityCoverage = $capacitySamples.Count -gt 0 -and @($capacitySamples | Where-Object { -not $_.Completed -or $null -eq $_.After }).Count -eq 0 -and
        @($capacitySamples | Where-Object { $_.Effect.Value -lt 0 }).Count -gt 0 -and @($capacitySamples | Where-Object { $_.Effect.Value -eq 0 }).Count -gt 0 -and
        @($capacitySamples | Where-Object SourceCardId -EQ 0).Count -gt 0 -and @($capacitySamples | Where-Object { $null -ne $_.Effect.Range }).Count -gt 0 -and
        @($trace.CapacityTests | Where-Object { -not $_.Actual }).Count -gt 0
    if ($RoomCapacityLethal) { $capacityCoverage = $capacityCoverage -and @($trace.Actions | Where-Object ActualOutcome -EQ 3).Count -eq 1 }
}
$companionCoverage = -not $CompanionBoss
if ($CompanionBoss) {
    $companionCoverage = @($trace.CompanionBossActions).Count -gt 0 -and @($trace.RelentlessTriggerRemovals).Count -eq 1 -and
        @($trace.RelentlessTriggerRemovals[0].Before.Triggers | Where-Object RemoveOnRelentlessChange -EQ $true).Count -eq 4 -and
        @($trace.RelentlessTriggerRemovals[0].Actual.Triggers | Where-Object RemoveOnRelentlessChange -EQ $true).Count -eq 0
    if (-not $companionCoverage) { throw 'The requested companion Boss transition/removal did not execute.' }
}
$sentryCoverage = -not ($Sentry -or $SentryLethal)
if ($Sentry -or $SentryLethal) {
    $sentryCoverage = @($trace.Sentries).Count -gt 0 -and @($trace.Sentries | Where-Object { $_.Target.IsBoss }).Count -gt 0
    if ($SentryLethal) {
        $sentryCoverage = $sentryCoverage -and @($trace.Sentries | Where-Object { $_.Target.Health -eq 0 -and $_.ActualTarget.Health -eq 0 }).Count -gt 0
    }
    if (-not $sentryCoverage) { throw 'The requested native Sentry callbacks and Boss target did not execute.' }
}
$abilityCoverage = -not $AbilityCooldown
if ($AbilityCooldown) {
    $abilitySamples = @($trace.AbilityCooldownEffects)
    $abilityCoverage = @($abilitySamples | Where-Object { $_.Effect.Type -eq 'ResetCooldown' }).Count -gt 0 -and
        @($abilitySamples | Where-Object { $_.Effect.Type -eq 'AdjustAbilityCooldown' }).Count -gt 0 -and
        @($abilitySamples | Where-Object { $_.Effect.Type -eq 'RemoveStatus' }).Count -gt 0 -and
        @($abilitySamples | Where-Object { @($_.Before.Units | Where-Object { $_.Ability.HasAbility }).Count -gt 0 }).Count -gt 0
    if (-not $abilityCoverage) { throw 'Requested native ability spawn/cooldown/availability effects did not execute.' }
}
$abilityCacheCoverage = -not $AbilityCache
if ($AbilityCache) {
    $cacheOps = @($trace.AbilityCardOperations)
    $cacheAccesses = @($trace.AbilityCardAccesses)
    $shared = @($cacheAccesses | Group-Object -Property { $_.DataId } | Where-Object {
        @($_.Group | ForEach-Object { $_.UnitId } | Sort-Object -Unique).Count -ge 2 -and
        @($_.Group | ForEach-Object { $_.CardId } | Sort-Object -Unique).Count -eq 1
    })
    $abilityCacheCoverage = @($cacheOps | Where-Object Created -EQ $true).Count -gt 0 -and
        @($cacheOps | Where-Object Created -EQ $false).Count -gt 0 -and $shared.Count -gt 0
    $abilityCacheCoverage = $abilityCacheCoverage -and
        @($cacheOps | Where-Object { $_.Created } | ForEach-Object { $_.Creation.DataId } | Sort-Object -Unique).Count -ge 2
    if (-not $abilityCacheCoverage) { throw 'Requested native cache creation, reuse and shared unit identity did not execute.' }
}
$lifecycleCoverage = -not $AbilityLifecycle
if ($AbilityLifecycle) {
    $lifecycle = @($trace.AbilityLifecycleOperations)
    $labels = @('remove-base', 'remove-missing-noop', 'null-definition-noop', 'ordinary-card-noop',
        'assign-base', 'reassign-base', 'zero-spawn', 'equipment-over-zero', 'equipment-replace-keeps-original',
        'restore-zero-with-activation-cooldown', 'equipment-again', 'direct-replace-clears-original',
        'equipment-before-permanent', 'permanent-equipment-restores-original', 'equipment-restricted-noop',
        'equipment-over-modified-duration', 'restore-raw-activation-cooldown', 'permanent-base',
        'permanent-missing-noop', 'explicit-assign-disabled', 'duplicate-permanent-base')
    $lifecycleCoverage = $lifecycle.Count -eq $labels.Count -and
        @($labels | Where-Object { $_ -notin $lifecycle.Label }).Count -eq 0 -and
        @($lifecycle | Where-Object { $null -eq $_.After -or $_.Difference -or $_.UnsupportedReason }).Count -eq 0
    $lastDisabled = @($lifecycle[-1].After.Context.PermanentlyDisabledAbilities)
    $lifecycleCoverage = $lifecycleCoverage -and $lastDisabled.Count -eq 3 -and $lastDisabled[1] -eq $lastDisabled[2]
    $laterSummons = @($trace.Actions | Where-Object { $_.Before.PlayRules.Cards.Effect -contains 'SpawnMonster' } |
        ForEach-Object { $r = $_; $r.Actual.Spawn.Train.Rooms.Units | Where-Object {
            $_.SpawnerCardId -eq $r.Action.CardInstanceId -and $_.AssetKey -like '*Steward*' -and $null -eq $_.Ability } })
    $lifecycleCoverage = $lifecycleCoverage -and $laterSummons.Count -gt 0
    if (-not $lifecycleCoverage) { throw 'Requested ability lifecycle operations, duplicate permanent disable list or naturally disabled summons did not execute.' }
}
$activationCoverage = -not ($AbilityActivation -or $AbilityActivationX -or $AbilityActivationLethal)
if (-not $activationCoverage) {
    $activations = @($trace.Actions | Where-Object { $_.Action.ActivatorUnitId -gt 0 })
    $activationCoverage = $activations.Count -ge 2 -and
        @($activations | ForEach-Object { $_.Action.ActivatorUnitId } | Sort-Object -Unique).Count -ge 2 -and
        @($activations | ForEach-Object { $_.Action.CardInstanceId } | Sort-Object -Unique).Count -eq 1
    $selfHeals = @($activations | Where-Object {
        $actorId = $_.Action.ActivatorUnitId
        $priorActor = @($_.Before.Spawn.Train.Rooms.Units | Where-Object { $_.Id -eq $actorId })[0]
        $actualActor = @($_.Actual.Spawn.Train.Rooms.Units | Where-Object { $_.Id -eq $actorId })[0]
        $armor = @($priorActor.Statuses | Where-Object { $_.Id -eq 'armor' })[0]
        $damagedHealth = $priorActor.Health - [Math]::Max(0, 3 - [int]$armor.Stacks)
        $actualActor.Health -eq [Math]::Min($priorActor.MaxHealth, $damagedHealth + 1) -and $actualActor.Health -gt $damagedHealth -and
            @($actualActor.Triggers | Where-Object { $_.Kind -eq 'OnPreOwnAbilityActivated' -and $_.HasTriggered }).Count -gt 0 -and
            @($actualActor.Triggers | Where-Object { $_.Kind -eq 'OnOwnAbilityActivated' -and $_.HasTriggered }).Count -gt 0
    })
    $activationCoverage = $activationCoverage -and $selfHeals.Count -gt 0
    if ($AbilityActivationX) {
        $activationCoverage = $activationCoverage -and
            @($activations | Where-Object { $_.Before.Energy -eq 0 }).Count -gt 0 -and
            @($activations | Where-Object { $_.Before.Energy -gt 0 -and $_.Actual.Energy -eq 0 }).Count -gt 0
    }
    if ($AbilityActivationLethal) {
        $activationCoverage = $activationCoverage -and @($activations | Where-Object { $_.ActualOutcome -eq 3 }).Count -eq 1 -and
            @($activations | Where-Object {
                @($_.Actual.Spawn.Train.Rooms.Units | Where-Object { $_.Team -eq 0 -and $_.Health -gt 0 }).Count -lt
                @($_.Before.Spawn.Train.Rooms.Units | Where-Object { $_.Team -eq 0 -and $_.Health -gt 0 }).Count
            }).Count -gt 0
    }
    if (-not $activationCoverage) { throw 'Requested native activation, shared card, own callbacks, self healing or payment/death coverage did not execute.' }
}
$abilityEffectsCoverage = -not $AbilityEffects
if ($AbilityEffects) {
    $effects = @($trace.AbilityEffectOperations)
    $abilityEffectsCoverage = $effects.Count -gt 0 -and @($effects | Where-Object { -not $_.Completed }).Count -eq 0 -and
        @($effects | Where-Object { $_.Effect.Type -eq 'SetUnitAbility' -and @($_.Targets).Count -ge 2 }).Count -gt 0 -and
        @($effects | Where-Object { $_.Effect.Type -eq 'SetUnitAbility' -and $_.RunningTriggerQueue }).Count -gt 0 -and
        @($effects | Where-Object { $_.Effect.Type -eq 'RemoveAbility' -and $_.HasRelicGate }).Count -gt 0
    $activations = @($trace.Actions | Where-Object { $_.Action.ActivatorUnitId -gt 0 })
    $replaced = @($activations | Where-Object {
        $actorId = $_.Action.ActivatorUnitId
        $prior = @($_.Before.Spawn.Train.Rooms.Units | Where-Object { $_.Id -eq $actorId })[0]
        $actual = @($_.Actual.Spawn.Train.Rooms.Units | Where-Object { $_.Id -eq $actorId })[0]
        $prior.Ability.DataId -eq 'c2f6ed7f-18ce-4070-b65f-7dd9f5160063' -and
            $actual.Ability.DataId -eq 'c2f6ed7f-18ce-4070-b65f-7dd9f5160071' -and $actual.Ability.Cooldown -eq 1
    })
    $removed = @($activations | Where-Object {
        $actorId = $_.Action.ActivatorUnitId
        $prior = @($_.Before.Spawn.Train.Rooms.Units | Where-Object { $_.Id -eq $actorId })[0]
        $actual = @($_.Actual.Spawn.Train.Rooms.Units | Where-Object { $_.Id -eq $actorId })[0]
        $prior.Ability.DataId -eq 'c2f6ed7f-18ce-4070-b65f-7dd9f5160071' -and $null -eq $actual.Ability
    })
    $disabled = @($activations[-1].Actual.Spawn.Train.Context.PermanentlyDisabledAbilities)
    $abilityEffectsCoverage = $abilityEffectsCoverage -and $activations.Count -eq 4 -and $replaced.Count -eq 2 -and $removed.Count -eq 2 -and
        $disabled.Count -eq 2 -and $disabled[0] -eq 'c2f6ed7f-18ce-4070-b65f-7dd9f5160072' -and $disabled[1] -eq $disabled[0]
    if (-not $abilityEffectsCoverage) { throw 'Requested multi-target ability effects, queued replacement or cached self replacement/removal did not execute.' }
}
$equipmentAbilityCoverage = -not $EquipmentAbilities
if ($TriggerRepeats) {
    $repeatBatches = @($trace.TriggerRepeatBatches)
    $repeatLabels = @('batch-first', 'batch-once-spent', 'batch-silenced', 'batch-fire-blocked',
        'zero-first', 'zero-after', 'negative-first', 'negative-after')
    if ($repeatBatches.Count -ne 8 -or @($repeatLabels | Where-Object { $_ -notin $repeatBatches.Label }).Count -gt 0 -or
        @($repeatBatches | Where-Object { $null -eq $_.After }).Count -gt 0) {
        throw 'Requested native trigger repeat batches did not complete.'
    }
}
if ($PhysicalSpawnPoints) {
    $physical = $trace.Stages[0].Before.Context.SpawnPoints
    if ($null -eq $physical -or @($physical.Groups).Count -eq 0 -or @($trace.PhysicalCompactions).Count -eq 0 -or
        @($trace.PhysicalCompactions | Where-Object { $null -ne $_.Error -or $null -eq $_.After }).Count -gt 0) {
        throw 'Requested complete battle physical point state was not captured.'
    }
}
if ($SpawnPoints) {
    $operations = @($trace.SpawnPointOperations)
    if ($operations.Count -ne 21 -or @($operations | Where-Object {
        -not $_.Completed -or $null -eq $_.After -or $null -ne $_.Difference -or @($_.Queries).Count -ne 3
    }).Count -gt 0) {
        throw 'Requested native physical spawn point operations did not complete.'
    }
}
if ($MultiSummon -and -not $MultiSummonFresh -and -not $MultiSummonAdditional -and -not $MultiSummonPool) {
    $births = @($trace.UnitBirths)
    $clones = @($trace.DetachedCardClones)
    if ($births.Count -lt 7 -or $clones.Count -lt 5 -or
        @($births | Where-Object { -not $_.Completed -or $null -ne $_.Difference }).Count -gt 0 -or
        @($clones | Where-Object { -not $_.Completed -or $null -ne $_.Difference }).Count -gt 0 -or
        @($births | Where-Object { $_.IsCardless -and $_.SpawnerCardId -gt 0 }).Count -lt 5) {
        throw 'Requested native repeated births, copied source references and cardless Rally did not complete.'
    }
}
if ($MultiSummonAdditional) {
    $kinds = @($trace.UnitBirths | ForEach-Object { $_.Definition.SpawnUnit.AssetKey } | Sort-Object -Unique)
    if ($kinds.Count -lt 2 -or @($trace.SpawnUpgrades).Count -lt 7) {
        throw 'Additional character births and extra upgrades were not exercised.'
    }
}
if ($MultiSummonPool) {
    if (@($trace.PoolSummonSelections).Count -lt 7 -or @($trace.PoolSummonSelections | Where-Object {
        -not $_.Completed -or $null -ne $_.Difference
    }).Count -gt 0) { throw 'Pooled character sampling and complete RNG contexts were not verified.' }
}
if ($MultiSummonFresh) {
    if (-not $missingFresh -and @($trace.FreshSpawners).Count -lt 7 -or @($trace.SpawnUpgrades).Count -lt 7 -or @($trace.GlobalStandbyChecks).Count -eq 0) {
        throw 'Fresh fallback setup, extra upgrades and global standby checks were not exercised.'
    }
    if ($missingFresh -and @($trace.UnitBirths | Where-Object { $_.SpawnerCardId -eq 0 -and $_.IsCardless }).Count -eq 0) {
        throw 'Known missing fallback sources and cardless births were not exercised.'
    }
    if ($MultiSummonFreshDeaths -and @($trace.GlobalStandbyChecks | Where-Object {
        @($_.Before.OtherPiles | ForEach-Object { $_.UnitConditions | Where-Object { $_.Ready } }).Count -gt 0
    }).Count -eq 0) { throw 'Fresh-source bound-unit deaths were not exercised.' }
}
if ($MultiSummonUpgrade) {
    $extra = @($trace.SpawnUpgrades)
    if ($extra.Count -lt 4 -or @($extra | Where-Object { -not $_.Completed -or $null -ne $_.Difference }).Count -gt 0 -or
        @($extra | Where-Object { $_.SourceAdded }).Count -eq 0) {
        throw 'Requested native extra spawn upgrades and source writes did not complete.'
    }
    if ($MultiSummonUpgradeUnique -and @($extra | Where-Object { -not $_.SourceAdded }).Count -eq 0) {
        throw 'Unique extra spawn upgrade did not exercise a rejected duplicate source write.'
    }
    if ($MultiSummonUpgradeRestricted -and @($extra | Where-Object {
        $old = @($_.Before.Units | Where-Object Id -eq $_.UnitId)
        $new = @($_.AfterDirect.Units | Where-Object Id -eq $_.UnitId)
        $_.SourceAdded -and $old.Count -eq 1 -and $new.Count -eq 1 -and $old[0].Size -eq $new[0].Size
    }).Count -eq 0) {
        throw 'Restricted extra spawn upgrade did not exercise a rejected unit change followed by a source write.'
    }
}
if ($MultiSummonZero) {
    $zeroDispatches = @($trace.RallyTriggers | Where-Object { $_.Label -eq 'multi-summon-zero-dispatch' })
    if ($zeroDispatches.Count -ne 1 -or $zeroDispatches[0].TriggerCount -ne 0 -or -not $zeroDispatches[0].Completed) {
        throw 'Requested native zero-count Rally dispatch did not execute.'
    }
}

if ($LethalRally) {
    $lethalSummons = @($trace.Actions | Where-Object { $_.ActualOutcome -eq 3 -and $_.Action.PlayerPosition -ge 0 })
    if ($lethalSummons.Count -ne 1 -or @($trace.RallyPhases | Where-Object { $_.Before.Context.AllScenarioBossesDead -eq $false -and
        $_.After.Context.AllScenarioBossesDead -eq $true }).Count -ne 1 -or @($trace.RallyTriggers | Where-Object {
        $_.Actor.EndsBattleOnDeath -and $_.AfterActor.DeathState.HasFinishedDying -and -not $_.AfterActor.DeathState.IsBeingRemoved }).Count -ne 1 -or
        $lethalSummons[0].Actual.Spawn.Train.Context.LastSpawnedUnitId -le 0) {
        throw 'Requested native lethal Rally, deferred Boss removal and terminal summon did not execute.'
    }
}
if ($RallyTriggers) {
    $rallyLabels = @('grow-first-player-by-two', 'zero-player-growth', 'remove-one-player-troop', 'first-enemy-horde', 'grow-enemy-by-two')
    if (@($trace.RallyOperations).Count -ne 5 -or @($rallyLabels | Where-Object { $_ -notin $trace.RallyOperations.Label }).Count -gt 0 -or
        @($trace.RallyPhases).Count -ne 10 -or @($trace.RallyTriggers).Count -ne 12 -or
        @($trace.RallyOperations | Where-Object { $null -eq $_.AfterApi -or $null -eq $_.After -or $_.QueueAfter -ne 0 }).Count -gt 0) {
        throw 'Requested native Rally and Horde birth/growth operations did not complete.'
    }
}
if ($DyingHordeUpgrades) {
    $operations = @($trace.DyingHordeUpgradeOperations)
    if ($operations.Count -ne 5 -or @($operations | Where-Object { $null -eq $_.After -or
        $null -eq $_.AfterActor -or $_.QueueAfter -ne 0 -or -not $_.HasFinishedDying -or
        -not $_.IsBeingRemoved -or -not $_.IsSacrifice }).Count -gt 0 -or @($trace.DyingUpgrades).Count -ne 7 -or
        @($trace.DyingUpgrades | Where-Object { -not $_.Completed -or $null -eq $_.Actual -or $null -eq $_.ActualUnits -or $_.Interactions.Count -gt 0 }).Count -gt 0) {
        throw 'Requested dying Horde upgrade/removal operations did not complete.'
    }
}
if ($HordeUpgrades) {
    $operations = @($trace.HordeUpgradeOperations)
    if ($operations.Count -ne 16 -or @($operations | Where-Object { $null -eq $_.AfterApi -or
        $null -eq $_.After -or $null -eq $_.AfterActor -or $_.QueueAfter -ne 0 }).Count -gt 0) {
        throw 'Requested ordered Horde runtime upgrade operations did not complete.'
    }
}
if ($HordeDeath) {
    $operations = @($trace.HordeDeathOperations)
    $labels = @('queued-final-player', 'dying-final-enemy', 'dying-final-player')
    if ($operations.Count -ne 3 -or @($labels | Where-Object { $_ -notin $operations.Label }).Count -gt 0 -or
        @($operations | Where-Object { $null -eq $_.After -or $null -eq $_.AfterActor -or $_.QueueAfter -ne 0 -or
            -not $_.HasFinishedDying -or -not $_.IsSacrifice -or -not $_.IsBeingRemoved }).Count -gt 0) {
        throw 'Requested queued and repeated Horde death operations did not complete.'
    }
}
if ($HordeRemoval) {
    $removals = @($trace.HordeRemovalOperations)
    if (@($trace.HordeRemovalCasts).Count -ne 1 -or $null -eq $trace.HordeRemovalCasts[0].After -or $removals.Count -ne 9 -or @($removals | Where-Object { $null -eq $_.After -or $null -eq $_.AfterDrain -or
        $null -eq $_.AfterActor -or $null -eq $_.AfterDrainActor -or $_.QueueAfterDrain -ne 0 }).Count -gt 0) {
        throw 'Requested native Horde final-removal operations did not complete.'
    }
}
if ($HarvestTriggers) {
    $operations = @($trace.HarvestOperations)
    $labels = @('enemy-horde-health-lethal', 'player-horde-health-lethal', 'enemy-horde-damage-lethal', 'player-horde-damage-lethal')
    if ($operations.Count -ne 4 -or @($labels | Where-Object { $_ -notin $operations.Label }).Count -gt 0 -or
        @($operations | Where-Object { $null -eq $_.After -or $null -eq $_.AfterActor -or $_.QueueAfter -ne 0 }).Count -gt 0) {
        throw 'Requested native Harvest deaths did not complete.'
    }
}
if ($HordeStatuses) {
    $hordeOperations = @($trace.HordeStatusOperations)
    $hordeLabels = @('cooldown-ready', 'troops-grow', 'zero-add', 'negative-add', 'troops-remove',
        'damage-casualty', 'troops-regrow', 'maxhp-casualty')
    if ($hordeOperations.Count -ne 8 -or @($hordeLabels | Where-Object { $_ -notin $hordeOperations.Label }).Count -gt 0 -or
        @($hordeOperations | Where-Object { $null -eq $_.After -or $null -eq $_.AfterDrain -or
            $_.QueueAfterDrain -ne 0 -or $_.RunningBeforeDrain -or $_.ActorPreviewBeforeDrain -or $_.SavePreviewBeforeDrain }).Count -gt 0) {
        throw 'Requested native Horde status operations did not complete.'
    }
}
if ($HordeMerge) {
    $mergeOperations = @($trace.HordeMergeOperations)
    $mergeLabels = @('preview-player-clone', 'preview-player-bump-merge-with-equipment',
        'preview-enemy-ordinary-merge', 'preview-enemy-self-merge',
        'player-clone-adds-one-without-birth', 'player-bump-merge-mixed-equipment',
        'enemy-clone-adds-one-without-birth', 'enemy-ordinary-merge-different-definition',
        'null-source-merge', 'null-target-merge', 'non-horde-source-merge', 'non-horde-target-merge',
        'enemy-self-merge-removes-positive-hp-recipient', 'cross-team-direct-merge',
        'immune-target-still-removes-source', 'immune-clone-no-growth', 'null-source-clone')
    if ($mergeOperations.Count -ne $mergeLabels.Count -or @($mergeLabels | Where-Object { $_ -notin $mergeOperations.Label }).Count -gt 0 -or
        @($trace.HordeMergeSelections).Count -ne 5 -or @($mergeOperations | Where-Object {
            $null -eq $_.AfterApi -or $null -eq $_.After -or $_.QueueAfter -ne 0 -or
            $_.QueueAfterApi -ne @($_.Queued).Count -or ($_.SourceId -gt 0 -and ($null -eq $_.SourceAfter -or $null -eq $_.SourceAfterDrain)) -or
            ($_.Before.Preview -and ($null -eq $_.PrimaryBeforePreview -or $null -eq $_.PrimaryAfterPreview))
        }).Count -gt 0) {
        throw 'Requested native Horde merge/clone operations did not complete.'
    }
}
if ($Bump) {
    $bumpOperations = @($trace.BumpOperations)
    $bumpLabels = @('player-up', 'player-down', 'player-partial-up-ignores-range', 'player-pyre-blocked',
        'player-zero', 'player-clamped-down', 'enemy-up', 'enemy-down', 'enemy-rooted', 'enemy-immobile-before-rooted',
        'enemy-loop-up-to-bottom', 'enemy-room-multiple-targets', 'enemy-to-top-before-merge',
        'player-cross-room-horde-merge', 'enemy-cross-room-horde-merge', 'enemy-into-pyre',
        'enemy-full-room-blocked', 'enemy-partial-full-room')
    if ($bumpOperations.Count -ne $bumpLabels.Count -or @($bumpLabels | Where-Object { $_ -notin $bumpOperations.Label }).Count -gt 0 -or
        @($bumpOperations | Where-Object { $null -eq $_.After -or $null -eq $_.TargetsAfter -or $_.QueueAfter -ne 0 }).Count -gt 0) {
        throw 'Requested native paid Bump operations did not complete.'
    }
}
if ($UnitClone) {
    $cloneLabels = @('null-source', 'invalid-room', 'ordinary-front', 'ordinary-back-cardless', 'wounded-buffed-excluded',
        'negative-damage-buff', 'equipped-source', 'runtime-equipment-ability', 'clone-of-clone', 'cardless-source',
        'full-room-card-allocation', 'selected-no-adjacent', 'horde-no-birth')
    $clones = @($trace.UnitCloneOperations)
    if ($clones.Count -ne $cloneLabels.Count -or @($cloneLabels | Where-Object { $_ -notin $clones.Label }).Count -gt 0 -or
        @($clones | Where-Object { $null -eq $_.AfterApi -or $null -eq $_.After -or $_.QueueAfter -ne 0 }).Count -gt 0) {
        throw 'Requested native ordinary clone boundaries did not complete.'
    }
}
if ($UnitCopy) {
    $copyLabels = @('zero-targeted', 'negative-room', 'single-targeted', 'multiple-targeted', 'clone-of-clone-ignores-range',
        'status-before-copy', 'ability-before-copy', 'equipped-source', 'cardless-source', 'room-multiple-targets',
        'full-room-two-card-allocations', 'selected-last-no-allocation', 'partial-room-many-targets', 'horde-no-birth')
    $copies = @($trace.UnitCopyOperations)
    if ($copies.Count -ne $copyLabels.Count -or @($copyLabels | Where-Object { $_ -notin $copies.Label }).Count -gt 0 -or
        @($copies | Where-Object { $null -eq $_.After }).Count -gt 0) {
        throw 'Requested native paid unit-copy operations did not complete.'
    }
}
if ($SpawnEnchant) {
    if (@($trace.SpawnEnchantments).Count -lt 3 -or @($trace.UnitBirths).Count -lt 3 -or
        @($trace.SpawnEnchantments | Where-Object { -not $_.Completed -or $null -eq $_.After -or $null -ne $_.Difference -or $null -ne $_.UnsupportedReason }).Count -gt 0) {
        throw 'Requested native spawning enchant phases did not complete.'
    }
}
if ($HeroCopy) {
    $heroCopyLabels = @('zero-targeted', 'negative-room', 'raw-source-no-stats', 'copy-live-stats', 'multiple-targeted',
        'clone-of-copy-ignores-range', 'status-before-copy', 'ability-before-copy', 'equipped-no-stats', 'equipped-live-stats',
        'room-multiple-targets', 'horde-selected-last', 'full-room-no-allocations', 'partial-room-many-targets',
        'mixed-mask-enemy-to-player', 'mixed-mask-player')
    $heroCopies = @($trace.HeroCopyOperations)
    if ($heroCopies.Count -ne $heroCopyLabels.Count -or @($heroCopyLabels | Where-Object { $_ -notin $heroCopies.Label }).Count -gt 0 -or
        @($heroCopies | Where-Object { $null -eq $_.After }).Count -gt 0) {
        throw 'Requested native hero and mixed-team copying operations did not complete.'
    }
}
$equipmentActivations = @()
if ($EquipmentAbilities) {
    $skillB = 'c2f6ed7f-18ce-4070-b65f-7dd9f5160074'
    $skillC = 'c2f6ed7f-18ce-4070-b65f-7dd9f5160075'
    $initial = @($trace.InitialAbilitySpawns)
    $initialCoverage = $initial.Count -eq 1 -and $null -ne $initial[0].Actual -and -not $initial[0].Difference -and $initial[0].Predicted.Supported
    if ($initialCoverage) {
        $sourceId = $initial[0].Action.CardInstanceId
        $source = $initial[0].Before.Spawn.Train.Context.CardInstances | Where-Object InstanceId -EQ $sourceId
        $born = $initial[0].Actual.Spawn.Train.Rooms.Units | Where-Object SpawnerCardId -EQ $sourceId
        $initialCoverage = $source.Permanent.Upgrades.AbilityUpgrade.Definition.DataId -contains $skillB -and
            $source.Temporary.Upgrades.AbilityUpgrade.Definition.DataId -contains $skillC -and
            $born.Ability.DataId -eq $skillC -and $born.Ability.Cooldown -eq 6 -and $born.Ability.CooldownAtSpawn -eq 2 -and -not $born.Ability.FromEquipment
    }
    $equipmentOps = @($trace.EquipmentOperations)
    $restoration = @($equipmentOps | Where-Object {
        $entry = $_
        $prior = $entry.Before.Units | Where-Object Id -EQ $entry.UnitId
        $actual = $entry.After.Units | Where-Object Id -EQ $entry.UnitId
        $entry.Remove -and $prior.Ability.DataId -eq $skillB -and $prior.Ability.FromEquipment -and $prior.Ability.PreviousDataId -eq $skillC -and
            $actual.Ability.DataId -eq $skillC -and $actual.Ability.Cooldown -eq 6 -and -not $actual.Ability.FromEquipment -and
            $null -eq $actual.Ability.PreviousDataId -and @($actual.Statuses | Where-Object { $_.Id -eq 'cooldown' -and $_.Stacks -eq 6 }).Count -eq 1
    })
    $equipmentCoverageBothContexts = @($equipmentOps | Where-Object DeferAbilityCallbacks -EQ $true).Count -gt 0 -and
        @($equipmentOps | Where-Object DeferAbilityCallbacks -EQ $false).Count -gt 0
    $direct = @($trace.DirectUnitUpgrades)
    $directLabels = @('ability-keep-existing', 'ability-remove-nonmatching', 'ability-direct-clears-equipment-history',
        'ability-remove-current', 'ability-add-to-empty', 'ability-remove-added', 'ability-explicit-reassign-disabled')
    $directCoverage = $direct.Count -eq $directLabels.Count -and @($directLabels | Where-Object { $_ -notin $direct.Label }).Count -eq 0 -and
        @($direct | Where-Object { $null -eq $_.After -or $_.Difference -or $_.UnsupportedReason }).Count -eq 0
    $equipmentActivations = @($trace.Actions | Where-Object {
        $entry = $_
        $entry.Action.ActivatorUnitId -gt 0 -and @($entry.Before.Spawn.Train.Rooms.Units | Where-Object {
            $_.Id -eq $entry.Action.ActivatorUnitId -and $_.Ability.FromEquipment -and $_.Ability.DataId -eq $skillB
        }).Count -eq 1
    })
    $keptBirths = @($trace.Actions | Where-Object {
        $entry = $_
        $source = $entry.Before.Spawn.Train.Context.CardInstances | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
        $baseRule = $entry.Before.PlayRules.Cards | Where-Object DataId -EQ $source.DataId
        $baseRule.SpawnUnit.Ability.HasAbility -and @($source.Permanent.Upgrades | Where-Object {
            $_.DoNotReplaceExistingAbility -and $_.AbilityUpgrade.Definition.DataId -eq $skillB
        }).Count -gt 0 -and @($entry.Actual.Spawn.Train.Rooms.Units | Where-Object {
            $_.Id -ge $entry.Before.Spawn.NextUnitId -and $_.SpawnerCardId -eq $entry.Action.CardInstanceId -and $_.Ability.DataId -eq $baseRule.SpawnUnit.Ability.DataId
        }).Count -gt 0
    })
    $disabledBirths = @($trace.Actions | Where-Object {
        $entry = $_
        $source = $entry.Before.Spawn.Train.Context.CardInstances | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
        $baseRule = $entry.Before.PlayRules.Cards | Where-Object DataId -EQ $source.DataId
        $source.Permanent.Upgrades.AbilityUpgrade.Definition.DataId -contains $skillC -and
            $baseRule.SpawnUnit.Ability.HasAbility -and
            $entry.Before.Spawn.Train.Context.PermanentlyDisabledAbilities -contains $skillC -and
            @($entry.Actual.Spawn.Train.Rooms.Units | Where-Object {
                $_.Id -ge $entry.Before.Spawn.NextUnitId -and $_.SpawnerCardId -eq $entry.Action.CardInstanceId -and $null -eq $_.Ability -and
                    @($_.Triggers | Where-Object { $_.Origin.UpgradeId -eq 'UnitAbilityCommonData' }).Count -eq 0
            }).Count -gt 0
    })
    $callbacks = @($trace.CharacterCallbackFires)
    $callbackKinds = @('OnPreOwnAbilityActivated', 'OnOwnAbilityActivated', 'OnUnitAbilityAvailable', 'OnUnitAbilityUnavailable')
    $callbackCoverage = @($callbackKinds | Where-Object { $_ -notin $callbacks.Kind }).Count -eq 0 -and
        @($callbacks | Where-Object { -not $_.Completed -or $null -eq $_.Actual -or $null -eq $_.ActualUnit }).Count -eq 0
    $equipmentAbilityCoverage = $initialCoverage -and $restoration.Count -gt 0 -and $equipmentCoverageBothContexts -and $directCoverage -and
        $equipmentActivations.Count -ge 2 -and $keptBirths.Count -gt 0 -and $disabledBirths.Count -gt 0 -and $callbackCoverage
    if (-not $equipmentAbilityCoverage) {
        throw "Equipment ability coverage incomplete: initial=$initialCoverage restorations=$($restoration.Count) contexts=$equipmentCoverageBothContexts direct=$directCoverage activations=$($equipmentActivations.Count) keptBirths=$($keptBirths.Count) disabledBirths=$($disabledBirths.Count) callbacks=$callbackCoverage"
    }
}
$triggeredSummonCoverage = -not $TriggeredSummons
$triggeredSummonBirths = 0
$triggeredSummonZeroBirths = 0
$triggeredSummonDyingSources = 0
if ($TriggeredSummons) {
    $summonRecords = @($trace.TriggeredSummons)
    foreach ($record in $summonRecords) {
        $born = $record.After.Context.NextUnitId - $record.Before.Context.NextUnitId
        $triggeredSummonBirths += $born
        if ($born -eq 0) { $triggeredSummonZeroBirths++ }
        if ($record.ActorBefore.Health -le 0) { $triggeredSummonDyingSources++ }
    }
    $complete = $summonRecords.Count -ge 2 -and @($summonRecords | Where-Object {
        -not $_.Completed -or $null -ne $_.Error -or -not $_.QueueRunning -or $null -eq $_.After -or $null -eq $_.RuleAfter
    }).Count -eq 0
    $deathPhases = @($trace.TriggeredSummonDamages | Where-Object { -not $_.QueueRunning -and @($_.PendingDeathsBefore).Count -gt 0 })
    $triggeredSummonCoverage = $complete -and $triggeredSummonBirths -ge 2 -and
        $(if ($TriggeredSummonsDeath) { $triggeredSummonDyingSources -ge 2 -and $deathPhases.Count -ge 2 } else { $triggeredSummonZeroBirths -ge 2 })
    if (-not $triggeredSummonCoverage) {
        throw "Triggered summon coverage incomplete: complete=$complete births=$triggeredSummonBirths zero=$triggeredSummonZeroBirths dying=$triggeredSummonDyingSources deathPhases=$($deathPhases.Count)"
    }
}
$triggeredEquipmentCoverage = -not $TriggeredSummonsEquipment
$triggeredEquipmentApplications = 0
$queuedEquipmentAttachments = 0
$ownedSummonApplications = 0
$ownedSummonBirths = 0
$ownedSummonDyingBirths = 0
$ownedSummonRevivedBirths = 0
$summonRevivalHostSamples = 0
$summonRevivalChildSamples = 0
if ($TriggeredSummonsEquipment) {
    $equipped = @($trace.TriggeredSummons | Where-Object {
        @($_.ActorBefore.EquipmentCards).Count -gt 0 -and $_.After.Context.NextUnitId -gt $_.Before.Context.NextUnitId
    })
    $triggeredEquipmentApplications = $equipped.Count
    $queuedEquipmentAttachments = @($trace.EquipmentOperations | Where-Object { -not $_.Remove -and $_.QueueRunning }).Count
    $triggeredEquipmentCoverage = $equipped.Count -gt 0 -and
        $(if ($TriggeredSummonsFresh) { $queuedEquipmentAttachments -eq 0 } else { $queuedEquipmentAttachments -ge 2 })
    if (-not $triggeredEquipmentCoverage) {
        throw "Triggered equipment coverage incomplete: applications=$triggeredEquipmentApplications queuedAttachments=$queuedEquipmentAttachments"
    }
}
if ($TriggeredSummonsEquipmentOwned) {
    $owned = @($trace.TriggeredSummons | Where-Object { $_.EquipmentCardId -gt 0 })
    $ownedSummonApplications = $owned.Count
    $ownedBirths = @($owned | Where-Object { $_.After.Context.NextUnitId -gt $_.Before.Context.NextUnitId })
    $ownedSummonBirths = $ownedBirths.Count
    $ownedSummonDyingBirths = @($ownedBirths | Where-Object { $_.ActorBefore.Health -le 0 }).Count
    $ownedSummonRevivedBirths = @($ownedBirths | Where-Object { $_.Kind -eq 'OnDeath' -and $_.ActorBefore.Health -gt 0 -and
        $_.ActorBefore.Id -in @($trace.Revivals.ActorId) }).Count
    if ($owned.Count -eq 0 -or $ownedBirths.Count -eq 0 -or @($owned | Where-Object {
        $_.SourceCardId -ne $_.ActorBefore.SpawnerCardId -or $_.SourceCardId -eq $_.EquipmentCardId -or
        $_.RuleBefore.HasParentCard -or $null -eq $_.Queued -or $_.EffectIndex -lt 0
    }).Count -gt 0 -or ($TriggeredSummonsDeath -and -not $TriggeredSummonsRevival -and $ownedSummonDyingBirths -eq 0) -or
        ($TriggeredSummonsRevival -and $ownedSummonRevivedBirths -eq 0)) {
        throw 'Equipment-owned native summon source, queue or live/death coverage incomplete.'
    }
}
if ($Revival) {
    $revivals = @($trace.Revivals)
    $operations = @($trace.RevivalOperations)
    if ($operations.Count -ne 7 -or $revivals.Count -lt 7 -or
        @($revivals | Where-Object QueueRunning).Count -lt 2 -or
        @($revivals | Where-Object { -not $_.Completed -or $null -eq $_.After -or $null -ne $_.Error }).Count -gt 0 -or
        @($operations | Where-Object { $null -eq $_.After -or $_.QueueAfter -ne 0 }).Count -gt 0) {
        throw 'Revival coverage requires seven native operations and both direct and queued revivals.'
    }
}
if ($TriggeredSummonsRevival) {
    $revivals = @($trace.Revivals)
    $equippedHostRevivals = @($revivals | Where-Object { @($_.AfterActor.EquipmentCards).Count -gt 0 -and
        @($_.AfterActor.Triggers | ForEach-Object { $_.Effects } | Where-Object { $null -ne $_.Summon }).Count -gt 0 })
    $childRevivals = @($revivals | Where-Object { @($_.AfterActor.Statuses | Where-Object Id -eq 'cardless').Count -gt 0 -and
        @($_.AfterActor.Triggers | ForEach-Object { $_.Effects } | Where-Object { $null -ne $_.Summon }).Count -eq 0 })
    $summonRevivalHostSamples = $equippedHostRevivals.Count
    $summonRevivalChildSamples = $childRevivals.Count
    if ($equippedHostRevivals.Count -lt 2 -or $childRevivals.Count -eq 0 -or $ownedSummonRevivedBirths -eq 0 -or
        @($revivals | Where-Object { -not $_.Completed -or $null -eq $_.After -or $null -ne $_.Error -or $_.AutomaticQueueDeferrals -ne 0 }).Count -gt 0) {
        throw 'Triggered summon revival requires equipped host/child revivals and live-source death births without queue deferrals.'
    }
}
$purifyCoverage = -not $Purify
if ($Purify) {
    $purifySamples = @($trace.TriggeredStatuses)
    $blockedPositive = 0; $blockedZero = 0; $blockedNegative = 0; $zeroPurifyClears = 0; $positivePurifyClears = 0
    foreach ($sample in $purifySamples) {
        $status = $sample.Effect.Action.Statuses[0]
        foreach ($unit in @($sample.BeforeUnits | Where-Object { $_.Id -in $sample.Targets })) {
            if (@($unit.Statuses | Where-Object { $_.Id -eq 'purify' -and $_.Stacks -gt 0 }).Count -gt 0) {
                if ($status.Stacks -gt 0) { $blockedPositive++ } elseif ($status.Stacks -eq 0) { $blockedZero++ } else { $blockedNegative++ }
            } elseif ($status.Id -eq 'purify' -and @($unit.Statuses | Where-Object { $_.Id -ne 'purify' -and $_.Stacks -gt 0 }).Count -gt 0) {
                if ($status.Stacks -eq 0) { $zeroPurifyClears++ } elseif ($status.Stacks -gt 0) { $positivePurifyClears++ }
            }
        }
    }
    $purifyRemovals = @($trace.AbilityCooldownEffects | Where-Object { $_.Effect.Type -eq 'RemoveStatus' -and $_.Effect.Statuses.Id -contains 'purify' })
    $purifyPaidActions = @($trace.Actions | Where-Object {
        $sample = $_
        $source = $sample.Before.Spawn.Train.Context.CardInstances | Where-Object InstanceId -EQ $sample.Action.CardInstanceId
        $rule = $sample.Before.PlayRules.Cards | Where-Object DataId -EQ $source.DataId
        $rule.Effects.Statuses.Id -contains 'purify'
    })
    $purifyCoverage = $trace.ModifierScenario -in @('purify', 'purify-queues') -and $blockedPositive -gt 0 -and $blockedZero -gt 0 -and $blockedNegative -gt 0 -and
        $zeroPurifyClears -gt 0 -and $positivePurifyClears -gt 0 -and $purifyRemovals.Count -gt 0 -and $purifyPaidActions.Count -gt 0 -and
        @($purifySamples | Where-Object { -not $_.Completed -or $null -eq $_.Actual -or @($_.Interactions).Count -gt 0 }).Count -eq 0 -and
        @($purifyRemovals | Where-Object { -not $_.Completed -or $null -eq $_.After }).Count -eq 0 -and
        @($trace.StatusCallbackFires).Count -gt 0 -and @($trace.StatusCallbackFires).Count -eq @($trace.StatusCallbacks).Count
    if (-not $purifyCoverage) {
        throw "Purify coverage incomplete: blocked=$blockedPositive/$blockedZero/$blockedNegative clears=$positivePurifyClears/$zeroPurifyClears removals=$($purifyRemovals.Count) paid=$($purifyPaidActions.Count)"
    }
}
$purifyQueueCoverage = -not $PurifyQueues
if ($PurifyQueues) {
    $admissions = @($trace.PurifyQueueAdmissions)
    $rejectedKinds = @($admissions | Where-Object { $_.Purified -and $_.Overload -eq 'Character' -and $_.QueueAfter -eq $_.QueueBefore } | ForEach-Object Kind | Sort-Object -Unique)
    $requiredKinds = @('OnSpawn', 'OnUnscaledSpawn', 'AfterSpawnEnchant', 'CardMonsterPlayed', 'OnSentry', 'OnDeath', 'OnAnyUnitDeathOnFloor')
    $purifyQueueCoverage = $admissions.Count -gt 0 -and @($admissions | Where-Object { -not $_.Completed -or @($_.Interactions).Count -gt 0 }).Count -eq 0 -and
        @($requiredKinds | Where-Object { $_ -notin $rejectedKinds }).Count -eq 0
    if (-not $purifyQueueCoverage) { throw "Purify queue coverage incomplete: rejected=$($rejectedKinds -join ',')" }
}
$incantCoverage = -not $Incant
if ($Incant) {
    $incantPhases = @($trace.IncantPhases)
    $incantFires = @($trace.IncantTriggers)
    $incantRequests = @($trace.PurifyQueueAdmissions | Where-Object { $_.Kind -eq 'CardSpellPlayed' })
    $incantRejected = @($incantRequests | Where-Object { $_.Purified -and $_.QueueBefore -eq $_.QueueAfter })
    $emptySpellPlays = 0; $silencedFires = 0; $visibleFires = 0
    foreach ($action in $trace.Actions) {
        $source = $action.Before.Spawn.Train.Context.CardInstances | Where-Object InstanceId -EQ $action.Action.CardInstanceId
        $rule = $action.Before.PlayRules.Cards | Where-Object DataId -EQ $source.DataId
        if ($rule.CardType -eq 'Spell' -and -not $rule.IsAnyAbility -and $rule.Effect -eq 'Null') { $emptySpellPlays++ }
    }
    foreach ($fire in $incantFires) {
        if (@($fire.Actor.Statuses | Where-Object { $_.Id -eq 'silenced' -and $_.Stacks -gt 0 }).Count -gt 0) { $silencedFires++ }
        elseif (@($fire.AfterActor.Triggers | Where-Object { $_.Kind -eq 'CardSpellPlayed' -and -not $_.IgnoreSilence -and $_.HasTriggered }).Count -gt 0) { $visibleFires++ }
    }
    $incantCoverage = $trace.ModifierScenario -eq $(if ($IncantRelics) { 'incant-relic-combined' } elseif ($IncantRelic) { 'incant-relic' } elseif ($IncantThresholds) { 'incant-thresholds' } else { 'incant' }) -and $incantPhases.Count -ge 10 -and $incantFires.Count -gt 5 -and
        @($incantPhases | Where-Object { -not $_.Completed -or $null -eq $_.After -or $null -ne $_.Difference }).Count -eq 0 -and
        @($incantFires | Where-Object { -not $_.Completed -or $null -eq $_.After -or $null -eq $_.AfterActor -or $null -ne $_.Difference }).Count -eq 0 -and
        @($incantFires | Where-Object { $_.Actor.Team -eq 1 }).Count -gt 0 -and @($incantFires | Where-Object { $_.Actor.Team -eq 0 }).Count -gt 0 -and
        $incantRejected.Count -gt 0 -and $emptySpellPlays -gt 0 -and $silencedFires -gt 0 -and $visibleFires -gt 0
    if (-not $incantCoverage) { throw "Incant coverage incomplete: phases=$($incantPhases.Count) fires=$($incantFires.Count) rejected=$($incantRejected.Count) empty=$emptySpellPlays silenced=$silencedFires visible=$visibleFires" }
}
if ($SpawnStatusRelics) {
    $births = @($trace.RelicSpawnStatuses)
    $spawnRelicPlayers = 0
    $spawnRelicEnemies = 0
    $spawnRelicFirsts = 0
    $spawnRelicSkips = 0
    $cardlessUpgradeApplied = 0
    $cardlessCloneSkipped = 0
    $spawnRelicTurns = [Collections.Generic.HashSet[int]]::new()
    $cardlessUpgradeId = $null
    if ($SpawnStatusRelicsClones -and $births.Count -gt 0) {
        $cardlessUpgradeRelics = @($births[0].Before.Context.Relics | Where-Object AssetKey -EQ 'PyreHeartSon')
        if ($cardlessUpgradeRelics.Count -eq 1 -and $cardlessUpgradeRelics[0].CardModifiers.Count -eq 1 -and
            $cardlessUpgradeRelics[0].CardModifiers[0].ApplyToCardlessSpawns) {
            $cardlessUpgradeId = $cardlessUpgradeRelics[0].CardModifiers[0].Upgrade.DataId
        }
    }
    foreach ($birth in $births) {
        $actor = @($birth.After.Units | Where-Object { $_.Id -eq $birth.UnitId })
        $beforeActor = @($birth.Before.Units | Where-Object { $_.Id -eq $birth.UnitId })
        if ($actor.Count -ne 1) { throw 'Native relic birth actor is missing or duplicated.' }
        if ($beforeActor.Count -ne 1) { throw 'Native relic birth before-state actor is missing or duplicated.' }
        if ($SpawnStatusRelicsClones -and $cardlessUpgradeId -and
            @($beforeActor[0].Statuses | Where-Object Id -EQ 'cardless').Count -gt 0) {
            $beforeUpgradeCount = @($beforeActor[0].Modifiers.Upgrades | Where-Object DataId -EQ $cardlessUpgradeId).Count
            $afterUpgradeCount = @($actor[0].Modifiers.Upgrades | Where-Object DataId -EQ $cardlessUpgradeId).Count
            if ($beforeActor[0].Modifiers.IsClone) {
                if ($afterUpgradeCount -eq $beforeUpgradeCount) { $cardlessCloneSkipped++ }
            } elseif ($afterUpgradeCount -gt $beforeUpgradeCount) { $cardlessUpgradeApplied++ }
        }
        if ($actor[0].Team -eq 1) {
            $spawnRelicPlayers++
            $oldShield = @($birth.Before.Context.Relics | Where-Object AssetKey -EQ 'FirstUnitGainDamageShield')[0].SpawnStatuses[0].Conditions[0]
            $newShield = @($birth.After.Context.Relics | Where-Object AssetKey -EQ 'FirstUnitGainDamageShield')[0].SpawnStatuses[0].Conditions[0]
            if ($oldShield.DurationTriggerCount -eq 0 -and $newShield.DurationTriggerCount -eq 1) {
                $spawnRelicFirsts++
                [void]$spawnRelicTurns.Add($birth.Before.Context.QueryFrame.Turn)
            } elseif ($oldShield.DurationTriggerCount -eq 1 -and $newShield.DurationTriggerCount -eq 1) { $spawnRelicSkips++ }
        } else { $spawnRelicEnemies++ }
    }
    if ($trace.Schema -ne $(if ($SpawnStatusRelicsClones) { 111 } else { 110 }) -or $trace.ModifierScenario -ne $(if ($SpawnStatusRelicsClones) { 'spawn-status-relics-clones' } else { 'spawn-status-relics' }) -or $births.Count -lt 3 -or
        $spawnRelicPlayers -lt 2 -or $spawnRelicEnemies -lt 1 -or $spawnRelicFirsts -lt 1 -or $spawnRelicSkips -lt 1 -or
        (-not $SpawnStatusRelicsClones -and ($spawnRelicFirsts -lt 2 -or $spawnRelicTurns.Count -lt 2)) -or
        ($SpawnStatusRelicsClones -and (-not $cardlessUpgradeId -or $cardlessUpgradeApplied -lt 1 -or $cardlessCloneSkipped -lt 1)) -or
        @($births | Where-Object { -not $_.Completed -or -not $_.After -or $_.Difference }).Count -ne 0 -or
        @($trace.Actions[0].Before.Spawn.Train.Context.Relics | Where-Object { $_.AssetKey -in @('SpawnWithArmor','FirstUnitGainDamageShield','FrostbiteOnEnemies') }).Count -ne 3) {
        throw 'Native original spawn-status relic coverage is incomplete or failed.'
    }
    if ($SpawnStatusRelicsClones) {
        Write-Output "NATIVE-CARDLESS-RELIC-UPGRADE PASS: upgrade applied to $cardlessUpgradeApplied non-clone cardless births and skipped on $cardlessCloneSkipped cardless clones."
    }
}
if ($IncantRelic) {
    $players = @($incantFires | Where-Object { $_.Actor.Team -eq 1 })
    $enemies = @($incantFires | Where-Object { $_.Actor.Team -eq 0 })
    $context = $trace.Actions[0].Before.Spawn.Train.Context
    if ($trace.Schema -ne 109 -or $players.Count -eq 0 -or $enemies.Count -eq 0 -or
        @($players.Actor.Triggers | Where-Object { $_.Kind -eq 'CardSpellPlayed' -and $_.FireCount -ne 2 }).Count -ne 0 -or
        @($enemies.Actor.Triggers | Where-Object { $_.Kind -eq 'CardSpellPlayed' -and $_.FireCount -ne 1 }).Count -ne 0 -or
        @($context.Relics | Where-Object { $_.DataId -eq '410ba540-7c4f-4dc5-a84f-b1d8af508891' -and $_.AssetKey -eq 'ExtraSpellCastTrigger' }).Count -ne 1 -or
        @($context.TriggerCounts.Modifiers | Where-Object { $_.Kind -eq 'CardSpellPlayed' -and $_.Value -eq 1 }).Count -ne 1 -or
        @($context.TriggerCounts.EnemyAllowedKinds).Count -ne 0) { throw 'Original relic trigger-count coverage incomplete.' }
    Write-Output "NATIVE-RELIC-TRIGGER-COUNT PASS: $($players.Count) player dispatches with count2 and $($enemies.Count) enemy dispatches with count1; original acquired relic and compiled cache."
    if ($IncantRelics) {
        foreach ($kind in @('OnSpawn','OnDeath')) {
            if (@($context.TriggerCounts.Modifiers | Where-Object { $_.Kind -eq $kind -and $_.Value -eq 1 }).Count -ne 1 -or
                @($players.Actor.Triggers | Where-Object { $_.Kind -eq $kind -and $_.FireCount -eq 2 }).Count -eq 0 -or
                @($enemies.Actor.Triggers | Where-Object { $_.Kind -eq $kind -and $_.FireCount -eq 1 }).Count -eq 0) { throw "Combined relic count coverage missing: $kind" }
        }
        if (@($trace.Turns | Where-Object { $_.Actual.Spawn.Train.Context.Statistics.MonstersDeadThisBattle -gt 0 }).Count -eq 0) { throw 'Combined relic battle lacks real player deaths.' }
        Write-Output 'NATIVE-RELIC-COUNTS-COMBINED PASS: original Incant/spawn/death relics, new paid births and real player deaths.'
    }
}
$abilityIncantCoverage = -not $AbilityIncant
if ($AbilityIncant) {
    $abilityPhases = @($trace.IncantPhases | Where-Object IsAnyAbility)
    $abilityActions = @($trace.Actions | Where-Object { $_.Action.ActivatorUnitId -gt 0 })
    $abilityIncantCoverage = $trace.ModifierScenario -eq 'ability-incant' -and $abilityPhases.Count -ge 4 -and
        @($abilityActions | ForEach-Object { $_.Action.ActivatorUnitId } | Sort-Object -Unique).Count -ge 2 -and
        @($abilityPhases | Where-Object { -not $_.Completed -or $null -eq $_.After -or $null -ne $_.Difference }).Count -eq 0 -and
        @($trace.IncantTriggers | Where-Object { -not $_.Completed -or $null -eq $_.After -or $null -eq $_.AfterActor -or $null -ne $_.Difference }).Count -eq 0 -and
        @($trace.IncantTriggers | Where-Object { $_.Actor.Team -eq 0 }).Count -gt 0 -and
        @($trace.IncantTriggers | Where-Object { $_.Actor.Team -eq 1 }).Count -gt 0
    if (-not $abilityIncantCoverage) { throw "Ability Incant coverage incomplete: phases=$($abilityPhases.Count) actions=$($abilityActions.Count)" }
}
$incantThresholdCoverage = -not $IncantThresholds
if ($IncantThresholds) {
    $positiveThresholds = 0; $zeroThresholds = 0; $negativeThresholds = 0; $spentThresholds = 0; $incorrectThresholds = 0
    foreach ($fire in $incantFires) {
        if ($fire.ParamInt -ne 0 -or $fire.TriggerCount -ne 1) { $incorrectThresholds++ }
        foreach ($trigger in $fire.AfterActor.Triggers) {
            if ($trigger.Kind -ne 'CardSpellPlayed') { continue }
            if ($trigger.TriggerAtThreshold -gt 0 -and $trigger.HasTriggered) { $incorrectThresholds++ }
            if ($trigger.TriggerAtThreshold -eq 1 -and @($trigger.Effects | Where-Object { $_.Type -eq 'CardEffectRewardGold' -and $_.Value -eq 23 }).Count -gt 0) { $positiveThresholds++ }
            if ($trigger.TriggerAtThreshold -eq 0 -and $trigger.HasTriggered -and @($trigger.Effects | Where-Object { $_.Type -eq 'CardEffectRewardGold' -and $_.Value -eq 31 }).Count -gt 0) { $zeroThresholds++ }
            if ($trigger.TriggerAtThreshold -eq -3 -and $trigger.Once -and $trigger.HasTriggered -and @($trigger.Effects | Where-Object { $_.Type -eq 'CardEffectRewardGold' -and $_.Value -eq 13 }).Count -gt 0) { $negativeThresholds++ }
        }
        if (@($fire.Actor.Triggers | Where-Object { $_.Kind -eq 'CardSpellPlayed' -and $_.TriggerAtThreshold -eq -3 -and $_.Once -and $_.HasTriggered }).Count -gt 0) { $spentThresholds++ }
    }
    $incantThresholdCoverage = $positiveThresholds -gt 0 -and $zeroThresholds -gt 0 -and $negativeThresholds -gt 0 -and $spentThresholds -gt 0 -and $incorrectThresholds -eq 0
    if (-not $incantThresholdCoverage) { throw "Incant threshold coverage incomplete: positive=$positiveThresholds zero=$zeroThresholds negative=$negativeThresholds spent=$spentThresholds incorrect=$incorrectThresholds" }
}
$result = [pscustomobject]@{
    PurifyCoverage = $purifyCoverage
    PurifyQueueCoverage = $purifyQueueCoverage
    IncantCoverage = $incantCoverage
    AbilityIncantCoverage = $abilityIncantCoverage
    IncantThresholdCoverage = $incantThresholdCoverage
    RevivalOperations = @($trace.RevivalOperations).Count
    Revivals = @($trace.Revivals).Count
    TriggeredEquipmentCoverage = $triggeredEquipmentCoverage
    TriggeredEquipmentApplications = $triggeredEquipmentApplications
    OwnedSummonApplications = $ownedSummonApplications
    OwnedSummonBirths = $ownedSummonBirths
    OwnedSummonDyingBirths = $ownedSummonDyingBirths
    OwnedSummonRevivedBirths = $ownedSummonRevivedBirths
    SummonRevivalHostSamples = $summonRevivalHostSamples
    SummonRevivalChildSamples = $summonRevivalChildSamples
    QueuedEquipmentAttachments = $queuedEquipmentAttachments
    TriggeredSummonCoverage = $triggeredSummonCoverage
    TriggeredSummonBirths = $triggeredSummonBirths
    TriggeredSummonZeroBirths = $triggeredSummonZeroBirths
    TriggeredSummonDyingSources = $triggeredSummonDyingSources
    PhysicalSpawnPoints = $PhysicalSpawnPoints.IsPresent
    PhysicalCompactions = @($trace.PhysicalCompactions).Count
    SpawnPointOperations = @($trace.SpawnPointOperations).Count
    PoolSummonSelections = @($trace.PoolSummonSelections).Count
    UnitBirths = @($trace.UnitBirths).Count
    DetachedCardClones = @($trace.DetachedCardClones).Count
    SpawnUpgrades = @($trace.SpawnUpgrades).Count
    FreshSpawners = @($trace.FreshSpawners).Count
    GlobalStandbyChecks = @($trace.GlobalStandbyChecks).Count
    RallyOperations = @($trace.RallyOperations).Count
    RallyPhases = @($trace.RallyPhases).Count
    RallyTriggers = @($trace.RallyTriggers).Count
    DyingHordeUpgradeOperations = @($trace.DyingHordeUpgradeOperations).Count
    HordeUpgradeOperations = @($trace.HordeUpgradeOperations).Count
    HordeDeathOperations = @($trace.HordeDeathOperations).Count
    HordeRemovalOperations = @($trace.HordeRemovalOperations).Count
    HarvestOperations = @($trace.HarvestOperations).Count
    HarvestTriggers = @($trace.HarvestTriggers).Count
    HordeStatusCoverage = (-not $HordeStatuses -or @($trace.HordeStatusOperations).Count -eq 8)
    HordeStatusOperations = @($trace.HordeStatusOperations).Count
    HordeMergeCoverage = (-not $HordeMerge -or @($trace.HordeMergeOperations).Count -eq 17)
    HordeMergeOperations = @($trace.HordeMergeOperations).Count
    HordeMergeSelections = @($trace.HordeMergeSelections).Count
    BumpCoverage = (-not $Bump -or @($trace.BumpOperations).Count -eq 18)
    BumpOperations = @($trace.BumpOperations).Count
    UnitCloneOperations = @($trace.UnitCloneOperations).Count
    UnitCopyOperations = @($trace.UnitCopyOperations).Count
    HeroCopyOperations = @($trace.HeroCopyOperations).Count
    SpawnEnchantments = @($trace.SpawnEnchantments).Count
    EquipmentAbilityCoverage = $equipmentAbilityCoverage
    InitialAbilitySpawns = @($trace.InitialAbilitySpawns).Count
    EquipmentAbilityActivations = $equipmentActivations.Count
    AbilityEffectsCoverage = $abilityEffectsCoverage
    AbilityEffectOperations = @($trace.AbilityEffectOperations).Count
    AbilityLifecycleCoverage = $lifecycleCoverage
    AbilityLifecycleOperations = @($trace.AbilityLifecycleOperations).Count
    AbilityActivationCoverage = $activationCoverage
    AbilityActivationSamples = @($trace.Actions | Where-Object { $_.Action.ActivatorUnitId -gt 0 }).Count
    AbilityCacheCoverage = $abilityCacheCoverage
    AbilityCacheSamples = @($trace.AbilityCardOperations).Count
    AbilityCooldownCoverage = $abilityCoverage
    AbilityCooldownSamples = @($trace.AbilityCooldownEffects).Count
    SentryCoverage = $sentryCoverage
    SentrySamples = @($trace.Sentries).Count
    CompanionBossCoverage = $companionCoverage
    Policy = $Policy
    Profile = $profile
    ExitCode = $process.ExitCode
    NativePassed = $nativePassed
    NativeWon = $trace.NativeWon
    CaptureFailures = $trace.CaptureFailures
    Mismatches = $trace.Mismatches
    Unsupported = $trace.Unsupported
    Pending = $trace.Pending
    Stages = @($trace.Stages).Count
    CardCycles = @($trace.CardCycles).Count
    TrainPhases = @($trace.TrainPhases).Count
    Spawns = @($trace.Spawns).Count
    EndTurns = @($trace.Turns).Count
    Actions = @($trace.Actions).Count
    DecisionTurns = @($trace.Checkpoints).Count - 1
    OriginalFilesUnchanged = $originalUnchanged
    ModifierActions = $modifierActions.Count
    ModifierCoverage = $modifierCoverage
    HealActions = $healActions.Count
    HealingCoverage = $healingCoverage
    OnHealCoverage = $onHealCoverage
    RoomSpellCoverage = $roomSpellCoverage
    TerminalEffectsSettled = $terminalSettled
    TerminalSpellCoverage = $terminalSpellCoverage
    PostKillCoverage = $postKillCoverage
    RandomCoverage = $randomCoverage
    RandomStatusCoverage = $randomStatusCoverage
    CrossRoomCoverage = $crossRoomCoverage
    AttackCoverage = $attackCoverage
    MaxHealthCoverage = $maxHealthCoverage
    NumericRangeCoverage = $numericRangeCoverage
    TargetFilterCoverage = $targetFilterCoverage
    DrawCoverage = $drawCoverage
    HandRemovalCoverage = $handRemovalCoverage
    GenerationCoverage = $generationCoverage
    DamageScalingCoverage = $scalingCoverage
    StatusScalingCoverage = $statusScalingCoverage
    UnitUpgradeScalingCoverage = $unitUpgradeScalingCoverage
    UnitTriggerUpgradeCoverage = $unitTriggerUpgradeCoverage
    SpawnTriggerCoverage = $spawnTriggerCoverage
    UnitTurnBeginCoverage = $unitTurnBeginCoverage
    TeamTurnBeginCoverage = $teamTurnBeginCoverage
    PreHandDiscardCoverage = $preHandDiscardCoverage
    CloneUpgradeRefreshCoverage = $cloneUpgradeRefreshCoverage
    PreCombatCoverage = $preCombatCoverage
    DamageDeathQueueCoverage = $damageDeathQueueCoverage
    TerminalDeathDamageCoverage = $terminalDeathDamageCoverage
    DyingUpgradeCoverage = $dyingUpgradeCoverage
    DyingUpgradeSamples = @($trace.DyingUpgrades).Count
    TriggeredStatusCoverage = $triggeredStatusCoverage
    StatusCallbackCoverage = $statusCallbackCoverage
    EnergyCoverage = $energyCoverage
    EnergySamples = @($trace.EnergyEffects).Count
    XCostCoverage = $xCostCoverage
    BonusDrawCoverage = $bonusDrawCoverage
    BonusDrawSamples = @($trace.BonusDrawEffects).Count
    DirectUnitUpgradeCoverage = $directUpgradeCoverage
    EquipmentCoverage = $equipmentCoverage
    EquipmentOperations = @($trace.EquipmentOperations).Count
    CapacityCoverage = $capacityCoverage
    CapacitySamples = @($trace.CapacityEffects).Count
    CapacityTests = @($trace.CapacityTests).Count
    TriggeredStatusSamples = @($trace.TriggeredStatuses).Count
    AttackTriggerCoverage = $attackTriggerCoverage
    AttackTriggerSamples = @($trace.AttackTriggers).Count
    HitKillCoverage = $hitKillCoverage
    HitKillSamples = @($trace.HitKills).Count
    TriggeredDamageCoverage = $triggeredDamageCoverage
    TriggeredHealingCoverage = $triggeredHealingCoverage
    TriggeredHealSamples = @($trace.TriggeredHeals).Count
    PostCombatHealingCoverage = $postCombatHealingCoverage
    UnitPostCombatPhases = @($trace.UnitPostCombats).Count
    StatisticQueryCalibration = $(if ($StatisticQueries) { Join-Path $profile 'statistic-query-calibration.json' } else { $null })
    StatisticOverflowCalibration = $(if ($StatisticOverflow) { Join-Path $profile 'statistic-overflow-calibration.json' } else { $null })
    StatisticZeroIncrementCalibration = $(if ($StatisticOverflow) { Join-Path $profile 'statistic-zero-increment-calibration.json' } else { $null })
    Trace = $tracePath
    HordeStatCalibration = $(if ($HordeStats) { Join-Path $profile 'horde-stat-calibration.mt2f' } else { $null })
    EnchantmentLifecycleCalibration = $(if ($EnchantmentLifecycle) { Join-Path $profile 'enchantment-lifecycle-calibration.mt2f' } else { $null })
    EnchantmentCombatCalibration = $(if ($EnchantmentCombat) { Join-Path $profile 'enchantment-combat-calibration.mt2f' } else { $null })
    EnchantmentWorldCalibration = $(if ($EnchantmentWorld) { Join-Path $profile 'enchantment-world-calibration.mt2f' } else { $null })
    EnchantmentSourceOrderCalibration = $(if ($EnchantmentWorld) { Join-Path $profile 'enchantment-source-order-calibration.mt2f' } else { $null })
    CharacterRemovalCalibration = $(if ($CharacterRemoval) { Join-Path $profile 'character-removal-calibration.mt2f' } else { $null })
}
$result | ConvertTo-Json
if ($SettleDeathDissolves) {
    if (-not $trace.DeathDissolveSettlementEnabled -or $trace.Schema -ne $(if ($SpawnStatusRelicsClones) { 111 } elseif ($SpawnStatusRelics) { 110 } elseif ($IncantRelic) { 109 } elseif ($AbilityIncant) { 108 } elseif ($SettleCardAnimations) { 106 } else { 105 }) -or
        @($trace.DeathDissolveSettlements).Count -eq 0 -or
        @($trace.DeathDissolveSettlements | Where-Object { -not $_.Completed -or $_.PendingAfter -ne 0 -or $_.Error }).Count -ne 0 -or
        @($trace.DeathDissolveCallbacks | Where-Object { $_.Error }).Count -ne 0) {
        throw 'Native death-dissolve settlement is incomplete or failed.'
    }
}
if ($SettleCardAnimations) {
    if ($trace.Schema -ne $(if ($SpawnStatusRelicsClones) { 111 } elseif ($SpawnStatusRelics) { 110 } elseif ($IncantRelic) { 109 } elseif ($AbilityIncant) { 108 } else { 106 }) -or -not $trace.CardAnimationSettlement.Enabled -or
        $trace.CardAnimationSettlement.PreviewWaits -lt 0 -or $trace.CardAnimationSettlement.DecisionWaits -lt 0 -or
        $trace.CardAnimationSettlement.ScheduledMovements -le 0 -or $trace.CardAnimationSettlement.PendingMovements -ne 0) {
        throw 'Native card-animation settlement protocol is missing or invalid.'
    }
}
if ($CharacterRemoval) {
    $removalPath = Join-Path $profile 'character-removal-calibration.mt2f'
    if (-not (Test-Path -LiteralPath $removalPath)) { throw 'Missing native character removal calibration.' }
    Add-Type -Path (Join-Path $workspace 'src\FixtureArchive\bin\Release\net8.0\FixtureArchive.dll')
    $removalArchive = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read($removalPath)
    try { $removal = $removalArchive.RootElement.ToObjectGraph() }
    finally { $removalArchive.Dispose() }
    if ($removal.Boundary -ne 'NativeCharacterRemovalApis' -or $removal.GameplaySuppressed -or
        @($removal.Errors).Count -ne 0 -or @($removal.Samples).Count -eq 0 -or
        @($removal.Samples | Where-Object { -not $_.Completed -or $null -ne $_.Difference }).Count -ne 0) {
        throw 'Native character removal API calibration is incomplete or differs.'
    }
}
if ($EnchantmentWorld) {
    $sourceOrderPath = Join-Path $profile 'enchantment-source-order-calibration.mt2f'
    if (-not (Test-Path -LiteralPath $sourceOrderPath)) { throw 'Missing native enchantment source-order calibration.' }
    Add-Type -Path (Join-Path $workspace 'src\FixtureArchive\bin\Release\net8.0\FixtureArchive.dll')
    $sourceOrderArchive = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read($sourceOrderPath)
    try { $sourceOrderData = $sourceOrderArchive.RootElement.ToObjectGraph() }
    finally { $sourceOrderArchive.Dispose() }
    if ($sourceOrderData.Boundary -ne 'NativeManagerListCollection' -or $sourceOrderData.GameplaySuppressed -or
        @($sourceOrderData.Samples | Where-Object { $_.Label.StartsWith('sort-stress-') }).Count -ne 32 -or
        @($sourceOrderData.Samples | Where-Object { @($_.Positions.Team | Select-Object -Unique).Count -eq 2 }).Count -eq 0) {
        throw 'Native source-order both-team/sort-threshold coverage is incomplete.'
    }
    $enchantmentPath = Join-Path $profile 'enchantment-world-calibration.mt2f'
    if (-not (Test-Path -LiteralPath $enchantmentPath)) { throw 'Missing native automatic aura world calibration.' }
    Add-Type -Path (Join-Path $workspace 'src\FixtureArchive\bin\Release\net8.0\FixtureArchive.dll')
    $enchantmentArchive = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read($enchantmentPath)
    try { $enchantment = $enchantmentArchive.RootElement.ToObjectGraph() }
    finally { $enchantmentArchive.Dispose() }
    if (-not $enchantment.LiveContextUnchanged -or $enchantment.Mismatches -ne 0 -or
        $enchantment.Boundary -ne 'AutomaticControlStatusAndQueue' -or $enchantment.StatusMutationsSuppressed -or
        -not $enchantment.ExternalPreviewPreparationsRecorded -or @($enchantment.Samples).Count -ne 32 -or
        @($enchantment.Samples | Where-Object Completed -NE $true).Count -gt 0 -or
        @($enchantment.Samples | Where-Object { $null -ne $_.Difference }).Count -gt 0) {
        throw 'Native automatic aura world status coverage is incomplete or differs.'
    }
}
if ($EnchantmentCombat) {
    $enchantmentPath = Join-Path $profile 'enchantment-combat-calibration.mt2f'
    if (-not (Test-Path -LiteralPath $enchantmentPath)) { throw 'Missing native real-status enchantment combat calibration.' }
    Add-Type -Path (Join-Path $workspace 'src\FixtureArchive\bin\Release\net8.0\FixtureArchive.dll')
    $enchantmentArchive = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read($enchantmentPath)
    try { $enchantment = $enchantmentArchive.RootElement.ToObjectGraph() }
    finally { $enchantmentArchive.Dispose() }
    if (-not $enchantment.LiveContextUnchanged -or $enchantment.Mismatches -ne 0 -or
        $enchantment.Boundary -ne 'RealStatusAndDrainedQueue' -or $enchantment.StatusMutationsSuppressed -or
        -not $enchantment.ExternalPreviewPreparationsRecorded -or
        @($enchantment.Samples).Count -ne 32 -or @($enchantment.Samples | Where-Object Completed -NE $true).Count -gt 0 -or
        @($enchantment.Samples | Where-Object { $null -ne $_.Difference }).Count -gt 0) {
        throw 'Native real-status enchantment combat coverage is incomplete or differs.'
    }
}
if ($EnchantmentLifecycle) {
    $enchantmentPath = Join-Path $profile 'enchantment-lifecycle-calibration.mt2f'
    if (-not (Test-Path -LiteralPath $enchantmentPath)) { throw 'Missing native enchantment lifecycle calibration.' }
    Add-Type -Path (Join-Path $workspace 'src\FixtureArchive\bin\Release\net8.0\FixtureArchive.dll')
    $enchantmentArchive = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read($enchantmentPath)
    try { $enchantment = $enchantmentArchive.RootElement.ToObjectGraph() }
    finally { $enchantmentArchive.Dispose() }
    if (-not $enchantment.LiveContextUnchanged -or $enchantment.Mismatches -ne 0 -or
        $enchantment.Boundary -ne 'StatusApiRequests' -or -not $enchantment.StatusMutationsSuppressed -or
        @($enchantment.Samples).Count -lt 1000 -or @($enchantment.Samples | Where-Object Completed -NE $true).Count -gt 0 -or
        @($enchantment.Samples | Where-Object { $null -ne $_.Difference }).Count -gt 0) {
        throw 'Native enchantment lifecycle coverage is incomplete or differs.'
    }
}
if ($HordeStats) {
    $hordePath = Join-Path $profile 'horde-stat-calibration.mt2f'
    if (-not (Test-Path -LiteralPath $hordePath)) { throw 'Missing native Horde numerical calibration.' }
    Add-Type -Path (Join-Path $workspace 'src\FixtureArchive\bin\Release\net8.0\FixtureArchive.dll')
    $hordeArchive = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read($hordePath)
    $horde = $hordeArchive.RootElement.ToObjectGraph()
    $hordeArchive.Dispose()
    if (-not $horde.LiveContextUnchanged -or $horde.Mismatches -ne 0 -or @($horde.Samples).Count -ne 560 -or @($horde.Casualties).Count -ne 175) {
        throw 'Native Horde numerical coverage is incomplete or differs.'
    }
}
if ($StatisticQueries) {
    $queryPath = Join-Path $profile 'statistic-query-calibration.json'
    if (-not (Test-Path -LiteralPath $queryPath)) { throw 'Missing native statistic-query calibration.' }
    $queryCalibration = Get-Content -LiteralPath $queryPath -Raw | ConvertFrom-Json
    if (-not $queryCalibration.LiveContextUnchanged -or @($queryCalibration.Batches).Count -ne 6 -or
        @($queryCalibration.Batches.Samples).Count -lt 500) { throw 'Native statistic-query coverage is incomplete.' }
}
if ($StatisticOverflow) {
    $overflow = Get-Content -LiteralPath (Join-Path $profile 'statistic-overflow-calibration.json') -Raw | ConvertFrom-Json
    if (-not $overflow.LiveContextUnchanged -or @($overflow.Samples).Count -ne 65) { throw 'Native statistic-overflow coverage is incomplete.' }
    $zero = Get-Content -LiteralPath (Join-Path $profile 'statistic-zero-increment-calibration.json') -Raw | ConvertFrom-Json
    if (-not $zero.LiveContextUnchanged -or @($zero.Samples).Count -ne 48 -or
        @($zero.Samples | Where-Object DetachedSource -EQ $true).Count -ne 24 -or
        @($zero.Samples | Where-Object Amount -NE 0).Count -ne 0) { throw 'Native zero-increment coverage is incomplete.' }
}
if ($null -eq $trace.NativeWon -or $process.ExitCode -ne 0 -or -not $nativePassed -or -not $originalUnchanged -or -not $modifierCoverage -or -not $healingCoverage -or -not $onHealCoverage -or -not $roomSpellCoverage -or -not $terminalSettled -or -not $terminalSpellCoverage -or
    -not $postKillCoverage -or -not $randomCoverage -or -not $randomStatusCoverage -or -not $crossRoomCoverage -or -not $attackCoverage -or -not $maxHealthCoverage -or -not $numericRangeCoverage -or -not $targetFilterCoverage -or -not $drawCoverage -or -not $handRemovalCoverage -or -not $generationCoverage -or -not $scalingCoverage -or -not $statusScalingCoverage -or -not $unitUpgradeScalingCoverage -or -not $unitTriggerUpgradeCoverage -or -not $spawnTriggerCoverage -or -not $unitTurnBeginCoverage -or -not $teamTurnBeginCoverage -or -not $preHandDiscardCoverage -or -not $cloneUpgradeRefreshCoverage -or -not $preCombatCoverage -or -not $triggeredHealingCoverage -or -not $postCombatHealingCoverage -or -not $triggeredDamageCoverage -or -not $damageDeathQueueCoverage -or -not $terminalDeathDamageCoverage -or -not $hitKillCoverage -or -not $dyingUpgradeCoverage -or -not $attackTriggerCoverage -or -not $triggeredStatusCoverage -or -not $statusCallbackCoverage -or -not $energyCoverage -or -not $xCostCoverage -or -not $bonusDrawCoverage -or -not $capacityCoverage -or -not $directUpgradeCoverage -or -not $equipmentCoverage -or $trace.CaptureFailures -ne 0 -or $trace.Mismatches -ne 0 -or $trace.Unsupported -ne 0 -or $trace.Pending -ne 0) {
    throw "Full battle differential probe failed; inspect $tracePath and $unityLog"
}
