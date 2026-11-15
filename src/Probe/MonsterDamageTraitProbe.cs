using System;
using System.Collections.Generic;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class MonsterDamageTraitProbe
    {
        internal sealed class Record
        {
            public int Amount { get; set; }
            public int FriendlyAmount { get; set; }
            public BattleStatistics Before { get; set; } = null!;
            public BattleStatistics? Actual { get; set; }
            public List<Callback> Callbacks { get; set; } = new List<Callback>();
            public bool Completed { get; set; }
        }
        internal sealed class Callback
        {
            public int CardId { get; set; }
            public string TrackedValue { get; set; } = "";
            public CardInstanceState Before { get; set; } = null!;
            public CardInstanceState? Actual { get; set; }
        }
        internal static readonly List<Record> Records = new List<Record>();
        private static Record? current;
        private static bool Enabled => OriginalPunishedSpellsScenario.Prepared && FullBattleTrace.Active != null &&
            !AllGameManagers.Instance!.GetSaveManager().PreviewMode;
        [HarmonyPatch(typeof(CardStatistics), "OnMonstersDamaged")]
        private static class DamagePatch
        {
            private static void Prefix(CardStatistics __instance, int amount, int friendlyAmount, out Record? __state)
            {
                __state = current;
                if (!Enabled) return;
                try
                {
                    current = new Record { Amount = amount, FriendlyAmount = friendlyAmount,
                        Before = BattleStatisticsProbe.Capture(__instance, FullBattleTrace.Active!.CardId) };
                    Records.Add(current);
                }
                catch (Exception error) { FullBattleTrace.Active!.CaptureFailure(error); }
            }
            private static void Postfix(CardStatistics __instance, Record? __state)
            {
                if (current == __state) return;
                try { current!.Actual = BattleStatisticsProbe.Capture(__instance, FullBattleTrace.Active!.CardId); current.Completed = true; }
                catch (Exception error) { FullBattleTrace.Active!.CaptureFailure(error); }
                finally { current = __state; }
            }
        }
        [HarmonyPatch(typeof(CardTraitScalingAddDamage), nameof(CardTraitScalingAddDamage.OnTraitUpdated))]
        private static class TraitPatch
        {
            private static void Prefix(CardTraitScalingAddDamage __instance, CardState thisCard, out Callback? __state)
            {
                __state = null; if (current == null || !Enabled) return;
                try
                {
                    __state = new Callback { CardId = FullBattleTrace.Active!.CardId(thisCard),
                        TrackedValue = __instance.GetParamTrackedValue().ToString(),
                        Before = CardModifierProbe.Capture(new[] { thisCard }, FullBattleTrace.Active.CardId)[0] };
                    current.Callbacks.Add(__state);
                }
                catch (Exception error) { FullBattleTrace.Active!.CaptureFailure(error); }
            }
            private static void Postfix(CardState thisCard, Callback? __state)
            {
                if (__state == null) return;
                try { __state.Actual = CardModifierProbe.Capture(new[] { thisCard }, FullBattleTrace.Active!.CardId)[0]; }
                catch (Exception error) { FullBattleTrace.Active!.CaptureFailure(error); }
            }
        }
    }
}
