using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class HivemindTurnUpgrade
    {
        internal const string StatusId = "hivemind";
        private const string UpgradeId = "RoomStateHivemindModifierUpgrade";
        private const string Hunter = "RAny_Attacker_Hunter_Hivemind_Ver2";
        private const string Infested = "R2_Support_FeedInfested";
        private const string Heavy = "R2_Heavy_HealGorge";
        private const string FeedUpgrade = "661fb64c-20d2-484c-8c30-309931b8148c";

        internal static bool HasOriginalFirstWavePair(IEnumerable<CombatUnit> units)
        {
            var owners = units.Where(unit => unit.RegisteredStatus(StatusId) != null ||
                unit.RegisteredStatus("hivemindkiller") != null).ToArray();
            return owners.Length == 2 && owners[0].AssetKey != owners[1].AssetKey &&
                owners.All(unit => unit.Status(StatusId) is CombatStatus status && IsOriginalFreshOwner(unit, status) &&
                    unit.RegisteredStatus("hivemindkiller") == null);
        }

        internal static bool IsOriginalFreshOwner(CombatUnit unit, CombatStatus status) =>
            IsOriginalOwner(unit, status) && unit.Modifiers!.Upgrades.Count == 0;

        private static bool IsOriginalOwner(CombatUnit unit, CombatStatus status) =>
            unit.Team == CombatTeam.Enemy && unit.SpawnerCardId == 0 && unit.IsBoss != true && unit.Health > 0 &&
            unit.Modifiers is { IsClone: false } &&
            status.Id == StatusId && status.Stacks == 1 && status.ParamInt == 1 &&
            status.DisplayCategory == "Positive" && status.Stackable == true && status.Hidden == false &&
            status.Propagatable == false && !status.RemoveWhenTriggered && !status.RemoveStackAtEnd &&
            !status.RemoveAllAtEnd && !status.RemoveAfterPostCombat && !status.SkipDuringDeployment &&
            !status.RemoveDuringDeployment && !status.RemoveWhenTriggeredAfterCardPlayed &&
            !status.RemoveAtEndOfTurnIfTriggered && !status.MarkedForRemovalAfterCardPlayed &&
            (unit.AssetKey == Hunter && unit.Triggers.Count == 0 ||
             unit.AssetKey == Infested && unit.Triggers.Count > 0 && unit.Triggers.All(trigger => trigger.Kind == "OnEaten"));

        internal static bool IsFirstPreview(RoomCombatState room) =>
            room.Preview && room.Context?.QueryFrame?.Turn == 1 &&
            room.Units.Any(unit => unit.AssetKey == Hunter && HasFirstUpgrade(unit)) &&
            room.Units.Any(unit => unit.AssetKey == Infested && HasFirstUpgrade(unit)) &&
            room.Units.All(unit => unit.RegisteredStatus("hivemindkiller") == null &&
                (unit.Status(StatusId) == null || HasFirstUpgrade(unit) || IsFedPreviewRecipient(unit)));

        private static bool HasFirstUpgrade(CombatUnit unit) =>
            unit.Status(StatusId) is CombatStatus status && IsOriginalOwner(unit, status) &&
            unit.Modifiers!.Upgrades is [var upgrade] && upgrade.DataId == UpgradeId && upgrade.Stats.Damage == 2 &&
            upgrade.Stats.Health == 0 && upgrade.Statuses.Count == 0 && upgrade.Unique;

        internal static bool MatchesRoomMarker(string marker, CombatUnit unit) =>
            unit.Status(StatusId) is CombatStatus status && (IsOriginalOwner(unit, status) || IsFedPreviewRecipient(unit)) &&
            marker == Marker(unit);

        internal static string Marker(CombatUnit unit) => unit.AssetKey + " room modifiers";

        internal static bool IsPreviewFeedUpgrade(RoomCombatState room, CombatUnit unit, CardUpgradeModifier upgrade, string? kind) =>
            kind == "OnEaten" && IsFirstPreview(room) && IsFeedRecipient(unit) &&
            unit.RegisteredStatus(StatusId) == null && IsFeedUpgrade(upgrade);

        private static bool IsFeedRecipient(CombatUnit unit) => unit.AssetKey == Heavy &&
            unit.Team == CombatTeam.Enemy && unit.SpawnerCardId == 0 && unit.Health > 0 && unit.IsBoss != true &&
            unit.Modifiers is { IsClone: false };

        private static bool IsFeedUpgrade(CardUpgradeModifier upgrade) => upgrade.DataId == FeedUpgrade &&
            upgrade.Statuses is [var status] && status.Id == StatusId && status.Stacks == 3 && status.ParamInt == 1 &&
            upgrade.Stats.Damage == 0 && upgrade.Stats.Health == 0 && upgrade.ExternalInteractions.Count == 0;

        internal static bool IsFedPreviewRecipient(CombatUnit unit) => IsFeedRecipient(unit) &&
            unit.Status(StatusId)?.Stacks == 3 &&
            unit.Modifiers!.Upgrades.Count(IsFeedUpgrade) == 1 &&
            unit.Modifiers.Upgrades.Count(upgrade => upgrade.DataId == UpgradeId) <= 1;

        internal static bool NeedsPreviewFeedMarker(RoomCombatState room, CombatUnit unit) => IsFirstPreview(room) &&
            IsFedPreviewRecipient(unit) && !unit.Modifiers!.Upgrades.Any(upgrade => upgrade.DataId == UpgradeId);

        // This first ordinary turn projects the native attack-only upgrade
        // applied to the two untouched wave births. Other lifecycle boundaries
        // continue to require native feed, removal and death calibration.
        internal static TrainCombatState? ApplyFirstOrdinaryTurn(TrainCombatState source, out string? error)
        {
            error = null;
            var owners = source.Rooms
                .SelectMany(room => room.Units.Select(unit => (Room: room, Unit: unit, Status: unit.Status(StatusId))))
                .Where(item => item.Status != null)
                .ToArray();
            if (owners.Length == 0) return source;
            if (owners.Length != 2 || owners[0].Room.RoomIndex != owners[1].Room.RoomIndex ||
                owners[0].Unit.AssetKey == owners[1].Unit.AssetKey ||
                owners.Any(item => item.Room.Preview || !IsOriginalFreshOwner(item.Unit, item.Status!)) ||
                source.Rooms.SelectMany(room => room.Units).Any(unit => unit.Status("hivemindkiller") != null))
            {
                error = "Hivemind requires the captured first-wave native status owners.";
                return null;
            }

            CardUpgradeModifier upgrade = AttackUpgrade(owners.Sum(item => item.Status!.Stacks));
            RoomCombatState affected = owners[0].Room;
            CombatUnit[] units = affected.Units.ToArray();
            foreach (var owner in owners)
            {
                var local = new RoomCombatState(affected.RoomIndex, false, new[] { owner.Unit }, Array.Empty<string>(), source.Context);
                var callbacks = new List<RoomCombatModel.QueuedCharacterTrigger>();
                RoomCombatResult applied = UnitModifierModel.ApplyWithSettlement(local, owner.Unit.Id, upgrade,
                    "TemporaryUntilUnitDeath", false, null, 0, null, (state, actor, _) =>
                        new RoomCombatResult(state.WithUnits(new[] { actor }), RoomOutcome.Exchanged, 0, new List<CombatEvent>()),
                    enqueueCallback: callbacks.Add, directApi: true, upgradeId: UpgradeId, allowFreshHivemind: true);
                if (!applied.Supported || callbacks.Count != 0 || applied.State!.Units.Count != 1 ||
                    applied.Outcome != RoomOutcome.Exchanged)
                {
                    error = applied.UnsupportedReason ?? "Hivemind produced an unobserved direct upgrade interaction.";
                    return null;
                }
                units[Array.FindIndex(units, unit => unit.Id == owner.Unit.Id)] = applied.State.Units[0];
            }
            RoomCombatState changed = new RoomCombatState(affected.RoomIndex, affected.Deployment, units,
                affected.ExternalInteractions.Concat(owners.Select(owner => Marker(owner.Unit)))
                    .Distinct().OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                affected.Context, affected.Preview, affected.ActiveCompanionBossIds);
            var rooms = source.Rooms.Select(room => room.RoomIndex == affected.RoomIndex ? changed : room).ToArray();
            return EnchantmentWorldModel.Rebase(new TrainCombatState(rooms, source.Movement, source.EnemySlotsPerRoom, source.Context));
        }

        internal static CardUpgradeModifier AttackUpgrade(int damage)
        {
            var stats = new CardStatModifier(damage: damage);
            var mask = new CardUpgradeMaskMetadata(Array.Empty<UpgradeMaskStatus>(), false, false, false, false);
            return new CardUpgradeModifier(UpgradeId, "", stats, Array.Empty<CombatStatus>(),
                false, true, false, 0, 0, Array.Empty<string>(), maskMetadata: mask, lifecycle:
                new CardLifecycleUpgrade(new CardMaskUpgrade(UpgradeId, stats, mask.Statuses, false, false, false, false),
                    "", true, false, Array.Empty<CardTraitValue>(), Array.Empty<string>(), false,
                    Array.Empty<string>(), Array.Empty<string>(), Array.Empty<CardLifecycleTrigger>()));
        }
    }
}
