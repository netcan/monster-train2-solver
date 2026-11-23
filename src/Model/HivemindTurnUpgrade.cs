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
        private const string Zealot = "R2_Attacker_Zealot_Ver2";
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

        internal static bool IsFirstCombat(RoomCombatState room) =>
            room.Context?.QueryFrame?.Turn == 1 &&
            room.Units.Count(unit => unit.AssetKey == Hunter && HasFirstUpgrade(unit)) == 1 &&
            room.Units.Count(unit => unit.AssetKey == Infested && HasFirstUpgrade(unit)) == 1 &&
            room.Units.Count(IsFedPreviewRecipient) <= 1 &&
            room.Units.All(unit => unit.RegisteredStatus("hivemindkiller") == null &&
                (unit.Status(StatusId) == null || HasFirstUpgrade(unit) || IsFedPreviewRecipient(unit)));

        internal static bool IsSecondCombat(RoomCombatState room)
        {
            if (room.Context?.QueryFrame?.Turn != 2 || room.RoomIndex is not (0 or 1) ||
                room.Units.Any(unit => unit.RegisteredStatus("hivemindkiller") != null))
                return false;
            CombatUnit[] carriers = room.Units.Where(unit => unit.RegisteredStatus(StatusId) != null).ToArray();
            if (room.RoomIndex == 0)
                return carriers.Length is 1 or 2 && carriers.Count(unit => unit.AssetKey == Infested && HasAttackUpgrade(unit, 6)) == 1 &&
                    carriers.All(unit => unit.AssetKey == Infested && HasAttackUpgrade(unit, 6) ||
                        unit.AssetKey == Zealot && IsFedSecondPreviewRecipient(unit));
            return carriers.Length == 3 &&
                carriers.Count(unit => unit.AssetKey == Hunter && HasAttackUpgrade(unit, 6)) == 1 &&
                carriers.Count(unit => unit.AssetKey == Infested && HasAttackUpgrade(unit, 6)) == 1 &&
                carriers.Count(unit => unit.AssetKey == Heavy && IsFedSecondPreviewRecipient(unit)) == 1;
        }

        internal static bool IsThirdPreview(RoomCombatState room)
        {
            if (!room.Preview || room.Context?.QueryFrame?.Turn != 3 || room.RoomIndex is < 0 or > 2 ||
                room.Units.Any(unit => unit.RegisteredStatus("hivemindkiller") != null))
                return false;
            CombatUnit[] carriers = room.Units.Where(unit => unit.RegisteredStatus(StatusId) != null).ToArray();
            return carriers.Count(unit => unit.AssetKey == Infested && HasAttackUpgrade(unit, 13)) == 1 &&
                (room.RoomIndex == 0 && carriers.Length <= 2 &&
                    carriers.All(unit => unit.AssetKey == Infested && HasAttackUpgrade(unit, 13) || IsThirdNewHeavy(unit)) ||
                 room.RoomIndex == 1 && carriers.Length == 2 &&
                    carriers.Count(unit => unit.AssetKey == Zealot &&
                        (NativeFedUpgrade(unit, 3, 13) || NativeFedUpgrade(unit, 6, 13))) == 1 ||
                 room.RoomIndex == 2 && carriers.Count(unit => unit.AssetKey == Hunter && HasAttackUpgrade(unit, 13)) == 1 &&
                    carriers.Length == 3 && carriers.Count(unit => unit.AssetKey == Heavy &&
                        (NativeFedUpgrade(unit, 6, 13) || NativeFedUpgrade(unit, 9, 13))) == 1);
        }

        private static bool IsThirdNewHeavy(CombatUnit unit) => unit.AssetKey == Heavy && IsFeedRecipient(unit) &&
            unit.Status(StatusId)?.Stacks == 3 && unit.Modifiers!.Upgrades.Count(IsFeedUpgrade) == 1 &&
            unit.Modifiers.Upgrades.Count(upgrade => upgrade.DataId == UpgradeId) ==
                unit.Modifiers.Upgrades.Count(upgrade => IsAttackUpgrade(upgrade, 0)) &&
            unit.Modifiers.Upgrades.Count(upgrade => IsAttackUpgrade(upgrade, 0)) <= 1;

        internal static bool IsObservedThirdPreviewFeedDeath(RoomCombatState room, int targetId, int? actorId) =>
            targetId == actorId && room.RoomIndex == 2 && IsThirdPreview(room) &&
            room.Units.Any(unit => unit.Id == targetId && unit.AssetKey == Infested && HasAttackUpgrade(unit, 13)) &&
            room.Context?.Enchantments is EnchantmentWorld world &&
            world.Rooms.SelectMany(floor => floor.Units).All(unit =>
                unit.RegisteredStatus("hivemindkiller") == null && unit.Modifiers?.Upgrades.Any(upgrade =>
                    upgrade.DataId == "RoomStateHivemindKillerModifierUpgrade") != true);

        private static bool NativeFedUpgrade(CombatUnit unit, int stacks, int damage) =>
            IsFeedRecipient(unit) && unit.Status(StatusId)?.Stacks == stacks &&
            unit.Modifiers!.Upgrades.Count == stacks / 3 + 1 &&
            unit.Modifiers.Upgrades.Count(IsFeedUpgrade) == stacks / 3 &&
            unit.Modifiers.Upgrades.Count(upgrade => IsAttackUpgrade(upgrade, damage)) == 1;

        private static bool HasFirstUpgrade(CombatUnit unit) =>
            HasAttackUpgrade(unit, 2);

        private static bool HasAttackUpgrade(CombatUnit unit, int damage) =>
            unit.Status(StatusId) is CombatStatus status && IsOriginalOwner(unit, status) &&
            unit.Modifiers!.Upgrades is [var upgrade] && upgrade.DataId == UpgradeId && upgrade.Stats.Damage == damage &&
            upgrade.Stats.Health == 0 && upgrade.Statuses.Count == 0 && upgrade.Unique;

        internal static bool HasSecondWaveCarriers(IEnumerable<CombatUnit> units)
        {
            CombatUnit[] carriers = units.Where(unit => unit.RegisteredStatus(StatusId) != null ||
                unit.RegisteredStatus("hivemindkiller") != null).ToArray();
            return carriers.Length == 3 && carriers.All(unit => unit.RegisteredStatus("hivemindkiller") == null) &&
                carriers.Count(unit => unit.AssetKey == Hunter && HasFirstUpgrade(unit)) == 1 &&
                carriers.Count(unit => unit.AssetKey == Infested && HasFirstUpgrade(unit)) == 1 &&
                carriers.Count(unit => IsFedPreviewRecipient(unit) && unit.Modifiers!.Upgrades.Any(upgrade =>
                    upgrade.DataId == UpgradeId && upgrade.Stats.Damage == 0 && upgrade.Unique)) == 1;
        }

        internal static bool HasSecondWave(TrainCombatState train)
        {
            var carriers = train.Rooms.SelectMany(room => room.Units)
                .Where(unit => unit.RegisteredStatus(StatusId) != null || unit.RegisteredStatus("hivemindkiller") != null).ToArray();
            return carriers.Length == 4 &&
                carriers.Count(unit => unit.AssetKey == Infested && unit.Status(StatusId) is CombatStatus status &&
                    IsOriginalFreshOwner(unit, status)) == 1 &&
                HasSecondWaveCarriers(carriers.Where(unit => unit.Modifiers?.Upgrades.Count > 0));
        }

        internal static bool HasThirdWaveCarriers(IEnumerable<CombatUnit> units)
        {
            CombatUnit[] carriers = units.Where(unit => unit.RegisteredStatus(StatusId) != null ||
                unit.RegisteredStatus("hivemindkiller") != null).ToArray();
            return carriers.Length == 5 && carriers.All(unit => unit.RegisteredStatus("hivemindkiller") == null) &&
                carriers.Count(unit => unit.AssetKey == Hunter && HasAttackUpgrade(unit, 6)) == 1 &&
                carriers.Count(unit => unit.AssetKey == Infested && HasAttackUpgrade(unit, 6)) == 2 &&
                carriers.Count(unit => unit.AssetKey == Heavy && unit.Status(StatusId)?.Stacks == 6 &&
                    IsFedSecondPreviewRecipient(unit)) == 1 &&
                carriers.Count(unit => unit.AssetKey == Zealot && IsFedSecondPreviewRecipient(unit) &&
                    unit.Modifiers!.Upgrades.Count == 2) == 1;
        }

        internal static bool HasThirdWave(TrainCombatState train)
        {
            var carriers = train.Rooms.SelectMany(room => room.Units)
                .Where(unit => unit.RegisteredStatus(StatusId) != null || unit.RegisteredStatus("hivemindkiller") != null).ToArray();
            return carriers.Length == 6 &&
                carriers.Count(unit => unit.AssetKey == Infested && unit.Status(StatusId) is CombatStatus status &&
                    IsOriginalFreshOwner(unit, status)) == 1 &&
                HasThirdWaveCarriers(carriers.Where(unit => unit.Modifiers?.Upgrades.Count > 0));
        }

        internal static bool IsWaitingSecondWaveOwner(CombatUnit unit, CombatStatus status) =>
            IsOriginalFreshOwner(unit, status) || HasFirstUpgrade(unit) || IsFedPreviewRecipient(unit) ||
            HasAttackUpgrade(unit, 6) || IsFedSecondPreviewRecipient(unit) ||
            (unit.AssetKey == Heavy && IsFeedRecipient(unit) && status.Stacks == 6 &&
                unit.Modifiers!.Upgrades.Count == 2 && unit.Modifiers.Upgrades.All(IsFeedUpgrade));

        internal static TrainCombatState? ApplySecondOrdinaryTurn(TrainCombatState source, out string? error)
        {
            error = null;
            if (!HasSecondWave(source))
            {
                error = "Hivemind requires the observed three carried owners and one second-wave birth.";
                return null;
            }
            return ApplyObservedRefresh(source, out error);
        }

        internal static TrainCombatState? ApplyThirdOrdinaryTurn(TrainCombatState source, out string? error)
        {
            if (!HasThirdWave(source))
            {
                error = "Hivemind requires the observed five carried owners and one third-wave birth.";
                return null;
            }
            return ApplyObservedRefresh(source, out error);
        }

        private static TrainCombatState? ApplyObservedRefresh(TrainCombatState source, out string? error)
        {
            error = null;
            int total = source.Rooms.SelectMany(room => room.Units).Sum(unit => unit.Status(StatusId)?.Stacks ?? 0);
            var rooms = new List<RoomCombatState>();
            foreach (RoomCombatState room in source.Rooms)
            {
                var units = new List<CombatUnit>();
                foreach (CombatUnit unit in room.Units)
                {
                    CombatUnit? actor = unit;
                    if (unit.Status(StatusId) != null)
                    {
                        CardUpgradeModifier? previous = unit.Modifiers!.Upgrades.SingleOrDefault(upgrade => upgrade.DataId == UpgradeId);
                        if (previous != null)
                            actor = ApplyUnit(room, source.Context, unit, previous, remove: true, out error);
                        if (actor == null) return null;
                        actor = ApplyUnit(room, source.Context, actor, AttackUpgrade(total), remove: false, out error);
                        if (actor == null) return null;
                    }
                    units.Add(actor);
                }
                rooms.Add(new RoomCombatState(room.RoomIndex, room.Deployment, units,
                    room.ExternalInteractions.Where(marker => !IsKnownMarker(marker))
                        .Concat(units.Where(unit => unit.Status(StatusId) != null).Select(Marker))
                        .Distinct().OrderBy(marker => marker, StringComparer.Ordinal).ToArray(),
                    room.Context, room.Preview, room.ActiveCompanionBossIds));
            }
            return EnchantmentWorldModel.Rebase(new TrainCombatState(rooms, source.Movement, source.EnemySlotsPerRoom, source.Context));
        }

        internal static bool IsKnownMarker(string marker) =>
            marker is Hunter + " room modifiers" or Infested + " room modifiers" or Heavy + " room modifiers" or Zealot + " room modifiers";

        private static CombatUnit? ApplyUnit(RoomCombatState room, CombatContext? context, CombatUnit unit, CardUpgradeModifier upgrade,
            bool remove, out string? error)
        {
            var local = new RoomCombatState(room.RoomIndex, false, new[] { unit }, Array.Empty<string>(), context);
            var callbacks = new List<RoomCombatModel.QueuedCharacterTrigger>();
            RoomCombatResult applied = UnitModifierModel.ApplyWithSettlement(local, unit.Id, upgrade,
                "TemporaryUntilUnitDeath", remove, null, 0, null, (state, actor, _) =>
                    new RoomCombatResult(state.WithUnits(new[] { actor }), RoomOutcome.Exchanged, 0, new List<CombatEvent>()),
                enqueueCallback: callbacks.Add, directApi: true, upgradeId: UpgradeId, allowFreshHivemind: true);
            if (!applied.Supported || callbacks.Count != 0 || applied.State!.Units.Count != 1 ||
                applied.Outcome != RoomOutcome.Exchanged)
            {
                error = applied.UnsupportedReason ?? "Hivemind produced an unobserved direct upgrade interaction.";
                return null;
            }
            error = null;
            return applied.State.Units[0];
        }

        internal static bool MatchesRoomMarker(string marker, CombatUnit unit) =>
            unit.Status(StatusId) is CombatStatus status && (IsOriginalOwner(unit, status) || IsFedPreviewRecipient(unit) ||
                IsFedSecondPreviewRecipient(unit) ||
                unit.AssetKey == Heavy && (NativeFedUpgrade(unit, 6, 13) || NativeFedUpgrade(unit, 9, 13) ||
                    IsThirdNewHeavy(unit)) ||
                unit.AssetKey == Zealot && (NativeFedUpgrade(unit, 3, 13) || NativeFedUpgrade(unit, 6, 13))) &&
            marker == Marker(unit);

        internal static string Marker(CombatUnit unit) => unit.AssetKey + " room modifiers";

        internal static bool IsObservedFeedUpgrade(RoomCombatState room, CombatUnit unit, CardUpgradeModifier upgrade, string? kind) =>
            kind == "OnEaten" && IsFeedUpgrade(upgrade) && (
                IsFirstCombat(room) && unit.AssetKey == Heavy && IsFeedRecipient(unit) && unit.RegisteredStatus(StatusId) == null ||
                IsSecondCombat(room) && IsFeedRecipient(unit) && (unit.AssetKey == Heavy && unit.Status(StatusId)?.Stacks == 3 ||
                    unit.AssetKey == Zealot && unit.RegisteredStatus(StatusId) == null) ||
                IsThirdPreview(room) && (room.RoomIndex == 0 && unit.AssetKey == Heavy &&
                    IsFeedRecipient(unit) && unit.RegisteredStatus(StatusId) == null ||
                    room.RoomIndex == 1 && unit.AssetKey == Zealot && NativeFedUpgrade(unit, 3, 13) ||
                    room.RoomIndex == 2 && unit.AssetKey == Heavy && NativeFedUpgrade(unit, 6, 13)));

        internal static bool IsObservedCombatUpgrade(RoomCombatState room, CardUpgradeModifier upgrade, string? kind) =>
            (IsFirstCombat(room) || IsSecondCombat(room)) &&
            ((kind == "OnEaten" && IsFeedUpgrade(upgrade)) ||
             (kind == "OnStatusEffectChanged" && upgrade.DataId == UpgradeId && upgrade.Stats.Damage == 0 &&
                upgrade.Stats.Health == 0 && upgrade.Statuses.Count == 0 && upgrade.Unique));

        private static bool IsFeedRecipient(CombatUnit unit) => unit.AssetKey is Heavy or Zealot &&
            unit.Team == CombatTeam.Enemy && unit.SpawnerCardId == 0 && unit.Health > 0 && unit.IsBoss != true &&
            unit.Modifiers is { IsClone: false };

        private static bool IsFeedUpgrade(CardUpgradeModifier upgrade) => upgrade.DataId == FeedUpgrade &&
            upgrade.Statuses is [var status] && status.Id == StatusId && status.Stacks == 3 && status.ParamInt == 1 &&
            upgrade.Stats.Damage == 0 && upgrade.Stats.Health == 0 && upgrade.ExternalInteractions.Count == 0;

        internal static bool IsFedPreviewRecipient(CombatUnit unit) => unit.AssetKey == Heavy && IsFeedRecipient(unit) &&
            unit.Status(StatusId)?.Stacks == 3 &&
            unit.Modifiers!.Upgrades.Count(IsFeedUpgrade) == 1 &&
            unit.Modifiers.Upgrades.Count(upgrade => upgrade.DataId == UpgradeId) <= 1;

        private static bool IsFedSecondPreviewRecipient(CombatUnit unit)
        {
            if (!IsFeedRecipient(unit) || unit.Status(StatusId) is not { } status) return false;
            IReadOnlyList<CardUpgradeModifier> upgrades = unit.Modifiers!.Upgrades;
            int feeds = upgrades.Count(IsFeedUpgrade);
            if (unit.AssetKey == Heavy)
                return status.Stacks is 3 or 6 && feeds == status.Stacks / 3 && upgrades.Count == feeds + 1 &&
                    upgrades.Count(upgrade => IsAttackUpgrade(upgrade, 6)) == 1;
            return status.Stacks == 3 && feeds == 1 && upgrades.Count is 1 or 2 &&
                upgrades.Count(upgrade => IsAttackUpgrade(upgrade, 0)) == upgrades.Count - 1;
        }

        private static bool IsAttackUpgrade(CardUpgradeModifier upgrade, int damage) =>
            upgrade.DataId == UpgradeId && upgrade.Stats.Damage == damage && upgrade.Stats.Health == 0 &&
            upgrade.Statuses.Count == 0 && upgrade.Unique;

        internal static bool NeedsFeedMarker(RoomCombatState room, CombatUnit unit) =>
            (IsFirstCombat(room) && IsFedPreviewRecipient(unit) ||
             IsSecondCombat(room) && unit.AssetKey == Zealot && IsFedSecondPreviewRecipient(unit) ||
             IsThirdPreview(room) && room.RoomIndex == 0 && IsThirdNewHeavy(unit)) &&
            !unit.Modifiers!.Upgrades.Any(upgrade => upgrade.DataId == UpgradeId);

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
                CombatUnit? applied = ApplyUnit(affected, source.Context, owner.Unit, upgrade, remove: false, out error);
                if (applied == null) return null;
                units[Array.FindIndex(units, unit => unit.Id == owner.Unit.Id)] = applied;
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
