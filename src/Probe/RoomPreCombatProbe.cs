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
        internal const string SirenSongId = "8ccceee7-0eb9-4aa0-9268-ff83b6f6c54e";
        internal static bool RequiresWorld(FullBattleTrace trace) => trace.KnownCards.Any(card => card.GetCardDataID() == SirenSongId);
        internal static RoomPreCombatRule? Rule(CardUpgradeData upgrade, List<string> interactions)
        {
            RoomModifierData[] modifiers = upgrade.GetRoomModifierUpgrades().ToArray();
            if (modifiers.Length == 1 && modifiers[0].GetRoomStateModifierClassName() == "RoomStateDrawRandomConsumedCardsPreCombatModifier")
            {
                var consumed = new RoomPreCombatRule(Array.Empty<CardActionEffect>(), modifiers[0].DisallowedInDeploymentPhase,
                    false, modifiers[0].GetParamInt());
                string? consumedError = RoomPreCombatModel.Validate(consumed);
                if (consumedError != null) interactions.Add(consumedError);
                return consumed;
            }
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
        [HarmonyPatch(typeof(RoomStateDrawRandomConsumedCardsPreCombatModifier), nameof(RoomStateDrawRandomConsumedCardsPreCombatModifier.PreCombat))]
        private static class ConsumedPatch
        {
            private static void Postfix(RoomState room, ref IEnumerator __result)
            {
                if (OriginalSupportSpellsScenario.IsolatedIds?.Contains("93fd40da-054d-4cc2-a8b3-da77e619a1d0") == true &&
                    FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode)
                    __result = Observe(__result, room);
            }
        }
        [HarmonyPatch(typeof(RoomStateAddEffectPreCombatModifier), nameof(RoomStateAddEffectPreCombatModifier.PreCombat))]
        private static class Patch
        {
            private static void Postfix(RoomState room, ref IEnumerator __result)
            {
                if (FullBattleTrace.Active != null && (InfernoRoomScenario.Enabled || RequiresWorld(FullBattleTrace.Active)) &&
                    !AllGameManagers.Instance!.GetSaveManager().PreviewMode)
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
