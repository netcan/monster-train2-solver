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
        internal static bool Enabled => OriginalBalatroEventCavendishScenario.Enabled || OriginalRelicCountAttackScenario.Enabled || OriginalHealingPowerScenario.Enabled || Environment.GetEnvironmentVariable("MT2_PROBE_BATTLE_RELIC_UPGRADES") == "1" ||
            OriginalRelicBatchScenario.Enabled || OriginalCardAttackRelicScenario.Enabled || OriginalCardTypeCostScenario.Enabled || OriginalUpgradeCountRelicsScenario.Enabled ||
            OriginalPurgeToExhaustScenario.Enabled || OriginalNoUpgradeCardCostScenario.Enabled || OriginalExpensiveCardUpgradeScenario.Enabled || OriginalWolosBloodletterScenario.Enabled;
        internal static bool CaptureReady => OriginalRelicCountAttackScenario.CaptureReady || OriginalHealingPowerScenario.CaptureReady || RelicCardModifierScenario.CaptureReady || OriginalRelicBatchScenario.CaptureReady ||
            MagebladeScenario.CaptureReady || OriginalCardAttackRelicScenario.CaptureReady || OriginalBalatroEventCavendishScenario.CaptureReady || OriginalCardTypeCostScenario.CaptureReady || OriginalUpgradeCountRelicsScenario.CaptureReady ||
            OriginalPurgeToExhaustScenario.CaptureReady || OriginalNoUpgradeCardCostScenario.CaptureReady || OriginalExpensiveCardUpgradeScenario.Prepared || OriginalWolosBloodletterScenario.Prepared;
        internal static readonly List<Record> Records = new List<Record>();
        private static readonly List<Record> active = new List<Record>();
        internal sealed class Record
        {
            public CardInstanceState Before { get; set; } = null!;
            public CombatContext ContextBefore { get; set; } = null!;
            public CombatRelicState[] Relics { get; set; } = null!;
            public bool ResetTemporary { get; set; }
            public CardInstanceState? Actual { get; set; }
            public CombatContext? ContextAfter { get; set; }
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
                Conditions(effect));
        }
        internal static RelicConditionState[] Conditions(IRelicEffect effect)
        {
            object Field(string name) => AccessTools.Field(effect.GetType(), name).GetValue(effect);
            return ((IEnumerable<RelicEffectCondition>)Field("_effectConditions")).Select(condition => {
                object C(string name) => AccessTools.Field(typeof(RelicEffectCondition), name).GetValue(condition);
                var input = new CardStatistics.StatValueData {
                    trackedValue = (CardStatistics.TrackedValueType)C("paramTrackedValue"),
                    entryDuration = (CardStatistics.EntryDuration)C("paramEntryDuration"),
                    cardTypeTarget = (CardStatistics.CardTypeTarget)C("paramCardType"),
                    paramSubtype = SubtypeManager.GetSubtypeData((string)C("paramSubtype")) };
                bool trackCount = (bool)C("paramTrackTriggerCount");
                CardStatisticQuery query = trackCount ? new CardStatisticQuery(input.trackedValue.ToString(), input.entryDuration.ToString()) :
                    DamageScalingProbe.Query(input, 0, false);
                return new RelicConditionState(query, trackCount, Convert.ToInt32(C("paramComparator")), (int)C("paramInt"),
                    (bool)C("allowMultipleTriggersPerDuration"), (bool)C("triggered"), (int)C("valueAtLastTrigger"),
                    (int)C("durationTriggerCount"));
            }).ToArray();
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
                    // A generated card may not have a trace identity yet. Register it
                    // before freezing the context so this observation does not appear
                    // to allocate a new card during the modifier operation.
                    CardInstanceState before = CardModifierProbe.Capture(new[] { cardState }, trace.CardId).Single();
                    CombatContext context = trace.CaptureContext();
                    __state = new Record { Before = before,
                        ContextBefore = context, Relics = context.Relics?.ToArray() ?? RelicProbe.Capture(managers),
                        ResetTemporary = resetTempCardModifiers, NativeRelics = nativeRelics };
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
                    __state.ContextAfter = FullBattleTrace.Active.CaptureContext();
                    var predicted = RelicCardModifierModel.Apply(__state.Before, __state.Relics,
                        __state.ResetTemporary, __state.ContextBefore);
                    __state.Difference = !predicted.Supported ? predicted.UnsupportedReason :
                        !JToken.DeepEquals(JToken.FromObject(predicted.Card!), JToken.FromObject(__state.Actual)) ? "Native relic manager card modifiers differ." :
                        !JToken.DeepEquals(JToken.FromObject(predicted.Context!.Relics!), JToken.FromObject(__state.ContextAfter!.Relics!)) ? "Native relic modifier condition state differs." :
                        !JToken.DeepEquals(JToken.FromObject(predicted.Context.Statistics!), JToken.FromObject(__state.ContextAfter.Statistics!)) ? "Native relic modifier statistic cache differs." : null;
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
        [HarmonyPatch(typeof(RelicEffectAddCardUpgradeToExpensiveCards), nameof(RelicEffectAddCardUpgradeToExpensiveCards.ApplyCardStateModifiers))]
        private static class ExpensiveEffectPatch
        {
            private static void Prefix(RelicEffectAddCardUpgradeToExpensiveCards __instance, CardState cardState, out Dispatch? __state)
            {
                __state = null; if (active.Count == 0) return;
                Record record = active[active.Count - 1]; var index = Index(record, __instance);
                __state = new Dispatch { RelicIndex = index.Relic, EffectIndex = index.Effect,
                    UpgradeCount = cardState.GetTemporaryCardStateModifiers().GetCardUpgrades().Count };
                record.Dispatches.Add(__state);
            }
            private static void Postfix(CardState cardState, bool __result, Dispatch? __state)
            { if (__state == null) return; __state.Returned = __result;
                __state.UpgradeAdded = cardState.GetTemporaryCardStateModifiers().GetCardUpgrades().Count > __state.UpgradeCount; }
        }
        [HarmonyPatch(typeof(RelicEffectModifyCardCostIfNoUpgrades),
            nameof(RelicEffectModifyCardCostIfNoUpgrades.ApplyCardStateModifiers))]
        private static class NoUpgradeCostEffectPatch
        {
            private static void Prefix(RelicEffectModifyCardCostIfNoUpgrades __instance, CardState cardState,
                out Dispatch? __state)
            {
                __state = null; if (active.Count == 0) return;
                var record = active[active.Count - 1]; var index = Index(record, __instance);
                __state = new Dispatch { RelicIndex = index.Relic, EffectIndex = index.Effect,
                    UpgradeCount = cardState.GetTemporaryCardStateModifiers().GetCardUpgrades().Count };
                record.Dispatches.Add(__state);
            }
            private static void Postfix(CardState cardState, bool __result, Dispatch? __state)
            {
                if (__state == null) return;
                __state.Returned = __result;
                __state.UpgradeAdded = cardState.GetTemporaryCardStateModifiers().GetCardUpgrades().Count > __state.UpgradeCount;
            }
        }
        [HarmonyPatch(typeof(RelicEffectModifyCardCostIfNoUpgrades), nameof(RelicEffectModifyCardCostIfNoUpgrades.OnCardAdded))]
        private static class NoUpgradeCostHandAddedPatch
        {
            private static void Prefix(RelicEffectModifyCardCostIfNoUpgrades __instance,
                CardAddedToHandRelicEffectParams relicEffectParams,
                out OriginalNoUpgradeCardCostScenario.CallbackRecord? __state)
            {
                __state = OriginalNoUpgradeCardCostScenario.Begin("OnCardAdded", __instance, relicEffectParams.cardState);
            }
            private static void Postfix(CardAddedToHandRelicEffectParams relicEffectParams, bool __result,
                OriginalNoUpgradeCardCostScenario.CallbackRecord? __state)
            {
                OriginalNoUpgradeCardCostScenario.Complete(__state, relicEffectParams.cardState, __result);
            }
        }
        [HarmonyPatch(typeof(RelicEffectModifyCardCostIfNoUpgrades),
            nameof(RelicEffectModifyCardCostIfNoUpgrades.OnPermanentCardUpgradeApplied))]
        private static class NoUpgradeCostPermanentUpgradePatch
        {
            private static void Prefix(RelicEffectModifyCardCostIfNoUpgrades __instance, CardState cardState,
                out OriginalNoUpgradeCardCostScenario.CallbackRecord? __state)
            {
                __state = OriginalNoUpgradeCardCostScenario.Begin("OnPermanentCardUpgradeApplied", __instance, cardState);
            }
            private static void Postfix(CardState cardState, OriginalNoUpgradeCardCostScenario.CallbackRecord? __state)
            {
                OriginalNoUpgradeCardCostScenario.Complete(__state, cardState);
            }
        }
        [HarmonyPatch(typeof(RelicEffectModifyCardHealth), nameof(RelicEffectModifyCardHealth.ApplyCardStateModifiers))]
        private static class HealthEffectPatch
        {
            private static void Prefix(RelicEffectModifyCardHealth __instance, out Dispatch? __state)
            {
                __state = null; if (active.Count == 0) return;
                var record = active[active.Count - 1]; var index = Index(record, __instance);
                __state = new Dispatch { RelicIndex = index.Relic, EffectIndex = index.Effect };
                record.Dispatches.Add(__state);
            }
            private static void Postfix(bool __result, Dispatch? __state)
            { if (__state != null) __state.Returned = __result; }
        }
        [HarmonyPatch(typeof(RelicEffectModifyCardTypeCost), nameof(RelicEffectModifyCardTypeCost.ApplyCardStateModifiers))]
        private static class CardTypeCostEffectPatch
        {
            private static void Prefix(RelicEffectModifyCardTypeCost __instance, out Dispatch? __state)
            {
                __state = null; if (active.Count == 0) return;
                var record = active[active.Count - 1]; var index = Index(record, __instance);
                __state = new Dispatch { RelicIndex = index.Relic, EffectIndex = index.Effect };
                record.Dispatches.Add(__state);
            }
            private static void Postfix(bool __result, Dispatch? __state)
            { if (__state != null) __state.Returned = __result; }
        }
        [HarmonyPatch(typeof(RelicEffectModifyTrait), nameof(RelicEffectModifyTrait.ApplyCardStateModifiers))]
        private static class ModifyTraitEffectPatch
        {
            private static IEnumerable<HarmonyLib.CodeInstruction> Transpiler(IEnumerable<HarmonyLib.CodeInstruction> instructions)
            {
                int replaced = 0;
                foreach (var instruction in instructions)
                {
                    if (instruction.operand is System.Reflection.MethodInfo method &&
                        method.DeclaringType == typeof(CardUpgradeMaskData) && method.Name == nameof(CardUpgradeMaskData.FilterCard))
                    {
                        instruction.opcode = System.Reflection.Emit.OpCodes.Call;
                        instruction.operand = AccessTools.Method(typeof(RelicCardUpgradeCalibration), "Filter");
                        replaced++;
                    }
                    yield return instruction;
                }
                if (replaced != 2) throw new InvalidOperationException("Unexpected native ModifyTrait filter call sites.");
            }
            private static void Prefix(RelicEffectModifyTrait __instance, out Dispatch? __state)
            {
                __state = null; if (active.Count == 0) return;
                var record = active[active.Count - 1]; var index = Index(record, __instance);
                __state = new Dispatch { RelicIndex = index.Relic, EffectIndex = index.Effect };
                record.Dispatches.Add(__state);
            }
            private static void Postfix(bool __result, Dispatch? __state)
            { if (__state != null) __state.Returned = __result; }
        }
        [HarmonyPatch(typeof(RelicEffectModifyCardAttack), nameof(RelicEffectModifyCardAttack.ApplyCardStateModifiers))]
        private static class AttackEffectPatch
        {
            // Instrument this call site separately from AddTempUpgrade: Mono
            // shares generic FilterCard bodies with definition-only callers.
            private static IEnumerable<HarmonyLib.CodeInstruction> Transpiler(IEnumerable<HarmonyLib.CodeInstruction> instructions)
            {
                int replaced = 0;
                foreach (var instruction in instructions)
                {
                    if (instruction.operand is System.Reflection.MethodInfo method &&
                        method.DeclaringType == typeof(CardUpgradeMaskData) && method.Name == nameof(CardUpgradeMaskData.FilterCard))
                    {
                        instruction.opcode = System.Reflection.Emit.OpCodes.Call;
                        instruction.operand = AccessTools.Method(typeof(RelicCardUpgradeCalibration), "Filter");
                        replaced++;
                    }
                    yield return instruction;
                }
                if (replaced != 1) throw new InvalidOperationException("Unexpected native attack relic filter call site.");
            }
            private static void Prefix(RelicEffectModifyCardAttack __instance, out Dispatch? __state)
            {
                __state = null; if (active.Count == 0) return;
                var record = active[active.Count - 1]; var index = Index(record, __instance);
                __state = new Dispatch { RelicIndex = index.Relic, EffectIndex = index.Effect };
                record.Dispatches.Add(__state);
            }
            private static void Postfix(bool __result, Dispatch? __state)
            { if (__state != null) __state.Returned = __result; }
        }
        [HarmonyPatch(typeof(RelicEffectModifyCardMagicalPower), nameof(RelicEffectModifyCardMagicalPower.ApplyCardStateModifiers))]
        private static class MagicPowerEffectPatch
        {
            private static void Prefix(RelicEffectModifyCardMagicalPower __instance, out Dispatch? __state)
            {
                __state = null; if (active.Count == 0) return;
                var record = active[active.Count - 1]; var index = Index(record, __instance);
                __state = new Dispatch { RelicIndex = index.Relic, EffectIndex = index.Effect };
                record.Dispatches.Add(__state);
            }
            private static void Postfix(bool __result, Dispatch? __state)
            { if (__state != null) __state.Returned = __result; }
        }
        [HarmonyPatch(typeof(RelicEffectModifyCardHealingPower), nameof(RelicEffectModifyCardHealingPower.ApplyCardStateModifiers))]
        private static class HealingPowerEffectPatch
        {
            private static void Prefix(RelicEffectModifyCardHealingPower __instance, out Dispatch? __state)
            {
                __state = null; if (active.Count == 0) return;
                var record = active[active.Count - 1]; var index = Index(record, __instance);
                __state = new Dispatch { RelicIndex = index.Relic, EffectIndex = index.Effect };
                record.Dispatches.Add(__state);
            }
            private static void Postfix(bool __result, Dispatch? __state)
            { if (__state != null) __state.Returned = __result; }
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
