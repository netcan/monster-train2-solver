using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class TroopAuraProbe
    {
        internal const string Type = "RoomStateTroopUnitUpgradeModifier";
        internal const string CardId = "54a24002-b7b2-4e1c-967b-59d40b67231c";
        internal static bool RequiresWorld(FullBattleTrace trace) => trace.KnownCards.Any(card => card.GetCardDataID() == CardId ||
            card.GetCardStateModifiers().GetCardUpgrades().Concat(card.GetTemporaryCardStateModifiers().GetCardUpgrades())
                .Any(upgrade => upgrade.GetCardUpgradeDataId() is CardUnderlegionTroopUpgradeModel.DataId or
                    CardUnderlegionTroopUpgradeModel.SecondDataId or CardUnderlegionTroopUpgradeModel.ThirdDataId));

        internal static bool KnownChampionUpgrade(CardUpgradeState upgrade)
        {
            bool second = upgrade.GetCardUpgradeDataId() == CardUnderlegionTroopUpgradeModel.SecondDataId;
            bool third = upgrade.GetCardUpgradeDataId() == CardUnderlegionTroopUpgradeModel.ThirdDataId;
            if ((!second && !third && upgrade.GetCardUpgradeDataId() != CardUnderlegionTroopUpgradeModel.DataId) ||
                upgrade.GetAssetName() != (third ? CardUnderlegionTroopUpgradeModel.ThirdAssetKey :
                    second ? CardUnderlegionTroopUpgradeModel.SecondAssetKey : CardUnderlegionTroopUpgradeModel.AssetKey) ||
                upgrade.GetRoomModifierUpgrades().Count != 1)
                return false;
            RoomModifierData room = upgrade.GetRoomModifierUpgrades()[0];
            if (room.GetRoomStateModifierClassName() != Type || room.DisallowedInDeploymentPhase ||
                room.GetParamSubtype().Key != CardUnderlegionTroopUpgradeModel.NoneSubtype ||
                room.GetParamStatusEffects().Length != 0 || room.GetParamCardUpgradeData() == null) return false;
            var state = new CardUpgradeState(); state.Setup(room.GetParamCardUpgradeData());
            CardUpgradeModifier captured = CardModifierProbe.Upgrade(state, false, true);
            var aura = CardUnderlegionTroopUpgradeModel.AuraDefinition(upgrade.GetCardUpgradeDataId());
            return captured.DataId == aura.Id && captured.AssetKey == aura.AssetKey && captured.Stats.Health == aura.Health &&
                new[] { "Damage", "Cost", "Heal", "Size", "XCost", "EquipmentLimit", "UpgradeSlotCount" }
                    .All(stat => captured.Stats.Value(stat) == 0) && captured.Statuses.Count == 0 &&
                captured.ExternalInteractions.Count == 0 && !captured.Unique && !captured.RemoveOnDiscard &&
                !captured.ExcludeFromClones && captured.DamageBuff == 0 && captured.UnhealedHealth == 0 &&
                captured.TriggerUpgrades?.Count is not > 0 && captured.AbilityUpgrade == null &&
                captured.Lifecycle is { AddedTraits.Count: 0, RemovedRuntimeTypes.Count: 0, Triggers.Count: 0, ReplacedAssets.Count: 0 };
        }
        private static TroopAuraState Rule(RoomModifierData data, int[] targets)
        {
            var upgrade = new CardUpgradeState(); upgrade.Setup(data.GetParamCardUpgradeData());
            return new TroopAuraState(data.GetParamSubtype().Key, CardModifierProbe.Upgrade(upgrade, false, true),
                data.GetParamStatusEffects().Select(status => BattleActionProbe.Status(status.statusId, status.count)).ToArray(), targets);
        }
        internal static TroopAuraState? Definition(CharacterData data)
        {
            RoomModifierData? rule = data.GetRoomModifiersData().SingleOrDefault(modifier => modifier.GetRoomStateModifierClassName() == Type);
            return rule == null ? null : Rule(rule, Array.Empty<int>());
        }
        internal static TroopAuraState? Capture(CharacterState actor)
        {
            RoomStateTroopUnitUpgradeModifier? modifier = actor.GetRoomStateModifiers<RoomStateTroopUnitUpgradeModifier>().SingleOrDefault();
            return modifier == null ? null : Rule(modifier.GetRoomModifierData(), ((List<CharacterState>)AccessTools.Field(modifier.GetType(),
                "cachedAffectedTargets").GetValue(modifier)).Select(target => FullBattleTrace.Active!.UnitId(target)).ToArray());
        }
        internal static CardUpgradeModifier Normalize(CardUpgradeState upgrade, CardUpgradeModifier captured)
        {
            var trace = FullBattleTrace.Active;
            if (trace == null || captured.DataId is not ("8538457b-b33a-4a3d-9596-5929a2de5936" or CardUnderlegionTroopUpgradeModel.AuraUpgradeId or
                CardUnderlegionTroopUpgradeModel.SecondAuraUpgradeId or CardUnderlegionTroopUpgradeModel.ThirdAuraUpgradeId))
                return captured;
            foreach (CharacterState owner in trace.KnownUnits)
            foreach (RoomStateTroopUnitUpgradeModifier modifier in owner.GetRoomStateModifiers<RoomStateTroopUnitUpgradeModifier>())
                if (ReferenceEquals(upgrade, AccessTools.Field(modifier.GetType(), "cardUpgradeState").GetValue(modifier)))
                    return captured.WithRuntimeIdentity(TroopAuraModel.Key(trace.UnitId(owner)));
            return captured;
        }
    }
}
