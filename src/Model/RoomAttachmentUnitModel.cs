using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal sealed class RoomAttachmentUnitSyncResult
    {
        internal TrainCombatState? State { get; }
        internal string? UnsupportedReason { get; }
        internal bool Supported => State != null;
        internal RoomAttachmentUnitSyncResult(TrainCombatState? state, string? error = null)
        { State = state; UnsupportedReason = error; }
    }

    internal static class RoomAttachmentUnitModel
    {
        internal static RoomCombatState? ApplyToNewCharacter(RoomCombatState source, int unitId, out string? error)
        {
            error = null;
            CombatUnit? unit = source.Units.FirstOrDefault(item => item.Id == unitId);
            if (unit == null) { error = "Room attachment birth synchronization requires a live character."; return null; }
            RoomCapacityState? room = source.Context?.RoomCapacities?.FirstOrDefault(item => item.RoomIndex == source.RoomIndex);
            if (room == null) return source;
            if (room.Attachments == null)
            {
                if (room.AttachmentsAddedFromCards > 0)
                    error = "Room attachment birth synchronization requires captured attachment payloads.";
                return error == null ? source : null;
            }

            RoomCombatState state = source;
            foreach (RoomCapacityAttachmentState attachment in room.Attachments)
            {
                CombatUnit changed = state.Units.First(item => item.Id == unitId);
                if (!Affected(changed, attachment, state.Context)) continue;
                if (attachment.CharacterUpgrades == null)
                { error = "Room attachment birth synchronization requires captured character upgrades."; return null; }
                if (changed.Modifiers == null)
                { error = "Room attachment birth synchronization requires captured unit modifiers."; return null; }
                foreach (CardUpgradeModifier upgrade in attachment.CharacterUpgrades)
                {
                    if (!ApplyUpgrade(ref state, unitId, attachment, upgrade, remove: false, out error)) return null;
                }
            }

            return ReferenceEquals(state, source) ? source : state;
        }

        internal static RoomAttachmentUnitSyncResult Synchronize(TrainCombatState source,
            IReadOnlyList<RoomCapacityState>? before, IReadOnlyList<RoomCapacityState>? after)
        {
            if (before == null && after == null) return new RoomAttachmentUnitSyncResult(source);
            if (before == null || after == null)
                return Fail("Room attachment character synchronization requires before/after room states.");

            var rooms = new List<RoomCombatState>(source.Rooms.Count);
            CombatContext? context = source.Context;
            bool moonChanged = false;
            foreach (RoomCombatState room in source.Rooms)
            {
                RoomCapacityState? previous = before.FirstOrDefault(item => item.RoomIndex == room.RoomIndex);
                RoomCapacityState? current = after.FirstOrDefault(item => item.RoomIndex == room.RoomIndex);
                if (SameAttachment(previous, current))
                {
                    rooms.Add(room);
                    continue;
                }

                RoomCombatState state = new RoomCombatState(room.RoomIndex, room.Deployment, room.Units,
                    room.ExternalInteractions, context, room.Preview, room.ActiveCompanionBossIds);
                if (previous?.Attachments?.SingleOrDefault() is RoomCapacityAttachmentState removed)
                {
                    foreach (CombatUnit original in state.Units.ToArray())
                    {
                        if (!Affected(original, removed, state.Context)) continue;
                        if (removed.CharacterUpgrades == null)
                            return Fail("Removing a room attachment requires its captured character upgrades.");
                        if (original.Modifiers == null)
                            return Fail("Room attachment character synchronization requires captured unit modifiers.");
                        foreach (CardUpgradeModifier upgrade in removed.CharacterUpgrades)
                        {
                            if (!ApplyUpgrade(ref state, original.Id, removed, upgrade, remove: true, out string? error))
                                return Fail(error!);
                        }
                    }
                }

                if (current?.Attachments?.SingleOrDefault() is RoomCapacityAttachmentState added)
                {
                    foreach (CombatUnit original in state.Units.ToArray())
                    {
                        if (!Affected(original, added, state.Context)) continue;
                        if (added.CharacterUpgrades == null)
                            return Fail("Adding a room attachment requires its captured character upgrades.");
                        if (original.Modifiers == null)
                            return Fail("Room attachment character synchronization requires captured unit modifiers.");
                        foreach (CardUpgradeModifier upgrade in added.CharacterUpgrades)
                        {
                            if (!ApplyUpgrade(ref state, original.Id, added, upgrade, remove: false, out string? error))
                                return Fail(error!);
                        }
                    }
                }

                if (state.Context?.QueryFrame is { MoonPhaseTrackingActive: false, MoonPhase: 1 or 2 } inactive)
                {
                    var oldAttachment = previous?.Attachments?.SingleOrDefault();
                    var newAttachment = current?.Attachments?.SingleOrDefault();
                    bool Updates(CombatUnit unit) => oldAttachment?.CharacterUpgrades?.Count > 0 && Affected(unit, oldAttachment, state.Context) ||
                        newAttachment?.CharacterUpgrades?.Count > 0 && Affected(unit, newAttachment, state.Context);
                    var affected = state.Units.Where(unit => unit.MoonPhaseMask.HasValue && Updates(unit)).ToArray();
                    if (affected.Any(unit => unit.Triggers.Any(trigger => trigger.Kind is
                        "OnMoonLit" or "OnMoonlitLost" or "OnMoonShade" or "OnMoonshadeLost") ||
                        state.Context.FindCard(unit.SpawnerCardId)?.MaskDescriptor?.BaseTraits.Any(trait =>
                            trait.RuntimeType.Contains("MoonPhase", StringComparison.Ordinal)) == true))
                        return Fail("Inactive room attachment moon callbacks require native calibration.");
                    // TrainRoomAttachmentState updates understanding after each
                    // applied/removed character upgrade even without tracking.
                    int mask = RoomMoonPhaseModel.Effective(state.Context, room.RoomIndex, inactive.MoonPhase.Value);
                    state = state.WithUnits(state.Units.Select(unit => affected.Any(item => item.Id == unit.Id)
                        ? unit.WithMoonPhaseMask(mask) : unit).ToArray());
                }
                if (previous?.Attachments?.Any(item => item.MoonPhase != null) == true || current?.Attachments?.Any(item => item.MoonPhase != null) == true)
                {
                    if (state.Context?.QueryFrame?.MoonPhase is int phase && state.Context.QueryFrame.MoonPhaseTrackingActive == true)
                    {
                        // Installation/removal updates affected characters before
                        // draining their original New gained/lost callbacks.
                        if (state.Units.Any(unit => unit.Health > 0 && unit.Triggers.Any(trigger =>
                                trigger.Kind is "OnMoonLit" or "OnMoonlitLost") &&
                            (!unit.MoonPhaseMask.HasValue || (unit.MoonPhaseMask.Value & 2) != (phase & 2))))
                            return Fail("Room attachment callbacks require settled prior Full understanding.");
                        RoomCapacityAttachmentState? oldAttachment = previous?.Attachments?.SingleOrDefault();
                        RoomCapacityAttachmentState? newAttachment = current?.Attachments?.SingleOrDefault();
                        int mask = RoomMoonPhaseModel.Effective(state.Context, room.RoomIndex, phase);
                        state = state.WithUnits(state.Units.Select(unit => (oldAttachment != null && Affected(unit, oldAttachment, state.Context) || newAttachment != null && Affected(unit, newAttachment, state.Context))
                            ? unit.WithMoonPhaseMask(mask) : unit).ToArray());
                        moonChanged = true;
                    }
                }
                context = state.Context;
                rooms.Add(state);
            }
            var synchronized = new TrainCombatState(rooms.Select(room => new RoomCombatState(room.RoomIndex,
                room.Deployment, room.Units, room.ExternalInteractions, context, room.Preview, room.ActiveCompanionBossIds)).ToArray(),
                source.Movement, source.EnemySlotsPerRoom, context);
            if (moonChanged && context != null)
            {
                // Installing/removing the native room modifier refreshes hand
                // resource traits too, even when there are no callback actors.
                context = CardMoonUpgradeModel.RefreshHand(context, out string? handError);
                if (handError != null) return Fail(handError);
                synchronized = CardSpellModel.WithContext(synchronized, context);
            }
            if (moonChanged && source.Rooms.SelectMany(room => room.Units).Any(unit => unit.Triggers.Any(trigger =>
                trigger.Kind is "OnMoonLit" or "OnMoonlitLost" or "OnMoonShade" or "OnMoonshadeLost")))
            {
                if (source.Rooms.Any(room => room.Preview))
                    return Fail("Room attachment moon callback previews require separate native calibration.");
                string? error = RoomMoonPhaseModel.ValidateCallbacks(source);
                if (error != null) return Fail(error);
                MoonPhaseTransitionResult callbacks = RoomMoonPhaseModel.ApplyUnderstanding(source, synchronized, phaseShift: false);
                return new RoomAttachmentUnitSyncResult(callbacks.State, callbacks.UnsupportedReason);
            }
            return new RoomAttachmentUnitSyncResult(synchronized);
        }

        internal static RoomAttachmentUnitSyncResult SynchronizeMovement(TrainCombatState before, TrainCombatState after)
        {
            IReadOnlyList<RoomCapacityState>? previousRooms = before.Context?.RoomCapacities;
            IReadOnlyList<RoomCapacityState>? currentRooms = after.Context?.RoomCapacities;
            if (previousRooms == null && currentRooms == null) return new RoomAttachmentUnitSyncResult(after);
            if (previousRooms == null || currentRooms == null)
                return Fail("Room attachment movement synchronization requires before/after room states.");

            TrainCombatState state = after;
            foreach (CombatUnit previousUnit in before.Rooms.SelectMany(room => room.Units))
            {
                RoomCombatState? oldRoom = before.Rooms.FirstOrDefault(room => room.Units.Any(unit => unit.Id == previousUnit.Id));
                RoomCombatState? newRoom = state.Rooms.FirstOrDefault(room => room.Units.Any(unit => unit.Id == previousUnit.Id));
                if (oldRoom == null || newRoom == null || oldRoom.RoomIndex == newRoom.RoomIndex) continue;
                RoomCapacityState? oldCapacity = previousRooms.FirstOrDefault(room => room.RoomIndex == oldRoom.RoomIndex);
                RoomCapacityState? newCapacity = currentRooms.FirstOrDefault(room => room.RoomIndex == newRoom.RoomIndex);
                if (previousUnit.Team == CombatTeam.Player && (oldCapacity?.Attachments?.Any(attachment => attachment.MoonPhase != null) == true ||
                    newCapacity?.Attachments?.Any(attachment => attachment.MoonPhase != null) == true))
                {
                    if (oldRoom.Preview || newRoom.Preview)
                        return Fail("Friendly movement through moon-override rooms in preview requires native calibration.");
                    foreach (RoomCapacityAttachmentState attachment in (oldCapacity?.Attachments ?? Array.Empty<RoomCapacityAttachmentState>())
                        .Concat(newCapacity?.Attachments ?? Array.Empty<RoomCapacityAttachmentState>()))
                        if (attachment.MoonPhase != null && RoomMoonPhaseModel.Validate(attachment.MoonPhase) is string error)
                            return Fail(error);
                    // Original spawn-point callbacks refresh resources only.
                    // Bump performs the character refresh after its shift/sentry
                    // callbacks; physical attachment synchronization does not.
                }
                if (oldCapacity == null || newCapacity == null) continue;
                if ((oldCapacity.Attachments == null && oldCapacity.AttachmentsAddedFromCards > 0) ||
                    (newCapacity.Attachments == null && newCapacity.AttachmentsAddedFromCards > 0))
                    return Fail("Room attachment movement requires captured attachment payloads.");

                RoomCombatState currentRoom = state.Rooms.Single(room => room.RoomIndex == newRoom.RoomIndex);
                CombatUnit currentUnit = currentRoom.Units.Single(unit => unit.Id == previousUnit.Id);
                foreach (RoomCapacityAttachmentState removed in oldCapacity.Attachments ?? Array.Empty<RoomCapacityAttachmentState>())
                {
                    if (!IsApplied(currentUnit, removed) ||
                        (newCapacity.Attachments ?? Array.Empty<RoomCapacityAttachmentState>()).Any(item => SameAttachment(removed, item))) continue;
                    if (removed.CharacterUpgrades == null)
                        return Fail("Leaving a room attachment requires its captured character upgrades.");
                    foreach (CardUpgradeModifier upgrade in removed.CharacterUpgrades)
                    {
                        if (!ApplyUpgrade(ref currentRoom, currentUnit.Id, removed, upgrade, remove: true, out string? error,
                            movementExit: true))
                            return Fail(error!);
                    }
                    if (state.Context?.QueryFrame is { MoonPhaseTrackingActive: false, MoonPhase: 1 or 2 } inactive &&
                        previousUnit is { Team: CombatTeam.Enemy, MoonPhaseMask: 5 or 6 } &&
                        (previousUnit.MoonPhaseMask.Value & 3) != inactive.MoonPhase.Value)
                    {
                        bool originalObservedExit = OriginalMushyExit(removed, previousUnit) ||
                            OriginalHellfurnaceExit(removed, previousUnit, inactive.MoonPhase.Value) ||
                            OriginalCardlessRoomMoonSync(removed, previousUnit);
                        if (!originalObservedExit && (previousUnit is not { AssetKey: "Boss_Level8_Fragile", MaxHealth: 3000 } &&
                            previousUnit is not { AssetKey: "HeavyT3_Basic", BaseAttack: 8, MaxHealth: 325, Size: 2, Triggers.Count: 0 } &&
                            previousUnit is not { AssetKey: "HeavyT3_PyreDamageIfUnblocked", BaseAttack: 5, MaxHealth: 250, Size: 2,
                                Triggers: [{ Kind: "PostCombat", FireCount: 1, SkipDuringDeployment: true,
                                    Effects: [{ Type: "CardEffectPyreDamageIfNoPlayerUnits", Value: 5,
                                        Action: { Type: "Damage", Target: "Pyre", Value: 5 } }] }] } &&
                            previousUnit is not { AssetKey: "HeavyT2_Zealot", BaseAttack: 5 or 11, MaxHealth: 110, Size: 2,
                                Triggers: [{ Kind: "OnAnyUnitDeathOnFloor", FireCount: 1,
                                    Effects: [{ Type: "CardEffectBuffDamage", Value: 1, Action: { Type: "BuffAttack", Target: "Self", Value: 1 } }] }] } &&
                            !(previousUnit is { AssetKey: "HeavyT2_Zealot", BaseAttack: 4 or 6 or 7 or 15, MaxHealth: 110, Size: 2,
                                HordeDefinition: { Attack: 3, Health: 110 }, Modifiers.DamageBuff: 1 or 3 or 4 or 12,
                                Triggers: [{ Kind: "OnAnyUnitDeathOnFloor", FireCount: 1,
                                    Effects: [{ Type: "CardEffectBuffDamage", Value: 1, Action: { Type: "BuffAttack", Target: "Self", Value: 1 } }] }] } &&
                                (previousUnit.BaseAttack, previousUnit.Modifiers.DamageBuff) is (4, 1) or (6, 3) or (7, 4) or (15, 12) &&
                                previousUnit.Status("multistrike")?.Stacks == 1) ||
                            !OriginalInferno(removed)) ||
                            oldRoom.Preview || newRoom.Preview || currentUnit.Triggers.Any(trigger => trigger.Kind is
                                "OnMoonLit" or "OnMoonlitLost" or "OnMoonShade" or "OnMoonshadeLost"))
                            return Fail("Inactive enemy room-exit moon understanding requires native calibration.");
                        int mask = (previousUnit.MoonPhaseMask.Value & ~3) | inactive.MoonPhase.Value;
                        currentRoom = currentRoom.WithUnits(currentRoom.Units.Select(unit => unit.Id == currentUnit.Id
                            ? unit.WithMoonPhaseMask(mask) : unit).ToArray());
                    }
                    state = WithRoom(state, currentRoom);
                    currentUnit = currentRoom.Units.Single(unit => unit.Id == previousUnit.Id);
                }

                foreach (RoomCapacityAttachmentState added in newCapacity.Attachments ?? Array.Empty<RoomCapacityAttachmentState>())
                {
                    if (!Affected(currentUnit, added, state.Context) || IsApplied(currentUnit, added) ||
                        (oldCapacity.Attachments ?? Array.Empty<RoomCapacityAttachmentState>()).Any(item => SameAttachment(added, item))) continue;
                    if (added.CharacterUpgrades == null)
                        return Fail("Entering a room attachment requires its captured character upgrades.");
                    foreach (CardUpgradeModifier upgrade in added.CharacterUpgrades)
                    {
                        if (!ApplyUpgrade(ref currentRoom, currentUnit.Id, added, upgrade, remove: false, out string? error))
                            return Fail(error!);
                    }
                    if (state.Context?.QueryFrame is { MoonPhaseTrackingActive: false, MoonPhase: 1 or 2 } inactive &&
                        previousUnit is { Team: CombatTeam.Enemy, MoonPhaseMask: 5 or 6 } &&
                        (previousUnit.MoonPhaseMask.Value & 3) != inactive.MoonPhase.Value)
                    {
                        bool originalObservedEntry = inactive.MoonPhase == 1 && OriginalMushyEntry(added, previousUnit) ||
                            inactive.MoonPhase == 2 && OriginalMushyDarkEntry(added, previousUnit) ||
                            OriginalHellfurnaceEntry(added, previousUnit, inactive.MoonPhase.Value) ||
                            OriginalCardlessRoomMoonSync(added, previousUnit);
                        if (!originalObservedEntry && ((inactive.MoonPhase == 1 && previousUnit is not { AssetKey: "Boss_Level8_Fragile", MaxHealth: 3000 } &&
                            previousUnit.AssetKey != "HeavyT3_PyreDamageIfUnblocked") ||
                            previousUnit is not { AssetKey: "Boss_Level8_Fragile", MaxHealth: 3000 } &&
                            previousUnit is not { AssetKey: "HeavyT3_Basic", BaseAttack: 8, MaxHealth: 325, Size: 2, Triggers.Count: 0 } &&
                            previousUnit is not { AssetKey: "HeavyT3_PyreDamageIfUnblocked", BaseAttack: 5, MaxHealth: 250, Size: 2,
                                Triggers: [{ Kind: "PostCombat", FireCount: 1, SkipDuringDeployment: true,
                                    Effects: [{ Type: "CardEffectPyreDamageIfNoPlayerUnits", Value: 5,
                                        Action: { Type: "Damage", Target: "Pyre", Value: 5 } }] }] } &&
                            !(previousUnit is { AssetKey: "AttackerT3_Sweep", BaseAttack: 5, MaxHealth: 5, Size: 1, Triggers.Count: 0 } &&
                                previousUnit.Status("sweep")?.Stacks == 1) || !OriginalInferno(added)) ||
                            oldRoom.Preview || newRoom.Preview || currentUnit.Triggers.Any(trigger => trigger.Kind is
                                "OnMoonLit" or "OnMoonlitLost" or "OnMoonShade" or "OnMoonshadeLost"))
                            return Fail("Inactive enemy room-entry moon understanding requires native calibration.");
                        int mask = (previousUnit.MoonPhaseMask.Value & ~3) | inactive.MoonPhase.Value;
                        currentRoom = currentRoom.WithUnits(currentRoom.Units.Select(unit => unit.Id == currentUnit.Id
                            ? unit.WithMoonPhaseMask(mask) : unit).ToArray());
                    }
                    state = WithRoom(state, currentRoom);
                    currentUnit = currentRoom.Units.Single(unit => unit.Id == previousUnit.Id);
                }
            }
            return new RoomAttachmentUnitSyncResult(state);
        }

        internal static bool OriginalHellfurnace(RoomCapacityAttachmentState attachment) =>
            attachment is { AssetKey: "Hellfurnace", DataId: "5d34a8d1-be5c-44e0-bd7e-b6cd576df0bb", AttachmentType: 0,
                CapacityDelta: 0, AffectsHeroes: true, AffectsMonsters: true, IgnoreBosses: false,
                SpellCostDelta: 0, EnergyPerTurnDelta: 0, MoonPhase: null, PreCombat: null, PostCombat: null,
                CharacterFilters.Count: 0, CharacterUpgrades.Count: 1,
                StatusStackBonuses: [{ StatusId: "pyregel", Amount: 3 }], SpawnCopySubtype: null,
                SpellUpgrade: null, DeathEffect: null, Ability: null, CorruptionModifier: null, Echo: null } &&
            attachment.CharacterUpgrades[0] is { AssetKey: "AmplificationChamber", DataId: "63c7092d-84ce-4904-a4a5-eeedbdc74b2b" } upgrade &&
            new[] { "Damage", "Health", "Cost", "Heal", "Size", "XCost", "EquipmentLimit", "UpgradeSlotCount" }.All(stat => upgrade.Stats.Value(stat) == 0) &&
            upgrade.Statuses.Count == 0 && upgrade.ExternalInteractions.Count == 0 && upgrade.TriggerUpgrades?.Count is not > 0 && upgrade.AbilityUpgrade == null;

        internal static bool OriginalHellfurnaceExit(RoomCapacityAttachmentState attachment, CombatUnit unit, int moonPhase = 2) =>
            moonPhase is 1 or 2 && OriginalHellfurnace(attachment) &&
            (unit is { AssetKey: "HeavyT3_Basic", BaseAttack: 8, Health: > 0 and <= 325, MaxHealth: 325, Size: 2, SpawnerCardId: 0, Triggers.Count: 0 } ||
             unit is { AssetKey: "HeavyT3_PyreDamageIfUnblocked", BaseAttack: 5, Health: > 0 and <= 250, MaxHealth: 250, Size: 2, SpawnerCardId: 0,
                Triggers: [{ Kind: "PostCombat", FireCount: 1, SkipDuringDeployment: true,
                    Effects: [{ Type: "CardEffectPyreDamageIfNoPlayerUnits", Value: 5,
                        Action: { Type: "Damage", Target: "Pyre", Value: 5 } }] }] } ||
             unit is { AssetKey: "AttackerT3_Sweep", BaseAttack: 5, Health: > 0 and <= 5, MaxHealth: 5,
                Size: 1, SpawnerCardId: 0, Triggers.Count: 0 } && unit.Status("sweep")?.Stacks == 1 ||
             moonPhase == 2 && OriginalRax(unit));

        internal static bool OriginalHellfurnaceEntry(RoomCapacityAttachmentState attachment, CombatUnit unit, int moonPhase = 1) =>
            (((unit.AssetKey is "HeavyT3_PyreDamageIfUnblocked" or "HeavyT3_Basic") && (moonPhase is 1 or 2) ||
              unit.AssetKey == "AttackerT3_Sweep" && moonPhase is 1 or 2) && OriginalHellfurnaceExit(attachment, unit, 2)) ||
            (moonPhase == 1 && OriginalHellfurnace(attachment) && OriginalRax(unit));

        internal static bool OriginalMushyDarkEntry(RoomCapacityAttachmentState added, CombatUnit previousUnit) =>
            (previousUnit.AssetKey is "HeavyT3_PyreDamageIfUnblocked" or "AttackerT3_Sweep" or "Boss_Level7_Rax") && OriginalMushyExit(added, previousUnit);

        internal static bool OriginalMushyEntry(RoomCapacityAttachmentState added, CombatUnit previousUnit) =>
            OriginalMushy(added) && previousUnit is
                { AssetKey: "Boss_Level6_Cael_DamageShield", BaseAttack: 22, Health: > 0 and <= 1800, MaxHealth: 1800, Size: 6, SpawnerCardId: 0,
                    Triggers: [{ Kind: "PostCombat", FireCount: 1, SkipDuringDeployment: true,
                        Effects: [{ Type: "CardEffectAddTempCardUpgradeToUnits", UnitUpgrade: { Type: "UnitUpgrade",
                            Target: "FrontInRoom", Upgrade.DataId: "04897158-64c9-4828-9541-b5f20f627eee" } }] }] };

        internal static bool OriginalMushyExit(RoomCapacityAttachmentState removed, CombatUnit previousUnit) =>
            OriginalMushy(removed) &&
            (previousUnit is { AssetKey: "HeavyT2_Basic", BaseAttack: 6, Health: > 0 and <= 120, MaxHealth: 120, Size: 2, SpawnerCardId: 0, Triggers.Count: 0 } ||
             previousUnit is { AssetKey: "HeavyT3_Basic", BaseAttack: 8, Health: > 0 and <= 325, MaxHealth: 325, Size: 2, SpawnerCardId: 0, Triggers.Count: 0 } ||
             previousUnit is { AssetKey: "HeavyT3_PyreDamageIfUnblocked", BaseAttack: 5, Health: > 0 and <= 250, MaxHealth: 250, Size: 2, SpawnerCardId: 0,
                Triggers: [{ Kind: "PostCombat", FireCount: 1, SkipDuringDeployment: true,
                    Effects: [{ Type: "CardEffectPyreDamageIfNoPlayerUnits", Value: 5,
                        Action: { Type: "Damage", Target: "Pyre", Value: 5 } }] }] } ||
             OriginalRax(previousUnit) ||
             previousUnit is { AssetKey: "AttackerT3_Sweep", BaseAttack: 5, Health: > 0 and <= 5, MaxHealth: 5, Size: 1, SpawnerCardId: 0, Triggers.Count: 0 } &&
                previousUnit.Status("sweep")?.Stacks == 1 ||
             OriginalMushyEntry(removed, previousUnit) ||
             previousUnit is { AssetKey: "Boss_Level6_FeedDamageShield", BaseAttack: 0, Health: > 0 and <= 60, MaxHealth: 60, Size: 1, SpawnerCardId: 0,
                Triggers: [{ Kind: "OnEaten", FireCount: 1, SkipDuringDeployment: true,
                    Effects: [{ Type: "CardEffectAddTempCardUpgradeToUnits", UnitUpgrade: { Type: "UnitUpgrade",
                        Target: "LastFeederCharacter", Upgrade.DataId: "04897158-64c9-4828-9541-b5f20f627eee" } },
                        { Type: "CardEffectDamage", Value: 20, Action: { Type: "Damage", Target: "Self", Value: 20 } }] }] });

        private static bool OriginalRax(CombatUnit unit) =>
            unit is { AssetKey: "Boss_Level7_Rax", BaseAttack: 9, Health: > 0 and <= 1100, MaxHealth: 1100, Size: 5, SpawnerCardId: 0,
                Triggers: [{ Kind: "PostCombat", FireCount: 1, SkipDuringDeployment: true, Effects: [
                    { Type: "CardEffectAddStatusEffect", Action: { Type: "AddStatus", Target: "Self", Statuses: [{ Id: "buff", Stacks: 8, ParamInt: 2 }] } },
                    { Type: "CardEffectAddStatusEffect", Action: { Type: "AddStatus", Target: "Self", Statuses: [{ Id: "regen", Stacks: 20, ParamInt: 1 }] } },
                    { Type: "CardEffectAddStatusEffect", Action: { Type: "AddStatus", Target: "Self", Statuses: [{ Id: "spell shield", Stacks: 2, ParamInt: 0 }] } }] }] };

        private static bool OriginalCardlessRoomMoonSync(RoomCapacityAttachmentState attachment, CombatUnit unit) =>
            unit.Team == CombatTeam.Enemy && unit.SpawnerCardId == 0 &&
            (OriginalBatchRoom(attachment) || OriginalTriggerRoom(attachment) ||
                RoomEnemyEntryModel.IsStatusOnly(attachment) ||
                OriginalMushy(attachment) || OriginalHellfurnace(attachment) ||
                OriginalInferno(attachment) && attachment.AffectsHeroes);

        internal static bool OriginalTriggerRoom(RoomCapacityAttachmentState attachment)
        {
            if (attachment is not { AttachmentType: 0, CapacityDelta: 0, AffectsHeroes: true,
                AffectsMonsters: true, IgnoreBosses: false, SpellCostDelta: 0, EnergyPerTurnDelta: 0,
                MoonPhase: null, CharacterFilters.Count: 0, CharacterUpgrades.Count: 1,
                PostCombat: null, PreCombat: null, StatusStackBonuses: null, SpawnCopySubtype: null,
                SpellUpgrade: null, DeathEffect: null, Ability: null, CorruptionModifier: null, Echo: null } ||
                !Guid.TryParse(attachment.AttachmentId, out Guid id) || id == Guid.Empty) return false;

            CardUpgradeModifier upgrade = attachment.CharacterUpgrades[0];
            if (upgrade.Stats is not { Damage: 0, Health: 0, Cost: 0, Heal: 0, Size: 0,
                XCost: 0, EquipmentLimit: 0, UpgradeSlotCount: 0 } ||
                upgrade.Statuses.Count != 0 || upgrade.ExternalInteractions.Count != 0 ||
                upgrade.AbilityUpgrade != null || upgrade.TriggerUpgrades is not [
                    { Kind: "OnDeath", Once: false, HasTriggered: false, IgnoreSilence: true,
                        FireCount: 1, Effects.Count: 1 }]) return false;

            CombatEffect effect = upgrade.TriggerUpgrades[0].Effects[0];
            return attachment switch
            {
                { AssetKey: "BoilerRoom", DataId: "08001aa4-2e9d-42da-9ba7-ff4b0fd5ce80" } =>
                    upgrade is { AssetKey: "FuelCar_Upgrade", DataId: "05e215c6-600c-4ecb-a75e-c806dd596bcf" } &&
                    effect is { Type: "CardEffectGainEnergy", Value: 1,
                        Action: { Type: "GainEnergy", Target: "Room", Value: 1 } },
                { AssetKey: "SacrificialChamber", DataId: "627f12a8-081e-437d-8a54-a20d719163b7" } =>
                    upgrade is { AssetKey: "SacrificialChamber", DataId: "e3b37138-8f47-4cd0-a52c-44494f65b369" } &&
                    effect is { Type: "CardEffectRewardGold", Value: 10, Action: null },
                _ => false
            };
        }

        internal static bool OriginalBatchRoom(RoomCapacityAttachmentState attachment)
        {
            if (attachment is not { AttachmentType: 0, CapacityDelta: 0, AffectsHeroes: true,
                IgnoreBosses: false, EnergyPerTurnDelta: 0, MoonPhase: null, PreCombat: null, PostCombat: null,
                CharacterFilters.Count: 0, CharacterUpgrades.Count: 1, StatusStackBonuses: null,
                SpawnCopySubtype: null, CorruptionModifier: null, Echo: null }) return false;

            CardUpgradeModifier upgrade = attachment.CharacterUpgrades[0];
            if (upgrade.Stats is not { Damage: 0, Health: 0, Cost: 0, Heal: 0, Size: 0,
                XCost: 0, EquipmentLimit: 0, UpgradeSlotCount: 0 } ||
                upgrade.ExternalInteractions.Count != 0 || upgrade.TriggerUpgrades?.Count is > 0 ||
                upgrade.AbilityUpgrade != null) return false;

            bool zone = attachment is { AssetKey: "ZoneOfSilence", DataId: "56468ba5-86b6-49be-ba30-02baf7ffd6dd",
                AffectsMonsters: false, SpellCostDelta: 0, SpellUpgrade: null, DeathEffect: null, Ability: null } &&
                upgrade is { AssetKey: "ZoneOfSilence", DataId: "5c47b876-dad4-4b81-9169-bfcb8ab62ca6",
                    Statuses: [{ Id: "silenced", Stacks: 1 }] };
            if (zone) return true;
            if (upgrade.Statuses.Count != 0 || !attachment.AffectsMonsters) return false;
            return attachment switch
            {
                { AssetKey: "EnchantedChamber", DataId: "00f55f64-c5c1-4d5e-9f59-f50413341c1e",
                    SpellCostDelta: 0, DeathEffect: null, Ability: null,
                    SpellUpgrade.Upgrade.DataId: "0ce30902-963a-4662-b9e7-fca70d79735c" } =>
                    upgrade is { AssetKey: "EnchantedChamber", DataId: "68d8696e-ec20-425e-b23d-8a0b9da0dfaf" },
                { AssetKey: "Infectory", DataId: "e1a39d88-4a77-4399-98b2-0b81f44fe6a8",
                    SpellCostDelta: 0, SpellUpgrade: null, Ability: null, DeathEffect: not null } =>
                    upgrade is { AssetKey: "InfectoryEffect_Upgrade", DataId: "118977af-2756-44ff-aa49-ac31ef21fabc" } &&
                    RoomDeathEffectModel.Validate(attachment.DeathEffect) == null,
                { AssetKey: "SpellSanctum", DataId: "5af36c0f-ab98-4a49-903a-b08d93705af1",
                    SpellCostDelta: -1, SpellUpgrade: null, DeathEffect: null, Ability: null } =>
                    upgrade is { AssetKey: "SpellSanctum_Upgrade", DataId: "35178e4a-0880-440b-a929-de29de6e88f8" },
                { AssetKey: "TrapChute", DataId: "8d65e688-d93d-4bfa-975d-8c5f046fe2db",
                    SpellCostDelta: 0, SpellUpgrade: null, DeathEffect: null,
                    Ability.DataId: "1fd6ef15-e044-4e0e-93c4-3766602282ab" } =>
                    upgrade is { AssetKey: "TrapChute_Upgrade", DataId: "1354d4ad-3f96-4089-8144-ab8a7f956d8d" },
                _ => false
            };
        }

        internal static bool OriginalInferno(RoomCapacityAttachmentState attachment) =>
            attachment.DataId == "378cea0c-14e9-45fb-9918-fb853bd04a7d" && InfernoPayload(attachment);

        internal static bool InfernoPayload(RoomCapacityAttachmentState attachment) =>
            attachment is { AssetKey: RelicStartCombatAttachmentModel.InfernoUpgradeKey, CharacterUpgrades.Count: 1,
                PreCombat: { DisallowedInDeployment: true, FocusRoom: true, Effects.Count: 1 } preCombat } &&
            RoomPreCombatModel.Validate(preCombat) == null &&
            attachment.CharacterUpgrades[0] is { AssetKey: RelicStartCombatAttachmentModel.InfernoUpgradeKey,
                DataId: RelicStartCombatAttachmentModel.InfernoUpgradeId } upgrade &&
            new[] { "Damage", "Health", "Cost", "Heal", "Size", "XCost", "EquipmentLimit", "UpgradeSlotCount" }.All(stat => upgrade.Stats.Value(stat) == 0) &&
            upgrade.Statuses.Count == 0 && upgrade.ExternalInteractions.Count == 0 && upgrade.TriggerUpgrades?.Count is not > 0 && upgrade.AbilityUpgrade == null;

        internal static bool OriginalMushy(RoomCapacityAttachmentState attachment) =>
            attachment is { AssetKey: "MushyRoom", DataId: "83e40135-9e3a-4290-ab9f-d4c8c7e1ac0b", AffectsHeroes: true,
                AffectsMonsters: true, IgnoreBosses: false, CharacterUpgrades.Count: 1,
                PostCombat: { DisallowedInDeployment: false, Effects: [{ Type: "AddStatus", Target: "Room", Value: 0,
                    AllowEnemy: true, AllowPlayer: false, Range: null, Statuses: [{ Id: "debuff", Stacks: 3, ParamInt: 2 }] }] } } &&
            attachment.CharacterUpgrades[0] is { AssetKey: "MushyRoom_Upgrade", DataId: "5364e378-3dbb-43c3-8c9b-ff305ef779f5" } upgrade &&
            new[] { "Damage", "Health", "Cost", "Heal", "Size", "XCost", "EquipmentLimit", "UpgradeSlotCount" }.All(stat => upgrade.Stats.Value(stat) == 0) &&
            upgrade.Statuses.Count == 0 && upgrade.ExternalInteractions.Count == 0 && upgrade.TriggerUpgrades?.Count is not > 0 && upgrade.AbilityUpgrade == null;

        internal static RoomAttachmentUnitSyncResult SynchronizeTargetability(TrainCombatState before, TrainCombatState after)
        {
            foreach (RoomCombatState oldRoom in before.Rooms)
            foreach (CombatUnit previous in oldRoom.Units)
            {
                CombatUnit? current = after.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.Id == previous.Id);
                if (current == null || previous.Status("untouchable") != null || current.Status("untouchable") == null ||
                    previous.Status("horde")?.Stacks is not > 0) continue;
                foreach (CombatUnit owner in oldRoom.Units.Where(owner => owner.Health > 0 && owner.Modifiers?.TroopAura?.Targets.Contains(previous.Id) == true))
                    if (!after.Rooms.Any(room => room.RoomIndex == oldRoom.RoomIndex && room.Units.Any(unit => unit.Id == current.Id)) ||
                        !TroopAuraModel.KnownStasisTarget(oldRoom, owner, current, after.Context))
                        return Fail("Untouchable troop-aura target removal requires native calibration.");
            }
            if (after.Context?.RoomCapacities?.Any(room => room.Attachments?.Count > 0) != true)
                return new RoomAttachmentUnitSyncResult(after);
            TrainCombatState state = after;
            foreach (RoomCombatState oldRoom in before.Rooms)
            foreach (CombatUnit previous in oldRoom.Units)
            {
                RoomCombatState? room = state.Rooms.FirstOrDefault(item => item.RoomIndex == oldRoom.RoomIndex &&
                    item.Units.Any(unit => unit.Id == previous.Id));
                CombatUnit? current = room?.Units.FirstOrDefault(unit => unit.Id == previous.Id);
                if (current == null || (previous.Status("untouchable") != null) == (current.Status("untouchable") != null)) continue;
                foreach (RoomCapacityAttachmentState attachment in state.Context?.RoomCapacities?
                    .FirstOrDefault(item => item.RoomIndex == room!.RoomIndex)?.Attachments ?? Array.Empty<RoomCapacityAttachmentState>())
                {
                    bool remove = current.Status("untouchable") != null;
                    if (remove ? !IsApplied(current, attachment) : !Affected(current, attachment, state.Context) || IsApplied(current, attachment)) continue;
                    if (attachment.CharacterUpgrades is not { Count: 1 } ||
                        attachment.CharacterUpgrades[0] is not CardUpgradeModifier upgrade ||
                        !(attachment.AssetKey == "EnchantedChamber" && upgrade.DataId == "68d8696e-ec20-425e-b23d-8a0b9da0dfaf" && upgrade.Stats.Damage == 0 ||
                          attachment.AssetKey == "FightClub" && upgrade.DataId == "f93ac9f7-45d2-47a4-a159-a1b326bd047f" && upgrade.Stats.Damage == 15) ||
                        upgrade.Stats.Health != 0 || upgrade.Stats.Size != 0 ||
                        upgrade.UnhealedHealth != 0 || upgrade.DamageBuff != 0 || upgrade.ExternalInteractions.Count > 0 ||
                        upgrade.Statuses.Count > 0 || upgrade.TriggerUpgrades?.Count > 0 || upgrade.AbilityUpgrade != null)
                        return Fail("Untouchable room attachment payload requires independent native calibration.");
                    RoomCombatState changed = room!;
                    if (!ApplyUpgrade(ref changed, current.Id, attachment, upgrade, remove, out string? error)) return Fail(error!);
                    state = WithRoom(state, changed);
                    room = state.Rooms.Single(item => item.RoomIndex == room!.RoomIndex);
                    current = room.Units.Single(unit => unit.Id == previous.Id);
                }
            }
            return new RoomAttachmentUnitSyncResult(state);
        }

        private static bool SameAttachment(RoomCapacityState? left, RoomCapacityState? right)
        {
            RoomCapacityAttachmentState? a = left?.Attachments?.SingleOrDefault();
            RoomCapacityAttachmentState? b = right?.Attachments?.SingleOrDefault();
            if (a == null || b == null) return a == null && b == null;
            // A freshly attached copy has a new native upgrade object even if
            // its payload is identical. Remove/reinstall it in native list order.
            return left!.AttachmentsAddedFromCards == right!.AttachmentsAddedFromCards &&
                a.DataId == b.DataId && a.AssetKey == b.AssetKey && a.AttachmentType == b.AttachmentType &&
                a.CapacityDelta == b.CapacityDelta && a.AffectsHeroes == b.AffectsHeroes &&
                a.AffectsMonsters == b.AffectsMonsters && a.IgnoreBosses == b.IgnoreBosses &&
                a.AttachmentId == b.AttachmentId &&
                a.CharacterUpgrades?.Select(upgrade => upgrade.DataId).SequenceEqual(
                    b.CharacterUpgrades?.Select(upgrade => upgrade.DataId) ?? Array.Empty<string>()) == true;
        }

        private static bool SameAttachment(RoomCapacityAttachmentState left, RoomCapacityAttachmentState right) =>
            left.DataId == right.DataId && left.AssetKey == right.AssetKey && left.AttachmentType == right.AttachmentType &&
            left.CapacityDelta == right.CapacityDelta && left.AffectsHeroes == right.AffectsHeroes &&
            left.AffectsMonsters == right.AffectsMonsters && left.IgnoreBosses == right.IgnoreBosses &&
            left.AttachmentId == right.AttachmentId &&
            left.CharacterUpgrades?.Select(upgrade => upgrade.DataId).SequenceEqual(
                right.CharacterUpgrades?.Select(upgrade => upgrade.DataId) ?? Array.Empty<string>()) == true;

        private static bool IsApplied(CombatUnit unit, RoomCapacityAttachmentState attachment)
        {
            if (attachment.CharacterUpgrades == null || unit.Modifiers?.Upgrades == null) return false;
            bool hasTriggerUpgrade = attachment.CharacterUpgrades.Any(expected => expected.TriggerUpgrades?.Count > 0);
            if (hasTriggerUpgrade && (attachment.AttachmentId.Length == 0 ||
                !unit.Triggers.Any(trigger => trigger.Origin?.UpgradeId == attachment.AttachmentId))) return false;
            return attachment.CharacterUpgrades.Count > 0 && attachment.CharacterUpgrades.All(expected =>
                unit.Modifiers.Upgrades.Any(applied => applied.DataId == expected.DataId && applied.AssetKey == expected.AssetKey));
        }

        private static bool ApplyUpgrade(ref RoomCombatState state, int unitId,
            RoomCapacityAttachmentState attachment, CardUpgradeModifier upgrade, bool remove, out string? error,
            bool movementExit = false)
        {
            error = null;
            if (upgrade.TriggerUpgrades?.Count > 0)
            {
                if (string.IsNullOrEmpty(attachment.AttachmentId))
                { error = "Room attachment trigger synchronization requires its native attachment identity."; return false; }
                RoomCombatResult applied = UnitModifierModel.ApplyDirect(state, unitId, upgrade,
                    remove, attachment.AttachmentId);
                if (!applied.Supported || applied.State == null)
                { error = applied.UnsupportedReason ?? "Room attachment trigger synchronization failed."; return false; }
                state = applied.State;
                return true;
            }

            if (upgrade.Stats.Damage != 0 || upgrade.Statuses.Count > 0)
            {
                RoomCombatResult applied = UnitModifierModel.ApplyDirect(state, unitId, upgrade, remove,
                    upgrade.DataId);
                if (!applied.Supported || applied.State == null)
                { error = applied.UnsupportedReason ?? "Room attachment stat synchronization failed."; return false; }
                // Ascension drains the original Zone's status removal callbacks at the destination.
                if (movementExit && attachment is { DataId: "56468ba5-86b6-49be-ba30-02baf7ffd6dd", IgnoreBosses: true } &&
                    upgrade is { DataId: "5c47b876-dad4-4b81-9169-bfcb8ab62ca6", Statuses: [{ Id: "silenced", Stacks: 1 }] } &&
                    applied.PendingCallbacks.Any(callback => callback.Kind == "OnSilenceLost" &&
                        (callback.Unit.Status("fullmoon") != null || callback.Unit.Status("newmoon") != null)))
                {
                    applied = RoomCombatModel.DrainCharacterCallbacks(applied.State, applied.PendingCallbacks);
                    if (!applied.Supported || applied.State == null)
                    { error = applied.UnsupportedReason ?? "Room exit silence callback failed."; return false; }
                }
                state = applied.State;
                return true;
            }

            CombatUnit? unit = state.Units.FirstOrDefault(item => item.Id == unitId);
            if (unit?.Modifiers == null)
            { error = "Room attachment character synchronization requires captured unit modifiers."; return false; }
            CardUpgradeModifier[] upgrades;
            if (remove)
            {
                int appliedIndex = LastMatchingUpgrade(unit.Modifiers.Upgrades, upgrade);
                if (appliedIndex < 0)
                { error = "A replaced room attachment upgrade was missing from an impacted unit."; return false; }
                upgrades = unit.Modifiers.Upgrades.Where((_, upgradeIndex) => upgradeIndex != appliedIndex).ToArray();
            }
            else upgrades = unit.Modifiers.Upgrades.Concat(new[] { upgrade }).ToArray();
            state = WithUnit(state, unitId, WithUpgrades(unit, upgrades));
            return true;
        }

        private static bool Affected(CombatUnit unit, RoomCapacityAttachmentState attachment, CombatContext? context)
        {
            bool teamMatches = unit.Team == CombatTeam.Player ? attachment.AffectsMonsters : attachment.AffectsHeroes;
            if (!teamMatches || unit.Statuses.Any(status => status.Id == "untouchable" && status.Stacks > 0)) return false;
            if (attachment.CharacterFilters?.Any(filter => !CardUpgradeMaskModel.FilterCharacter(filter,
                new CardUpgradeMaskCharacter(unit.Subtypes, Array.Empty<string>(), unit.Size),
                context?.Relics?.Any(relic => relic.EffectTypes.Contains("RelicEffectMonstersAreAllSubtypes")) == true)) == true) return false;
            if (!attachment.IgnoreBosses) return true;
            return unit.IsBoss == false;
        }

        private static int LastMatchingUpgrade(IReadOnlyList<CardUpgradeModifier> upgrades, CardUpgradeModifier target)
        {
            for (int index = upgrades.Count - 1; index >= 0; index--)
                if (upgrades[index].DataId == target.DataId && upgrades[index].AssetKey == target.AssetKey) return index;
            return -1;
        }

        private static CombatUnit WithUpgrades(CombatUnit unit, IReadOnlyList<CardUpgradeModifier> upgrades)
        {
            UnitModifiers modifiers = unit.Modifiers!;
            return unit.WithModifiers(new UnitModifiers(modifiers.AttackDamage, modifiers.AttackDamageAdded,
                modifiers.DamageBuff, modifiers.RawSize, modifiers.EquipmentLimit, modifiers.CanBeHealed,
                modifiers.IsClone, upgrades, modifiers.HealthFromUpgrades, modifiers.SpawnerMatchesDefinition,
                modifiers.DamageBuffFromStatusEffects, modifiers.MagebladeDamagePerMagicPower,
                modifiers.StatusAttackModifiers, modifiers.IsChampion, modifiers.SelfAttackRules, modifiers.RoomResources, modifiers.RoomStatuses, modifiers.RoomTriggerCounts, modifiers.SpawnCopySubtype, modifiers.Graft, modifiers.SynergizeStatuses, modifiers.Sentry, modifiers.TroopAura));
        }

        private static RoomCombatState WithUnit(RoomCombatState room, int unitId, CombatUnit changed) =>
            new RoomCombatState(room.RoomIndex, room.Deployment,
                room.Units.Select(item => item.Id == unitId ? changed : item).ToArray(), room.ExternalInteractions,
                room.Context, room.Preview, room.ActiveCompanionBossIds);

        private static TrainCombatState WithRoom(TrainCombatState train, RoomCombatState changed)
        {
            CombatContext? context = changed.Context ?? train.Context;
            RoomCombatState[] rooms = train.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment,
                room.RoomIndex == changed.RoomIndex ? changed.Units : room.Units, room.ExternalInteractions,
                context, room.Preview, room.ActiveCompanionBossIds)).ToArray();
            return EnchantmentWorldModel.Rebase(new TrainCombatState(rooms, train.Movement, train.EnemySlotsPerRoom, context));
        }

        private static RoomAttachmentUnitSyncResult Fail(string error) => new(null, error);
    }
}
