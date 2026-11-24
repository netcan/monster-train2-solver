using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class HivemindTurnUpgrade
    {
        internal const string StatusId = "hivemind";
        internal const string KillerStatusId = "hivemindkiller";
        private const string UpgradeId = "RoomStateHivemindModifierUpgrade";
        private const string KillerUpgradeId = "RoomStateHivemindKillerModifierUpgrade";
        private const string SproutCarrierCardId = "b9788a77-50da-4654-acfa-960c23649984";
        private const string Hunter = "RAny_Attacker_Hunter_Hivemind_Ver2";
        private const string Infested = "R2_Support_FeedInfested";
        private const string Heavy = "R2_Heavy_HealGorge";
        private const string Zealot = "R2_Attacker_Zealot_Ver2";
        private const string HeavyT2 = "R2_Heavy_HealGorge_T2";
        private const string DeathHorde = "RAny_Horde_T1_DeathAddsHivemind_Ver2";
        private const string FanningTheFlame = "3f974014-72f2-407e-a8c1-28ba5b273cfa";
        private const string SlamAbility = "3366ff9e-1beb-4246-a025-c448b47dacae";
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

        internal static bool HasScaledWave(EnemySpawnState source, IReadOnlyList<EnemyDefinition> definitions) =>
            source.Turn is >= 1 and <= 4 && source.Phase == source.Turn && IsScaledCampaign(source.Train.Context) &&
            definitions.Any(entry => entry.Unit.Status(StatusId) != null) &&
            definitions.All(entry => entry.Unit.RegisteredStatus("hivemindkiller") == null &&
                (entry.Unit.RegisteredStatus(StatusId) == null || entry.Unit.Status(StatusId) is CombatStatus status &&
                    IsOriginalFreshOwner(entry.Unit, status))) &&
            source.Train.Rooms.All(room => room.Units.All(unit =>
                (unit.RegisteredStatus(KillerStatusId) == null || source.Turn == 1 && IsObservedSproutCarrier(room, unit)) &&
                (unit.RegisteredStatus(StatusId) == null || IsScaledFirstWaveOwner(unit, source.Train.Context) ||
                    IsScaledFeedRecipient(unit, source.Turn))));

        internal static bool IsSproutOwner(CombatRelicState relic) => SproutTier(relic) > 0;

        private static int SproutTier(CombatRelicState relic) =>
            relic is { IsCovenant: false, DisallowedInPlacementPhase: false, EffectTypes: ["RelicEffectCardUpgrade"] }
                ? (relic.DataId, relic.AssetKey) switch
                {
                    ("89f86b55-f867-42d4-9128-0ebc6c82bcaf", "SproutT1") => 1,
                    ("f6bfdfc3-b8ff-466a-8e5e-7a080eb9c9be", "SproutT2") => 2,
                    ("da7a1058-271b-4436-a567-102c7566c991", "SproutT3") => 3,
                    _ => 0
                } : 0;

        private static bool IsKillerStatus(CombatStatus status) => status is
        {
            Id: KillerStatusId, Stacks: 1, ParamInt: 0, DisplayCategory: "Persistent", Hidden: true,
            Stackable: false, Propagatable: false, ExcludeMonsterPropagation: true, ExcludeHeroPropagation: false,
            RemoveWhenTriggered: false, RemoveStackAtEnd: false, RemoveAllAtEnd: false, RemoveAfterPostCombat: false,
            SkipDuringDeployment: false, RemoveDuringDeployment: false, RemoveWhenTriggeredAfterCardPlayed: false,
            RemoveAtEndOfTurnIfTriggered: false, MarkedForRemovalAfterCardPlayed: false
        };

        internal static bool IsObservedSproutCarrier(RoomCombatState room, CombatUnit unit, bool replacingUpgrade = false)
        {
            CombatContext? context = room.Context;
            if (room.RoomIndex is not (0 or 2) || context?.QueryFrame?.Turn is not (1 or 2) || !IsScaledCampaign(context) ||
                context.Relics!.Where(IsSproutOwner).ToArray() is not [var relic] || unit is not
                    { AssetKey: "ParrafinEnforcer", Team: CombatTeam.Player, Health: > 0, SpawnerCardId: > 0, Modifiers.IsClone: false } ||
                unit.RegisteredStatus(KillerStatusId) is not CombatStatus killer || !IsKillerStatus(killer) ||
                context.FindCard(unit.SpawnerCardId) is not { DataId: SproutCarrierCardId } card)
                return false;

            int tier = SproutTier(relic);
            if (tier > 1 && unit.Triggers.Count(trigger => IsSproutDeathTrigger(trigger, tier)) != 1) return false;
            string binding = tier switch
            {
                1 => "7bdad45c-2981-452c-ac06-3aa5137f8ce4",
                2 => "581706b5-1402-4f66-8324-9b76601ce360",
                _ => "52c9e5c1-afc3-4096-94bc-a784ebd905da"
            };
            return room.RoomIndex == (tier == 1 ? 2 : 0) && card.Permanent.Upgrades.Count(upgrade => upgrade is
            {
                Unique: false, ExcludeFromClones: true, AbilityUpgrade: null, ExternalInteractions.Count: 0, Statuses: [var status]
            } && upgrade.DataId == binding && upgrade.AssetKey == relic.AssetKey + "_Upgrade" && IsKillerStatus(status) &&
                (tier == 1 ? upgrade.TriggerUpgrades == null : upgrade.TriggerUpgrades is [var trigger] && IsSproutDeathTrigger(trigger, tier))) == 1 &&
                (TryKillerUpgrade(unit, out _) || replacingUpgrade && context.QueryFrame!.Turn == 2 && room.Units.Count == 1 &&
                    room.ExternalInteractions.Count == 0 && unit.Modifiers.Upgrades.All(upgrade => upgrade.DataId != KillerUpgradeId));
        }

        private static bool IsSproutDeathTrigger(CombatTrigger trigger, int tier)
        {
            string upgradeId = tier == 2 ? "110b42fe-b02e-45be-9782-96140d27a232" : "60fd575c-03f5-4885-aa6c-b2a7e6524d43";
            return trigger is
            {
                Kind: "OnAnyHeroDeathOnFloor", Once: false, IgnoreSilence: false, FireCount: 1,
                Conditions: { RequiredStatuses.Count: 0, RequiredDyingStatuses: [StatusId] },
                Effects: [{ Type: "CardEffectAddCardUpgradeToUnits", UnitUpgrade: { Type: "UnitUpgrade", Target: "Self",
                    Lifetime: "Permanent", ScaleUpgrade: true, Range: null, Upgrade: { Statuses.Count: 0,
                        TriggerUpgrades: null, AbilityUpgrade: null, ExternalInteractions.Count: 0,
                        Stats: { Health: 0, Cost: 0, Heal: 0, Size: 0, XCost: 0, EquipmentLimit: 0, UpgradeSlotCount: 0 } } } action }]
            } && action.Upgrade.DataId == upgradeId && action.Upgrade.Stats.Damage == (tier == 2 ? 2 : 4);
        }

        private static bool TryKillerUpgrade(CombatUnit unit, out CardUpgradeModifier? native)
        {
            native = null;
            if (unit.Modifiers?.Upgrades.Where(upgrade => upgrade.DataId == KillerUpgradeId).ToArray() is not [var upgrade] ||
                upgrade is not
                {
                    AssetKey: "", Unique: true, ExcludeFromClones: false, RemoveOnDiscard: false,
                    Stats: { Damage: 0 or 6 or 8, Health: 0, Cost: 0, Heal: 0, Size: 0, XCost: 0, EquipmentLimit: 0, UpgradeSlotCount: 0 },
                    Statuses.Count: 0, TriggerUpgrades: null, AbilityUpgrade: null, ExternalInteractions.Count: 0,
                    Lifecycle.Values.DataId: KillerUpgradeId
                }) return false;
            native = upgrade;
            return true;
        }

        private static bool IsScaledCampaign(CombatContext? context) => context?.Relics?.Any(relic =>
            relic is { DataId: "f2c1e86b-ac5c-474f-8c3a-ebee76981fb8", AssetKey: "SoulSavior_DifficultyTier2" } &&
            relic.EffectTypes.Count == 12 && relic.EffectTypes.Take(11).All(type => type == RelicPercentageStatModel.Effect) &&
            relic.EffectTypes[11] == RelicSpawnStatusScalingModel.Effect) == true &&
            context.Relics.Any(relic => relic is { DataId: "36dc3689-be84-4a01-8bb1-e69c38d78208",
                AssetKey: "SoulSavior_Region1Complete_3", EffectTypes: [RelicModel.AddTrigger] });

        internal static bool IsScaledFirstWaveOwner(CombatUnit unit, CombatContext? context) =>
            IsScaledCampaign(context) && unit.Status(StatusId) is { Stacks: 2 } status &&
            IsOriginalStatus(unit, status.WithStacks(1)) && unit.RegisteredStatus("hivemindkiller") == null &&
            (unit.AssetKey == Hunter && unit.MaxHealth == 4 && unit.Triggers is [{ Kind: "OnKill" }] ||
             unit.AssetKey == Infested && unit.MaxHealth == 38 && unit.Triggers is [{ Kind: "OnKill" }, { Kind: "OnEaten" }]) &&
            (unit.Modifiers!.Upgrades.Count == 0 || unit.Modifiers.Upgrades is [var upgrade] &&
                (IsAttackUpgrade(upgrade, 0) || IsAttackUpgrade(upgrade, 8) || IsAttackUpgrade(upgrade, 18) ||
                 context?.QueryFrame?.Turn is 4 or 5 && IsAttackUpgrade(upgrade, 17) ||
                 context?.QueryFrame?.Turn == 5 && IsAttackUpgrade(upgrade, 26)));

        internal static bool IsScaledFirstWaveBirth(CombatUnit unit, CombatContext? context) =>
            context?.QueryFrame?.Turn is >= 1 and <= 4 && IsScaledCampaign(context) && unit.Status(StatusId) is { Stacks: 1 or 2 } status &&
            IsOriginalStatus(unit, status.WithStacks(1)) && unit.RegisteredStatus("hivemindkiller") == null &&
            (unit.AssetKey == Hunter && unit.MaxHealth is >= 3 and <= 4 ||
             unit.AssetKey == Infested && unit.MaxHealth is >= 30 and <= 38) &&
            (unit.Modifiers!.Upgrades.Count == 0 || IsScaledFirstWaveOwner(unit, context));

        internal static bool IsObservedScaledWaveDeath(RoomCombatState room, CombatUnit unit, int sourceCardId) =>
            (sourceCardId == 0 && unit.RegisteredStatus("hivemindkiller") == null &&
                room.Units.Any(owner => owner.Id == unit.Id && IsScaledRoomCombatDeath(room, owner))) ||
            (room is { RoomIndex: 2, Preview: false } && room.Context?.QueryFrame?.Turn == 4 &&
                room.Context.FindCard(sourceCardId)?.DataId == "8de281af-ab9a-4d3d-ac5f-89cd43ff6c84" &&
                IsScaledRoomCombatDeath(room, unit)) ||
            (room is { RoomIndex: 0, Preview: false } && room.Context?.QueryFrame?.Turn == 2 &&
                room.Context.FindCard(sourceCardId)?.DataId == "87e45427-921e-45d3-b4c4-9c156a83cedf" &&
                unit.AssetKey == Hunter && IsScaledFirstWaveOwner(unit, room.Context) &&
                unit.Modifiers!.Upgrades is [var upgrade] && IsAttackUpgrade(upgrade, 8) &&
                room.Units.Any(survivor => survivor.Id != unit.Id && survivor.AssetKey == Hunter &&
                    IsScaledFirstWaveOwner(survivor, room.Context)));

        internal static bool IsScaledRoomCombatDeath(RoomCombatState room, CombatUnit unit) =>
            room.ExternalInteractions.Contains(Marker(unit)) &&
            (room.RoomIndex == 1 && room.Context?.QueryFrame?.Turn == 3 &&
                (unit.AssetKey == Hunter && IsScaledFirstWaveOwner(unit, room.Context) ||
                    IsScaledCarriedFeedOwner(unit, room.Context) && unit.Status(StatusId)?.Stacks == 6) &&
                unit.Modifiers!.Upgrades.Any(upgrade => IsAttackUpgrade(upgrade, 18)) ||
             room.RoomIndex == 2 && room.Context?.QueryFrame?.Turn == 4 && unit.AssetKey == Infested &&
                IsScaledFirstWaveOwner(unit, room.Context) &&
                unit.Modifiers!.Upgrades is [var upgrade] && IsAttackUpgrade(upgrade, 17));

        internal static bool IsObservedScaledWaveInteraction(TrainCombatState train, string interaction)
        {
            if (interaction is not (Infested + " room modifiers" or Hunter + " room modifiers" or
                Infested + " room modifiers; " + Hunter + " room modifiers") ||
                train.Context?.QueryFrame?.Turn is not (>= 2 and <= 4))
                return false;

            return train.Rooms.Any(room => room.RoomIndex == 0 && !room.Preview &&
                string.Join("; ", room.ExternalInteractions) == interaction &&
                room.ExternalInteractions.All(marker => room.Units.Any(unit => Marker(unit) == marker &&
                    IsScaledFirstWaveOwner(unit, train.Context))));
        }

        private static bool ScaledFeedRoom(RoomCombatState room) => IsScaledCampaign(room.Context) &&
            (room.RoomIndex == 0 && room.Context?.QueryFrame?.Turn == 2 ||
             (room.RoomIndex == 0 || room is { RoomIndex: 1, Preview: true }) && room.Context?.QueryFrame?.Turn == 3 ||
             room.RoomIndex is 0 or 1 && room.Context?.QueryFrame?.Turn == 4 ||
             room.RoomIndex == 1 && room.Context?.QueryFrame?.Turn == 5);

        internal static bool IsScaledFirstFeedRecipient(RoomCombatState room, CombatUnit unit) =>
            ScaledFeedRoom(room) && IsScaledFeedRecipient(unit, room.Context?.QueryFrame?.Turn);

        internal static bool IsScaledPreviewFeedRecipient(RoomCombatState room, CombatUnit unit)
        {
            bool secondWave = room is { RoomIndex: 1, Preview: true } && room.Context?.QueryFrame?.Turn == 3;
            bool sproutFirstWave = room is { RoomIndex: 0, Preview: true } && room.Context?.QueryFrame?.Turn == 2 &&
                room.Context.Relics?.Any(relic => SproutTier(relic) is 2 or 3) == true;
            if (!(secondWave || sproutFirstWave) || !IsScaledCampaign(room.Context) || unit is not { Team: CombatTeam.Enemy, SpawnerCardId: 0, IsBoss: false,
                    Modifiers: { IsClone: false } modifiers } || unit.RegisteredStatus("hivemindkiller") != null)
                return false;
            int feeds = modifiers.Upgrades.Count(IsFeedUpgrade);
            if (feeds > 2) return false;
            CombatStatus? status = unit.Status(StatusId);
            if (unit is { AssetKey: Hunter, Health: > 0, MaxHealth: 4, Triggers: [{ Kind: "OnKill" }] })
                return status != null && IsOriginalStatus(unit, status.WithStacks(1)) && status.Stacks == 2 + 3 * feeds &&
                    modifiers.Upgrades.Count == feeds + 1 && modifiers.Upgrades.Count(upgrade => IsAttackUpgrade(upgrade, sproutFirstWave ? 8 : 18)) == 1;
            return secondWave && unit is { AssetKey: "TreasureBagLvl1Gold", Health: 1, MaxHealth: 1, BaseAttack: 0,
                    Triggers: [{ Kind: "OnKill" }, { Kind: "OnDeath" }, { Kind: "PostCombat" }] } &&
                modifiers.Upgrades.Count == feeds && (feeds == 0 ? status == null :
                    status != null && status.Stacks == 3 * feeds && IsOriginalStatus(unit, status.WithStacks(1)));
        }

        internal static bool IsScaledCarriedFeedOwner(CombatUnit unit, CombatContext? context) =>
            context?.QueryFrame?.Turn is >= 2 and <= 5 && IsScaledCampaign(context) &&
            IsScaledFeedRecipient(unit, context.QueryFrame.Turn);

        private static bool IsScaledFeedRecipient(CombatUnit unit, int? observedTurn = null) =>
            unit is { AssetKey: Heavy, MaxHealth: 75 } &&
            IsFeedRecipient(unit) && unit.RegisteredStatus("hivemindkiller") == null &&
            unit.Status(StatusId) is { Stacks: 3 or 6 or 9 or 12, ParamInt: 1 } status &&
            unit.Modifiers!.Upgrades.Count(IsFeedUpgrade) == status.Stacks / 3 &&
            unit.Modifiers.Upgrades.Count(upgrade => IsAttackUpgrade(upgrade, unit.BaseAttack - 12)) <= 1 &&
            unit.Modifiers.Upgrades.All(upgrade => IsFeedUpgrade(upgrade) ||
                (upgrade.Stats.Damage is 0 or 18 || observedTurn is 4 or 5 && upgrade.Stats.Damage == 17 ||
                    observedTurn == 5 && upgrade.Stats.Damage == 26) &&
                IsAttackUpgrade(upgrade, unit.BaseAttack - 12));

        internal static CombatUnit? RegisterScaledWaveOwner(RoomCombatState room, CombatUnit unit, out string? error)
        {
            if (IsScaledFirstWaveOwner(unit, room.Context) && unit.Modifiers!.Upgrades.Count == 0)
                return ApplyUnit(room, room.Context, unit, AttackUpgrade(0), remove: false, out error);
            error = "Missing observed native Hivemind wave birth.";
            return null;
        }

        internal static bool HasScaledDraw(TrainCombatState source)
        {
            if (source.Context?.QueryFrame?.Turn is not (>= 2 and <= 5) || !IsScaledCampaign(source.Context)) return false;
            CombatUnit[] units = source.Rooms.SelectMany(room => room.Units).ToArray();
            return units.Any(unit => IsScaledFirstWaveOwner(unit, source.Context) &&
                    unit.Modifiers!.Upgrades is [var upgrade] && IsAttackUpgrade(upgrade, 0)) &&
                source.Rooms.All(room => room.Units.All(unit =>
                    (unit.RegisteredStatus(KillerStatusId) == null || source.Context.QueryFrame.Turn == 2 &&
                        IsObservedSproutCarrier(room, unit)) &&
                    (unit.RegisteredStatus(StatusId) == null || IsScaledFirstWaveOwner(unit, source.Context) ||
                        IsScaledFeedRecipient(unit, source.Context.QueryFrame.Turn))));
        }

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

        internal static bool IsInfestedOpeningPreviewOwner(CombatUnit unit, CombatContext? context)
        {
            unit = RoomAttachmentUnitModel.OriginalIdentityWithoutSacrificialChamber(unit, context);
            return unit.Status(StatusId) is CombatStatus status &&
                (IsOriginalStatus(unit, status) || status.Stacks == 2 && HasObservedHordeInput(context) &&
                    IsOriginalStatus(unit, status.WithStacks(1))) &&
                unit.Modifiers!.Upgrades is [var upgrade] && IsAttackUpgrade(upgrade, 3) &&
                IsInfestedOpeningUnit(unit, upgrade.Stats.Damage);
        }

        internal static bool IsInfestedOpeningCombat(RoomCombatState room) =>
            room.RoomIndex == 0 &&
            room.Context?.QueryFrame is { Turn: 1, MoonPhase: 2, MoonPhaseTrackingActive: false } &&
            OuterBossDefinitions.HasLifemotherInfestedOwner(room.Context) &&
            (room.Units.Count(unit => unit.AssetKey == DeathHorde && IsInfestedOpeningPreviewOwner(unit, room.Context)) == 2 &&
                room.Units.Count(unit => IsInfestedPreviewFeedRecipient(unit, room.Context)) == 1 ||
             room.Units is [var retained] && IsInfestedPreviewFeedRecipient(retained, room.Context) ||
             IsInfestedHordeGrantState(room) ||
             HasObservedHordeInput(room.Context) && room.Units is [var recipient] && IsObservedHordeRecipient(recipient, room.Context));

        private static bool IsInfestedStatusUpgrade(CardUpgradeModifier upgrade) =>
            upgrade is { DataId: "2ffda46d-a18b-4659-a709-0c758a688337", AssetKey: "R2_Support_FeedTitanskinUpgrade_T2",
                Unique: false, RemoveOnDiscard: false, ExcludeFromClones: false, ExternalInteractions.Count: 0,
                Stats: { Damage: 0, Health: 0, Cost: 0, Heal: 0, Size: 0, XCost: 0, EquipmentLimit: 0, UpgradeSlotCount: 0 },
                Statuses: [{ Id: "burst", Stacks: 1, ParamInt: 2 }, { Id: StatusId, Stacks: 6, ParamInt: 1 }] };

        private static bool IsInfestedPreviewFeedRecipient(CombatUnit unit, CombatContext? context)
        {
            unit = RoomAttachmentUnitModel.OriginalIdentityWithoutSacrificialChamber(unit, context);
            return unit.AssetKey == HeavyT2 && unit.Status(StatusId) is { Stacks: var stacks } && IsInfestedOpeningUnit(unit, 3) &&
                unit.Team == CombatTeam.Enemy && unit.SpawnerCardId == 0 && unit.Health > 0 && unit.IsBoss != true &&
                unit.Modifiers is { IsClone: false } modifiers && ((stacks == 1 || stacks == 2 && HasObservedHordeInput(context)) &&
                    modifiers.Upgrades is [var opening] && IsAttackUpgrade(opening, 3) ||
                    (stacks == 7 || stacks == 8 && HasObservedHordeInput(context)) && modifiers.Upgrades is [var active, var feeding] &&
                    IsAttackUpgrade(active, 3) && IsInfestedStatusUpgrade(feeding));
        }

        internal static bool IsInfestedWaveCombatOwner(CombatUnit unit, CombatContext? context) =>
            IsInfestedOpeningPreviewOwner(unit, context) || IsInfestedPreviewFeedRecipient(unit, context) ||
            HasObservedHordeInput(context) && IsObservedHordeRecipient(unit, context);

        private static bool IsOpeningDeathGrant(CombatTrigger trigger) =>
            trigger is { Kind: "OnDeath", Once: false, IgnoreSilence: false, FireCount: 1, TriggerAtThreshold: 0,
                Conditions: null, Effects: [{ Type: "CardEffectAddStatusEffect", Action: { Type: "AddStatus", Target: "Room",
                    AllowEnemy: true, AllowPlayer: false, Value: 0, Range: null, Statuses: [{ Id: StatusId, Stacks: 1 }] } }] };

        internal static bool HasDormantOpeningDeathGrant(CombatUnit unit, CombatTrigger trigger) =>
            unit.AssetKey == DeathHorde && IsOpeningDeathGrant(trigger) &&
            unit.Status(StatusId) is CombatStatus status && IsInfestedOpeningOwner(unit, status);

        internal static bool IsObservedInfestedHordeDeath(RoomCombatState room, CombatUnit unit, int sourceCardId) =>
            sourceCardId > 0 && !room.Preview && IsInfestedOpeningCombat(room) &&
            room.Context?.FindCard(sourceCardId)?.DataId is FanningTheFlame or SlamAbility &&
            unit.AssetKey == DeathHorde && IsInfestedOpeningPreviewOwner(unit, room.Context);

        internal static bool IsObservedInfestedHordeGrant(RoomCombatState room, CombatUnit unit, CombatEffect effect) =>
            IsInfestedHordeGrantFrame(room) && unit is { AssetKey: DeathHorde, Health: 0, BaseAttack: 5 } &&
            unit.Status(StatusId)?.Stacks == 1 && unit.Triggers is [var trigger] &&
            IsOpeningDeathGrant(trigger) && ReferenceEquals(trigger.Effects[0], effect);

        private static bool IsInfestedHordeGrantFrame(RoomCombatState room) =>
            room.RoomIndex == 0 && room.Context?.QueryFrame is { Turn: 1, MoonPhase: 2, MoonPhaseTrackingActive: false } &&
            OuterBossDefinitions.HasLifemotherInfestedOwner(room.Context);

        private static bool HasObservedHordeInput(CombatContext? context) =>
            context?.Statistics?.HandCardsPlayedThisTurn?.Any(id => context.FindCard(id)?.DataId == FanningTheFlame) == true ||
            context?.LastAbilityActivatorUnitId is int id && id > 0 && context.Enchantments?.Rooms
                .Any(room => room.Units.Any(unit => unit.Id == id && unit.Ability?.DataId == SlamAbility)) == true;

        private static bool IsInfestedHordeGrantState(RoomCombatState room) => HasObservedHordeInput(room.Context) &&
            room.Units.Count(unit => unit.AssetKey == DeathHorde && IsInfestedWaveCombatOwner(unit, room.Context)) == 1 &&
            room.Units.Count(unit => unit.AssetKey == HeavyT2 && IsInfestedWaveCombatOwner(unit, room.Context)) == 1 &&
            room.Units.Count(unit => IsObservedHordeRecipient(unit, room.Context)) == 2;

        private static bool IsObservedHordeRecipient(CombatUnit unit, CombatContext? context) =>
            unit is { Team: CombatTeam.Enemy, AssetKey: "R2_Support_FeedInfested_T2" or "R2_Support_FeedBuffs_Ver2", BaseAttack: 0,
                Health: > 0, SpawnerCardId: 0, Modifiers.IsClone: false } &&
            (unit.RegisteredStatus(StatusId) == null && unit.Modifiers.Upgrades.Count == 0 ||
             HasObservedHordeInput(context) && unit.Status(StatusId) is { Stacks: 1, ParamInt: 1 } &&
             (unit.Modifiers.Upgrades.Count == 0 ||
              unit.Modifiers.Upgrades is [var upgrade] && IsAttackUpgrade(upgrade, 0)));

        internal static CombatContext ObserveInfestedRandomCastTests(TrainCombatState train, RoomCombatState room,
            CombatContext context, CardPlayRule rule)
        {
            if (room.Preview || !IsInfestedHordeGrantFrame(room) || !IsInfestedOpeningCombat(room) ||
                rule.DataId != FanningTheFlame ||
                rule.Effects is not [{ Type: "Damage", Target: "RandomInRoom", Value: 3, AllowEnemy: true,
                    AllowPlayer: false, Range: null } effect] || effect.Tests?.ShouldTest == false ||
                context.Enchantments is not { AutomaticLifecycle: true } world)
                return context;
            CardTargets targets = CardTargetModel.RandomCandidates(train, room.RoomIndex, effect);
            if (!targets.Supported || targets.UnitIds.Count != 5) return context;
            // The paid cast preflight and runtime effect test each select from the same captured room.
            // Their selections affect only test RNG. The committed target still comes from Battle RNG.
            UnityRng test = world.TestRng.Range(0, targets.UnitIds.Count).State;
            return context.WithEnchantments(world.WithTestRng(test.Range(0, targets.UnitIds.Count).State));
        }

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

        internal static bool IsInfestedTerminalPyre(RoomCombatState room) =>
            room.RoomIndex == 3 && HasInfestedTerminalFrame(room.Context) && !room.Preview &&
            NoCapturedKiller(room) && room.Units.Count == 4 && room.Units.Any(unit => unit.IsPyre && unit.Health == 78) &&
            HasInfestedTerminalAttackers(room.Units.Where(unit => unit.Team == CombatTeam.Enemy));

        internal static bool IsInfestedTerminalAscension(TrainCombatState train) =>
            HasInfestedTerminalFrame(train.Context) && train.Rooms.Count == 4 && train.Rooms.All(room => !room.Preview) &&
            train.Rooms[3].Units is [{ IsPyre: true, Health: 78 }] &&
            HasInfestedTerminalAttackers(train.Rooms[2].Units.Where(unit =>
                !OuterBossDefinitions.IsLifemotherInfestedCastingActor(unit, train.Context))) &&
            train.Rooms.SelectMany(room => room.Units).Where(unit => unit.RegisteredStatus(StatusId) != null)
                .All(unit => IsInfestedCombatOwner(unit, 3, train.Context)) &&
            train.Rooms.All(NoCapturedKiller);

        private static bool HasInfestedTerminalFrame(CombatContext? context) =>
            context?.QueryFrame is { RunningCombat: true, Turn: 3, MoonPhase: 2, MoonPhaseTrackingActive: false } &&
            OuterBossDefinitions.HasLifemotherInfestedOwner(context);

        private static bool HasInfestedTerminalAttackers(IEnumerable<CombatUnit> units) =>
            units.ToArray() is [{ AssetKey: HeavyT2, Health: 250, Attack: 101 } heavy,
                { AssetKey: DeathHorde, Health: 3, Attack: 34 } first,
                { AssetKey: DeathHorde, Health: 3, Attack: 34 } second] &&
            heavy.Status(StatusId)?.Stacks == 19 && IsInfestedTerminalOwner(heavy) &&
            IsInfestedTerminalOwner(first) && IsInfestedTerminalOwner(second);

        internal static bool IsInfestedTerminalOwner(CombatUnit unit)
        {
            if (unit.Team != CombatTeam.Enemy || unit.SpawnerCardId != 0 || unit.Health <= 0 ||
                unit.Status(StatusId) is not { ParamInt: 1 } status || unit.RegisteredStatus("hivemindkiller") != null ||
                unit.Modifiers is not { IsClone: false } modifiers ||
                modifiers.Upgrades.Count(upgrade => IsAttackUpgrade(upgrade, 32)) != 1)
                return false;
            if (unit.AssetKey == OuterBossDefinitions.LifemotherInfested)
                return unit is { BaseAttack: 72, MaxHealth: 9000, BumpRules.OuterBoss: true, Triggers:
                    [{ Kind: "OnAnyUnitDeathOnFloor" }, { Kind: "OnTeamTurnBegin" }] } &&
                    status.Stacks == 6 && modifiers.Upgrades.Count == 1;
            if (!IsInfestedOpeningUnit(unit, 32)) return false;
            if (unit.AssetKey == DeathHorde) return IsOriginalStatus(unit, status) && modifiers.Upgrades.Count == 1;
            int feeds = modifiers.Upgrades.Count(IsInfestedStatusUpgrade);
            int shieldFeeds = modifiers.Upgrades.Count(upgrade => upgrade.DataId == FeedBuffUpgrade);
            return feeds is 1 or 2 or 3 && status.Stacks == 1 + 6 * feeds && shieldFeeds <= feeds &&
                modifiers.Upgrades.Count == 1 + feeds + shieldFeeds;
        }

        private static int InfestedCombatTurn(CombatContext? context) =>
            context?.QueryFrame is { Turn: >= 1 and <= 3, MoonPhaseTrackingActive: false } frame &&
            OuterBossDefinitions.HasLifemotherInfestedOwner(context) ? frame.Turn.Value : 0;

        internal static bool IsInfestedOrdinaryRoom(RoomCombatState room)
        {
            int turn = InfestedCombatTurn(room.Context);
            if (room.RoomIndex is < 0 or >= 3) return false;
            bool firstChamberEntry = turn == 1 && !room.Preview && room.RoomIndex == 1 &&
                room.Context?.EnergyState?.Phase == "HeroTurn" &&
                RoomAttachmentUnitModel.HasOriginalSacrificialChamber(room.Context, 1);
            return ((turn is 2 or 3 || firstChamberEntry) && HasInfestedCombatOwners(room.Units, turn, room.Context)) ||
                IsInfestedChamberCastingMovement(room) &&
                HasInfestedCombatOwners(room.Units, turn - 1, room.Context);
        }

        internal static bool IsInfestedOrdinaryAscension(TrainCombatState train)
        {
            int turn = InfestedCombatTurn(train.Context);
            if (turn is not (1 or 2) || train.Rooms.Count != 4 || train.Rooms.Any(room => room.Preview))
                return false;
            CombatUnit[] units = train.Rooms.SelectMany(room => room.Units).ToArray();
            return units.Count(unit => unit.RegisteredStatus(StatusId) != null) == (turn == 1 ? 4 : 7) &&
                HasInfestedCombatOwners(units, turn, train.Context);
        }

        private static bool HasInfestedCombatOwners(IEnumerable<CombatUnit> units, int turn, CombatContext? context)
        {
            CombatUnit[] owners = units.Where(unit => unit.RegisteredStatus(StatusId) != null ||
                unit.RegisteredStatus("hivemindkiller") != null).ToArray();
            return owners.Length > 0 && owners.All(unit => IsInfestedCombatOwner(unit, turn, context));
        }

        internal static bool IsInfestedCombatOwner(RoomCombatState room, CombatUnit unit) =>
            IsInfestedCombatOwner(unit, room.Context?.QueryFrame?.Turn ?? 0, room.Context) ||
            IsInfestedChamberCastingMovement(room) &&
            IsInfestedCombatOwner(unit, (room.Context?.QueryFrame?.Turn ?? 0) - 1, room.Context);

        private static bool IsInfestedChamberCastingMovement(RoomCombatState room) =>
            !room.Preview && room.Context?.EnergyState?.Phase == "BossActionPreCombat" &&
            InfestedCombatTurn(room.Context) switch
            {
                2 => room.RoomIndex == 1 && RoomAttachmentUnitModel.HasOriginalSacrificialChamber(room.Context, 1),
                3 => room.RoomIndex == 2 && RoomAttachmentUnitModel.HasOriginalSacrificialChamber(room.Context, 1),
                _ => false
            };

        private static bool IsInfestedCombatOwner(CombatUnit unit, int turn, CombatContext? context)
        {
            unit = RoomAttachmentUnitModel.OriginalIdentityWithoutSacrificialChamber(unit, context);
            if (turn is < 1 or > 3 || unit.Team != CombatTeam.Enemy || unit.SpawnerCardId != 0 || unit.Health <= 0 ||
                unit.Status(StatusId) is not { ParamInt: 1 } status || unit.RegisteredStatus("hivemindkiller") != null ||
                unit.Modifiers is not { IsClone: false } modifiers)
                return false;
            int damage = turn == 1 ? 3 : turn == 2 ? 14 : 32;
            if (unit.AssetKey == OuterBossDefinitions.LifemotherInfested)
            {
                int bossDamage = turn == 1 ? 0 : damage;
                return OuterBossDefinitions.IsLifemotherInfestedCastingActor(unit, context) &&
                    unit.BaseAttack == 40 + bossDamage &&
                    modifiers.Upgrades is [var upgrade] && IsAttackUpgrade(upgrade, bossDamage) &&
                    (turn == 1 && status.Stacks == 2 || turn == 2 && status.Stacks is 2 or 4 or 6 ||
                     turn == 3 && status.Stacks is 6 or 8 or 10 or 12);
            }
            if (!IsInfestedOpeningUnit(unit, damage) || modifiers.Upgrades.Count(upgrade => IsAttackUpgrade(upgrade, damage)) != 1)
                return false;
            if (unit.AssetKey == DeathHorde) return IsOriginalStatus(unit, status) && modifiers.Upgrades.Count == 1;
            int feeds = modifiers.Upgrades.Count(IsInfestedStatusUpgrade);
            int shieldFeeds = modifiers.Upgrades.Count(upgrade => upgrade.DataId == FeedBuffUpgrade);
            // The third bottom-floor Heavy receives shields before its first +6 feed.
            return feeds <= turn && status.Stacks == 1 + 6 * feeds &&
                (shieldFeeds <= feeds || turn == 3 && feeds == 0 && shieldFeeds == 1) &&
                modifiers.Upgrades.Count == 1 + feeds + shieldFeeds;
        }

        internal static TrainCombatState MoveInfestedBossMarker(TrainCombatState train, CombatUnit boss)
        {
            string marker = Marker(boss);
            if (!train.Rooms.Any(room => room.ExternalInteractions.Contains(marker))) return train;

            var rooms = train.Rooms.Select(room =>
            {
                IEnumerable<string> markers = room.ExternalInteractions.Where(item => item != marker);
                if (room.Units.Any(unit => unit.Id == boss.Id)) markers = markers.Append(marker);
                return new RoomCombatState(room.RoomIndex, room.Deployment, room.Units,
                    markers.OrderBy(item => item, StringComparer.Ordinal).ToArray(), room.Context, room.Preview,
                    room.ActiveCompanionBossIds);
            }).ToArray();
            return EnchantmentWorldModel.Rebase(new TrainCombatState(rooms, train.Movement, train.EnemySlotsPerRoom, train.Context));
        }

        private static bool NoCapturedKiller(RoomCombatState room) => room.Context?.Enchantments is EnchantmentWorld world &&
            world.Rooms.SelectMany(floor => floor.Units).Concat(world.RetainedUnits.Select(actor => actor.Unit)).All(unit =>
                unit.RegisteredStatus("hivemindkiller") == null && unit.Modifiers?.Upgrades.Any(upgrade =>
                    upgrade.DataId == "RoomStateHivemindKillerModifierUpgrade") != true);

        internal static bool PublishesAtCombat(RoomCombatState room) =>
            !room.Preview && (IsFirstCombat(room) || IsSecondCombat(room) || IsThirdCombat(room) || IsObservedPyreCombat(room) ||
                ScaledFeedRoom(room) || room.Units.Any(unit => IsScaledRoomCombatDeath(room, unit)));

        internal static bool PublishesAtAscension(TrainCombatState train) =>
            (train.Context?.QueryFrame?.Turn is >= 1 and <= 3 ||
                train.Context?.QueryFrame?.Turn == 4 && IsScaledCampaign(train.Context)) && train.Rooms.All(room => !room.Preview) &&
            train.Rooms.Any(room => room.ExternalInteractions.Any(IsKnownMarker)) &&
            train.Rooms.SelectMany(room => room.Units).All(unit => unit.RegisteredStatus("hivemindkiller") == null &&
                (unit.Status(StatusId) == null || MatchesRoomMarker(Marker(unit), unit) ||
                 IsScaledFirstWaveOwner(unit, train.Context) || IsScaledCarriedFeedOwner(unit, train.Context) ||
                 IsScaledCampaign(train.Context) && IsScaledFeedRecipient(unit)));

        internal static RoomCombatState PublishRoomMarkers(RoomCombatState room, bool infested = false)
        {
            bool InfestedMarker(string marker) => infested && marker is
                HeavyT2 + " room modifiers" or DeathHorde + " room modifiers" or OuterBossDefinitions.LifemotherInfested + " room modifiers";
            string[] markers = room.ExternalInteractions.Where(marker => !IsKnownMarker(marker) && !InfestedMarker(marker)).Concat(
                room.Units.Where(unit => MatchesRoomMarker(Marker(unit), unit) || infested && IsInfestedCombatOwner(room, unit) ||
                    IsScaledFirstWaveOwner(unit, room.Context) || IsScaledCarriedFeedOwner(unit, room.Context) ||
                    IsScaledCampaign(room.Context) && IsScaledFeedRecipient(unit))
                    .Select(Marker)).Distinct().OrderBy(marker => marker, StringComparer.Ordinal).ToArray();
            return room.ExternalInteractions.SequenceEqual(markers) ? room :
                new RoomCombatState(room.RoomIndex, room.Deployment, room.Units, markers, room.Context, room.Preview,
                    room.ActiveCompanionBossIds);
        }

        internal static bool IsInfestedFirstBossMarkerOwner(RoomCombatState room, CombatUnit unit) =>
            room is { RoomIndex: 2, Preview: false } && OuterBossDefinitions.IsLifemotherInfestedFirstGrant(unit, room.Context);

        internal static bool CanRegisterInfestedFirstBossMarker(RoomCombatState room, CardUpgradeModifier upgrade, string? kind) =>
            kind == "OnStatusEffectChanged" && IsAttackUpgrade(upgrade, 0) &&
            room.Units.Any(unit => IsInfestedFirstBossMarkerOwner(room, unit) && unit.Modifiers?.Upgrades.Count == 0);

        internal static RoomCombatState PublishInfestedFirstBossMarker(RoomCombatState room)
        {
            CombatUnit? owner = room.Units.FirstOrDefault(unit => IsInfestedFirstBossMarkerOwner(room, unit) &&
                unit.Modifiers?.Upgrades is [var upgrade] && IsAttackUpgrade(upgrade, 0));
            if (owner == null) return room;
            string[] markers = room.ExternalInteractions.Append(Marker(owner)).Distinct().OrderBy(marker => marker, StringComparer.Ordinal).ToArray();
            return new RoomCombatState(room.RoomIndex, room.Deployment, room.Units, markers, room.Context, room.Preview,
                room.ActiveCompanionBossIds);
        }

        internal static RoomCombatState PublishInfestedHordeMarkers(RoomCombatState room)
        {
            if (room.Preview || !IsInfestedHordeGrantFrame(room) || !IsInfestedHordeGrantState(room)) return room;
            string[] markers = room.ExternalInteractions.Concat(room.Units.Where(unit =>
                IsObservedHordeRecipient(unit, room.Context) && unit.Modifiers?.Upgrades is [var upgrade] &&
                IsAttackUpgrade(upgrade, 0)).Select(Marker)).Distinct().OrderBy(marker => marker, StringComparer.Ordinal).ToArray();
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
            return MatchesInfestedDrawRoster(source.Rooms.SelectMany(room => room.Units), frame.Turn.Value, source.Context);
        }

        internal static bool IsInfestedWaveSpawn(EnemySpawnState source, IReadOnlyList<EnemyDefinition> definitions)
        {
            if (source.Phase is not (1 or 2) || source.Turn != source.Phase || source.Train.Rooms.Any(room => room.Preview) ||
                source.OuterBoss is not { AssetKey: OuterBossDefinitions.LifemotherInfested, Phase: "Casting" } ||
                !OuterBossDefinitions.HasLifemotherInfestedOwner(source.Train.Context))
                return false;
            return MatchesInfestedDrawRoster(source.Train.Rooms.SelectMany(room => room.Units)
                .Concat(definitions.Select(definition => definition.Unit)), source.Turn + 1, source.Train.Context);
        }

        private static bool MatchesInfestedDrawRoster(IEnumerable<CombatUnit> roster, int turn, CombatContext? context)
        {
            CombatUnit[] units = roster.ToArray();
            var owners = units.Where(unit => unit.RegisteredStatus(StatusId) != null).ToArray();
            return owners.Length == (turn == 2 ? 7 : 9) &&
                owners.Count(unit => unit.AssetKey == OuterBossDefinitions.LifemotherInfested) == 1 &&
                owners.Count(unit => unit.AssetKey == HeavyT2) == turn &&
                owners.Count(unit => unit.AssetKey == DeathHorde) == (turn == 2 ? 4 : 5) &&
                owners.Count(unit => unit.Modifiers?.Upgrades.Count == 0) == (turn == 2 ? 3 : 2) &&
                units.All(unit => unit.RegisteredStatus("hivemindkiller") == null) &&
                owners.All(unit => IsInfestedDrawOwner(unit, turn, afterRemoval: false, context)) &&
                owners.Sum(unit => unit.Status(StatusId)?.Stacks) == (turn == 2 ? 14 : 32);
        }

        internal static bool IsInfestedDrawOwner(CombatUnit unit, int turn, bool afterRemoval, CombatContext? context)
        {
            unit = RoomAttachmentUnitModel.OriginalIdentityWithoutSacrificialChamber(unit, context);
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

        internal static TrainCombatState? ApplyObservedRefresh(TrainCombatState source, out string? error)
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
                    if (unit.Status(KillerStatusId) != null && IsObservedSproutCarrier(room, unit))
                    {
                        actor = RefreshSproutAttack(room, unit, total, out error);
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

        internal static CombatUnit? RefreshSproutAttack(RoomCombatState room, CombatUnit carrier, int towerHivemind, out string? error)
        {
            if (!TryKillerUpgrade(carrier, out CardUpgradeModifier? native))
            {
                error = "Missing observed Sprout attack modifier.";
                return null;
            }
            CombatUnit? removed = ApplyUnit(room, room.Context, carrier, native!, remove: true, out error);
            return removed == null ? null : ApplyUnit(room, room.Context, removed,
                native!.WithStats(new CardStatModifier(damage: towerHivemind)), remove: false, out error);
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
                enqueueCallback: callbacks.Add, directApi: true, upgradeId: upgrade.DataId, allowFreshHivemind: true);
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
            kind == "OnEaten" && IsInfestedStatusUpgrade(upgrade) &&
            (IsInfestedOpeningCombat(room) && IsInfestedPreviewFeedRecipient(unit, room.Context) ||
             IsObservedLaterInfestedFeedRecipient(room, unit));

        private static bool IsObservedLaterInfestedFeedRecipient(RoomCombatState room, CombatUnit unit)
        {
            unit = RoomAttachmentUnitModel.OriginalIdentityWithoutSacrificialChamber(unit, room.Context);
            if (room.Context?.QueryFrame is not { Turn: 2 or 3, MoonPhaseTrackingActive: false } frame ||
                frame.MoonPhase != frame.Turn - 1 || !OuterBossDefinitions.HasLifemotherInfestedOwner(room.Context) ||
                unit is not { AssetKey: HeavyT2, Team: CombatTeam.Enemy, SpawnerCardId: 0,
                    IsBoss: false, Health: > 0, Modifiers: { IsClone: false } modifiers } ||
                unit.RegisteredStatus("hivemindkiller") != null)
                return false;
            int damage = frame.Turn == 2 ? 14 : 32;
            if (!IsInfestedOpeningUnit(unit, damage) ||
                modifiers.Upgrades.Count(upgrade => IsAttackUpgrade(upgrade, damage)) != 1)
                return false;
            int earlierFeeds = room.RoomIndex;
            return room.RoomIndex >= 0 && room.RoomIndex < frame.Turn &&
                modifiers.Upgrades.Count(IsInfestedStatusUpgrade) == earlierFeeds &&
                modifiers.Upgrades.All(upgrade => IsAttackUpgrade(upgrade, damage) ||
                    IsInfestedStatusUpgrade(upgrade) || upgrade.DataId == FeedBuffUpgrade) &&
                modifiers.Upgrades.Count(upgrade => upgrade.DataId == FeedBuffUpgrade) <= earlierFeeds + 1 &&
                unit.Status(StatusId) is { ParamInt: 1 } status && status.Stacks == 1 + 6 * earlierFeeds;
        }

        internal static bool HasObservedLaterInfestedDeathGrant(RoomCombatState room, CombatUnit unit, CombatTrigger trigger)
        {
            unit = RoomAttachmentUnitModel.OriginalIdentityWithoutSacrificialChamber(unit, room.Context);
            if (room.Context?.QueryFrame is not { MoonPhaseTrackingActive: false } frame ||
                unit.Modifiers is not { IsClone: false, Upgrades: [var upgrade] } ||
                unit.Status(StatusId) is not { ParamInt: 1 } status || unit.RegisteredStatus("hivemindkiller") != null ||
                !OuterBossDefinitions.IsLifemotherInfestedCastingActor(unit, room.Context) ||
                !ReferenceEquals(unit.Triggers[0], trigger))
                return false;
            return room.RoomIndex == 1 && frame is { Turn: 2, MoonPhase: 1 } &&
                unit is { BaseAttack: 54, Attack: 54 } && IsAttackUpgrade(upgrade, 14) && status.Stacks is 2 or 4 ||
                room.RoomIndex == 2 && frame is { Turn: 3, MoonPhase: 2 } &&
                unit is { BaseAttack: 72, Attack: 72 } && IsAttackUpgrade(upgrade, 32) && status.Stacks is 6 or 8 or 10;
        }

        internal static bool IsObservedFeedUpgrade(RoomCombatState room, CombatUnit unit, CardUpgradeModifier upgrade, string? kind) =>
            IsObservedInfestedFeedUpgrade(room, unit, upgrade, kind) || kind == "OnEaten" && IsFeedUpgrade(upgrade) && (
                IsScaledPreviewFeedRecipient(room, unit) && unit.Modifiers!.Upgrades.Count(IsFeedUpgrade) < 2 ||
                ScaledFeedRoom(room) && unit is { AssetKey: Heavy, MaxHealth: 75 } && IsFeedRecipient(unit) &&
                    (unit.BaseAttack == 12 && unit.RegisteredStatus(StatusId) == null || IsScaledFirstFeedRecipient(room, unit)) ||
                IsFirstCombat(room) && unit.AssetKey == Heavy && IsFeedRecipient(unit) && unit.RegisteredStatus(StatusId) == null ||
                IsSecondCombat(room) && IsFeedRecipient(unit) && (unit.AssetKey == Heavy && unit.Status(StatusId)?.Stacks == 3 ||
                    unit.AssetKey == Zealot && unit.RegisteredStatus(StatusId) == null) ||
                IsThirdCombat(room) && (room.RoomIndex == 0 && unit.AssetKey == Heavy &&
                    IsFeedRecipient(unit) && unit.RegisteredStatus(StatusId) == null ||
                    room.RoomIndex == 1 && unit.AssetKey == Zealot && NativeFedUpgrade(unit, 3, 13) ||
                    room.RoomIndex == 2 && unit.AssetKey == Heavy && NativeFedUpgrade(unit, 6, 13)));

        internal static bool IsObservedCombatUpgrade(RoomCombatState room, CardUpgradeModifier upgrade, string? kind) =>
            (kind == "OnStatusEffectChanged" && ScaledFeedRoom(room) && IsAttackUpgrade(upgrade, 0) &&
                room.Units.Any(unit => IsScaledFirstFeedRecipient(room, unit))) ||
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
            (IsInfestedFirstBossMarkerOwner(room, unit) && unit.Modifiers?.Upgrades.Count == 0 ||
             IsInfestedHordeGrantFrame(room) && !room.Preview && HasObservedHordeInput(room.Context) &&
                 IsObservedHordeRecipient(unit, room.Context) && unit.Status(StatusId) != null ||
             IsFirstCombat(room) && IsFedPreviewRecipient(unit) ||
             IsSecondCombat(room) && unit.AssetKey == Zealot && IsFedSecondPreviewRecipient(unit) ||
             IsThirdCombat(room) && room.RoomIndex == 0 && IsThirdNewHeavy(unit) || IsScaledFirstFeedRecipient(room, unit)) &&
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
