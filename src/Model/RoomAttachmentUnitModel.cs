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

            CombatUnit changed = unit;
            foreach (RoomCapacityAttachmentState attachment in room.Attachments)
            {
                if (!Affected(changed, attachment)) continue;
                if (attachment.CharacterUpgrades == null)
                { error = "Room attachment birth synchronization requires captured character upgrades."; return null; }
                if (changed.Modifiers == null)
                { error = "Room attachment birth synchronization requires captured unit modifiers."; return null; }
                foreach (CardUpgradeModifier upgrade in attachment.CharacterUpgrades)
                    changed = WithUpgrades(changed, changed.Modifiers!.Upgrades.Concat(new[] { upgrade }).ToArray());
            }

            return ReferenceEquals(changed, unit) ? source : new RoomCombatState(source.RoomIndex, source.Deployment,
                source.Units.Select(item => item.Id == unitId ? changed : item).ToArray(), source.ExternalInteractions,
                source.Context, source.Preview, source.ActiveCompanionBossIds);
        }

        internal static RoomAttachmentUnitSyncResult Synchronize(TrainCombatState source,
            IReadOnlyList<RoomCapacityState>? before, IReadOnlyList<RoomCapacityState>? after)
        {
            if (before == null && after == null) return new RoomAttachmentUnitSyncResult(source);
            if (before == null || after == null)
                return Fail("Room attachment character synchronization requires before/after room states.");

            var rooms = new List<RoomCombatState>(source.Rooms.Count);
            foreach (RoomCombatState room in source.Rooms)
            {
                RoomCapacityState? previous = before.FirstOrDefault(item => item.RoomIndex == room.RoomIndex);
                RoomCapacityState? current = after.FirstOrDefault(item => item.RoomIndex == room.RoomIndex);
                if (SameAttachment(previous, current))
                {
                    rooms.Add(room);
                    continue;
                }

                CombatUnit[] units = room.Units.ToArray();
                if (previous?.Attachments?.SingleOrDefault() is RoomCapacityAttachmentState removed)
                {
                    for (int unitIndex = 0; unitIndex < units.Length; unitIndex++)
                    {
                        if (!Affected(units[unitIndex], removed)) continue;
                        if (removed.CharacterUpgrades == null)
                            return Fail("Removing a room attachment requires its captured character upgrades.");
                        if (units[unitIndex].Modifiers == null)
                            return Fail("Room attachment character synchronization requires captured unit modifiers.");
                        foreach (CardUpgradeModifier upgrade in removed.CharacterUpgrades)
                        {
                            int appliedIndex = LastMatchingUpgrade(units[unitIndex].Modifiers!.Upgrades, upgrade);
                            if (appliedIndex < 0)
                                return Fail("A replaced room attachment upgrade was missing from an impacted unit.");
                            units[unitIndex] = WithUpgrades(units[unitIndex], units[unitIndex].Modifiers!.Upgrades
                                .Where((_, upgradeIndex) => upgradeIndex != appliedIndex).ToArray());
                        }
                    }
                }

                if (current?.Attachments?.SingleOrDefault() is RoomCapacityAttachmentState added)
                {
                    for (int index = 0; index < units.Length; index++)
                    {
                        if (!Affected(units[index], added)) continue;
                        if (added.CharacterUpgrades == null)
                            return Fail("Adding a room attachment requires its captured character upgrades.");
                        if (units[index].Modifiers == null)
                            return Fail("Room attachment character synchronization requires captured unit modifiers.");
                        foreach (CardUpgradeModifier upgrade in added.CharacterUpgrades)
                            units[index] = WithUpgrades(units[index], units[index].Modifiers!.Upgrades.Concat(new[] { upgrade }).ToArray());
                    }
                }

                rooms.Add(new RoomCombatState(room.RoomIndex, room.Deployment, units, room.ExternalInteractions,
                    source.Context, room.Preview, room.ActiveCompanionBossIds));
            }
            return new RoomAttachmentUnitSyncResult(new TrainCombatState(rooms, source.Movement,
                source.EnemySlotsPerRoom, source.Context));
        }

        private static bool SameAttachment(RoomCapacityState? left, RoomCapacityState? right)
        {
            RoomCapacityAttachmentState? a = left?.Attachments?.SingleOrDefault();
            RoomCapacityAttachmentState? b = right?.Attachments?.SingleOrDefault();
            if (a == null || b == null) return a == null && b == null;
            return a.DataId == b.DataId && a.AssetKey == b.AssetKey && a.AttachmentType == b.AttachmentType &&
                a.CapacityDelta == b.CapacityDelta && a.AffectsHeroes == b.AffectsHeroes &&
                a.AffectsMonsters == b.AffectsMonsters && a.IgnoreBosses == b.IgnoreBosses &&
                a.CharacterUpgrades?.Select(upgrade => upgrade.DataId).SequenceEqual(
                    b.CharacterUpgrades?.Select(upgrade => upgrade.DataId) ?? Array.Empty<string>()) == true;
        }

        private static bool Affected(CombatUnit unit, RoomCapacityAttachmentState attachment)
        {
            bool teamMatches = unit.Team == CombatTeam.Player ? attachment.AffectsMonsters : attachment.AffectsHeroes;
            if (!teamMatches || unit.Statuses.Any(status => status.Id == "untouchable" && status.Stacks > 0)) return false;
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
                modifiers.StatusAttackModifiers));
        }

        private static RoomAttachmentUnitSyncResult Fail(string error) => new(null, error);
    }
}
