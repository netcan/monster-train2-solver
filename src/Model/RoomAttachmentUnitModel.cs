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
                        if (!ApplyUpgrade(ref currentRoom, currentUnit.Id, removed, upgrade, remove: true, out string? error))
                            return Fail(error!);
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
                    state = WithRoom(state, currentRoom);
                    currentUnit = currentRoom.Units.Single(unit => unit.Id == previousUnit.Id);
                }
            }
            return new RoomAttachmentUnitSyncResult(state);
        }

        internal static RoomAttachmentUnitSyncResult SynchronizeTargetability(TrainCombatState before, TrainCombatState after)
        {
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
            RoomCapacityAttachmentState attachment, CardUpgradeModifier upgrade, bool remove, out string? error)
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
