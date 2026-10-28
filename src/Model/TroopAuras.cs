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
                bool livingDeparture = rule.Targets.Any(id => !targets.Contains(id) && train.Rooms.SelectMany(item => item.Units)
                    .Any(unit => unit.Id == id && unit.Health > 0 && unit.DeathState?.IsDestroyed != true));
                bool changesModifiers = targets.Any(id => !rule.Targets.Contains(id)) || livingDeparture;
                // No statuses and no horde stacks mean additions update only
                // the target cache; no modifier write can interleave here.
                bool cacheOnly = !livingDeparture && rule.Statuses.Count == 0 && targets.Except(rule.Targets)
                    .SelectMany(id => train.Rooms.SelectMany(item => item.Units).Where(unit => unit.Id == id &&
                        unit.Health > 0 && unit.DeathState?.IsDestroyed != true))
                    .All(unit => (unit.Status("horde")?.Stacks ?? 0) == 0);
                bool orderedAddition = source.EnchanterIds.Count > 0 && changesModifiers &&
                    (cacheOnly || KnownBirthAddition(source, room, owner, rule, targets) ||
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
            // Native tier-two/Titanskin and tier-three/Fragile preview births
            // receive repeated HP writes; the primary cache is not changed.
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
            if (added.Length == 0 || added.Any(id =>
                source.Train.Context?.SpawnPoints?.Units.Any(point => point.UnitId == id &&
                    (source.Preview ? point.SpawnedInPreview : !point.SpawnedInPreview &&
                        point.Current?.RoomIndex == room.RoomIndex && point.Current.Team == CombatTeam.Player)) != true)) return false;
            bool originalChampion = !source.Preview && source.Train.Context != null &&
                PreviewBirthModel.IsOriginalChampionEnemyDeath(owner, source.Train.Context);
            bool originalSycophant = originalChampion && added.Length == 1 && room.Units.Single(unit => unit.Id == added[0]) is
                { AssetKey: "MonsterSycophant", Team: CombatTeam.Player, BaseAttack: 20, Health: 25, MaxHealth: 25, Size: 2 } sycophant &&
                sycophant.Status("horde") == null && source.Train.Context!.FindCard(sycophant.SpawnerCardId)?.DataId == "6d2053e9-1a6a-4862-b348-76a92a92ea83";
            bool originalRecruit = originalChampion && added.Length == 1 && room.Units.Single(unit => unit.Id == added[0]) is
                { AssetKey: "MonsterGenerousHost", Team: CombatTeam.Player, BaseAttack: 5, Health: 5, MaxHealth: 5, Size: 1,
                    Modifiers.Upgrades.Count: 0 } recruit && recruit.Status("horde") == null &&
                PreviewBirthModel.IsOriginalGreenRecruit(recruit, source.Train.Context!);
            bool originalWhelp = originalChampion && added.Length == 1 && room.Units.Single(unit => unit.Id == added[0]) is
                { AssetKey: "MonsterSneezyWhelp", Team: CombatTeam.Player, BaseAttack: 4, Health: 1, MaxHealth: 1, Size: 1,
                    Modifiers.Upgrades.Count: 0, Ability: { DataId: "f3c7769a-4f9b-43cf-aacd-cf32c14e3082", HasAbility: true, Cooldown: 2 },
                    Triggers: [
                        { Kind: "OnSpawn", FireCount: 1, Effects: [{ Type: "CardEffectAddStatusEffect", Action: { Type: "AddStatus", Target: "Self", Statuses: [{ Id: "unit_ability_available", Stacks: 1, ParamInt: 1 }] } }, { Type: "CardEffectResetCooldown", Action: { Type: "ResetCooldown", Target: "Self" } }] },
                        { Kind: "OnOwnAbilityActivated", FireCount: 1, Effects: [{ Type: "CardEffectResetCooldown", Action: { Type: "ResetCooldown", Target: "Self" } }] },
                        { Kind: "OnUnitAbilityAvailable", FireCount: 1, Effects: [{ Type: "CardEffectAddStatusEffect", Action: { Type: "AddStatus", Target: "Self" } }] },
                        { Kind: "OnUnitAbilityUnavailable", FireCount: 1, Effects: [{ Type: "CardEffectRemoveStatusEffect", Action: { Type: "RemoveStatus", Target: "Self" } }] }] } whelp &&
                whelp.Status("horde") == null && whelp.Status("unit_ability")?.Stacks == 1 && whelp.Subtypes.SequenceEqual(["SubtypesData_Whelp"]) &&
                source.Train.Context!.FindCard(whelp.SpawnerCardId)?.DataId == "eef8abd9-3550-4da6-a8bd-d5f63e9d2b16";
            if (!originalSycophant && !originalRecruit && !originalWhelp && added.Any(id => room.Units.Single(unit => unit.Id == id) is not { AssetKey: "MonsterFunguy" })) return false;
            bool originalSingleBirth = originalChampion && added.Length == 1 && room.Units.Single(unit => unit.Id == added[0]) is
                { AssetKey: "MonsterFunguy", SpawnerCardId: 0, Health: 4, MaxHealth: 4, Modifiers.Upgrades.Count: 0 } newborn &&
                newborn.Status("horde")?.Stacks == 1;
            bool originalSingleRetiredBirth = originalSingleBirth && owner.Triggers.Count == 2 &&
                PreviewBirthModel.IsOriginalMeatShieldHeal(owner.Triggers[1], owner, source.Train.Context!) &&
                room.Units.Any(unit => unit is { AssetKey: "MonsterSycophant", Team: CombatTeam.Player, BaseAttack: 20 or 24,
                    Health: 25, MaxHealth: 25, Size: 2, Modifiers.Upgrades.Count: 0 } && rule.Targets.Contains(unit.Id) &&
                    source.Train.Context!.FindCard(unit.SpawnerCardId)?.DataId == "6d2053e9-1a6a-4862-b348-76a92a92ea83");
            bool fightClubBirth = originalChampion && added.Length == 1 && room.Units.Single(unit => unit.Id == added[0]) is
                { AssetKey: "MonsterFunguy", SpawnerCardId: 0, Health: 12, MaxHealth: 12,
                    HordeDefinition: { Attack: 3, Health: 4 }, Modifiers.Upgrades: [{
                        DataId: RelicStartCombatAttachmentModel.Floor3UpgradeId, AssetKey: RelicStartCombatAttachmentModel.Floor3UpgradeKey,
                        Stats: { Damage: 15, Health: 0, Cost: 0, Heal: 0, Size: 0, XCost: 0, EquipmentLimit: 0, UpgradeSlotCount: 0 },
                        Statuses.Count: 0, ExternalInteractions.Count: 0, AbilityUpgrade: null, TriggerUpgrades: null }] } clubBirth &&
                clubBirth.Status("horde")?.Stacks == 3;
            bool fightClubSingleBirth = originalChampion && added.Length == 1 && room.Units.Single(unit => unit.Id == added[0]) is
                { AssetKey: "MonsterFunguy", SpawnerCardId: 0, Health: 4, MaxHealth: 4,
                    HordeDefinition: { Attack: 3, Health: 4 }, Modifiers.Upgrades: [{
                        DataId: RelicStartCombatAttachmentModel.Floor3UpgradeId, AssetKey: RelicStartCombatAttachmentModel.Floor3UpgradeKey,
                        Stats: { Damage: 15, Health: 0, Cost: 0, Heal: 0, Size: 0, XCost: 0, EquipmentLimit: 0, UpgradeSlotCount: 0 },
                        Statuses.Count: 0, ExternalInteractions.Count: 0, AbilityUpgrade: null, TriggerUpgrades: null }] } singleClubBirth &&
                singleClubBirth.Status("horde")?.Stacks == 1;
            bool originalTripleBirth = originalChampion && added.Length == 1 && room.Units.Single(unit => unit.Id == added[0]) is
                { AssetKey: "MonsterFunguy", SpawnerCardId: 0, Health: 12, MaxHealth: 12,
                    HordeDefinition: { Attack: 3, Health: 4 }, Modifiers.Upgrades.Count: 0 } tripleBirth &&
                tripleBirth.Status("horde")?.Stacks == 3;
            bool originalPendingTripleBirth = originalTripleBirth && owner.Triggers.Count == 2 &&
                PreviewBirthModel.IsOriginalMeatShieldHeal(owner.Triggers[1], owner, source.Train.Context!) &&
                room.Units.Any(unit => (unit is { AssetKey: "MonsterSycophant", Team: CombatTeam.Player, BaseAttack: 20,
                    Health: 25, MaxHealth: 25, Size: 2, Modifiers.Upgrades.Count: 0 } ||
                    unit is { AssetKey: "MonsterSycophant", Team: CombatTeam.Player, BaseAttack: 24,
                    Health: 35, MaxHealth: 35, Size: 2, Modifiers.Upgrades: [
                        { DataId: "eecb44d8-f417-4815-8438-3e4618231102", AssetKey: "LavaArmor" }, { DataId: "", AssetKey: "" }] }) && rule.Targets.Contains(unit.Id) &&
                    source.Train.Context!.FindCard(unit.SpawnerCardId)?.DataId == "6d2053e9-1a6a-4862-b348-76a92a92ea83");
            bool OriginalLiveLavaCompanion() => source.Train.Context != null && room.Units.Any(unit =>
                unit is { AssetKey: "MonsterSycophant", Team: CombatTeam.Player, BaseAttack: 20 or 24,
                    Health: 35, MaxHealth: 35, Size: 2, Modifiers.Upgrades: [
                        { DataId: "eecb44d8-f417-4815-8438-3e4618231102", AssetKey: "LavaArmor" }, { DataId: "", AssetKey: "" }] } &&
                rule.Targets.Contains(unit.Id) && source.Train.Context.FindCard(unit.SpawnerCardId)?.DataId == "6d2053e9-1a6a-4862-b348-76a92a92ea83" &&
                (unit.BaseAttack == 20 || source.Train.Rooms.SelectMany(floor => floor.Units).Any(fan =>
                    fan is { AssetKey: "MonsterEnthusiasticFan", Team: CombatTeam.Player, Health: 5, MaxHealth: 5 } &&
                    source.Train.Context.FindCard(fan.SpawnerCardId)?.DataId == "adaaaa9d-56a9-4a5f-a347-ee71d4767df5") ||
                 source.RetainedUnits.Any(actor => actor.Preview && actor.RoomIndex == -1 && actor.Unit is
                    { AssetKey: "MonsterEnthusiasticFan", Team: CombatTeam.Player, Health: <= 0, MaxHealth: 5, DeathState.IsDestroyed: true } &&
                    source.Train.Context.FindCard(actor.Unit.SpawnerCardId)?.DataId == "adaaaa9d-56a9-4a5f-a347-ee71d4767df5")));
            // A cleared spawn point proves only that the cached target left;
            // its original identity is no longer available in this preview.
            bool OriginalMissingPreviewTarget() => source.Train.Context != null &&
                owner is { BaseAttack: 80, Health: 67, MaxHealth: 110, Size: 1, Triggers.Count: 1 } &&
                source.Train.Context.CardRegistry?.Any(card => card.DataId == "217d467c-77c2-481d-b78f-57aaf3dcd4a6" && card.EquippedUnitId == owner.Id) == true &&
                rule.Targets.Count == 4 && targets.Count == 4 && rule.Targets.Count(id => !targets.Contains(id)) == 1 &&
                room.Units.Any(unit => unit is { AssetKey: "MonsterSycophant", Team: CombatTeam.Player, BaseAttack: 20,
                    Health: 10, MaxHealth: 25, Size: 2, Modifiers.Upgrades.Count: 0 } && rule.Targets.Contains(unit.Id) &&
                    source.Train.Context.FindCard(unit.SpawnerCardId)?.DataId == "6d2053e9-1a6a-4862-b348-76a92a92ea83") &&
                room.Units.Any(unit => unit is { AssetKey: "MonsterPricklyPuffball", Team: CombatTeam.Player, BaseAttack: 10,
                    Health: 20, MaxHealth: 35, Size: 2, Modifiers.Upgrades.Count: 0 } && rule.Targets.Contains(unit.Id) &&
                    source.Train.Context.FindCard(unit.SpawnerCardId)?.DataId == "fec68cb5-1d4f-4ae5-b01a-288a06cc9502") &&
                rule.Targets.Where(id => !targets.Contains(id)).All(id =>
                    source.Train.Context.SpawnPoints?.Units.Any(point => point.UnitId == id && !point.SpawnedInPreview &&
                        point.Current == null && point.LastKnown == null) == true);
            bool OriginalStewardCompanion() => source.Train.Context != null &&
                owner is { BaseAttack: 80, Health: 47, MaxHealth: 110, Size: 1, Triggers.Count: 1 } &&
                source.Train.Context.CardRegistry?.Any(card => card.DataId == "217d467c-77c2-481d-b78f-57aaf3dcd4a6" && card.EquippedUnitId == owner.Id) == true &&
                rule.Targets.Count == 3 && targets.Count == 4 && rule.Targets.All(targets.Contains) &&
                room.Units.Any(unit => unit is { AssetKey: "TrainStewardSmall", Team: CombatTeam.Player, BaseAttack: 14,
                    Health: 5, MaxHealth: 5, Size: 2, Modifiers.Upgrades.Count: 0 } && rule.Targets.Contains(unit.Id) &&
                    source.Train.Context.FindCard(unit.SpawnerCardId)?.DataId == "38e70e6a-20f3-4e36-8d31-c5110be07f87") &&
                room.Units.Any(unit => unit is { AssetKey: "MonsterSycophant", Team: CombatTeam.Player, BaseAttack: 20,
                    Health: 25, MaxHealth: 25, Size: 2, Modifiers.Upgrades.Count: 0 } && rule.Targets.Contains(unit.Id) &&
                    source.Train.Context.FindCard(unit.SpawnerCardId)?.DataId == "6d2053e9-1a6a-4862-b348-76a92a92ea83");
            bool originalPreviewBirth = source.Preview && source.Train.Context != null &&
                PreviewBirthModel.IsOriginalChampionEnemyDeath(owner, source.Train.Context) && added.Length == 1 &&
                room.Units.Single(unit => unit.Id == added[0]) is { AssetKey: "MonsterFunguy", SpawnerCardId: 0,
                    HordeDefinition: { Attack: 3, Health: 4 }, Modifiers.Upgrades: var birthUpgrades } previewBirth &&
                previewBirth.Status("horde")?.Stacks == 3 && (birthUpgrades.Count is 0 or 3 or 6 ||
                    birthUpgrades.Count == 9 && owner is { BaseAttack: 80, Health: 5, MaxHealth: 110, Size: 1, Triggers.Count: 1,
                        Modifiers.Upgrades: [{ DataId: "", AssetKey: "" },
                            { DataId: "4f70b7b7-b507-4931-9b14-e13dcd6cd59f", AssetKey: "ExilesDiadem_Upgrade", Stats.Health: 10, Stats.Size: -2 },
                            { DataId: "", AssetKey: "" }] } &&
                    source.Train.Context.CardRegistry?.Any(card => card.DataId == "217d467c-77c2-481d-b78f-57aaf3dcd4a6" && card.EquippedUnitId == owner.Id) == true &&
                    rule.Targets.Count == 3 && targets.Count == 4 && rule.Targets.All(targets.Contains) &&
                    room.Units.Any(unit => unit is { AssetKey: "MonsterSycophant", Team: CombatTeam.Player, BaseAttack: 24,
                        Health: 40, MaxHealth: 40, Size: 2, Triggers.Count: 2, Modifiers.Upgrades: [
                            { DataId: "09546a2d-2f67-4585-8007-de11ee12753e", AssetKey: "MeatShield_Upgrade" }, { DataId: "", AssetKey: "" }] } &&
                        rule.Targets.Contains(unit.Id) && PreviewBirthModel.IsOriginalMeatShieldHeal(unit.Triggers[1], unit, source.Train.Context) &&
                        source.Train.Context.FindCard(unit.SpawnerCardId)?.DataId == "6d2053e9-1a6a-4862-b348-76a92a92ea83") &&
                    room.Units.Any(unit => unit is { AssetKey: "MonsterSycophant", Team: CombatTeam.Player, BaseAttack: 20,
                        Health: 25, MaxHealth: 25, Size: 2, Modifiers.Upgrades.Count: 0 } && rule.Targets.Contains(unit.Id) &&
                        source.Train.Context.FindCard(unit.SpawnerCardId)?.DataId == "6d2053e9-1a6a-4862-b348-76a92a92ea83") &&
                    source.RetainedUnits.Any(actor => actor.Preview && actor.RoomIndex == -1 && actor.Unit is
                        { AssetKey: "TrainStewardSmall", Team: CombatTeam.Player, Health: <= 0, MaxHealth: 5, DeathState.IsDestroyed: true } &&
                        source.Train.Context.FindCard(actor.Unit.SpawnerCardId)?.DataId == "38e70e6a-20f3-4e36-8d31-c5110be07f87") ||
                    birthUpgrades.Count == 9 && owner is { BaseAttack: 80, Health: 47, MaxHealth: 110, Size: 3, Triggers.Count: 2,
                        Modifiers.Upgrades: [{ DataId: "", AssetKey: "" },
                            { DataId: "eecb44d8-f417-4815-8438-3e4618231102", AssetKey: "LavaArmor" }, { DataId: "", AssetKey: "" }] } &&
                    source.Train.Context.CardRegistry?.Any(card => card.DataId == "924ad6ab-3d60-4760-b38e-78f5cf09c1d5" && card.EquippedUnitId == owner.Id) == true &&
                    rule.Targets.Count == 2 && targets.Count == 3 && rule.Targets.All(targets.Contains) &&
                    room.Units.Any(unit => unit is { AssetKey: "MonsterSycophant", Team: CombatTeam.Player, BaseAttack: 20,
                        Health: 25, MaxHealth: 25, Size: 2, Modifiers.Upgrades.Count: 0 } && rule.Targets.Contains(unit.Id) &&
                        source.Train.Context.FindCard(unit.SpawnerCardId)?.DataId == "6d2053e9-1a6a-4862-b348-76a92a92ea83") &&
                    source.RetainedUnits.Any(actor => actor.Preview && actor.RoomIndex == -1 && actor.Unit is
                        { AssetKey: "TrainStewardSmall", Team: CombatTeam.Player, Health: <= 0, MaxHealth: 5, DeathState.IsDestroyed: true } &&
                        source.Train.Context.FindCard(actor.Unit.SpawnerCardId)?.DataId == "38e70e6a-20f3-4e36-8d31-c5110be07f87") ||
                    birthUpgrades.Count is 9 or 12 && OriginalMissingPreviewTarget() ||
                    birthUpgrades.Count == 9 && OriginalStewardCompanion() ||
                    owner.Triggers.Count == 2 && PreviewBirthModel.IsOriginalMeatShieldHeal(owner.Triggers[1], owner, source.Train.Context) &&
                    (birthUpgrades.Count == 9 && rule.Targets.Count == 3 && rule.Targets.Count(id => !targets.Contains(id)) == 1 ||
                    (birthUpgrades.Count == 12 || birthUpgrades.Count == 15 &&
                        source.RetainedUnits.Any(actor => actor.Preview && actor.RoomIndex == -1 && actor.Unit is
                            { AssetKey: "MonsterEnthusiasticFan", Team: CombatTeam.Player, Health: <= 0, MaxHealth: 5, DeathState.IsDestroyed: true } &&
                            source.Train.Context.FindCard(actor.Unit.SpawnerCardId)?.DataId == "adaaaa9d-56a9-4a5f-a347-ee71d4767df5") &&
                        source.RetainedUnits.Any(actor => actor.Preview && actor.RoomIndex == -1 && actor.Unit is
                            { AssetKey: "MonsterGoldyWhelp", Team: CombatTeam.Player, Health: <= 0, MaxHealth: 1, DeathState.IsDestroyed: true } &&
                            source.Train.Context.FindCard(actor.Unit.SpawnerCardId)?.DataId == "5fcb0af2-7bab-4998-94c9-e74a828fff01")) &&
                    rule.Targets.Count == 3 && rule.Targets.Count(id => !targets.Contains(id)) == 1 &&
                    OriginalLiveLavaCompanion() &&
                    source.RetainedUnits.Any(actor => actor.Preview && actor.RoomIndex == -1 && actor.Unit is
                        { AssetKey: "TrainStewardSmall", Team: CombatTeam.Player, Health: <= 0, DeathState.IsDestroyed: true }) ||
                    birthUpgrades.Count is 9 or 12 && rule.Targets.Count == 2 && targets.Count == 2 &&
                    rule.Targets.Count(id => !targets.Contains(id)) == 1 &&
                    source.Train.Context.CardRegistry?.Any(card => card.DataId == "924ad6ab-3d60-4760-b38e-78f5cf09c1d5" &&
                        card.EquippedUnitId is int equipped && rule.Targets.Contains(equipped) && !targets.Contains(equipped) &&
                        source.Train.Context.SpawnPoints?.Units.Any(point => point.UnitId == equipped && !point.SpawnedInPreview &&
                            point.Current == null && point.LastKnown == null) == true) == true &&
                    (source.RetainedUnits.Any(actor => actor.Preview && actor.RoomIndex == -1 && actor.Unit is
                        { AssetKey: "TrainStewardSmall", Team: CombatTeam.Player, Health: <= 0, DeathState.IsDestroyed: true }) ||
                     source.RetainedUnits.Any(actor => actor.Preview && actor.RoomIndex == -1 && actor.Unit is
                        { AssetKey: "MonsterSneezyWhelp", Team: CombatTeam.Player, Health: <= 0, DeathState.IsDestroyed: true } &&
                        source.Train.Context.FindCard(actor.Unit.SpawnerCardId)?.DataId == "eef8abd9-3550-4da6-a8bd-d5f63e9d2b16") &&
                     (source.RetainedUnits.Any(actor => actor.Preview && actor.RoomIndex == -1 && actor.Unit is
                        { AssetKey: "MonsterGenerousHost", Team: CombatTeam.Player, Health: <= 0, DeathState.IsDestroyed: true } &&
                        PreviewBirthModel.IsOriginalGreenRecruit(actor.Unit, source.Train.Context)) ||
                      birthUpgrades.Count == 12 && source.Train.Rooms.Any(other => other.RoomIndex != room.RoomIndex &&
                        other.Units.Any(unit => unit is { AssetKey: "MonsterFunguy", Team: CombatTeam.Player, SpawnerCardId: 0,
                            BaseAttack: 12, Health: 16, MaxHealth: 16, Size: 1, Modifiers.Upgrades.Count: 0,
                            HordeDefinition: { Attack: 3, Health: 4 } } && unit.Status("horde")?.Stacks == 4 &&
                            source.Train.Context.SpawnPoints?.Units.Any(point => point.UnitId == unit.Id && point.SpawnedInPreview &&
                                point.Current?.RoomIndex == other.RoomIndex) == true)))) ||
                    birthUpgrades.Count is 9 or 12 && rule.Targets.Count == 3 && targets.Count == 2 &&
                    rule.Targets.Count(id => !targets.Contains(id)) == 2 &&
                    source.RetainedUnits.Any(actor => actor.Preview && actor.RoomIndex == -1 &&
                        actor.Unit is { AssetKey: "MonsterSycophant", Team: CombatTeam.Player, BaseAttack: 20,
                            Health: <= 0, MaxHealth: 35, Size: 2, DeathState.IsDestroyed: true, Modifiers.Upgrades: [
                                { DataId: "eecb44d8-f417-4815-8438-3e4618231102", AssetKey: "LavaArmor" }, { DataId: "", AssetKey: "" }] } &&
                        rule.Targets.Contains(actor.Unit.Id) && !targets.Contains(actor.Unit.Id) &&
                        source.Train.Context.FindCard(actor.Unit.SpawnerCardId)?.DataId == "6d2053e9-1a6a-4862-b348-76a92a92ea83" &&
                        source.Train.Context.CardRegistry?.Any(card => card.DataId == "924ad6ab-3d60-4760-b38e-78f5cf09c1d5" &&
                            card.EquippedUnitId == actor.Unit.Id) == true) &&
                    source.RetainedUnits.Any(actor => actor.Preview && actor.RoomIndex == -1 && actor.Unit is
                        { AssetKey: "TrainStewardSmall", Team: CombatTeam.Player, Health: <= 0, DeathState.IsDestroyed: true }) ||
                    birthUpgrades.Count is 9 or 12 && rule.Targets.Count == 2 && rule.Targets.All(targets.Contains) &&
                    room.Units.Any(unit => unit is { AssetKey: "MonsterSycophant", Team: CombatTeam.Player, BaseAttack: 24,
                        Health: 35, MaxHealth: 35, Size: 2, Modifiers.Upgrades: [
                            { DataId: "eecb44d8-f417-4815-8438-3e4618231102", AssetKey: "LavaArmor" }, { DataId: "", AssetKey: "" }] } &&
                        rule.Targets.Contains(unit.Id) && source.Train.Context.FindCard(unit.SpawnerCardId)?.DataId == "6d2053e9-1a6a-4862-b348-76a92a92ea83"))) &&
                previewBirth.Health == 12 + birthUpgrades.Count * 3 && previewBirth.MaxHealth == previewBirth.Health &&
                birthUpgrades.All(item => item.DataId == Key(owner.Id) && item.AssetKey == upgrade.AssetKey &&
                    item.Stats.Health == 3 && item.Stats.Damage == 0 && item.Stats.Cost == 0 && item.Stats.Heal == 0 &&
                    item.Stats.Size == 0 && item.Stats.XCost == 0 && item.Stats.EquipmentLimit == 0 &&
                    item.Stats.UpgradeSlotCount == 0 && item.Statuses.Count == 0 && item.ExternalInteractions.Count == 0 &&
                    item.AbilityUpgrade == null && item.TriggerUpgrades?.Count is not > 0);
            bool originalTripleCompanionsRetiredPreviewBirth = source.Preview && source.Train.Context != null &&
                PreviewBirthModel.IsOriginalChampionEnemyDeath(owner, source.Train.Context) &&
                owner is { BaseAttack: 80, MaxHealth: 110, Size: 1, Triggers.Count: 1 } && added.Length == 1 &&
                rule.Targets.Count == 4 && targets.Count == 5 && rule.Targets.All(targets.Contains) &&
                room.Units.Single(unit => unit.Id == added[0]) is { AssetKey: "MonsterFunguy", Team: CombatTeam.Player,
                    SpawnerCardId: 0, BaseAttack: 9, Size: 1, HordeDefinition: { Attack: 3, Health: 4 }, Modifiers.Upgrades: var tripleUpgrades } tripleCompanion &&
                tripleCompanion.Status("horde")?.Stacks == 3 && tripleUpgrades.Count is 0 or 3 or 6 or 9 or 12 or 15 or 18 or 21 &&
                tripleCompanion.Health == 12 + tripleUpgrades.Count * 3 && tripleCompanion.MaxHealth == tripleCompanion.Health &&
                tripleUpgrades.All(item => item.DataId == Key(owner.Id) && item.AssetKey == upgrade.AssetKey &&
                    item.Stats.Health == 3 && item.Stats.Damage == 0 && item.Stats.Cost == 0 && item.Stats.Heal == 0 &&
                    item.Stats.Size == 0 && item.Stats.XCost == 0 && item.Stats.EquipmentLimit == 0 &&
                    item.Stats.UpgradeSlotCount == 0 && item.Statuses.Count == 0 && item.ExternalInteractions.Count == 0 &&
                    item.AbilityUpgrade == null && item.TriggerUpgrades?.Count is not > 0) &&
                room.Units.Any(unit => unit is { AssetKey: "MonsterSycophant", Team: CombatTeam.Player, BaseAttack: 24,
                    Health: 25, MaxHealth: 25, Size: 2, Modifiers.Upgrades.Count: 0 } && rule.Targets.Contains(unit.Id) &&
                    source.Train.Context.FindCard(unit.SpawnerCardId)?.DataId == "6d2053e9-1a6a-4862-b348-76a92a92ea83") &&
                room.Units.Any(unit => unit is { AssetKey: "MonsterGenerousHost", Team: CombatTeam.Player, BaseAttack: 5,
                    Health: 5, MaxHealth: 5, Size: 1, Modifiers.Upgrades.Count: 0 } && rule.Targets.Contains(unit.Id) &&
                    PreviewBirthModel.IsOriginalGreenRecruit(unit, source.Train.Context)) &&
                room.Units.Any(unit => unit is { AssetKey: "MonsterSneezyWhelp", Team: CombatTeam.Player, BaseAttack: 4,
                    Health: 1, MaxHealth: 1, Size: 1, Modifiers.Upgrades.Count: 0 } && rule.Targets.Contains(unit.Id) &&
                    source.Train.Context.FindCard(unit.SpawnerCardId)?.DataId == "eef8abd9-3550-4da6-a8bd-d5f63e9d2b16");
            bool originalLavaTwoRetiredPreviewBirth = originalPreviewBirth && source.Train.Context != null &&
                rule.Targets.Count == 3 && rule.Targets.Count(id => !targets.Contains(id)) == 1 &&
                room.Units.Single(unit => unit.Id == added[0]).Modifiers!.Upgrades.Count is 9 or 12 or 15 &&
                OriginalLiveLavaCompanion() &&
                source.RetainedUnits.Any(actor => actor.Preview && actor.RoomIndex == -1 && actor.Unit is
                    { AssetKey: "TrainStewardSmall", Team: CombatTeam.Player, Health: <= 0, DeathState.IsDestroyed: true });
            bool originalFourHordePreviewBirth = source.Preview && source.Train.Context != null &&
                PreviewBirthModel.IsOriginalChampionEnemyDeath(owner, source.Train.Context) && added.Length == 1 &&
                (rule.Targets.Count == 3 && rule.Targets.Count(id => !targets.Contains(id)) == 1 ||
                 rule.Targets.Count == 5 && targets.Count == 3 && rule.Targets.Count(id => !targets.Contains(id)) == 3 &&
                 owner is { BaseAttack: 80, MaxHealth: 110, Size: 1, Triggers.Count: 1 } &&
                 room.Units.Any(unit => unit is { AssetKey: "MonsterSycophant", Team: CombatTeam.Player, BaseAttack: 24,
                     Health: 3, MaxHealth: 25, Size: 2, Modifiers.Upgrades.Count: 0 } && rule.Targets.Contains(unit.Id) &&
                     source.Train.Context.FindCard(unit.SpawnerCardId)?.DataId == "6d2053e9-1a6a-4862-b348-76a92a92ea83") &&
                 source.RetainedUnits.Any(actor => actor.Preview && actor.RoomIndex == -1 && rule.Targets.Contains(actor.Unit.Id) &&
                     actor.Unit is { AssetKey: "MonsterSneezyWhelp", Team: CombatTeam.Player, Health: <= 0, DeathState.IsDestroyed: true })) &&
                room.Units.Single(unit => unit.Id == added[0]) is { AssetKey: "MonsterFunguy", Team: CombatTeam.Player,
                    SpawnerCardId: 0, BaseAttack: 12, Size: 1,
                    HordeDefinition: { Attack: 3, Health: 4 }, Modifiers.Upgrades: var fourUpgrades } fourHorde &&
                fourHorde.Status("horde")?.Stacks == 4 && fourUpgrades.Count is 0 or 4 or 8 or 12 or 16 or 20 or 24 or 28 &&
                fourHorde.Health == 16 + 3 * fourUpgrades.Count && fourHorde.MaxHealth == fourHorde.Health &&
                fourUpgrades.All(item => item.DataId == Key(owner.Id) && item.AssetKey == upgrade.AssetKey &&
                    item.Stats.Health == 3 && item.Stats.Damage == 0 && item.Stats.Cost == 0 && item.Stats.Heal == 0 &&
                    item.Stats.Size == 0 && item.Stats.XCost == 0 && item.Stats.EquipmentLimit == 0 &&
                    item.Stats.UpgradeSlotCount == 0 && item.Statuses.Count == 0 && item.ExternalInteractions.Count == 0 &&
                    item.AbilityUpgrade == null && item.TriggerUpgrades?.Count is not > 0);
            bool originalSixHordePreviewBirth = source.Preview && source.Train.Context != null &&
                PreviewBirthModel.IsOriginalChampionEnemyDeath(owner, source.Train.Context) && added.Length == 1 &&
                room.Units.Single(unit => unit.Id == added[0]) is { AssetKey: "MonsterFunguy", Team: CombatTeam.Player,
                    SpawnerCardId: 0, BaseAttack: 18, Size: 1, HordeDefinition: { Attack: 3, Health: 4 }, Modifiers.Upgrades: var sixUpgrades } sixHorde &&
                sixHorde.Status("horde")?.Stacks == 6 &&
                (sixUpgrades.Count is 9 or 15 or 21 || sixUpgrades.Count is 27 or 33 && OriginalStewardCompanion()) &&
                sixHorde.Health == 24 + 3 * sixUpgrades.Count && sixHorde.MaxHealth == sixHorde.Health &&
                sixUpgrades.All(item => item.DataId == Key(owner.Id) && item.AssetKey == upgrade.AssetKey &&
                    item.Stats.Health == 3 && item.Stats.Damage == 0 && item.Stats.Cost == 0 && item.Stats.Heal == 0 &&
                    item.Stats.Size == 0 && item.Stats.XCost == 0 && item.Stats.EquipmentLimit == 0 &&
                    item.Stats.UpgradeSlotCount == 0 && item.Statuses.Count == 0 && item.ExternalInteractions.Count == 0 &&
                    item.AbilityUpgrade == null && item.TriggerUpgrades?.Count is not > 0);
            bool originalSixHordePuffballPreviewBirth = originalSixHordePreviewBirth && room.Units.Single(unit => unit.Id == added[0]).Modifiers!.Upgrades.Count is 9 or 15 && source.Train.Context != null &&
                owner.Triggers.Count == 2 && PreviewBirthModel.IsOriginalMeatShieldHeal(owner.Triggers[1], owner, source.Train.Context) &&
                rule.Targets.Count == 3 && rule.Targets.Count(id => !targets.Contains(id)) == 1 &&
                room.Units.Any(unit => unit is { AssetKey: "MonsterPricklyPuffball", Team: CombatTeam.Player, BaseAttack: 10,
                    Health: 8, MaxHealth: 45, Size: 2, Modifiers.Upgrades: [
                        { DataId: "eecb44d8-f417-4815-8438-3e4618231102", AssetKey: "LavaArmor" }, { DataId: "", AssetKey: "" }] } &&
                    rule.Targets.Contains(unit.Id) && source.Train.Context.FindCard(unit.SpawnerCardId)?.DataId == "fec68cb5-1d4f-4ae5-b01a-288a06cc9502");
            bool originalSixHordeRetiredPuffballPreviewBirth = originalSixHordePreviewBirth && source.Train.Context != null &&
                owner.Triggers.Count == 2 && PreviewBirthModel.IsOriginalMeatShieldHeal(owner.Triggers[1], owner, source.Train.Context) &&
                rule.Targets.Count == 3 && targets.Count == 2 && targets.Contains(owner.Id) &&
                rule.Targets.Count(id => !targets.Contains(id)) == 2 &&
                source.RetainedUnits.Any(actor => actor.Preview && actor.RoomIndex == -1 &&
                    actor.Unit is { AssetKey: "MonsterPricklyPuffball", Team: CombatTeam.Player, BaseAttack: 10,
                        Health: <= 0, MaxHealth: 45, Size: 2, DeathState.IsDestroyed: true, Modifiers.Upgrades: [
                            { DataId: "eecb44d8-f417-4815-8438-3e4618231102", AssetKey: "LavaArmor" }, { DataId: "", AssetKey: "" }] } &&
                    rule.Targets.Contains(actor.Unit.Id) && !targets.Contains(actor.Unit.Id) &&
                    source.Train.Context.FindCard(actor.Unit.SpawnerCardId)?.DataId == "fec68cb5-1d4f-4ae5-b01a-288a06cc9502" &&
                    rule.Targets.Any(id => id != actor.Unit.Id && !targets.Contains(id) &&
                        source.Train.Context.SpawnPoints?.Units.Any(point => point.UnitId == id && !point.SpawnedInPreview &&
                            point.Current == null && point.LastKnown == null) == true));
            bool originalSixHordeStewardFanPreviewBirth = originalSixHordePreviewBirth && source.Train.Context != null &&
                room.Units.Single(unit => unit.Id == added[0]).Modifiers!.Upgrades.Count is 9 or 15 &&
                owner is { BaseAttack: 80, Health: 55, MaxHealth: 110, Size: 1, Triggers.Count: 1 } &&
                source.Train.Context.CardRegistry?.Any(card => card.DataId == "217d467c-77c2-481d-b78f-57aaf3dcd4a6" && card.EquippedUnitId == owner.Id) == true &&
                rule.Targets.Count == 3 && targets.Count == 4 && rule.Targets.All(targets.Contains) &&
                room.Units.Any(unit => unit is { AssetKey: "TrainStewardBig", Team: CombatTeam.Player, BaseAttack: 8,
                    Health: 10, MaxHealth: 25, Size: 3, Modifiers.Upgrades.Count: 0 } && rule.Targets.Contains(unit.Id) &&
                    source.Train.Context.FindCard(unit.SpawnerCardId)?.DataId == "d14a50f3-728d-43e1-87f0-ef1b013f6678") &&
                room.Units.Any(unit => unit is { AssetKey: "MonsterEnthusiasticFan", Team: CombatTeam.Player, BaseAttack: 5,
                    Health: 5, MaxHealth: 5, Size: 1, Modifiers.Upgrades.Count: 0 } && rule.Targets.Contains(unit.Id) &&
                    source.Train.Context.FindCard(unit.SpawnerCardId)?.DataId == "adaaaa9d-56a9-4a5f-a347-ee71d4767df5");
            bool originalSixHordeStewardCompanionPreviewBirth = originalSixHordePreviewBirth && source.Train.Context != null &&
                room.Units.Single(unit => unit.Id == added[0]).Modifiers!.Upgrades.Count is 9 or 15 or 21 or 27 or 33 &&
                OriginalStewardCompanion();
            bool originalSixHordeCompanionsPreviewBirth = originalSixHordePreviewBirth && room.Units.Single(unit => unit.Id == added[0]).Modifiers!.Upgrades.Count is 9 or 15 && source.Train.Context != null &&
                owner is { BaseAttack: 80, MaxHealth: 110, Size: 1, Triggers.Count: 1 } &&
                owner.Modifiers?.Upgrades is [
                    { DataId: "", AssetKey: "" },
                    { DataId: "4f70b7b7-b507-4931-9b14-e13dcd6cd59f", AssetKey: "ExilesDiadem_Upgrade", Stats.Health: 10, Stats.Size: -2 },
                    { DataId: "", AssetKey: "" }] or [
                    { DataId: "4f70b7b7-b507-4931-9b14-e13dcd6cd59f", AssetKey: "ExilesDiadem_Upgrade", Stats.Health: 10, Stats.Size: -2 },
                    { DataId: "", AssetKey: "" }] &&
                rule.Targets.Count == 5 && targets.Count == 5 && rule.Targets.Count(id => !targets.Contains(id)) == 1 &&
                room.Units.Any(unit => unit is { AssetKey: "MonsterSycophant", Team: CombatTeam.Player, BaseAttack: 24,
                    Health: 25, MaxHealth: 25, Size: 2, Modifiers.Upgrades.Count: 0 } && rule.Targets.Contains(unit.Id) &&
                    source.Train.Context.FindCard(unit.SpawnerCardId)?.DataId == "6d2053e9-1a6a-4862-b348-76a92a92ea83") &&
                room.Units.Any(unit => unit is { AssetKey: "MonsterGenerousHost", Team: CombatTeam.Player, BaseAttack: 5,
                    Health: 5, MaxHealth: 5, Size: 1, Modifiers.Upgrades.Count: 0 } && rule.Targets.Contains(unit.Id) &&
                    PreviewBirthModel.IsOriginalGreenRecruit(unit, source.Train.Context)) &&
                room.Units.Any(unit => unit is { AssetKey: "MonsterSneezyWhelp", Team: CombatTeam.Player, BaseAttack: 4,
                    Health: 1, MaxHealth: 1, Size: 1, Modifiers.Upgrades.Count: 0 } && rule.Targets.Contains(unit.Id) &&
                    source.Train.Context.FindCard(unit.SpawnerCardId)?.DataId == "eef8abd9-3550-4da6-a8bd-d5f63e9d2b16");
            bool OriginalFightClub(CardUpgradeModifier item) => item is {
                DataId: RelicStartCombatAttachmentModel.Floor3UpgradeId, AssetKey: RelicStartCombatAttachmentModel.Floor3UpgradeKey,
                Stats: { Damage: 15, Health: 0, Cost: 0, Heal: 0, Size: 0, XCost: 0, EquipmentLimit: 0, UpgradeSlotCount: 0 },
                Statuses.Count: 0, ExternalInteractions.Count: 0, AbilityUpgrade: null, TriggerUpgrades: null };
            bool fightClubCompanionsBirth = source.Train.Context != null &&
                PreviewBirthModel.IsOriginalChampionEnemyDeath(owner, source.Train.Context) &&
                owner is { BaseAttack: 95, MaxHealth: 110, Size: 1, Triggers.Count: 1, Modifiers.Upgrades: [
                    { DataId: "", AssetKey: "" },
                    { DataId: "4f70b7b7-b507-4931-9b14-e13dcd6cd59f", AssetKey: "ExilesDiadem_Upgrade", Stats.Health: 10, Stats.Size: -2 },
                    { DataId: "", AssetKey: "" }, var ownerClub] } && OriginalFightClub(ownerClub) &&
                added.Length == 1 &&
                (targets.Count == 5 && (rule.Targets.Count == 5 && rule.Targets.Count(id => !targets.Contains(id)) == 1 ||
                 rule.Targets.Count == 4 && rule.Targets.All(targets.Contains)) ||
                 source.Preview && targets.Count == 4 && rule.Targets.Count == 4 && rule.Targets.Count(id => !targets.Contains(id)) == 1 &&
                 !room.Units.Any(unit => unit.AssetKey == "MonsterSneezyWhelp")) &&
                room.Units.Single(unit => unit.Id == added[0]) is { AssetKey: "MonsterFunguy", Team: CombatTeam.Player,
                    SpawnerCardId: 0, Size: 1, HordeDefinition: { Attack: 3, Health: 4 }, Modifiers.Upgrades: var clubCompanionUpgrades } clubCompanion &&
                (clubCompanion.Status("horde")?.Stacks == 3 && clubCompanion.BaseAttack == 24 && clubCompanionUpgrades.Count is 1 or 4 ||
                 clubCompanion.Status("horde")?.Stacks == 6 && clubCompanion.BaseAttack == 33 &&
                    (clubCompanionUpgrades.Count is 10 or 16 || source.Preview && clubCompanionUpgrades.Count == 22)) &&
                clubCompanion.Health == clubCompanion.Status("horde")!.Stacks * 4 + 3 * (clubCompanionUpgrades.Count - 1) &&
                clubCompanion.MaxHealth == clubCompanion.Health && OriginalFightClub(clubCompanionUpgrades[0]) &&
                clubCompanionUpgrades.Skip(1).All(item => item.DataId == Key(owner.Id) && item.AssetKey == upgrade.AssetKey &&
                    item.Stats.Health == 3 && item.Stats.Damage == 0 && item.Stats.Cost == 0 && item.Stats.Heal == 0 &&
                    item.Stats.Size == 0 && item.Stats.XCost == 0 && item.Stats.EquipmentLimit == 0 &&
                    item.Stats.UpgradeSlotCount == 0 && item.Statuses.Count == 0 && item.ExternalInteractions.Count == 0 &&
                    item.AbilityUpgrade == null && item.TriggerUpgrades?.Count is not > 0) &&
                room.Units.Any(unit => unit is { AssetKey: "MonsterSycophant", Team: CombatTeam.Player, BaseAttack: 39,
                    Health: 24 or 25, MaxHealth: 25, Size: 2, Modifiers.Upgrades: [var club] } && OriginalFightClub(club) &&
                    rule.Targets.Contains(unit.Id) && source.Train.Context.FindCard(unit.SpawnerCardId)?.DataId == "6d2053e9-1a6a-4862-b348-76a92a92ea83") &&
                room.Units.Any(unit => unit is { AssetKey: "MonsterGenerousHost", Team: CombatTeam.Player, BaseAttack: 20,
                    Health: 4 or 5, MaxHealth: 5, Size: 1, Modifiers.Upgrades: [var club] } && OriginalFightClub(club) &&
                    rule.Targets.Contains(unit.Id) && PreviewBirthModel.IsOriginalGreenRecruit(unit, source.Train.Context)) &&
                (targets.Count == 4 || room.Units.Any(unit => unit is { AssetKey: "MonsterSneezyWhelp", Team: CombatTeam.Player, BaseAttack: 19,
                    Health: 1, MaxHealth: 1, Size: 1, Modifiers.Upgrades: [var club] } && OriginalFightClub(club) &&
                    rule.Targets.Contains(unit.Id) && source.Train.Context.FindCard(unit.SpawnerCardId)?.DataId == "eef8abd9-3550-4da6-a8bd-d5f63e9d2b16"));
            bool fightClubPreviewBirth = source.Preview && source.Train.Context != null &&
                PreviewBirthModel.IsOriginalChampionEnemyDeath(owner, source.Train.Context) && added.Length == 1 &&
                room.Units.Single(unit => unit.Id == added[0]) is { AssetKey: "MonsterFunguy", Team: CombatTeam.Player,
                    SpawnerCardId: 0, BaseAttack: 24, Size: 1, HordeDefinition: { Attack: 3, Health: 4 },
                    Modifiers.Upgrades: var clubUpgrades } clubPreview && clubPreview.Status("horde")?.Stacks == 3 &&
                clubUpgrades.Count is 1 or 4 or 7 or 10 or 13 or 16 && clubPreview.Health == 12 + 3 * (clubUpgrades.Count - 1) &&
                clubPreview.MaxHealth == clubPreview.Health && clubUpgrades[0] is {
                    DataId: RelicStartCombatAttachmentModel.Floor3UpgradeId, AssetKey: RelicStartCombatAttachmentModel.Floor3UpgradeKey,
                    Stats: { Damage: 15, Health: 0, Cost: 0, Heal: 0, Size: 0, XCost: 0, EquipmentLimit: 0, UpgradeSlotCount: 0 },
                    Statuses.Count: 0, ExternalInteractions.Count: 0, AbilityUpgrade: null, TriggerUpgrades: null } &&
                clubUpgrades.Skip(1).All(item => item.DataId == Key(owner.Id) && item.AssetKey == upgrade.AssetKey &&
                    item.Stats.Health == 3 && item.Stats.Damage == 0 && item.Stats.Cost == 0 && item.Stats.Heal == 0 &&
                    item.Stats.Size == 0 && item.Stats.XCost == 0 && item.Stats.EquipmentLimit == 0 &&
                    item.Stats.UpgradeSlotCount == 0 && item.Statuses.Count == 0 && item.ExternalInteractions.Count == 0 &&
                    item.AbilityUpgrade == null && item.TriggerUpgrades?.Count is not > 0);
            bool fightClubSmallStewardMissingTarget = source.Preview && source.Train.Context != null &&
                PreviewBirthModel.IsOriginalChampionEnemyDeath(owner, source.Train.Context) &&
                owner is { BaseAttack: 95, Health: 58, MaxHealth: 110, Size: 1, Triggers.Count: 1, Modifiers.Upgrades: [
                    { DataId: "", AssetKey: "" }, { DataId: "", AssetKey: "" },
                    { DataId: "4f70b7b7-b507-4931-9b14-e13dcd6cd59f", AssetKey: "ExilesDiadem_Upgrade", Stats.Health: 10, Stats.Size: -2 },
                    { DataId: "", AssetKey: "" }, var missingOwnerClub] } && OriginalFightClub(missingOwnerClub) &&
                source.Train.Context.CardRegistry?.Any(card => card.DataId == "217d467c-77c2-481d-b78f-57aaf3dcd4a6" && card.EquippedUnitId == owner.Id) == true &&
                rule.Targets.Count == 4 && targets.Count == 4 && rule.Targets.Count(id => !targets.Contains(id)) == 1 &&
                rule.Targets.Where(id => !targets.Contains(id)).All(id =>
                    source.Train.Context.SpawnPoints?.Units.Any(point => point.UnitId == id && !point.SpawnedInPreview &&
                        point.Current == null && point.LastKnown == null) == true) &&
                room.Units.Any(unit => unit is { AssetKey: "TrainStewardSmall", Team: CombatTeam.Player, BaseAttack: 29,
                    Health: 5, MaxHealth: 5, Size: 2, Modifiers.Upgrades: [var club] } && OriginalFightClub(club) &&
                    rule.Targets.Contains(unit.Id) && source.Train.Context.FindCard(unit.SpawnerCardId)?.DataId == "38e70e6a-20f3-4e36-8d31-c5110be07f87") &&
                room.Units.Any(unit => unit is { AssetKey: "MonsterSycophant", Team: CombatTeam.Player, BaseAttack: 35,
                    Health: 25, MaxHealth: 25, Size: 2, Modifiers.Upgrades: [var club] } && OriginalFightClub(club) &&
                    rule.Targets.Contains(unit.Id) && source.Train.Context.FindCard(unit.SpawnerCardId)?.DataId == "6d2053e9-1a6a-4862-b348-76a92a92ea83") &&
                added.Length == 1 && room.Units.Single(unit => unit.Id == added[0]) is
                    { AssetKey: "MonsterFunguy", Team: CombatTeam.Player, SpawnerCardId: 0, Size: 1,
                        HordeDefinition: { Attack: 3, Health: 4 }, Modifiers.Upgrades: var missingUpgrades } missingBirth &&
                (missingBirth.Status("horde")?.Stacks == 3 && missingBirth.BaseAttack == 24 && missingUpgrades.Count is 1 or 4 ||
                 missingBirth.Status("horde")?.Stacks == 6 && missingBirth.BaseAttack == 33 && missingUpgrades.Count is 10 or 16) &&
                missingBirth.Health == missingBirth.Status("horde")!.Stacks * 4 + 3 * (missingUpgrades.Count - 1) &&
                missingBirth.MaxHealth == missingBirth.Health && OriginalFightClub(missingUpgrades[0]) &&
                missingUpgrades.Skip(1).All(item => item.DataId == Key(owner.Id) && item.AssetKey == upgrade.AssetKey &&
                    item.Stats.Health == 3 && item.Stats.Damage == 0 && item.Stats.Cost == 0 && item.Stats.Heal == 0 &&
                    item.Stats.Size == 0 && item.Stats.XCost == 0 && item.Stats.EquipmentLimit == 0 &&
                    item.Stats.UpgradeSlotCount == 0 && item.Statuses.Count == 0 && item.ExternalInteractions.Count == 0 &&
                    item.AbilityUpgrade == null && item.TriggerUpgrades?.Count is not > 0);
            bool fightClubLavaCompanion = source.Train.Context != null &&
                room.Units.Any(unit => unit is { AssetKey: "MonsterSycophant", Team: CombatTeam.Player, BaseAttack: 35,
                    Health: 35, MaxHealth: 35, Size: 2, Modifiers.Upgrades: [
                        { DataId: "eecb44d8-f417-4815-8438-3e4618231102", AssetKey: "LavaArmor" },
                        { DataId: "", AssetKey: "" },
                        { DataId: RelicStartCombatAttachmentModel.Floor3UpgradeId, AssetKey: RelicStartCombatAttachmentModel.Floor3UpgradeKey,
                            Stats.Damage: 15 }] } && rule.Targets.Contains(unit.Id) &&
                    source.Train.Context.FindCard(unit.SpawnerCardId)?.DataId == "6d2053e9-1a6a-4862-b348-76a92a92ea83");
            bool fightClubStewardPreviewBirth = fightClubPreviewBirth && source.Train.Context != null &&
                owner is { BaseAttack: 95, Health: 58, MaxHealth: 110, Size: 1, Triggers.Count: 1, Modifiers.Upgrades: [
                    { DataId: "", AssetKey: "" },
                    { DataId: "4f70b7b7-b507-4931-9b14-e13dcd6cd59f", AssetKey: "ExilesDiadem_Upgrade", Stats.Health: 10, Stats.Size: -2 },
                    { DataId: "", AssetKey: "" }, var stewardOwnerClub] } && OriginalFightClub(stewardOwnerClub) &&
                rule.Targets.Count == 4 && targets.Count == 4 && rule.Targets.Count(id => !targets.Contains(id)) == 1 &&
                room.Units.Any(unit => unit is { AssetKey: "TrainStewardBig", Team: CombatTeam.Player, BaseAttack: 23,
                    Health: 25, MaxHealth: 25, Size: 3, Modifiers.Upgrades: [var club] } && OriginalFightClub(club) &&
                    rule.Targets.Contains(unit.Id) && source.Train.Context.FindCard(unit.SpawnerCardId)?.DataId == "d14a50f3-728d-43e1-87f0-ef1b013f6678") &&
                room.Units.Any(unit => unit is { AssetKey: "MonsterGenerousHost", Team: CombatTeam.Player, BaseAttack: 20,
                    Health: 5, MaxHealth: 5, Size: 1, Modifiers.Upgrades: [var club] } && OriginalFightClub(club) &&
                    rule.Targets.Contains(unit.Id) && PreviewBirthModel.IsOriginalGreenRecruit(unit, source.Train.Context));
            bool fightClubWoundedStewardPreviewBirth = fightClubPreviewBirth && source.Train.Context != null &&
                room.Units.Single(unit => unit.Id == added[0]).Modifiers!.Upgrades.Count is 1 or 4 &&
                owner is { BaseAttack: 95, Health: 84, MaxHealth: 110, Size: 1, Triggers.Count: 1, Modifiers.Upgrades: [
                    { DataId: "", AssetKey: "" }, { DataId: "", AssetKey: "" },
                    { DataId: "4f70b7b7-b507-4931-9b14-e13dcd6cd59f", AssetKey: "ExilesDiadem_Upgrade", Stats.Health: 10, Stats.Size: -2 },
                    { DataId: "", AssetKey: "" }, var woundedOwnerClub] } && OriginalFightClub(woundedOwnerClub) &&
                source.Train.Context.CardRegistry?.Any(card => card.DataId == "217d467c-77c2-481d-b78f-57aaf3dcd4a6" && card.EquippedUnitId == owner.Id) == true &&
                rule.Targets.Count == 3 && targets.Count == 3 && rule.Targets.Count(id => !targets.Contains(id)) == 1 &&
                rule.Targets.Where(id => !targets.Contains(id)).All(id =>
                    source.Train.Context.SpawnPoints?.Units.Any(point => point.UnitId == id && !point.SpawnedInPreview &&
                        point.Current == null && point.LastKnown == null) == true) &&
                room.Units.Any(unit => unit is { AssetKey: "TrainStewardBig", Team: CombatTeam.Player, BaseAttack: 23,
                    Health: 10, MaxHealth: 25, Size: 3, Modifiers.Upgrades: [var club] } && OriginalFightClub(club) &&
                    rule.Targets.Contains(unit.Id) && source.Train.Context.FindCard(unit.SpawnerCardId)?.DataId == "d14a50f3-728d-43e1-87f0-ef1b013f6678");
            bool fightClubRetiredPuffballBirth = source.Preview && source.Train.Context != null && added.Length == 1 &&
                PreviewBirthModel.IsOriginalChampionEnemyDeath(owner, source.Train.Context) && owner.Triggers.Count == 2 &&
                PreviewBirthModel.IsOriginalMeatShieldHeal(owner.Triggers[1], owner, source.Train.Context) &&
                rule.Targets.Count == 3 && targets.Count == 2 && targets.Contains(owner.Id) && rule.Targets.Count(id => !targets.Contains(id)) == 2 &&
                source.RetainedUnits.Any(actor => actor.Preview && actor.RoomIndex == -1 && actor.Unit is
                    { AssetKey: "MonsterPricklyPuffball", Team: CombatTeam.Player, BaseAttack: 25, Health: <= 0, MaxHealth: 45,
                        Size: 2, DeathState.IsDestroyed: true, Modifiers.Upgrades: [
                            { DataId: "eecb44d8-f417-4815-8438-3e4618231102", AssetKey: "LavaArmor" }, { DataId: "", AssetKey: "" }, var club] } &&
                    OriginalFightClub(club) && rule.Targets.Contains(actor.Unit.Id) &&
                    source.Train.Context.FindCard(actor.Unit.SpawnerCardId)?.DataId == "fec68cb5-1d4f-4ae5-b01a-288a06cc9502") &&
                room.Units.Single(unit => unit.Id == added[0]) is { AssetKey: "MonsterFunguy", Team: CombatTeam.Player, SpawnerCardId: 0,
                    Size: 1, HordeDefinition: { Attack: 3, Health: 4 }, Modifiers.Upgrades: var retiredClubUpgrades } retiredClub &&
                (retiredClub.Status("horde")?.Stacks == 3 && retiredClub.BaseAttack == 24 && retiredClubUpgrades.Count is 1 or 4 ||
                 retiredClub.Status("horde")?.Stacks == 6 && retiredClub.BaseAttack == 33 && retiredClubUpgrades.Count is 10 or 16 ||
                 retiredClub.Status("horde")?.Stacks == 9 && retiredClub.BaseAttack == 42 && retiredClubUpgrades.Count is 19 or 28 or 37) &&
                retiredClub.Health == retiredClub.Status("horde")!.Stacks * 4 + 3 * (retiredClubUpgrades.Count - 1) &&
                retiredClub.MaxHealth == retiredClub.Health && OriginalFightClub(retiredClubUpgrades[0]) &&
                retiredClubUpgrades.Skip(1).All(item => item.DataId == Key(owner.Id) && item.AssetKey == upgrade.AssetKey &&
                    item.Stats.Health == 3 && item.Stats.Damage == 0 && item.Stats.Cost == 0 && item.Stats.Heal == 0 &&
                    item.Stats.Size == 0 && item.Stats.XCost == 0 && item.Stats.EquipmentLimit == 0 &&
                    item.Stats.UpgradeSlotCount == 0 && item.Statuses.Count == 0 && item.ExternalInteractions.Count == 0 &&
                    item.AbilityUpgrade == null && item.TriggerUpgrades?.Count is not > 0);
            bool fightClubLavaPreviewBirth = fightClubPreviewBirth && source.Train.Context != null && owner.Triggers.Count == 2 &&
                PreviewBirthModel.IsOriginalMeatShieldHeal(owner.Triggers[1], owner, source.Train.Context) &&
                (rule.Targets.Count == 3 && rule.Targets.Count(id => !targets.Contains(id)) == 1 ||
                 rule.Targets.Count == 2 && rule.Targets.All(targets.Contains)) && fightClubLavaCompanion;
            bool fightClubLavaCombatBirth = (fightClubBirth || fightClubSingleBirth) && source.Train.Context != null && owner.Triggers.Count == 2 &&
                PreviewBirthModel.IsOriginalMeatShieldHeal(owner.Triggers[1], owner, source.Train.Context) &&
                rule.Targets.Count == 2 && targets.Count == 3 && rule.Targets.All(targets.Contains) && fightClubLavaCompanion;
            return third ? OnlyFragileBuffSource(source, allowLiveCombat: originalChampion, allowTwoLiveCombat: originalSingleBirth || originalSycophant || originalTripleBirth || fightClubCompanionsBirth || fightClubLavaCombatBirth, allowSingleRetiredCombat: originalSingleRetiredBirth,
                    pendingDyingRoom: originalPendingTripleBirth || source.Preview && fightClubCompanionsBirth ? room.RoomIndex : (int?)null,
                    pendingDyingPreview: source.Preview && fightClubCompanionsBirth,
                    allowTwoLivePreview: originalPreviewBirth || originalFourHordePreviewBirth || originalSixHordePuffballPreviewBirth || originalSixHordeStewardFanPreviewBirth || originalSixHordeStewardCompanionPreviewBirth || fightClubLavaPreviewBirth || fightClubCompanionsBirth || fightClubRetiredPuffballBirth || fightClubStewardPreviewBirth || fightClubWoundedStewardPreviewBirth || fightClubSmallStewardMissingTarget, allowMixedCombat: fightClubBirth || originalTripleBirth || fightClubLavaCombatBirth,
                    allowSingleNonPreviewRetiredPreview: originalTripleCompanionsRetiredPreviewBirth,
                    allowPreviewRetired: originalPreviewBirth || originalFourHordePreviewBirth && rule.Targets.Count == 5 || originalSixHordeCompanionsPreviewBirth || originalSixHordeRetiredPuffballPreviewBirth || originalSixHordeStewardFanPreviewBirth || originalSixHordeStewardCompanionPreviewBirth ||
                        fightClubCompanionsBirth && (targets.Count == 4 || room.Units.Single(unit => unit.Id == added[0]).Status("horde")?.Stacks == 6) ||
                        fightClubPreviewBirth && room.Units.Single(unit => unit.Id == added[0]).Modifiers!.Upgrades.Count > 4 || fightClubRetiredPuffballBirth || fightClubStewardPreviewBirth || fightClubWoundedStewardPreviewBirth || fightClubSmallStewardMissingTarget, allowSingleRetiredPreview: originalFourHordePreviewBirth || originalTripleCompanionsRetiredPreviewBirth,
                    allowTwoRetiredMixedPreview: fightClubPreviewBirth && room.Units.Single(unit => unit.Id == added[0]).Modifiers!.Upgrades.Count > 4 || originalPreviewBirth && room.Units.Single(unit => unit.Id == added[0]).Modifiers!.Upgrades.Count == 6 || originalLavaTwoRetiredPreviewBirth || fightClubRetiredPuffballBirth || fightClubStewardPreviewBirth,
                    allowTwoPreviewRetired: originalSixHordeStewardCompanionPreviewBirth || originalPreviewBirth && OriginalStewardCompanion() || fightClubSmallStewardMissingTarget) :
                OnlyTitanskinSources(source.Train.Rooms, source.RetainedUnits, source.EnchanterIds);
        }

        internal static bool OnlyFragileBuffSource(EnchantmentCombatState source, bool allowLiveCombat = false, bool allowTwoLiveCombat = false, bool allowTwoLivePreview = false, bool allowMixedCombat = false, bool allowPreviewRetired = false, bool allowSingleRetiredPreview = false, bool allowTwoRetiredMixedPreview = false, bool allowSingleRetiredCombat = false, int? pendingDyingRoom = null, bool pendingDyingPreview = false, bool allowSingleNonPreviewRetiredPreview = false, bool allowTwoPreviewRetired = false)
        {
            CombatUnit[] owners = source.Train.Rooms.SelectMany(room => room.Units)
                .Where(unit => source.EnchanterIds.Contains(unit.Id) && unit.AssetKey == "Boss_Level8_Fragile").ToArray();
            if (owners.Length != 1 || source.EnchanterIds.Count(id => id == owners[0].Id) != 1) return false;
            CombatUnit owner = owners[0];
            if (owner is not { AssetKey: "Boss_Level8_Fragile", Team: CombatTeam.Enemy, MaxHealth: 3000 } ||
                owner.Health <= 0 || owner.DeathState?.IsDestroyed == true) return false;
            int[] retiredIds = source.EnchanterIds.Where(id => id != owner.Id).ToArray();
            bool livePreviewTitanskin = (source.Preview || allowLiveCombat) && retiredIds.Length == 1 &&
                source.Train.Rooms.SelectMany(room => room.Units).Count(unit => unit.Id == retiredIds[0] &&
                    unit.AssetKey == "EnchanterT2_Titanskin" && unit.Health > 0 && unit.DeathState?.IsDestroyed != true) == 1 &&
                OnlyTitanskinSources(source.Train.Rooms, source.RetainedUnits, retiredIds);
            bool mixedPreviewTitanskin = (source.Preview || allowMixedCombat) && retiredIds.Length == 2 && retiredIds.Distinct().Count() == 2 &&
                source.Train.Rooms.SelectMany(room => room.Units).Count(unit => retiredIds.Contains(unit.Id) &&
                    unit.AssetKey == "EnchanterT2_Titanskin" && unit.Health > 0 && unit.DeathState?.IsDestroyed != true) == 1 &&
                source.RetainedUnits.Count(actor => retiredIds.Contains(actor.Unit.Id) && actor.RoomIndex == -1 &&
                    (!actor.Preview || allowPreviewRetired) && actor.Unit.Health <= 0 && actor.Unit.DeathState?.IsDestroyed == true) == 1 &&
                OnlyTitanskinSources(source.Train.Rooms, source.RetainedUnits, retiredIds);
            bool twoLiveCombatTitanskin = (!source.Preview && allowTwoLiveCombat || source.Preview && allowTwoLivePreview) && retiredIds.Length == 2 &&
                retiredIds.Distinct().Count() == 2 && source.Train.Rooms.SelectMany(room => room.Units).Count(unit =>
                    retiredIds.Contains(unit.Id) && unit.AssetKey == "EnchanterT2_Titanskin" &&
                    unit.Health > 0 && unit.DeathState?.IsDestroyed != true) == 2 &&
                OnlyTitanskinSources(source.Train.Rooms, source.RetainedUnits, retiredIds);
            bool singleRetiredPreviewTitanskin = source.Preview && allowSingleRetiredPreview && retiredIds.Length == 1 &&
                !source.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == retiredIds[0]) &&
                source.RetainedUnits.Count(actor => actor.Unit.Id == retiredIds[0] && actor.RoomIndex == -1 && actor.Preview &&
                    actor.Unit.Health <= 0 && actor.Unit.DeathState?.IsDestroyed == true) == 1 &&
                OnlyTitanskinSources(source.Train.Rooms, source.RetainedUnits, retiredIds);
            bool singleNonPreviewRetiredPreviewTitanskin = source.Preview && allowSingleNonPreviewRetiredPreview && retiredIds.Length == 1 &&
                !source.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == retiredIds[0]) &&
                source.RetainedUnits.Count(actor => actor.Unit.Id == retiredIds[0] && actor.RoomIndex == -1 && !actor.Preview &&
                    actor.Unit.Health <= 0 && actor.Unit.DeathState?.IsDestroyed == true) == 1 &&
                OnlyTitanskinSources(source.Train.Rooms, source.RetainedUnits, retiredIds);
            bool singleRetiredCombatTitanskin = !source.Preview && allowSingleRetiredCombat && retiredIds.Length == 1 &&
                !source.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == retiredIds[0]) &&
                source.RetainedUnits.Count(actor => actor.Unit.Id == retiredIds[0] && actor.RoomIndex == -1 && !actor.Preview &&
                    actor.Unit.Health <= 0 && actor.Unit.DeathState?.IsDestroyed == true) == 1 &&
                OnlyTitanskinSources(source.Train.Rooms, source.RetainedUnits, retiredIds);
            bool pendingDyingTitanskin = source.Preview == pendingDyingPreview && pendingDyingRoom.HasValue && retiredIds.Length == 2 &&
                retiredIds.Distinct().Count() == 2 && source.Train.Rooms.SelectMany(room => room.Units).Count(unit =>
                    retiredIds.Contains(unit.Id) && unit.AssetKey == "EnchanterT2_Titanskin" && unit.Health > 0 &&
                    unit.DeathState?.IsDestroyed != true) == 1 &&
                source.RetainedUnits.Count(actor => retiredIds.Contains(actor.Unit.Id) && actor.RoomIndex == pendingDyingRoom && actor.Preview == pendingDyingPreview &&
                    actor.Unit.Health <= 0 && actor.Unit.DeathState is { IsDestroyed: false, HasFinishedDying: true, IsBeingRemoved: true, IsDespawned: false }) == 1 &&
                OnlyTitanskinSources(source.Train.Rooms, source.RetainedUnits, retiredIds, pendingDyingRoom, pendingDyingPreview);
            bool twoRetiredMixedPreviewTitanskin = source.Preview && allowTwoRetiredMixedPreview && retiredIds.Length == 2 &&
                retiredIds.Distinct().Count() == 2 && !source.Train.Rooms.SelectMany(room => room.Units).Any(unit => retiredIds.Contains(unit.Id)) &&
                source.RetainedUnits.Count(actor => retiredIds.Contains(actor.Unit.Id) && actor.RoomIndex == -1 && actor.Preview &&
                    actor.Unit.Health <= 0 && actor.Unit.DeathState?.IsDestroyed == true) == 1 &&
                source.RetainedUnits.Count(actor => retiredIds.Contains(actor.Unit.Id) && actor.RoomIndex == -1 && !actor.Preview &&
                    actor.Unit.Health <= 0 && actor.Unit.DeathState?.IsDestroyed == true) == 1 &&
                OnlyTitanskinSources(source.Train.Rooms, source.RetainedUnits, retiredIds);
            bool twoRetiredPreviewTitanskin = source.Preview && allowTwoPreviewRetired && retiredIds.Length == 2 &&
                retiredIds.Distinct().Count() == 2 && !source.Train.Rooms.SelectMany(room => room.Units).Any(unit => retiredIds.Contains(unit.Id)) &&
                retiredIds.All(id => source.RetainedUnits.Count(actor => actor.Unit.Id == id && actor.RoomIndex == -1 && actor.Preview &&
                    actor.Unit.Health <= 0 && actor.Unit.DeathState?.IsDestroyed == true) == 1) &&
                OnlyTitanskinSources(source.Train.Rooms, source.RetainedUnits, retiredIds);
            // The native registry can retain destroyed Titanskin owners after
            // their room bindings were removed. They cannot perform writes.
            if (retiredIds.Length != 0 && !livePreviewTitanskin && !mixedPreviewTitanskin && !twoLiveCombatTitanskin && !singleRetiredPreviewTitanskin && !singleNonPreviewRetiredPreviewTitanskin && !singleRetiredCombatTitanskin && !pendingDyingTitanskin && !twoRetiredMixedPreviewTitanskin && !twoRetiredPreviewTitanskin && (retiredIds.Length != 2 || retiredIds.Distinct().Count() != 2 ||
                source.Train.Rooms.SelectMany(room => room.Units).Any(unit => retiredIds.Contains(unit.Id)) ||
                retiredIds.Any(id => source.RetainedUnits.Count(actor => actor.Unit.Id == id &&
                    actor.RoomIndex == -1 && !actor.Preview && actor.Unit.Health <= 0 &&
                    actor.Unit.DeathState?.IsDestroyed == true) != 1) ||
                !OnlyTitanskinSources(source.Train.Rooms, source.RetainedUnits, retiredIds))) return false;
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
            IReadOnlyList<EnchantmentRetainedUnit> retainedUnits, IReadOnlyList<int> enchanterIds, int? pendingDyingRoom = null, bool pendingDyingPreview = false)
        {
            CombatUnit[] enchanters = rooms.SelectMany(floor => floor.Units)
                .Where(unit => enchanterIds.Contains(unit.Id)).ToArray();
            var retired = retainedUnits.Where(actor => enchanterIds.Contains(actor.Unit.Id) &&
                !enchanters.Any(unit => unit.Id == actor.Unit.Id)).ToArray();
            if (retired.Any(actor => actor.Unit.AssetKey != "EnchanterT2_Titanskin" || actor.Unit.Team != CombatTeam.Enemy ||
                actor.Unit.Health > 0 || !(actor.Unit.DeathState?.IsDestroyed == true && actor.RoomIndex == -1 ||
                    pendingDyingRoom.HasValue && actor.RoomIndex == pendingDyingRoom && actor.Preview == pendingDyingPreview &&
                    actor.Unit.DeathState is { IsDestroyed: false, HasFinishedDying: true, IsBeingRemoved: true, IsDespawned: false })) ||
                enchanters.Any(unit => unit.Health <= 0 || unit.DeathState?.IsDestroyed == true)) return false;
            return enchanterIds.Count > 0 && enchanters.Length + retired.Length == enchanterIds.Count &&
                enchanters.Concat(retired.Select(actor => actor.Unit)).All(unit =>
            {
                var effects = unit.Triggers.SelectMany(trigger => trigger.Effects).Where(effect => effect.Enchantment != null).ToArray();
                return unit.AssetKey == "EnchanterT2_Titanskin" && unit.Team == CombatTeam.Enemy &&
                    effects.Length == 1 && effects[0].Enchantment is { HasParentCard: false, StatusPool.Count: 1 } aura &&
                    (aura.Bound || unit.DeathState?.IsDestroyed == true || pendingDyingRoom.HasValue &&
                        unit.DeathState is { IsDestroyed: false, HasFinishedDying: true, IsBeingRemoved: true, IsDespawned: false }) &&
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
