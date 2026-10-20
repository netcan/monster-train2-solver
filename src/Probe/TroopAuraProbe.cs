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
        internal static bool RequiresWorld(FullBattleTrace trace) => trace.KnownCards.Any(card => card.GetCardDataID() == CardId);
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
            if (trace == null || captured.DataId != "8538457b-b33a-4a3d-9596-5929a2de5936") return captured;
            foreach (CharacterState owner in trace.KnownUnits)
            foreach (RoomStateTroopUnitUpgradeModifier modifier in owner.GetRoomStateModifiers<RoomStateTroopUnitUpgradeModifier>())
                if (ReferenceEquals(upgrade, AccessTools.Field(modifier.GetType(), "cardUpgradeState").GetValue(modifier)))
                    return captured.WithRuntimeIdentity(TroopAuraModel.Key(trace.UnitId(owner)));
            return captured;
        }
    }
}
