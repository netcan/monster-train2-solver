using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class RoomPreCombatProbe
    {
        internal static RoomPreCombatRule? Rule(CardUpgradeData upgrade, List<string> interactions)
        {
            RoomModifierData[] modifiers = upgrade.GetRoomModifierUpgrades().ToArray();
            if (modifiers.Length != 1 || modifiers[0].GetRoomStateModifierClassName() != "RoomStateAddEffectPreCombatModifier") return null;
            var rule = new RoomPreCombatRule(modifiers[0].GetParamCardEffectData()
                .Select(effect => BattleActionProbe.DescribeTriggerEffect(effect, interactions)).ToArray(), modifiers[0].DisallowedInDeploymentPhase,
                modifiers[0].GetTriggeredVFX() != null && !modifiers[0].GetTriggeredVFX().IsEmpty());
            string? error = RoomPreCombatModel.Validate(rule);
            if (error != null) interactions.Add(error);
            return rule;
        }
        internal sealed class Record
        {
            public int RoomIndex { get; set; }
            public int Turn { get; set; }
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? After { get; set; }
            public bool Completed { get; set; }
        }
        internal static readonly List<Record> Records = new List<Record>();
        [HarmonyPatch(typeof(RoomStateAddEffectPreCombatModifier), nameof(RoomStateAddEffectPreCombatModifier.PreCombat))]
        private static class Patch
        {
            private static void Postfix(RoomState room, ref IEnumerator __result)
            {
                if (InfernoRoomScenario.Enabled && FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode)
                    __result = Observe(__result, room);
            }
        }
        private static IEnumerator Observe(IEnumerator native, RoomState room)
        {
            var record = new Record { RoomIndex = room.GetRoomIndex(), Turn = AllGameManagers.Instance!.GetCombatManager()!.GetTurnCount(),
                Before = FullBattleTrace.Active!.CaptureCanonicalTrain() };
            Records.Add(record);
            try { while (native.MoveNext()) yield return native.Current; record.Completed = true; }
            finally { (native as IDisposable)?.Dispose(); record.After = FullBattleTrace.Active!.CaptureCanonicalTrain(); }
        }
    }
}
