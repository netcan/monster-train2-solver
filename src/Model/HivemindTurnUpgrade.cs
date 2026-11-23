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
        private const string HeavyT2 = "R2_Heavy_HealGorge_T2";
        private const string DeathHorde = "RAny_Horde_T1_DeathAddsHivemind_Ver2";
        private const string FeedUpgrade = "661fb64c-20d2-484c-8c30-309931b8148c";
        private const string FeedBuffUpgrade = "25bad7b2-5fc2-4f03-b735-39b1551275de";

        internal static bool HasOriginalFirstWavePair(IEnumerable<CombatUnit> units)
        {
            var owners = units.Where(unit => unit.RegisteredStatus(StatusId) != null ||
                unit.RegisteredStatus("hivemindkiller") != null).ToArray();
            return owners.Length == 2 && owners.Any(unit => unit.AssetKey == Hunter) && owners.Any(unit => unit.AssetKey == Infested) &&
                owners.All(unit => unit.Status(StatusId) is CombatStatus status && IsOriginalFreshOwner(unit, status) &&
                    unit.RegisteredStatus("hivemindkiller") == null);
        }

        internal static bool HasOriginalFirstWave(IEnumerable<CombatUnit> units)
            => HasOriginalFirstWavePair(units) || HasInfestedOpeningWave(units);

        internal static bool HasInfestedOpeningWave(IEnumerable<CombatUnit> units)
        {
            CombatUnit[] owners = units.Where(unit => unit.RegisteredStatus(StatusId) != null ||
                unit.RegisteredStatus("hivemindkiller") != null).ToArray();
            return owners.Length == 3 &&
                owners.Count(unit => unit.AssetKey == HeavyT2) == 1 && owners.Count(unit => unit.AssetKey == DeathHorde) == 2 &&
                owners.All(unit => unit.Status(StatusId) is CombatStatus status && IsInfestedOpeningOwner(unit, status) &&
                    unit.RegisteredStatus("hivemindkiller") == null);
        }

        internal static bool IsOriginalFreshOwner(CombatUnit unit, CombatStatus status) =>
            IsOriginalOwner(unit, status) && unit.Modifiers!.Upgrades.Count == 0;

        private static bool IsOriginalOwner(CombatUnit unit, CombatStatus status) =>
            IsOriginalStatus(unit, status) && (unit.AssetKey == Hunter && unit.Triggers.Count == 0 ||
                unit.AssetKey == Infested && unit.Triggers.Count > 0 && unit.Triggers.All(trigger => trigger.Kind == "OnEaten"));

        private static bool IsOriginalStatus(CombatUnit unit, CombatStatus status) =>
            unit.Team == CombatTeam.Enemy && unit.SpawnerCardId == 0 && unit.IsBoss != true && unit.Health > 0 &&
            unit.Modifiers is { IsClone: false } &&
            status.Id == StatusId && status.Stacks == 1 && status.ParamInt == 1 &&
            status.DisplayCategory == "Positive" && status.Stackable == true && status.Hidden == false &&
            status.Propagatable == false && !status.RemoveWhenTriggered && !status.RemoveStackAtEnd &&
            !status.RemoveAllAtEnd && !status.RemoveAfterPostCombat && !status.SkipDuringDeployment &&
            !status.RemoveDuringDeployment && !status.RemoveWhenTriggeredAfterCardPlayed &&
            !status.RemoveAtEndOfTurnIfTriggered && !status.MarkedForRemovalAfterCardPlayed;

        internal static bool IsInfestedOpeningOwner(CombatUnit unit, CombatStatus status) =>
            IsOriginalStatus(unit, status) && unit.Modifiers!.Upgrades.Count == 0 && IsInfestedOpeningUnit(unit);

        private static bool IsInfestedOpeningUnit(CombatUnit unit, int upgradeDamage = 0) =>
            unit is { AssetKey: HeavyT2, MaxHealth: 250, Size: 2, Triggers: [{ Kind: "OnFeed", Effects:
                 [{ Type: "CardEffectAddStatusEffect" }, { Type: "CardEffectAddStatusEffect" }, { Type: "CardEffectHeal" }] }] } &&
             unit.BaseAttack == 15 + upgradeDamage ||
             unit is { AssetKey: DeathHorde, MaxHealth: 3, Size: 1, Triggers: [var death] } &&
             unit.BaseAttack == 2 + upgradeDamage &&
             IsOpeningDeathGrant(death);

        internal static bool IsInfestedOpeningPreviewOwner(CombatUnit unit) =>
            unit.Status(StatusId) is CombatStatus status && IsOriginalStatus(unit, status) &&
            unit.Modifiers!.Upgrades is [var upgrade] && IsAttackUpgrade(upgrade, 3) && IsInfestedOpeningUnit(unit, upgrade.Stats.Damage);

        internal static bool IsInfestedWavePreview(RoomCombatState room) =>
            room is { Preview: true, RoomIndex: 0 } &&
            room.Context?.QueryFrame is { Turn: 1, MoonPhase: 2, MoonPhaseTrackingActive: false } &&
            OuterBossDefinitions.HasLifemotherInfestedOwner(room.Context) &&
            (room.Units.Count(unit => unit.AssetKey == DeathHorde && IsInfestedOpeningPreviewOwner(unit)) == 2 &&
                room.Units.Count(IsInfestedPreviewFeedRecipient) == 1 ||
             room.Units is [var retained] && IsInfestedPreviewFeedRecipient(retained));

        private static bool IsInfestedStatusUpgrade(CardUpgradeModifier upgrade) =>
            upgrade is { DataId: "2ffda46d-a18b-4659-a709-0c758a688337", AssetKey: "R2_Support_FeedTitanskinUpgrade_T2",
                Unique: false, RemoveOnDiscard: false, ExcludeFromClones: false, ExternalInteractions.Count: 0,
                Stats: { Damage: 0, Health: 0, Cost: 0, Heal: 0, Size: 0, XCost: 0, EquipmentLimit: 0, UpgradeSlotCount: 0 },
                Statuses: [{ Id: "burst", Stacks: 1, ParamInt: 2 }, { Id: StatusId, Stacks: 6, ParamInt: 1 }] };

        private static bool IsInfestedPreviewFeedRecipient(CombatUnit unit) =>
            unit.AssetKey == HeavyT2 && unit.Status(StatusId) is { Stacks: var stacks } && IsInfestedOpeningUnit(unit, 3) &&
            unit.Team == CombatTeam.Enemy && unit.SpawnerCardId == 0 && unit.Health > 0 && unit.IsBoss != true &&
            unit.Modifiers is { IsClone: false } modifiers && (stacks == 1 && modifiers.Upgrades is [var opening] &&
                IsAttackUpgrade(opening, 3) || stacks == 7 && modifiers.Upgrades is [var active, var feeding] &&
                IsAttackUpgrade(active, 3) && IsInfestedStatusUpgrade(feeding));

        internal static bool IsInfestedWavePreviewOwner(CombatUnit unit) =>
            IsInfestedOpeningPreviewOwner(unit) || IsInfestedPreviewFeedRecipient(unit);

        private static bool IsOpeningDeathGrant(CombatTrigger trigger) =>
            trigger is { Kind: "OnDeath", Once: false, IgnoreSilence: false, FireCount: 1, TriggerAtThreshold: 0,
                Conditions: null, Effects: [{ Type: "CardEffectAddStatusEffect", Action: { Type: "AddStatus", Target: "Room",
                    AllowEnemy: true, AllowPlayer: false, Value: 0, Range: null, Statuses: [{ Id: StatusId, Stacks: 1 }] } }] };

        internal static bool HasDormantOpeningDeathGrant(CombatUnit unit, CombatTrigger trigger) =>
            unit.AssetKey == DeathHorde && IsOpeningDeathGrant(trigger) &&
            unit.Status(StatusId) is CombatStatus status && IsInfestedOpeningOwner(unit, status);

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

        internal static bool IsThirdCombat(RoomCombatState room)
        {
            if (room.Context?.QueryFrame?.Turn != 3 || room.RoomIndex is < 0 or > 2 ||
                room.Units.Any(unit => unit.RegisteredStatus("hivemindkiller") != null))
                return false;
            CombatUnit[] carriers = room.Units.Where(unit => unit.RegisteredStatus(StatusId) != null).ToArray();
            int food = carriers.Count(unit => unit.AssetKey == Infested && HasAttackUpgrade(unit, 13));
            return food <= 1 && (food == 1 || room.RoomIndex == 2 && carriers.Any(unit => NativeFedUpgrade(unit, 9, 13))) &&
                (room.RoomIndex == 0 && carriers.Length <= 2 &&
                    carriers.All(unit => unit.AssetKey == Infested && HasAttackUpgrade(unit, 13) || IsThirdNewHeavy(unit)) ||
                 room.RoomIndex == 1 && carriers.Length == 2 &&
                    carriers.Count(unit => unit.AssetKey == Zealot &&
                        (NativeFedUpgrade(unit, 3, 13) || NativeFedUpgrade(unit, 6, 13))) == 1 ||
                 room.RoomIndex == 2 && carriers.Count(unit => unit.AssetKey == Hunter && HasAttackUpgrade(unit, 13)) == 1 &&
                    carriers.Length == food + 2 && carriers.Count(unit => unit.AssetKey == Heavy &&
                        (NativeFedUpgrade(unit, 6, 13) || NativeFedUpgrade(unit, 9, 13))) == 1);
        }

        private static bool IsThirdNewHeavy(CombatUnit unit) => unit.AssetKey == Heavy && IsFeedRecipient(unit) &&
            unit.Status(StatusId)?.Stacks == 3 && unit.Modifiers!.Upgrades.Count(IsFeedUpgrade) == 1 &&
            unit.Modifiers.Upgrades.Count(upgrade => upgrade.DataId == UpgradeId) ==
                unit.Modifiers.Upgrades.Count(upgrade => IsAttackUpgrade(upgrade, 0)) &&
            unit.Modifiers.Upgrades.Count(upgrade => IsAttackUpgrade(upgrade, 0)) <= 1;

        internal static bool IsObservedThirdFeedDeath(RoomCombatState room, int targetId, int? actorId) =>
            targetId == actorId && room.RoomIndex == 2 && IsThirdCombat(room) &&
            room.Units.Any(unit => unit.Id == targetId && unit.AssetKey == Infested && HasAttackUpgrade(unit, 13)) &&
            NoCapturedKiller(room);

        internal static bool IsObservedPyreCombat(RoomCombatState room)
        {
            if (room.Context?.QueryFrame?.Turn != 3 || room.RoomIndex != 3 || !NoCapturedKiller(room) ||
                room.Units.Count(unit => unit.IsPyre && unit.Health > 0) != 1 ||
                room.Units.Count(unit => unit.AssetKey == Heavy && NativeFedUpgrade(unit, 9, 13)) != 1)
                return false;
            return room.Units.Count == 2 ||
                room.Units.Count == 3 && room.Units.Count(unit =>
                    unit.AssetKey == Hunter && HasAttackUpgrade(unit, 13)) == 1;
        }

        internal static bool IsObservedPyreHunterDeath(RoomCombatState room, int targetId, bool pyreAttacker) =>
            pyreAttacker && IsObservedPyreCombat(room) && room.Units.Any(unit => unit.Id == targetId && unit.AssetKey == Hunter);

        private static bool NoCapturedKiller(RoomCombatState room) => room.Context?.Enchantments is EnchantmentWorld world &&
            world.Rooms.SelectMany(floor => floor.Units).Concat(world.RetainedUnits.Select(actor => actor.Unit)).All(unit =>
                unit.RegisteredStatus("hivemindkiller") == null && unit.Modifiers?.Upgrades.Any(upgrade =>
                    upgrade.DataId == "RoomStateHivemindKillerModifierUpgrade") != true);

        internal static bool PublishesAtCombat(RoomCombatState room) =>
            !room.Preview && (IsFirstCombat(room) || IsSecondCombat(room) || IsThirdCombat(room) || IsObservedPyreCombat(room));

        internal static bool PublishesAtAscension(TrainCombatState train) =>
            train.Context?.QueryFrame?.Turn is >= 1 and <= 3 && train.Rooms.All(room => !room.Preview) &&
            train.Rooms.Any(room => room.ExternalInteractions.Any(IsKnownMarker)) &&
            train.Rooms.SelectMany(room => room.Units).All(unit => unit.RegisteredStatus("hivemindkiller") == null &&
                (unit.Status(StatusId) == null || MatchesRoomMarker(Marker(unit), unit)));

        internal static RoomCombatState PublishRoomMarkers(RoomCombatState room)
        {
            string[] markers = room.ExternalInteractions.Where(marker => !IsKnownMarker(marker)).Concat(
                room.Units.Where(unit => MatchesRoomMarker(Marker(unit), unit))
                    .Select(Marker)).Distinct().OrderBy(marker => marker, StringComparer.Ordinal).ToArray();
            return room.ExternalInteractions.SequenceEqual(markers) ? room :
                new RoomCombatState(room.RoomIndex, room.Deployment, room.Units, markers, room.Context, room.Preview,
                    room.ActiveCompanionBossIds);
        }

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

        internal static bool HasInfestedDraw(TrainCombatState source)
        {
            if (source.Context?.QueryFrame is not { Turn: 2 or 3, MoonPhaseTrackingActive: false } frame ||
                frame.MoonPhase != frame.Turn - 1 ||
                !OuterBossDefinitions.HasLifemotherInfestedOwner(source.Context) || source.Rooms.Any(room => room.Preview))
                return false;
            CombatUnit[] units = source.Rooms.SelectMany(room => room.Units).ToArray();
            var owners = units.Where(unit => unit.RegisteredStatus(StatusId) != null).ToArray();
            return owners.Length == (frame.Turn == 2 ? 7 : 9) &&
                owners.Count(unit => unit.AssetKey == OuterBossDefinitions.LifemotherInfested) == 1 &&
                owners.Count(unit => unit.AssetKey == HeavyT2) == frame.Turn &&
                owners.Count(unit => unit.AssetKey == DeathHorde) == (frame.Turn == 2 ? 4 : 5) &&
                owners.Count(unit => unit.Modifiers?.Upgrades.Count == 0) == (frame.Turn == 2 ? 3 : 2) &&
                units.All(unit => unit.RegisteredStatus("hivemindkiller") == null) &&
                owners.All(unit => IsInfestedDrawOwner(unit, frame.Turn.Value, afterRemoval: false)) &&
                owners.Sum(unit => unit.Status(StatusId)?.Stacks) == (frame.Turn == 2 ? 14 : 32);
        }

        internal static bool IsInfestedDrawOwner(CombatUnit unit, int turn, bool afterRemoval)
        {
            if (turn is not (2 or 3) || unit.Status(StatusId) is not CombatStatus status ||
                unit.Modifiers is not { IsClone: false } modifiers || unit.RegisteredStatus("hivemindkiller") != null ||
                unit.Team != CombatTeam.Enemy || unit.SpawnerCardId != 0 || unit.Health <= 0)
                return false;
            int previousDamage = turn == 2 ? 3 : 14;
            CardUpgradeModifier[] previous = modifiers.Upgrades.Where(upgrade => upgrade.DataId == UpgradeId).ToArray();
            int oldDamage = previous.Length == 0 ? 0 : previous[0].Stats.Damage;
            if (previous.Length > 1 || previous.Length == 1 && !IsAttackUpgrade(previous[0], oldDamage))
                return false;
            if (unit.AssetKey == OuterBossDefinitions.LifemotherInfested)
                return status.Stacks == (turn == 2 ? 2 : 6) &&
                    (modifiers.Upgrades.Count == 1 && oldDamage == (turn == 2 ? 0 : 14) ||
                     afterRemoval && modifiers.Upgrades.Count == 0) &&
                    unit.MaxHealth == 9000 && unit.BaseAttack == 40 + oldDamage &&
                    unit.Triggers is [{ Kind: "OnAnyUnitDeathOnFloor" }, { Kind: "OnTeamTurnBegin" }];
            if (IsInfestedOpeningOwner(unit, status)) return true;
            if ((oldDamage != previousDamage && !(afterRemoval && previous.Length == 0)) ||
                unit.IsBoss == true || !IsInfestedOpeningUnit(unit, oldDamage))
                return false;
            CardUpgradeModifier[] feedUpgrades = modifiers.Upgrades.Where(upgrade => upgrade.DataId != UpgradeId).ToArray();
            if (unit.AssetKey == DeathHorde) return IsOriginalStatus(unit, status) && feedUpgrades.Length == 0;
            int feeds = feedUpgrades.Count(IsInfestedStatusUpgrade);
            return status.Stacks == 1 + 6 * feeds && feeds <= turn - 1 &&
                feedUpgrades.All(upgrade => IsInfestedStatusUpgrade(upgrade) || upgrade.DataId == FeedBuffUpgrade) &&
                feedUpgrades.Count(upgrade => upgrade.DataId == FeedBuffUpgrade) <= feeds;
        }

        internal static TrainCombatState? ApplyInfestedDraw(TrainCombatState source, out string? error)
        {
            if (HasInfestedDraw(source)) return ApplyObservedRefresh(source, out error);
            error = "Hivemind requires the observed original Infested wave and carried owners.";
            return null;
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
            unit.Status(StatusId) is CombatStatus status && (IsOriginalOwner(unit, status) || IsInfestedOpeningOwner(unit, status) || IsFedPreviewRecipient(unit) ||
                IsFedSecondPreviewRecipient(unit) ||
                unit.AssetKey == Heavy && (NativeFedUpgrade(unit, 6, 13) || NativeFedUpgrade(unit, 9, 13) ||
                    IsThirdNewHeavy(unit)) ||
                unit.AssetKey == Zealot && (NativeFedUpgrade(unit, 3, 13) || NativeFedUpgrade(unit, 6, 13))) &&
            marker == Marker(unit);

        internal static string Marker(CombatUnit unit) => unit.AssetKey + " room modifiers";

        private static bool IsObservedInfestedFeedUpgrade(RoomCombatState room, CombatUnit unit, CardUpgradeModifier upgrade, string? kind) =>
            kind == "OnEaten" && IsInfestedWavePreview(room) && IsInfestedPreviewFeedRecipient(unit) && IsInfestedStatusUpgrade(upgrade);

        internal static bool IsObservedFeedUpgrade(RoomCombatState room, CombatUnit unit, CardUpgradeModifier upgrade, string? kind) =>
            IsObservedInfestedFeedUpgrade(room, unit, upgrade, kind) || kind == "OnEaten" && IsFeedUpgrade(upgrade) && (
                IsFirstCombat(room) && unit.AssetKey == Heavy && IsFeedRecipient(unit) && unit.RegisteredStatus(StatusId) == null ||
                IsSecondCombat(room) && IsFeedRecipient(unit) && (unit.AssetKey == Heavy && unit.Status(StatusId)?.Stacks == 3 ||
                    unit.AssetKey == Zealot && unit.RegisteredStatus(StatusId) == null) ||
                IsThirdCombat(room) && (room.RoomIndex == 0 && unit.AssetKey == Heavy &&
                    IsFeedRecipient(unit) && unit.RegisteredStatus(StatusId) == null ||
                    room.RoomIndex == 1 && unit.AssetKey == Zealot && NativeFedUpgrade(unit, 3, 13) ||
                    room.RoomIndex == 2 && unit.AssetKey == Heavy && NativeFedUpgrade(unit, 6, 13)));

        internal static bool IsObservedCombatUpgrade(RoomCombatState room, CardUpgradeModifier upgrade, string? kind) =>
            (IsFirstCombat(room) || IsSecondCombat(room) || IsThirdCombat(room)) &&
            ((kind == "OnEaten" && (IsFeedUpgrade(upgrade) || IsThirdCombat(room) &&
                room.RoomIndex == 0 && upgrade.DataId == FeedBuffUpgrade &&
                upgrade.Statuses.Select(status => (status.Id, status.Stacks)).SequenceEqual(
                    new[] { ("damage shield", 1), ("spell shield", 1), ("lifesteal", 1) }))) ||
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
             IsThirdCombat(room) && room.RoomIndex == 0 && IsThirdNewHeavy(unit)) &&
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
            if (!HasOriginalFirstWave(owners.Select(item => item.Unit)) || owners.Select(item => item.Room.RoomIndex).Distinct().Count() != 1 ||
                owners.Any(item => item.Room.Preview) ||
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
