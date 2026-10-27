using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class TroopAuraState
    {
        public string Subtype { get; }
        public CardUpgradeModifier Upgrade { get; }
        public IReadOnlyList<CombatStatus> Statuses { get; }
        public IReadOnlyList<int> Targets { get; }
        public TroopAuraState(string subtype, CardUpgradeModifier upgrade, IReadOnlyList<CombatStatus> statuses, IReadOnlyList<int> targets)
        { Subtype = subtype; Upgrade = upgrade; Statuses = Array.AsReadOnly(statuses.ToArray()); Targets = Array.AsReadOnly(targets.ToArray()); }
        internal TroopAuraState WithTargets(IEnumerable<int> targets) => new TroopAuraState(Subtype, Upgrade, Statuses, targets.ToArray());
    }
    internal static class TroopAuraModel
    {
        internal static string Key(int id) => "troop-aura:" + id;
        internal static bool Matches(CombatUnit unit, TroopAuraState rule) => unit.Team == CombatTeam.Player && !unit.IsPyre &&
            unit.Health > 0 && unit.DeathState?.IsDestroyed != true && SubtypeMatches(unit, rule);

        private static bool SubtypeMatches(CombatUnit unit, TroopAuraState rule) =>
            rule.Subtype == CardUnderlegionTroopUpgradeModel.NoneSubtype || unit.Subtypes.Contains(rule.Subtype);

        internal static EnchantmentCombatResult UpdateAll(EnchantmentCombatState source, IReadOnlyList<int> order, bool automaticLifecycle = false)
        {
            TrainCombatState train = source.Train;
            var callbacks = new List<RoomCombatModel.QueuedCharacterTrigger>();
            string? error = null;
            foreach (int ownerId in order)
            {
                RoomCombatState? room = train.Rooms.FirstOrDefault(room => room.Units.Any(unit => unit.Id == ownerId));
                CombatUnit? owner = room?.Units.Single(unit => unit.Id == ownerId);
                TroopAuraState? rule = owner?.Modifiers?.TroopAura;
                if (rule == null || owner!.Health <= 0 || owner.DeathState?.IsDestroyed == true) continue;
                var targets = room!.Units.Where(unit => Matches(unit, rule)).Select(unit => unit.Id).ToArray();
                // A stable target cache does not apply/remove any modifiers. In
                // particular, a Squad Leader without Fun Guys must not reject an
                // unrelated enemy enchanter. Keep the ordering boundary whenever
                // a living target would actually gain or lose the troop aura.
                bool changesModifiers = targets.Any(id => !rule.Targets.Contains(id)) ||
                    rule.Targets.Any(id => !targets.Contains(id) && train.Rooms.SelectMany(item => item.Units)
                        .Any(unit => unit.Id == id && unit.Health > 0 && unit.DeathState?.IsDestroyed != true));
                bool orderedAddition = source.EnchanterIds.Count > 0 && changesModifiers &&
                    (KnownBirthAddition(source, room, owner, rule, targets) ||
                     KnownChampionAddition(source, room, owner, rule, targets));
                if (source.EnchanterIds.Count > 0 && changesModifiers && !orderedAddition)
                { error = "Combined troop upgrades and enchantment source ordering require native calibration."; break; }
                // The frame strips its shared world to stay finite. A troop
                // write still validates every actor, including enemy summoners.
                // Borrow the caller's complete world with native reentry closed.
                bool borrowWorld = orderedAddition || automaticLifecycle && source.EnchanterIds.Count == 0 &&
                    room.Units.Any(unit => unit.Triggers.Any(trigger => trigger.Effects.Any(effect => effect.Summon?.EnemyHero == true)));
                var cache = rule.Targets.ToList();
                foreach (int id in rule.Targets.Reverse())
                {
                    CombatUnit? unit = train.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.Id == id);
                    if (unit == null || unit.Health <= 0 || unit.DeathState?.IsDestroyed == true)
                    { if (!source.Preview) cache.Remove(id); continue; }
                    if (targets.Contains(id)) continue;
                    if (!ApplyAll(id, false)) break;
                    if (!source.Preview) cache.Remove(id);
                }
                if (error != null) break;
                foreach (int id in targets.Where(id => !rule.Targets.Contains(id)))
                {
                    if (!ApplyAll(id, true)) break;
                    if (!source.Preview) cache.Add(id);
                }
                if (error != null) break;
                RoomCombatState latest = train.Rooms.Single(room => room.Units.Any(unit => unit.Id == ownerId));
                train = Replace(train, latest.WithUnits(latest.Units.Select(unit => unit.Id == ownerId ?
                    unit.WithModifiers(unit.Modifiers!.WithTroopAura(rule.WithTargets(cache))) : unit).ToArray()));

                bool ApplyAll(int id, bool add)
                {
                    foreach (CombatStatus status in rule.Statuses)
                    {
                        RoomCombatState targetRoom = TargetRoom(id);
                        RoomCombatResult changed = add ? StatusApplicationModel.ApplyRetained(targetRoom, id, status, 0) :
                            StatusRemovalModel.Remove(targetRoom, id, status.Id, status.Stacks);
                        if (!Accept(changed)) return false;
                    }
                    CombatUnit unit = train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);
                    return Upgrade(id, unit.Status("horde")?.Stacks ?? 0, add);
                }
                bool Upgrade(int id, int count, bool add)
                {
                    CardUpgradeModifier upgrade = rule.Upgrade.WithRuntimeIdentity(Key(ownerId));
                    for (int i = 0; i < count; i++)
                    {
                        RoomCombatState targetRoom = TargetRoom(id);
                        if (!Accept(UnitModifierModel.ApplyDirect(targetRoom, id, upgrade, !add, upgrade.DataId))) return false;
                    }
                    return true;
                }
                bool Accept(RoomCombatResult changed)
                {
                    if (!changed.Supported) { error = changed.UnsupportedReason; return false; }
                    RoomCombatState updated = changed.State!;
                    if (borrowWorld) updated = updated.WithContext(updated.Context!.WithEnchantments(null));
                    train = Replace(train, updated); callbacks.AddRange(changed.PendingCallbacks); return true;
                }
                RoomCombatState TargetRoom(int id)
                {
                    RoomCombatState targetRoom = train.Rooms.Single(room => room.Units.Any(unit => unit.Id == id));
                    if (!borrowWorld) return targetRoom;
                    // The outer pass already updated these pure status sources.
                    // Borrow its captured world for validation while retaining
                    // the native reentry guard throughout the troop writes.
                    var world = new EnchantmentWorld(train.Rooms, train.Movement, train.EnemySlotsPerRoom,
                        source.RetainedUnits, source.EnchanterIds, source.AllowUpdates, true, source.Preview,
                        source.TestRng, true, source.TemporaryPreviewUnitIds);
                    return targetRoom.WithContext(targetRoom.Context!.WithEnchantments(world));
                }
            }
            return new EnchantmentCombatResult(error == null ? new EnchantmentCombatState(train, source.RetainedUnits,
                source.EnchanterIds, source.AllowUpdates, source.Updating, source.Preview, source.TestRng) : null,
                Array.Empty<EnchantmentRequest>(), callbacks, error);
        }

        private static bool KnownChampionAddition(EnchantmentCombatState source, RoomCombatState room,
            CombatUnit owner, TroopAuraState rule, IReadOnlyList<int> targets)
        {
            // Native tier-two/Titanskin preview births receive repeated HP
            // writes; the tier-three/Fragile observation covers primary mode.
            // These sources affect disjoint teams. Other enchantments, living
            // departures and modified champion auras retain the ordering guard.
            CardUpgradeModifier upgrade = rule.Upgrade;
            bool second = upgrade.DataId == CardUnderlegionTroopUpgradeModel.SecondAuraUpgradeId &&
                upgrade.AssetKey == CardUnderlegionTroopUpgradeModel.SecondAuraUpgradeKey && upgrade.Stats.Health == 2;
            bool third = upgrade.DataId == CardUnderlegionTroopUpgradeModel.ThirdAuraUpgradeId &&
                upgrade.AssetKey == CardUnderlegionTroopUpgradeModel.ThirdAuraUpgradeKey && upgrade.Stats.Health == 3;
            if (source.Preview != room.Preview || owner.AssetKey != "MonsterChampionUnderlegion1" ||
                owner.Team != CombatTeam.Player || rule.Subtype != CardUnderlegionTroopUpgradeModel.NoneSubtype ||
                rule.Statuses.Count != 0 || (!second && !third) ||
                new[] { "Damage", "Cost", "Heal", "Size", "XCost", "EquipmentLimit", "UpgradeSlotCount" }
                    .Any(stat => upgrade.Stats.Value(stat) != 0) ||
                upgrade.Statuses.Count != 0 || upgrade.ExternalInteractions.Count != 0 ||
                upgrade.AbilityUpgrade != null || upgrade.TriggerUpgrades?.Count > 0 ||
                upgrade.Unique || upgrade.RemoveOnDiscard || upgrade.ExcludeFromClones || upgrade.UnhealedHealth != 0 ||
                upgrade.DamageBuff != 0 || upgrade.RestrictSizeToRoomCapacity || upgrade.MagicPowerTraitScalingOnly ||
                upgrade.CloneDamageBase != null || upgrade.CloneHealBase != null ||
                upgrade.Lifecycle is not { AddedTraits.Count: 0, RemovedRuntimeTypes.Count: 0, Triggers.Count: 0,
                    ReplacedAssets.Count: 0, TraitsModified.Count: 0 } ||
                rule.Targets.Any(id => !targets.Contains(id) && source.Train.Rooms.SelectMany(floor => floor.Units)
                    .Any(unit => unit.Id == id && unit.Health > 0 && unit.DeathState?.IsDestroyed != true))) return false;
            int[] added = targets.Where(id => !rule.Targets.Contains(id)).ToArray();
            if (added.Length == 0 || added.Any(id => room.Units.Single(unit => unit.Id == id) is not
                    { AssetKey: "MonsterFunguy" } ||
                source.Train.Context?.SpawnPoints?.Units.Any(point => point.UnitId == id &&
                    (source.Preview ? point.SpawnedInPreview : !point.SpawnedInPreview &&
                        point.Current?.RoomIndex == room.RoomIndex && point.Current.Team == CombatTeam.Player)) != true)) return false;
            return third ? !source.Preview && OnlyFragileBuffSource(source) :
                OnlyTitanskinSources(source.Train.Rooms, source.RetainedUnits, source.EnchanterIds);
        }

        private static bool OnlyFragileBuffSource(EnchantmentCombatState source)
        {
            if (source.EnchanterIds.Count != 1) return false;
            CombatUnit? owner = source.Train.Rooms.SelectMany(room => room.Units)
                .SingleOrDefault(unit => unit.Id == source.EnchanterIds[0]);
            if (owner is not { AssetKey: "Boss_Level8_Fragile", Team: CombatTeam.Enemy, MaxHealth: 3000 } ||
                owner.Health <= 0 || owner.DeathState?.IsDestroyed == true) return false;
            var effects = owner.Triggers.SelectMany(trigger => trigger.Effects).Where(effect => effect.Enchantment != null).ToArray();
            return effects.Length == 1 && effects[0].Enchantment is
                { Bound: true, HasParentCard: false, StatusPool.Count: 1 } aura &&
                aura.Targeting is { Type: "AddStatus", Target: "Room", Value: 0, Range: null, OnlyIfNoEnemies: false,
                    AllowEnemy: true, AllowPlayer: false,
                    Filters: { Health: "Both", RequiredStatuses.Count: 0, ExcludedStatuses.Count: 0,
                        IgnoreBosses: false, Subtype: "", ExcludedSubtypes.Count: 0 } } &&
                aura.StatusPool[0] is { Id: "buff", Stacks: 3, ParamInt: 2 };
        }

        internal static bool OnlyTitanskinSources(IReadOnlyList<RoomCombatState> rooms,
            IReadOnlyList<EnchantmentRetainedUnit> retainedUnits, IReadOnlyList<int> enchanterIds)
        {
            CombatUnit[] enchanters = rooms.SelectMany(floor => floor.Units)
                .Where(unit => enchanterIds.Contains(unit.Id)).ToArray();
            var retired = retainedUnits.Where(actor => enchanterIds.Contains(actor.Unit.Id) &&
                !enchanters.Any(unit => unit.Id == actor.Unit.Id)).ToArray();
            if (retired.Any(actor => actor.Unit.AssetKey != "EnchanterT2_Titanskin" || actor.Unit.Team != CombatTeam.Enemy ||
                actor.Unit.Health > 0 || actor.Unit.DeathState?.IsDestroyed != true || actor.RoomIndex != -1) ||
                enchanters.Any(unit => unit.Health <= 0 || unit.DeathState?.IsDestroyed == true)) return false;
            return enchanterIds.Count > 0 && enchanters.Length + retired.Length == enchanterIds.Count &&
                enchanters.Concat(retired.Select(actor => actor.Unit)).All(unit =>
            {
                var effects = unit.Triggers.SelectMany(trigger => trigger.Effects).Where(effect => effect.Enchantment != null).ToArray();
                return unit.AssetKey == "EnchanterT2_Titanskin" && unit.Team == CombatTeam.Enemy &&
                    effects.Length == 1 && effects[0].Enchantment is { HasParentCard: false, StatusPool.Count: 1 } aura &&
                    (aura.Bound || unit.DeathState?.IsDestroyed == true) &&
                    aura.Targeting is { Type: "AddStatus", Target: "Room", Value: 0, Range: null, OnlyIfNoEnemies: false,
                        AllowEnemy: true, AllowPlayer: false,
                        Filters: { Health: "Both", RequiredStatuses.Count: 0, ExcludedStatuses.Count: 0,
                            IgnoreBosses: false, Subtype: "", ExcludedSubtypes.Count: 0 } } &&
                    aura.StatusPool[0] is { Id: "titanskin", Stacks: 5, ParamInt: 1 };
            });
        }

        private static bool KnownBirthAddition(EnchantmentCombatState source, RoomCombatState room,
            CombatUnit owner, TroopAuraState rule, IReadOnlyList<int> targets)
        {
            // Native Squad Leader applies its primary-cache-missing upgrade in
            // preview too. Dead cached actors do not cause a removal write and
            // stay in the primary cache during preview; a new birth still gets
            // its uncached upgrade. Living departures stay guarded.
            if (source.Preview != room.Preview || targets.Count != 1 || rule.Targets.Contains(targets[0]) ||
                rule.Targets.Any(id => !source.RetainedUnits.Any(actor => actor.Unit.Id == id &&
                    actor.Unit.Health <= 0 && actor.Unit.DeathState?.IsDestroyed == true)) ||
                owner.AssetKey != "MonsterSquadLeader" || rule.Subtype != "SubtypesData_FunGuy" ||
                rule.Upgrade.DataId != "8538457b-b33a-4a3d-9596-5929a2de5936" ||
                rule.Upgrade.AssetKey != "SquadLeader_Upgrade" || rule.Upgrade.Stats.Damage != 2 ||
                rule.Upgrade.Stats.Health != 0 || rule.Upgrade.Stats.Cost != 0 || rule.Upgrade.Stats.Heal != 0 ||
                rule.Upgrade.Stats.Size != 0 || rule.Upgrade.Stats.XCost != 0 || rule.Upgrade.Stats.EquipmentLimit != 0 ||
                rule.Upgrade.Stats.UpgradeSlotCount != 0 || rule.Upgrade.Statuses.Count != 0 ||
                rule.Upgrade.ExternalInteractions.Count != 0 || rule.Upgrade.AbilityUpgrade != null ||
                rule.Upgrade.TriggerUpgrades?.Count > 0 ||
                rule.Statuses is not [{ Id: "trample", Stacks: 1, ParamInt: 0 }] ||
                source.Train.Context?.SpawnPoints?.Units.Any(point => point.UnitId == targets[0] &&
                    (source.Preview ? point.SpawnedInPreview : !point.SpawnedInPreview &&
                        point.Current?.RoomIndex == room.RoomIndex && point.Current.Team == CombatTeam.Player)) != true ||
                room.Units.Single(unit => unit.Id == targets[0]).AssetKey != "MonsterFunguy" || source.EnchanterIds.Count is not (2 or 3))
                return false;
            var enchanters = source.Train.Rooms.SelectMany(floor => floor.Units.Select(unit => (floor.RoomIndex, Unit: unit)))
                .Where(actor => source.EnchanterIds.Contains(actor.Unit.Id)).ToArray();
            bool PlayerAura((int RoomIndex, CombatUnit Unit) actor) => actor.RoomIndex == room.RoomIndex &&
                Signature(actor.Unit, "MonsterFaeMooncaller", CombatTeam.Player, "conduit", 3, 1, false, true);
            if (enchanters.Length is not (1 or 2) || enchanters.Count(PlayerAura) != 1 ||
                enchanters.Any(actor => !PlayerAura(actor) && (actor.RoomIndex == room.RoomIndex ||
                    !Signature(actor.Unit, "EnchanterT1_Speed", CombatTeam.Enemy, "haste", 1, 2, true, false)))) return false;
            // Native RoomManager only updates actors still in the tower. A
            // destroyed Haste owner remains in the captured registry, both
            // before and after its deferred binding cleanup. Keep that object
            // unchanged; it contributes no troop writes. Never infer inactivity
            // from the registry length, binding flag or health alone.
            var retired = source.RetainedUnits.Where(actor => source.EnchanterIds.Contains(actor.Unit.Id) &&
                !enchanters.Any(active => active.Unit.Id == actor.Unit.Id)).ToArray();
            return retired.Length == source.EnchanterIds.Count - enchanters.Length && retired.All(actor =>
                actor.RoomIndex == -1 && !actor.Preview &&
                Signature(actor.Unit, "EnchanterT1_Speed", CombatTeam.Enemy, "haste", 1, 2, true, false, active: false));

            bool Signature(CombatUnit unit, string asset, CombatTeam team, string status, int count, int param,
                bool enemy, bool player, bool active = true)
            {
                var effects = unit.Triggers.SelectMany(trigger => trigger.Effects).Where(effect => effect.Enchantment != null).ToArray();
                return unit.AssetKey == asset && unit.Team == team && effects.Length == 1 &&
                    (active ? unit.Health > 0 && unit.DeathState?.IsDestroyed != true : unit.Health <= 0 && unit.DeathState?.IsDestroyed == true) &&
                    effects[0].Enchantment is EnchantmentRule aura && (!active || aura.Bound) && !aura.HasParentCard &&
                    aura.Targeting is { Type: "AddStatus", Target: "Room", Value: 0 } &&
                    aura.Targeting.Range == null && !aura.Targeting.OnlyIfNoEnemies &&
                    aura.Targeting.Filters is { Health: "Both", RequiredStatuses.Count: 0, ExcludedStatuses.Count: 0,
                        IgnoreBosses: false, Subtype: "", ExcludedSubtypes.Count: 0 } &&
                    aura.Targeting.AllowEnemy == enemy && aura.Targeting.AllowPlayer == player &&
                    aura.StatusPool.Count == 1 && aura.StatusPool[0].Id == status && aura.StatusPool[0].Stacks == count &&
                    aura.StatusPool[0].ParamInt == param;
            }
        }

        internal static RoomCombatResult TroopChanged(RoomCombatState source, CombatUnit target, string kind, int total, int delta)
        {
            var callbacks = new List<RoomCombatModel.QueuedCharacterTrigger>();
            int targetId = target.Id;
            RoomCombatState room = source.Units.Any(unit => unit.Id == targetId) ? source : source.WithUnits(source.Units.Concat(new[] { target }).ToArray());
            CombatUnit retained = target;
            if (kind == "OnTroopAdded" && total == delta) return new RoomCombatResult(room, RoomOutcome.Exchanged, 0, new List<CombatEvent>());
            foreach (CombatUnit owner in source.Units.Where(unit => unit.Modifiers?.TroopAura != null))
            {
                TroopAuraState rule = owner.Modifiers!.TroopAura!;
                if (target.Team != CombatTeam.Player || target.IsPyre || !SubtypeMatches(target, rule)) continue;
                CardUpgradeModifier upgrade = rule.Upgrade.WithRuntimeIdentity(Key(owner.Id));
                for (int i = 0; i < delta; i++)
                {
                    if (!room.Units.Any(unit => unit.Id == targetId)) room = room.WithUnits(room.Units.Concat(new[] { retained }).ToArray());
                    RoomCombatResult changed = UnitModifierModel.ApplyDirect(BorrowWriteWorld(room),
                        targetId, upgrade, kind == "OnTroopRemoved", upgrade.DataId, allowDyingTarget: true);
                    if (!changed.Supported) return changed;
                    callbacks.AddRange(changed.PendingCallbacks);
                    retained = changed.State!.Units.Concat(changed.RetainedUnits).First(unit => unit.Id == targetId);
                    room = changed.State!.WithContext(changed.State.Context?.WithEnchantments(source.Context?.Enchantments));
                }
            }
            return new RoomCombatResult(EnchantmentWorldModel.Sync(room, new[] { retained }), RoomOutcome.Exchanged, 0,
                new List<CombatEvent>(), pendingCallbacks: callbacks, retainedUnits: new[] { retained });
        }
        internal static RoomCombatResult OwnerKilled(RoomCombatState source, CombatUnit owner)
        {
            TroopAuraState rule = owner.Modifiers!.TroopAura!;
            RoomCombatState room = BorrowWriteWorld(source);
            var callbacks = new List<RoomCombatModel.QueuedCharacterTrigger>();
            CardUpgradeModifier upgrade = rule.Upgrade.WithRuntimeIdentity(Key(owner.Id));
            foreach (int id in source.Units.Where(unit => Matches(unit, rule)).Select(unit => unit.Id))
            {
                foreach (CombatStatus status in rule.Statuses)
                {
                    RoomCombatResult removed = StatusRemovalModel.Remove(room, id, status.Id, status.Stacks);
                    if (!removed.Supported) return removed;
                    room = removed.State!; callbacks.AddRange(removed.PendingCallbacks);
                }
                int count = room.Units.Single(unit => unit.Id == id).Status("horde")?.Stacks ?? 0;
                for (int index = 0; index < count; index++)
                {
                    RoomCombatResult removed = UnitModifierModel.ApplyDirect(room, id, upgrade, true, upgrade.DataId);
                    if (!removed.Supported) return removed;
                    room = removed.State!; callbacks.AddRange(removed.PendingCallbacks);
                }
            }
            room = room.WithContext(room.Context?.WithEnchantments(source.Context?.Enchantments));
            return new RoomCombatResult(EnchantmentWorldModel.Sync(room, new[] { owner }), RoomOutcome.Exchanged, 0,
                new List<CombatEvent>(), pendingCallbacks: callbacks, retainedUnits: new[] { owner });
        }

        private static TrainCombatState Replace(TrainCombatState source, RoomCombatState room) => new TrainCombatState(
            source.Rooms.Select(old => old.RoomIndex == room.RoomIndex ? room : old.WithContext(room.Context)).ToArray(),
            source.Movement, source.EnemySlotsPerRoom, room.Context);

        private static RoomCombatState BorrowWriteWorld(RoomCombatState room)
        {
            if (room.Context?.Enchantments is not { AutomaticLifecycle: true } world)
                return room.WithContext(room.Context?.WithEnchantments(null));
            // These direct troop writes run inside the caller's lifecycle
            // operation. Retain the actual world for validating other captured
            // enchanters while preventing an additional whole-train reentry.
            var borrowed = new EnchantmentWorld(world.Rooms.Select(old => old.RoomIndex == room.RoomIndex ? room : old).ToArray(),
                world.Movement, world.EnemySlotsPerRoom,
                world.RetainedUnits.Where(actor => !room.Units.Any(unit => unit.Id == actor.Unit.Id)).ToArray(),
                world.EnchanterIds, world.AllowUpdates, true, world.Preview, world.TestRng,
                world.AutomaticLifecycle, world.TemporaryPreviewUnitIds);
            return room.WithContext(room.Context.WithEnchantments(borrowed));
        }
    }
}
