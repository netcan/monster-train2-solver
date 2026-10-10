using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class RelicCardModifierProbe
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_BATTLE_RELIC_UPGRADES") == "1";
        internal static bool CaptureReady => RelicCardModifierScenario.CaptureReady;
        internal static readonly List<Record> Records = new List<Record>();
        private static readonly List<Record> active = new List<Record>();
        internal sealed class Record
        {
            public CardInstanceState Before { get; set; } = null!;
            public CombatRelicState[] Relics { get; set; } = null!;
            public bool ResetTemporary { get; set; }
            public CardInstanceState? Actual { get; set; }
            public List<Dispatch> Dispatches { get; } = new List<Dispatch>();
            public List<object> Notifications { get; } = new List<object>();
            public string? Difference { get; set; }
            [JsonIgnore] public List<RelicState> NativeRelics { get; set; } = null!;
        }
        internal sealed class Dispatch
        {
            public int RelicIndex { get; set; }
            public int EffectIndex { get; set; }
            public bool Returned { get; set; }
            public bool UpgradeAdded { get; set; }
            public List<RelicCardFilterResult> Filters { get; } = new List<RelicCardFilterResult>();
            [JsonIgnore] public int UpgradeCount { get; set; }
        }
        internal static RelicCardModifier Definition(IRelicEffect effect, int index)
        {
            object Field(string name) => AccessTools.Field(effect.GetType(), name).GetValue(effect);
            var source = (CardUpgradeData?)Field("_cardUpgradeData");
            CardLifecycleUpgrade? template = null; CardUpgradeModifier? upgrade = null;
            if (source != null)
            {
                var instance = new CardUpgradeState(); instance.Setup(source);
                template = CardUpgradeLifecycleProbe.Definition(instance);
                upgrade = CardModifierProbe.Upgrade(instance, false, filtersAlreadyApplied: true);
            }
            Team.Type sourceTeam = (Team.Type)Field("_sourceTeam");
            var rule = new RelicCardUpgradeRule(source?.name ?? "", sourceTeam.HasFlag(Team.Type.Monsters),
                template, source?.GetFilters().Select(CardUpgradeMaskProbe.Rule).ToArray() ?? Array.Empty<CardUpgradeMaskRule>(),
                sourceTeam.HasFlag(Team.Type.Heroes));
            return new RelicCardModifier(index, rule, upgrade, (bool)Field("_applyToCardlessSpawns"),
                ((IEnumerable<RelicEffectCondition>)Field("_effectConditions")).Count());
        }
        private static (int Relic, int Effect) Index(Record record, IRelicEffect effect)
        {
            for (int relic = 0; relic < record.NativeRelics.Count; relic++)
            {
                var effects = record.NativeRelics[relic].GetEffects();
                for (int index = 0; index < effects.Count; index++) if (ReferenceEquals(effects[index], effect)) return (relic, index);
            }
            throw new InvalidOperationException("Native relic modifier dispatch is outside the captured manager order.");
        }
        internal static void ObserveFilter(string asset, bool accepted)
        {
            if (active.Count == 0 || active[active.Count - 1].Dispatches.Count == 0) return;
            var record = active[active.Count - 1]; record.Dispatches[record.Dispatches.Count - 1].Filters.Add(new RelicCardFilterResult(asset, accepted));
        }
        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.ApplyCardStateModifiers), new[] { typeof(CardState), typeof(bool) })]
        private static class ManagerPatch
        {
            private static void Prefix(CardState cardState, bool resetTempCardModifiers, out Record? __state)
            {
                __state = null; var trace = FullBattleTrace.Active;
                if (!Enabled || !CaptureReady || trace == null || cardState == null) return;
                try
                {
                    var managers = AllGameManagers.Instance!; var nativeRelics = new List<RelicState>();
                    AccessTools.Method(typeof(RelicManager), "GetCurrentRelics").Invoke(managers.GetRelicManager(), new object[] { nativeRelics });
                    __state = new Record { Before = CardModifierProbe.Capture(new[] { cardState }, trace.CardId).Single(),
                        Relics = RelicProbe.Capture(managers), ResetTemporary = resetTempCardModifiers, NativeRelics = nativeRelics };
                    Records.Add(__state); active.Add(__state);
                }
                catch (Exception error) { trace.CaptureFailure(error); }
            }
            private static void Postfix(CardState cardState, Record? __state)
            {
                if (__state == null) return;
                try
                {
                    __state.Actual = CardModifierProbe.Capture(new[] { cardState }, FullBattleTrace.Active!.CardId).Single();
                    var predicted = RelicCardModifierModel.Apply(__state.Before, __state.Relics, __state.ResetTemporary);
                    __state.Difference = !predicted.Supported ? predicted.UnsupportedReason :
                        JToken.DeepEquals(JToken.FromObject(predicted.Card!), JToken.FromObject(__state.Actual)) ? null : "Native relic manager card modifiers differ.";
                }
                catch (Exception error) { FullBattleTrace.Active?.CaptureFailure(error); }
                finally { active.Remove(__state); }
            }
            private static Exception? Finalizer(Exception? __exception, Record? __state)
            { if (__exception != null && __state != null) { active.Remove(__state); FullBattleTrace.Active?.CaptureFailure(__exception); } return __exception; }
        }
        [HarmonyPatch(typeof(RelicEffectAddTempUpgrade), nameof(RelicEffectAddTempUpgrade.ApplyCardStateModifiers))]
        private static class EffectPatch
        {
            private static void Prefix(RelicEffectAddTempUpgrade __instance, CardState cardState, out Dispatch? __state)
            {
                __state = null; if (active.Count == 0) return;
                var record = active[active.Count - 1]; var index = Index(record, __instance);
                __state = new Dispatch { RelicIndex = index.Relic, EffectIndex = index.Effect,
                    UpgradeCount = cardState.GetTemporaryCardStateModifiers().GetCardUpgrades().Count };
                record.Dispatches.Add(__state);
            }
            private static void Postfix(CardState cardState, bool __result, Dispatch? __state)
            {
                if (__state == null) return; __state.Returned = __result;
                __state.UpgradeAdded = cardState.GetTemporaryCardStateModifiers().GetCardUpgrades().Count > __state.UpgradeCount;
            }
        }
        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.NotifyRelicTriggered))]
        private static class NotifyPatch
        {
            private static void Prefix(IRelicEffect triggeredEffect)
            {
                if (active.Count == 0) return; var record = active[active.Count - 1]; var index = Index(record, triggeredEffect);
                record.Notifications.Add(new { RelicIndex = index.Relic, EffectIndex = index.Effect });
            }
        }
    }
}
