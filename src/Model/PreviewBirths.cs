using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class PreviewBirthModel
    {
        internal static bool IsOriginalGreenRecruit(CombatUnit unit, CombatContext context) =>
            unit is { AssetKey: "MonsterGenerousHost", Team: CombatTeam.Player } &&
            context.FindCard(unit.SpawnerCardId)?.DataId == "c052c567-1e3c-4f36-a6cd-89b0648b9475" &&
            (unit.Triggers.Count == 1 || unit.Triggers.Count == 3 &&
                IsOriginalDuskMantleTrigger(unit.Triggers[1], unit, context, "OnMoonShade", "CardEffectAddStatusEffect", "AddStatus") &&
                IsOriginalDuskMantleTrigger(unit.Triggers[2], unit, context, "OnMoonshadeLost", "CardEffectRemoveStatusEffect", "RemoveStatus") &&
                unit.Triggers[1].Origin.EquipmentCardId == unit.Triggers[2].Origin.EquipmentCardId) &&
            unit.Triggers[0] is { Kind: "OnDeath", Effects: [{ Type: "CardEffectSpawnMonsterOrAddStatusEffect", Action:
                { Type: "SpawnOrStatus", Target: "Room", Value: 4, AllowPlayer: true, AllowEnemy: false, Range: null,
                    Statuses: [{ Id: "horde", Stacks: 4 }], Filters.Subtype: "SubtypesData_Horde",
                    SpawnFallbackUnit: { AssetKey: "MonsterFunguy", BaseAttack: 3, Health: 4 } } }] };

        private static bool IsOriginalDuskMantleTrigger(CombatTrigger trigger, CombatUnit unit, CombatContext context,
            string kind, string effectType, string actionType) =>
            trigger.Kind == kind && trigger.IgnoreSilence == (kind == "OnMoonshadeLost") &&
            trigger is { Once: false, FireCount: 1, SkipDuringDeployment: false,
                Origin: { UpgradeId: GeneratedMoonTrackingModel.EquipmentId, EquipmentCardId: > 0,
                    IsFromEquipment: true, OnlyIfEquipped: false } origin,
                Effects: [{ Type: var capturedEffect, Value: 0, Enchantment: null, Summon: null, Generation: null,
                    Action: { Type: var capturedAction, Target: "Self", Value: 0, Range: null, AllowPlayer: true, AllowEnemy: false,
                        Upgrade: null, Statuses: [{ Id: "titanskin", Stacks: 5 }] } }] } &&
            capturedEffect == effectType && capturedAction == actionType && unit.EquipmentCards.Contains(origin.EquipmentCardId) &&
            context.FindCard(origin.EquipmentCardId)?.DataId == GeneratedMoonTrackingModel.EquipmentId;

        internal static bool IsOriginalChampionEnemyDeath(CombatUnit unit, CombatContext context) =>
            unit is { AssetKey: "MonsterChampionUnderlegion1", Team: CombatTeam.Player } &&
            context.FindCard(unit.SpawnerCardId)?.DataId == "0422f6ca-896c-4676-a01f-71d8d4b232fb" &&
            (unit.Triggers.Count == 1 || unit.Triggers is [_, { Kind: "OnHit", Effects: [{ Type: "CardEffectAddStatusEffect", Action:
                { Type: "AddStatus", Target: "LastAttackerCharacter", Range: null,
                    AllowEnemy: true, AllowPlayer: true, Statuses: [{ Id: "pyregel", Stacks: 3 }] } }] }] ||
                unit.Triggers.Count == 2 && IsOriginalMeatShieldHeal(unit.Triggers[1], unit, context)) &&
            unit.Triggers[0] is { Kind: "OnAnyHeroDeathOnFloor", Effects: [{ Type: "CardEffectSpawnMonsterOrAddStatusEffect", Action:
                { Type: "SpawnOrStatus", Target: "Room", Value: var stacks, AllowPlayer: true, AllowEnemy: false, Range: null,
                    Statuses: [{ Id: "horde", Stacks: var statusStacks }], Filters.Subtype: "SubtypesData_Horde",
                    SpawnFallbackUnit: { AssetKey: "MonsterFunguy", BaseAttack: 3, Health: 4, Modifiers.Upgrades.Count: 0 } } }] } &&
            stacks == statusStacks && (stacks == 2 || stacks == 3 &&
                context.FindCard(unit.SpawnerCardId)!.Permanent.Upgrades.Any(upgrade =>
                    upgrade.DataId == CardUnderlegionTroopUpgradeModel.ThirdDataId && CardUnderlegionTroopUpgradeModel.Known(upgrade)));

        internal static bool IsOriginalMeatShieldHeal(CombatTrigger trigger, CombatUnit unit, CombatContext context) =>
            trigger is { Kind: "PostCombat", Once: false, IgnoreSilence: false, FireCount: 1, SkipDuringDeployment: true,
                Origin: { UpgradeId: CardGraftedEquipmentModel.EventEquipmentId, IsFromEquipment: true, EquipmentCardId: > 0 } origin,
                Effects: [{ Type: "CardEffectHeal", Value: 10, Action: { Type: "Heal", Target: "Self", Value: 10,
                    AllowEnemy: false, AllowPlayer: false, Range: null, Statuses.Count: 0, Upgrade: null } }] } &&
            unit.EquipmentCards.Contains(origin.EquipmentCardId) &&
            context.FindCard(origin.EquipmentCardId)?.DataId == CardGraftedEquipmentModel.EventEquipmentId;

        private static bool IsOriginalChampionAuraPreview(EnchantmentRetainedUnit actor, EnchantmentWorld world, CombatContext context) =>
            world.EnchanterIds.Count == 0 && actor.Unit is { Health: 12, MaxHealth: 12, Triggers.Count: 0,
                HordeDefinition: { Attack: 3, Health: 4 }, Modifiers.Upgrades.Count: 4 } &&
            actor.Unit.Status("horde")?.Stacks == 2 && actor.Unit.Status("trample") == null &&
            world.Rooms.Where(room => room.RoomIndex == actor.RoomIndex).SelectMany(room => room.Units).Any(owner =>
                owner.Triggers is [_, { Kind: "OnHit", Effects: [{ Type: "CardEffectAddStatusEffect", Action:
                    { Type: "AddStatus", Target: "LastAttackerCharacter", Range: null,
                        AllowEnemy: true, AllowPlayer: true, Statuses: [{ Id: "pyregel", Stacks: 3 }] } }] }] &&
                IsOriginalChampionEnemyDeath(owner.WithTriggers(new[] { owner.Triggers[0] }), context) &&
                owner.Health > 0 && owner.Modifiers?.TroopAura is
                    { Subtype: "SubtypesData_None", Statuses.Count: 0, Upgrade: { DataId: "9cb768b3-efb9-480c-819b-33882cf0908d",
                        AssetKey: "Underlegion1RegenOnHarvest1_Upgrade", Stats.Health: 1 } } aura &&
                !aura.Targets.Contains(actor.Unit.Id) && actor.Unit.Modifiers.Upgrades.All(upgrade =>
                    upgrade.DataId == TroopAuraModel.Key(owner.Id) && upgrade.AssetKey == aura.Upgrade.AssetKey &&
                    upgrade.Stats.Health == 1 && upgrade.Stats.Damage == 0 && upgrade.Stats.Cost == 0 &&
                    upgrade.Stats.Heal == 0 && upgrade.Stats.Size == 0 && upgrade.Stats.XCost == 0 &&
                    upgrade.Stats.EquipmentLimit == 0 && upgrade.Stats.UpgradeSlotCount == 0 && upgrade.Statuses.Count == 0 &&
                    upgrade.ExternalInteractions.Count == 0 && upgrade.TriggerUpgrades?.Count is not > 0 && upgrade.AbilityUpgrade == null));

        internal static int[] UnobservedRoomPlayers(RoomCombatState source, CombatContext preview,
            IEnumerable<CombatUnit> observed)
        {
            CombatContext context = source.Context!;
            // Room-only captures have no actor world to retain temporary objects.
            // The calibrated kill preview creates no card and no durable observer.
            if (context.Enchantments != null || preview.Enchantments != null || context.SummonCatalog != null ||
                context.NextUnitId is not int first || preview.NextUnitId is not int next || next <= first ||
                context.NextCardId != preview.NextCardId ||
                preview.CardRegistry?.Any(card => card.InstanceId >= context.NextCardId) == true ||
                !source.Units.Any(unit => IsOriginalGreenRecruit(unit, context))) return Array.Empty<int>();
            CombatUnit[] births = observed.Where(unit => unit.Id >= first).ToArray();
            if (births.Length != next - first || births.Select(unit => unit.Id).Distinct().Count() != births.Length || births.Any(unit => unit.Id >= next ||
                unit.Team != CombatTeam.Player || unit.AssetKey != "MonsterFunguy" || unit.SpawnerCardId != 0 ||
                unit.Triggers.SelectMany(trigger => trigger.Effects).Any(effect => effect.Enchantment != null)))
                return Array.Empty<int>();
            return births.Select(unit => unit.Id).ToArray();
        }

        internal static CombatContext ForgetUnobservedBossPlayers(RoomCombatState source, CombatContext preview,
            IReadOnlyCollection<CombatUnit> observedBirths, out int[] forgotten)
        {
            forgotten = Array.Empty<int>();
            if (source.Context?.Enchantments is not { AutomaticLifecycle: true } world ||
                !source.Units.Any(unit => IsOriginalGreenRecruit(unit, source.Context) ||
                    IsOriginalChampionEnemyDeath(unit, source.Context) && unit.Triggers.Count == 2 &&
                    IsOriginalMeatShieldHeal(unit.Triggers[1], unit, source.Context))) return preview;
            // Original Green Recruit and this equipped champion lose their
            // unowned fallback before boss-preview identity capture. Other
            // boss-preview players can retain aura references and stay observed.
            var train = new TrainCombatState(world.Rooms.Select(room => room.RoomIndex == source.RoomIndex
                ? source : room.WithContext(source.Context)).ToArray(), world.Movement, world.EnemySlotsPerRoom, source.Context);
            return ForgetUnobservedPlayers(train, preview, source.Context.NextUnitId ?? int.MaxValue,
                source.Context.NextCardId, observedBirths, out forgotten);
        }

        internal static CombatContext CompleteRemoteCombatBirths(CombatContext context, int roomIndex)
        {
            if (context.Enchantments is not { AutomaticLifecycle: true, Preview: false } world ||
                context.SpawnPoints is not BattleSpawnPoints points) return context;
            // A prior floor's temporary boss-kill preview can leave an unowned
            // Fun Guy in its copied group. The next real combat yields a frame
            // before damage, completing that pending player object destruction.
            bool removedTroopOwner = world.RetainedUnits.Any(actor => actor.Unit is
                { AssetKey: "MonsterSquadLeader", Health: <= 0, DeathState.IsDestroyed: true, Modifiers.TroopAura: not null } &&
                actor.Unit.Modifiers.Upgrades is [{ AssetKey: "InfectoryEffect_Upgrade" }]);
            var pending = world.RetainedUnits.Where(actor => actor.Preview && actor.RoomIndex != roomIndex &&
                actor.Unit is { Team: CombatTeam.Player, AssetKey: "MonsterFunguy", SpawnerCardId: 0,
                    Health: > 0, DeathState.IsDestroyed: false } &&
                (actor.Unit.Status("horde")?.Stacks == 1 &&
                (actor.Unit.Status("trample")?.Stacks == 2 && actor.Unit.Modifiers?.Upgrades.Count == 3 &&
                    actor.Unit.Modifiers.Upgrades.Count(upgrade => upgrade.AssetKey == "SquadLeader_Upgrade") == 2 &&
                    actor.Unit.Modifiers.Upgrades.Count(upgrade => upgrade.AssetKey == "InfectoryEffect_Upgrade") == 1 ||
                    removedTroopOwner && actor.Unit.Status("trample") == null &&
                        actor.Unit.Modifiers?.Upgrades is [{ AssetKey: "InfectoryEffect_Upgrade" }]) ||
                // The original Green Recruit death creates four unupgraded
                // troop stacks. Its preview object survives that floor's raw
                // capture, then retires during the next lower floor's real
                // combat yield, just like the calibrated one-stack births.
                actor.RoomIndex > roomIndex && actor.Unit.Status("horde")?.Stacks == 4 &&
                    actor.Unit.Status("trample") == null && actor.Unit.Triggers.Count == 0 &&
                    actor.Unit.Modifiers?.Upgrades.Count == 0 &&
                    actor.Unit.HordeDefinition is { Attack: 3, Health: 4 } &&
                    world.RetainedUnits.Any(owner => owner.Unit.Health <= 0 && IsOriginalGreenRecruit(owner.Unit, context) &&
                        (owner.RoomIndex == actor.RoomIndex || points.Units.Any(point => point.UnitId == owner.Unit.Id &&
                            point.LastKnown?.RoomIndex == actor.RoomIndex))) ||
                actor.RoomIndex > roomIndex && IsOriginalChampionAuraPreview(actor, world, context)) &&
                points.Units.Any(point => point.UnitId == actor.Unit.Id && point.SpawnedInPreview &&
                    point.Current?.PreviewCopyId != null)).ToArray();
            if (pending.Length == 0) return context;
            var ids = pending.Select(actor => actor.Unit.Id).ToHashSet();
            bool troopOwnerRemoval = removedTroopOwner && pending.Any(actor => actor.Unit.Modifiers!.Upgrades.Count == 1);
            var retained = world.RetainedUnits.Select(actor => ids.Contains(actor.Unit.Id) ? CancelPlayer(actor) :
                // Native waits settle the observed Sporelauncher death, or the
                // Steward/Fun Guy deaths preceding the retired troop owner.
                // That owner's own raw collection survives until the next frame.
                actor.Unit.Team == CombatTeam.Player &&
                    actor.Unit.DeathState?.IsDestroyed == true && actor.Unit.Health <= 0 &&
                    (actor.Unit.AssetKey == "MonsterSporelauncher" && actor.Unit.SpawnerCardId > 0 &&
                        actor.Unit.Modifiers?.Upgrades is [{ AssetKey: "InfectoryEffect_Upgrade" }] ||
                    troopOwnerRemoval && actor.Unit.AssetKey is "TrainStewardSmall" or "MonsterFunguy" &&
                        actor.Unit.Modifiers?.Upgrades.Any(upgrade => upgrade.AssetKey == "InfectoryEffect_Upgrade") == true &&
                        actor.Unit.Modifiers.Upgrades.All(upgrade => upgrade.AssetKey is "InfectoryEffect_Upgrade" or "SquadLeader_Upgrade"))
                    ? new EnchantmentRetainedUnit(actor.Unit.AfterDestruction().WithoutRemovedAttacker(new HashSet<int>()),
                        actor.RoomIndex, actor.Preview) : actor).ToArray();
            return context.WithSpawnPoints(points.ClearDestroyedPreviewBirths(ids)).WithEnchantments(new EnchantmentWorld(
                world.Rooms, world.Movement, world.EnemySlotsPerRoom, retained, world.EnchanterIds, world.AllowUpdates,
                world.Updating, world.Preview, world.TestRng, world.AutomaticLifecycle, world.TemporaryPreviewUnitIds));
        }

        internal static EnchantmentRetainedUnit CancelPlayer(EnchantmentRetainedUnit actor)
        {
            if (!actor.Preview || actor.Unit.Team != CombatTeam.Player || actor.Unit.DeathState is
                { IsDestroyed: true, HasStatisticsListener: false } && actor.Unit.Modifiers?.Upgrades.Count == 0) return actor;
            // MonsterManager removes preview-born players when the full UI
            // preview is reset. HideUnit/ProcessRemovals do not fire death
            // triggers or restore numeric state; OnDestroy clears collections
            // and statistics listeners on the retained managed object.
            CombatUnit unit = actor.Unit.AfterDestruction();
            UnitDeathState death = unit.DeathState ?? new UnitDeathState(false, false, false);
            unit = unit.WithDeathState(new UnitDeathState(death.HasFinishedDying, death.IsBeingRemoved, false,
                null, death.IsSacrifice, death.StatisticsListenerOnce.HasValue ? false : (bool?)null,
                death.IsDespawned, true));
            return new EnchantmentRetainedUnit(unit, -1, actor.Preview);
        }

        internal static CombatContext ForgetUnobservedPlayers(TrainCombatState source, CombatContext preview,
            int firstUnitId, int firstCardId, IReadOnlyCollection<CombatUnit> observedBirths, out int[] forgotten)
        {
            forgotten = Array.Empty<int>();
            CombatContext context = source.Context!;
            EnchantmentWorld? world = preview.Enchantments;
            bool OriginalExistingHorde() => source.Rooms.Any(room => room.Units.Any(owner => IsOriginalChampionEnemyDeath(owner, context) &&
                owner.Modifiers?.TroopAura?.Upgrade is { DataId: CardUnderlegionTroopUpgradeModel.ThirdAuraUpgradeId } aura &&
                room.Units.Any(unit => unit is { AssetKey: "MonsterFunguy", SpawnerCardId: 0, Health: 42, MaxHealth: 42,
                    HordeDefinition: { Attack: 3, Health: 4 }, Modifiers.Upgrades.Count: 6 } && unit.Status("horde")?.Stacks == 6 &&
                    unit.Modifiers.Upgrades.All(upgrade => upgrade.DataId == TroopAuraModel.Key(owner.Id) && upgrade.AssetKey == aura.AssetKey &&
                        upgrade.Stats.Health == 3 && upgrade.Stats.Damage == 0 && upgrade.Stats.Cost == 0 && upgrade.Stats.Heal == 0 &&
                        upgrade.Stats.Size == 0 && upgrade.Stats.XCost == 0 && upgrade.Stats.EquipmentLimit == 0 &&
                        upgrade.Stats.UpgradeSlotCount == 0 && upgrade.Statuses.Count == 0 && upgrade.ExternalInteractions.Count == 0 &&
                        upgrade.AbilityUpgrade == null && upgrade.TriggerUpgrades?.Count is not > 0))));
            bool OriginalSycophantCompanion() => source.Rooms.Any(room => room.Units.Any(owner => IsOriginalChampionEnemyDeath(owner, context) &&
                owner.Modifiers?.TroopAura?.Upgrade.DataId == CardUnderlegionTroopUpgradeModel.ThirdAuraUpgradeId) &&
                room.Units.Any(unit => unit is { AssetKey: "MonsterSycophant", Team: CombatTeam.Player, BaseAttack: 20,
                    Health: 35, MaxHealth: 35, Size: 1, Modifiers.Upgrades: [{ DataId: "4f70b7b7-b507-4931-9b14-e13dcd6cd59f", AssetKey: "ExilesDiadem_Upgrade" }, { DataId: "", AssetKey: "" }] } &&
                    unit.Status("horde") == null && context.FindCard(unit.SpawnerCardId)?.DataId == "6d2053e9-1a6a-4862-b348-76a92a92ea83"));
            bool OriginalLavaFourHorde() => source.Rooms.Any(room => room.Units.Any(owner =>
                IsOriginalChampionEnemyDeath(owner, context) && owner.Triggers.Count == 2 &&
                IsOriginalMeatShieldHeal(owner.Triggers[1], owner, context) &&
                owner.Modifiers?.TroopAura?.Upgrade is { DataId: CardUnderlegionTroopUpgradeModel.ThirdAuraUpgradeId } aura &&
                room.Units.Any(unit => unit is { AssetKey: "MonsterFunguy", Team: CombatTeam.Player, SpawnerCardId: 0,
                    BaseAttack: 12, Health: 28, MaxHealth: 28, Size: 1, HordeDefinition: { Attack: 3, Health: 4 },
                    Modifiers.Upgrades.Count: 4 } && unit.Status("horde")?.Stacks == 4 && unit.Modifiers.Upgrades.All(item =>
                    item.DataId == TroopAuraModel.Key(owner.Id) && item.AssetKey == aura.AssetKey && item.Stats.Health == 3 &&
                    item.Stats.Damage == 0 && item.Stats.Cost == 0 && item.Stats.Heal == 0 && item.Stats.Size == 0 &&
                    item.Stats.XCost == 0 && item.Stats.EquipmentLimit == 0 && item.Stats.UpgradeSlotCount == 0 &&
                    item.Statuses.Count == 0 && item.ExternalInteractions.Count == 0 && item.AbilityUpgrade == null &&
                    item.TriggerUpgrades?.Count is not > 0))) &&
                room.Units.Any(unit => unit is { AssetKey: "MonsterSycophant", Team: CombatTeam.Player, BaseAttack: 20 or 24,
                    Health: 35, MaxHealth: 35, Size: 2, Modifiers.Upgrades: [
                        { DataId: "eecb44d8-f417-4815-8438-3e4618231102", AssetKey: "LavaArmor" }, { DataId: "", AssetKey: "" }] } &&
                    context.FindCard(unit.SpawnerCardId)?.DataId == "6d2053e9-1a6a-4862-b348-76a92a92ea83" &&
                    (unit.BaseAttack == 20 || source.Rooms.SelectMany(floor => floor.Units).Any(fan => fan is
                        { AssetKey: "MonsterEnthusiasticFan", Team: CombatTeam.Player, Health: 5, MaxHealth: 5 } &&
                        context.FindCard(fan.SpawnerCardId)?.DataId == "adaaaa9d-56a9-4a5f-a347-ee71d4767df5") &&
                        source.Rooms.SelectMany(floor => floor.Units).Any(steward => steward is
                            { AssetKey: "TrainStewardSmall", Team: CombatTeam.Player, Health: 5, MaxHealth: 5 } &&
                            context.FindCard(steward.SpawnerCardId)?.DataId == "38e70e6a-20f3-4e36-8d31-c5110be07f87"))));
            bool OriginalTripleCompanions() => source.Rooms.Any(room => room.Units.Any(owner =>
                owner is { BaseAttack: 80, MaxHealth: 110, Size: 1, Triggers.Count: 1 } && IsOriginalChampionEnemyDeath(owner, context) &&
                owner.Modifiers?.TroopAura?.Upgrade.DataId == CardUnderlegionTroopUpgradeModel.ThirdAuraUpgradeId) &&
                room.Units.Any(unit => unit is { AssetKey: "MonsterSycophant", Team: CombatTeam.Player, BaseAttack: 24,
                    Health: 25, MaxHealth: 25, Size: 2, Modifiers.Upgrades.Count: 0 } &&
                    context.FindCard(unit.SpawnerCardId)?.DataId == "6d2053e9-1a6a-4862-b348-76a92a92ea83") &&
                room.Units.Any(unit => unit is { AssetKey: "MonsterGenerousHost", Team: CombatTeam.Player, BaseAttack: 5,
                    Health: 5, MaxHealth: 5, Size: 1, Modifiers.Upgrades.Count: 0 } && IsOriginalGreenRecruit(unit, context)) &&
                room.Units.Any(unit => unit is { AssetKey: "MonsterSneezyWhelp", Team: CombatTeam.Player, BaseAttack: 4,
                    Health: 1, MaxHealth: 1, Size: 1, Modifiers.Upgrades.Count: 0 } &&
                    context.FindCard(unit.SpawnerCardId)?.DataId == "eef8abd9-3550-4da6-a8bd-d5f63e9d2b16"));
            bool OriginalWoundedSteward() => source.Rooms.Any(room => room.Units.Any(owner =>
                owner is { BaseAttack: 80, Health: 84, MaxHealth: 110, Size: 1, Triggers.Count: 1 } &&
                IsOriginalChampionEnemyDeath(owner, context) &&
                owner.Modifiers?.TroopAura?.Upgrade.DataId == CardUnderlegionTroopUpgradeModel.ThirdAuraUpgradeId &&
                context.CardRegistry?.Any(card => card.DataId == "217d467c-77c2-481d-b78f-57aaf3dcd4a6" && card.EquippedUnitId == owner.Id) == true) &&
                room.Units.Any(unit => unit is { AssetKey: "TrainStewardBig", Team: CombatTeam.Player, BaseAttack: 8,
                    Health: 10, MaxHealth: 25, Size: 3, Modifiers.Upgrades.Count: 0 } &&
                    context.FindCard(unit.SpawnerCardId)?.DataId == "d14a50f3-728d-43e1-87f0-ef1b013f6678"));
            bool OriginalWoundedHorde() => source.Rooms.Any(room => room.Units.Any(owner => IsOriginalChampionEnemyDeath(owner, context) &&
                owner.Triggers.Count == 2 && IsOriginalMeatShieldHeal(owner.Triggers[1], owner, context) &&
                owner.Modifiers?.TroopAura?.Upgrade is { DataId: CardUnderlegionTroopUpgradeModel.ThirdAuraUpgradeId } aura &&
                room.Units.Any(unit => unit is { AssetKey: "MonsterFunguy", Team: CombatTeam.Player, SpawnerCardId: 0,
                    BaseAttack: 9, Health: 12, MaxHealth: 21, Size: 1, HordeDefinition: { Attack: 3, Health: 4 },
                    Modifiers.Upgrades.Count: 3 } && unit.Status("horde")?.Stacks == 3 &&
                    unit.Modifiers.Upgrades.All(item => item.DataId == TroopAuraModel.Key(owner.Id) && item.AssetKey == aura.AssetKey &&
                        item.Stats.Health == 3 && item.Stats.Damage == 0 && item.Stats.Cost == 0 && item.Stats.Heal == 0 &&
                        item.Stats.Size == 0 && item.Stats.XCost == 0 && item.Stats.EquipmentLimit == 0 &&
                        item.Stats.UpgradeSlotCount == 0 && item.Statuses.Count == 0 && item.ExternalInteractions.Count == 0 &&
                        item.AbilityUpgrade == null && item.TriggerUpgrades?.Count is not > 0))));
            bool OriginalFightClubHorde() => source.Rooms.Any(room => room.Units.Any(owner => IsOriginalChampionEnemyDeath(owner, context) &&
                owner.Modifiers?.TroopAura?.Upgrade is { DataId: CardUnderlegionTroopUpgradeModel.ThirdAuraUpgradeId } aura &&
                room.Units.Any(unit => unit is { AssetKey: "MonsterFunguy", Team: CombatTeam.Player, SpawnerCardId: 0,
                    BaseAttack: 27, Health: 28, MaxHealth: 28, Size: 1, HordeDefinition: { Attack: 3, Health: 4 },
                    Modifiers.Upgrades: var upgrades } && unit.Status("horde")?.Stacks == 4 && upgrades.Count == 5 &&
                    upgrades.Take(4).All(item => item.DataId == TroopAuraModel.Key(owner.Id) && item.AssetKey == aura.AssetKey &&
                        item.Stats.Health == 3 && item.Stats.Damage == 0 && item.Stats.Cost == 0 && item.Stats.Heal == 0 &&
                        item.Stats.Size == 0 && item.Stats.XCost == 0 && item.Stats.EquipmentLimit == 0 &&
                        item.Stats.UpgradeSlotCount == 0 && item.Statuses.Count == 0 && item.ExternalInteractions.Count == 0 &&
                        item.AbilityUpgrade == null && item.TriggerUpgrades?.Count is not > 0) &&
                    upgrades[4] is { DataId: RelicStartCombatAttachmentModel.Floor3UpgradeId, AssetKey: RelicStartCombatAttachmentModel.Floor3UpgradeKey,
                        Stats: { Damage: 15, Health: 0, Cost: 0, Heal: 0, Size: 0, XCost: 0, EquipmentLimit: 0, UpgradeSlotCount: 0 },
                        Statuses.Count: 0, ExternalInteractions.Count: 0, AbilityUpgrade: null, TriggerUpgrades: null })));
            bool OriginalFightClubWoundedHorde() => source.Rooms.Any(room => room.Units.Any(owner =>
                owner is { BaseAttack: 95, Health: 84, MaxHealth: 110, Size: 1, Triggers.Count: 1 } &&
                IsOriginalChampionEnemyDeath(owner, context) &&
                owner.Modifiers?.TroopAura?.Upgrade is { DataId: CardUnderlegionTroopUpgradeModel.ThirdAuraUpgradeId } aura &&
                context.CardRegistry?.Any(card => card.DataId == "217d467c-77c2-481d-b78f-57aaf3dcd4a6" && card.EquippedUnitId == owner.Id) == true &&
                room.Units.Any(unit => unit is { AssetKey: "MonsterFunguy", Team: CombatTeam.Player, SpawnerCardId: 0,
                    BaseAttack: 30, Health: 25, MaxHealth: 35, Size: 1, HordeDefinition: { Attack: 3, Health: 4 },
                    Modifiers.Upgrades: var upgrades } && unit.Status("horde")?.Stacks == 5 && upgrades.Count == 6 &&
                    upgrades.Take(5).All(item => item.DataId == TroopAuraModel.Key(owner.Id) && item.AssetKey == aura.AssetKey &&
                        item.Stats.Health == 3 && item.Stats.Damage == 0 && item.Stats.Cost == 0 && item.Stats.Heal == 0 &&
                        item.Stats.Size == 0 && item.Stats.XCost == 0 && item.Stats.EquipmentLimit == 0 &&
                        item.Stats.UpgradeSlotCount == 0 && item.Statuses.Count == 0 && item.ExternalInteractions.Count == 0 &&
                        item.AbilityUpgrade == null && item.TriggerUpgrades?.Count is not > 0) &&
                    upgrades[5] is { DataId: RelicStartCombatAttachmentModel.Floor3UpgradeId, AssetKey: RelicStartCombatAttachmentModel.Floor3UpgradeKey,
                        Stats: { Damage: 15, Health: 0, Cost: 0, Heal: 0, Size: 0, XCost: 0, EquipmentLimit: 0, UpgradeSlotCount: 0 },
                        Statuses.Count: 0, ExternalInteractions.Count: 0, AbilityUpgrade: null, TriggerUpgrades: null })) &&
                room.Units.Any(unit => unit is { AssetKey: "TrainStewardBig", Team: CombatTeam.Player, BaseAttack: 23,
                    Health: 10, MaxHealth: 25, Size: 3, Modifiers.Upgrades: [{ DataId: RelicStartCombatAttachmentModel.Floor3UpgradeId,
                        AssetKey: RelicStartCombatAttachmentModel.Floor3UpgradeKey, Stats.Damage: 15 }] } &&
                    context.FindCard(unit.SpawnerCardId)?.DataId == "d14a50f3-728d-43e1-87f0-ef1b013f6678"));
            bool fragileObserver = world != null && world.EnchanterIds.Count > 0 &&
                source.Rooms.SelectMany(room => room.Units).Any(unit => IsOriginalChampionEnemyDeath(unit, context) &&
                    unit.Modifiers?.TroopAura?.Upgrade.DataId == CardUnderlegionTroopUpgradeModel.ThirdAuraUpgradeId) &&
                TroopAuraModel.OnlyFragileBuffSource(world.Frame(context), allowTwoLivePreview:
                    OriginalExistingHorde() || OriginalSycophantCompanion() || OriginalWoundedHorde() || OriginalWoundedSteward() || OriginalFightClubWoundedHorde() || source.Rooms.SelectMany(room => room.Units).Any(unit => IsOriginalGreenRecruit(unit, context)),
                    allowPreviewRetired: true,
                    allowSingleRetiredPreview: source.Rooms.Any(room => room.Units.Any(unit => IsOriginalChampionEnemyDeath(unit, context)) &&
                        room.Units.Any(unit => IsOriginalGreenRecruit(unit, context))) || source.Rooms.Any(room =>
                        room.Units.Any(unit => IsOriginalGreenRecruit(unit, context)) && room.Units.Any(unit => unit is
                            { AssetKey: "MonsterGreedDragon", Team: CombatTeam.Player, BaseAttack: 34, Health: 35,
                                MaxHealth: 35, Size: 3 } && context.FindCard(unit.SpawnerCardId)?.DataId == "0060b286-4716-49e0-9714-3fd7811b9f72")),
                    allowTwoRetiredMixedPreview: OriginalSycophantCompanion() || OriginalFightClubHorde() || OriginalLavaFourHorde(),
                    allowSingleNonPreviewRetiredPreview: OriginalTripleCompanions());
            // Temporary player births without durable aura references are gone
            // before native decision capture assigns their synthetic identities.
            // Preserve observed births and all enemy births. Check the allocator
            // first so ordinary preview refreshes do not scan units or cards.
            if (context.Enchantments?.AutomaticLifecycle != true || preview.NextUnitId is not int nextId ||
                nextId <= firstUnitId || world == null || preview.CardRegistry == null ||
                world.EnchanterIds.Count != 0 && !fragileObserver &&
                    !TroopAuraModel.OnlyTitanskinSources(world.Rooms, world.RetainedUnits, world.EnchanterIds))
                return preview;
            bool enemyOnlyObservers = world.EnchanterIds.Count != 0;
            CardInstanceState[] newCards = preview.CardRegistry?.Where(card => card.InstanceId >= firstCardId).ToArray()
                ?? Array.Empty<CardInstanceState>();
            if (fragileObserver && newCards.Length != 0) return preview;
            if (newCards.Length == 0)
            {
                // Infectory and the original Green Recruit death fallback create an unowned character,
                // not a spawner card. Like an unobserved egg hatch, its preview
                // character is gone before native decision identity capture.
                // The calibrated player snapshot has no triggered-summon
                // observation catalog; controlled catalog captures observe
                // these temporary actors and retain their assigned identities.
                // Retain every other cardless birth until separately calibrated.
                if (context.SummonCatalog != null) return preview;
                CombatUnit[] births = world.Rooms.SelectMany(room => room.Units)
                    .Concat(world.RetainedUnits.Select(actor => actor.Unit)).Concat(observedBirths)
                    .Where(unit => unit.Id >= firstUnitId).GroupBy(unit => unit.Id).Select(group => group.First()).ToArray();
                bool roomFallback = context.RoomCapacities?.SelectMany(room => room.Attachments ??
                    Array.Empty<RoomCapacityAttachmentState>()).Any(attachment =>
                        attachment.DataId == "e1a39d88-4a77-4399-98b2-0b81f44fe6a8" &&
                        attachment.DeathEffect != null && RoomDeathEffectModel.Validate(attachment.DeathEffect) == null) == true;
                bool recruitFallback = source.Rooms.SelectMany(room => room.Units).Any(unit => IsOriginalGreenRecruit(unit, context));
                // The original calibrated champion enemy-death fallback has the
                // same unowned Fun Guy lifetime as Green Recruit. Its temporary
                // preview character is destroyed before decision identity capture.
                bool championFallback = source.Rooms.SelectMany(room => room.Units).Any(unit => IsOriginalChampionEnemyDeath(unit, context));
                if (!(roomFallback || recruitFallback || championFallback) || births.Length != nextId - firstUnitId || births.Any(unit =>
                    unit.Id >= nextId || unit.Team != CombatTeam.Player || unit.AssetKey != "MonsterFunguy" || unit.SpawnerCardId != 0)) return preview;
            }
            else if (context.SummonCatalog == null || nextId - firstUnitId != newCards.Length) return preview;
            if (world.Rooms.SelectMany(room => room.Units).Concat(world.RetainedUnits.Select(actor => actor.Unit)).Concat(observedBirths)
                .Any(unit => unit.Id >= firstUnitId && unit.Team != CombatTeam.Player ||
                    unit.Triggers.SelectMany(trigger => trigger.Effects).Any(effect => effect.Enchantment != null) &&
                    // The calibrated Titanskin sources only observe enemies;
                    // their presence does not retain an unowned player birth.
                    !(enemyOnlyObservers && unit.Team == CombatTeam.Enemy && world.EnchanterIds.Contains(unit.Id)))) return preview;
            var spawnerIds = (context.SummonCatalog?.Units ?? Array.Empty<SummonUnitDefinition>()).Where(unit => unit.Unit.Team == CombatTeam.Player && unit.Fallback != null)
                .Select(unit => unit.Fallback!.DataId).ToHashSet();
            foreach (CombatUnit actor in source.Rooms.SelectMany(room => room.Units).Where(unit =>
                unit.Team == CombatTeam.Player && unit.Triggers.SelectMany(trigger => trigger.Effects).Any(effect => effect.Summon != null)))
                if (context.FindCard(actor.SpawnerCardId) is CardInstanceState spawner) spawnerIds.Add(spawner.DataId);
            if (newCards.Any(card => !spawnerIds.Contains(card.DataId))) return preview;
            forgotten = Enumerable.Range(firstUnitId, nextId - firstUnitId).ToArray();
            preview = preview.WithNextUnitId(firstUnitId)
                .WithCardRegistry(preview.CardRegistry!.Where(card => card.InstanceId < firstCardId).ToArray())
                .WithNextCardId(firstCardId).WithEnchantments(new EnchantmentWorld(world.Rooms.Select(room =>
                    new RoomCombatState(room.RoomIndex, room.Deployment, room.Units.Where(unit => unit.Id < firstUnitId).ToArray(),
                        room.ExternalInteractions, null, room.Preview, room.ActiveCompanionBossIds)).ToArray(), world.Movement,
                    world.EnemySlotsPerRoom, world.RetainedUnits.Where(actor => actor.Unit.Id < firstUnitId).ToArray(),
                    world.EnchanterIds, world.AllowUpdates, world.Updating, world.Preview, world.TestRng,
                    world.AutomaticLifecycle, world.TemporaryPreviewUnitIds));
            if (preview.SpawnPoints is BattleSpawnPoints points)
                preview = preview.WithSpawnPoints(points.WithState(points.Groups, points.Units.Where(unit => unit.UnitId < firstUnitId).ToArray()));
            return preview;
        }

        internal static CombatUnit InitialPrimary(CardPlayRule definition, CardInstanceState? card, int unitId,
            IReadOnlyList<string> disabledAbilities, CombatStatus abilityMarker)
        {
            var cards = new CardCycleState(Array.Empty<CardToken>(), Array.Empty<CardToken>(), Array.Empty<CardToken>(),
                new UnityRng(1, 2, 3, 4), 0, Array.Empty<string>());
            var context = new CombatContext(cards, new UnityRng(1, 2, 3, 4), 0, 1, 10,
                new[] { abilityMarker }, permanentlyDisabledAbilities: disabledAbilities);
            CombatUnit raw = definition.SpawnUnit ?? throw new ArgumentException("A preview birth needs its character definition.");
            CombatUnit initialized = AbilityLifecycleModel.InitialAtSpawn(raw, card, context, out string? error);
            if (error != null) throw new ArgumentException(error);
            CombatUnit resolved = card == null ? initialized : CardModifierModel.Resolve(definition.WithSpawn(initialized), card).SpawnUnit!;
            return InitialPrimary(raw, resolved, unitId, card?.InstanceId ?? 0);
        }

        internal static CombatUnit InitialPrimary(CombatUnit raw, CombatUnit resolved, int unitId, int sourceCardId)
        {
            UnitModifiers? metadata = raw.Modifiers;
            var modifiers = metadata == null ? null : new UnitModifiers(metadata.AttackDamage, 0, 0,
                metadata.RawSize, metadata.EquipmentLimit, metadata.CanBeHealed, false,
                Array.Empty<CardUpgradeModifier>(), Array.Empty<StatisticCount>(), metadata.SpawnerMatchesDefinition,
                metadata.DamageBuffFromStatusEffects.HasValue ? 0 : (int?)null,
                metadata.MagebladeDamagePerMagicPower.HasValue ? 0 : (int?)null, metadata.StatusAttackModifiers,
                metadata.IsChampion, metadata.SelfAttackRules, metadata.RoomResources, metadata.RoomStatuses, metadata.RoomTriggerCounts, metadata.SpawnCopySubtype, metadata.Graft, metadata.SynergizeStatuses == null ? null : Array.Empty<StatisticCount>(), metadata.Sentry == null ? null : new SentryState(Array.Empty<int>()), metadata.TroopAura?.WithTargets(Array.Empty<int>()));
            var death = raw.DeathState == null ? new UnitDeathState(false, false, false) : new UnitDeathState(false, false, false,
                isSacrifice: raw.DeathState.IsSacrifice.HasValue ? false : (bool?)null,
                statisticsListenerOnce: raw.DeathState.StatisticsListenerOnce.HasValue ? false : (bool?)null,
                isDespawned: raw.DeathState.IsDespawned.HasValue ? false : (bool?)null,
                isDestroyed: raw.DeathState.IsDestroyed.HasValue ? false : (bool?)null);
            return new CombatUnit(unitId, raw.AssetKey, raw.Team, raw.BaseAttack, raw.MaxHealth, raw.MaxHealth,
                raw.CanAttack, false, false, Array.Empty<CombatStatus>(), resolved.Triggers, sourceCardId,
                Math.Max(1, Math.Min(6, metadata?.RawSize ?? raw.Size)), Array.Empty<string>(), raw.Subtypes,
                modifiers, raw.IsBoss, raw.LastAttackerId.HasValue ? 0 : (int?)null,
                raw.StatusRegistry == null ? null : Array.Empty<CombatStatus>(),
                raw.EquipmentCards == null ? null : Array.Empty<int>(), resolved.NextTriggerId, resolved.Ability,
                raw.StatusDictionary == null ? null : new StatusDictionaryState(Array.Empty<string?>(), Array.Empty<int>()),
                raw.AbilityRules, raw.HordeDefinition, false, raw.SacrificeCardId.HasValue ? 0 : (int?)null, death,
                bumpRules: raw.BumpRules, lastFeederId: raw.LastFeederId, moonPhaseMask: raw.MoonPhaseMask);
        }

        internal static CombatUnit Restore(CombatUnit preview, IReadOnlyCollection<int> removedIds, bool afterFrame = false)
        {
            CombatUnit primary = preview.DeathState?.PreviewPrimary ?? throw new ArgumentException("A preview-born actor needs its initial primary state.");
            CombatUnit restored = PreviewEffectsModel.Restore(primary, preview, removedIds);
            var triggers = restored.Triggers.Select(trigger =>
            {
                CombatTrigger observed = preview.Triggers.Single(item => item.StateId == trigger.StateId);
                return trigger.WithEffects(trigger.Effects.Select((effect, index) =>
                {
                    EnchantmentRule? shared = observed.Effects[index].Enchantment;
                    return effect.Enchantment == null || shared == null ? effect : effect.WithEnchantment(new EnchantmentRule(
                        effect.Enchantment.Targeting, effect.Enchantment.StatusPool, effect.Enchantment.State,
                        !afterFrame && shared.Bound, shared.HasParentCard));
                }).ToArray());
            }).ToArray();
            return new CombatUnit(restored.Id, restored.AssetKey, restored.Team, restored.BaseAttack, restored.Health,
                restored.MaxHealth, restored.CanAttack, restored.IsPyre, restored.EndsBattleOnDeath, restored.Statuses,
                triggers, restored.SpawnerCardId, restored.Size, preview.StatusImmunities, restored.Subtypes,
                restored.Modifiers, restored.IsBoss, restored.LastAttackerId, restored.StatusRegistry, restored.EquipmentCards,
                restored.NextTriggerId, restored.Ability, restored.StatusDictionary, restored.AbilityRules,
                restored.HordeDefinition, restored.IsSpawning, restored.SacrificeCardId,
                restored.DeathState!.WithLifecycle(restored.DeathState.IsDespawned,
                    restored.Team == CombatTeam.Player ? true : restored.DeathState.IsDestroyed),
                bumpRules: restored.BumpRules, lastFeederId: restored.LastFeederId, moonPhaseMask: restored.MoonPhaseMask);
        }

        internal static EnchantmentRetainedUnit[] Retain(EnchantmentWorld observed, int firstPreviewId, IReadOnlyCollection<int> removedIds)
            => observed.Rooms.SelectMany(room => room.Units).Concat(observed.RetainedUnits.Select(actor => actor.Unit))
                .Where(unit => unit.Id >= firstPreviewId).Select(unit => new EnchantmentRetainedUnit(
                    Restore(unit, removedIds), -1, false)).ToArray();

        internal static CardInstanceState[] RestoreSourceCards(CombatContext preview, int firstCardId, ISet<int> restoredIds)
            => (preview.CardRegistry ?? Array.Empty<CardInstanceState>()).Where(card => card.InstanceId >= firstCardId).Select(card =>
            {
                IReadOnlyList<int>? cached = card.RawPlayedRoomUnitIds ?? card.PlayedRoomUnitIds;
                if (card.PlayedRoomUnitIds == null || cached == null) return card;
                int[] restored = cached.Where(restoredIds.Contains).ToArray();
                return card.WithRoomCacheState(restored, card.RawPlayedRoomUnitIds == null ? null : restored);
            }).ToArray();
    }
}
