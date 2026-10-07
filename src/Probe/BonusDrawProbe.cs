using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class BonusDrawProbe
    {
        internal static CardUpgradeModifier? Upgrade(CardEffectData data)
        {
            if (data.GetParamCardUpgradeData() == null) return null;
            var state = new CardUpgradeState(); state.Setup(data.GetParamCardUpgradeData());
            return CardModifierProbe.Upgrade(state, rejectFilters: true);
        }
        private static int Count(CardEffectState effect) => (int)AccessTools.Field(typeof(CardEffectDrawAdditionalNextTurn), "upgradeApplyCount").GetValue(effect.GetCardEffect());
        internal static string Key(FullBattleTrace trace, CardEffectState effect, CardEffectParams? parameters = null)
        {
            CardState? card = effect.GetParentCardState();
            if (card != null)
            {
                int index = card.GetEffectStates().IndexOf(effect);
                if (index < 0) throw new NotSupportedException("Bonus draw from a card trigger.");
                return BonusDrawState.CardKey(trace.CardId(card), index);
            }
            CharacterState? actor = parameters?.selfTarget;
            CharacterTriggerState? trigger = parameters?.sourceCharacterTriggerState;
            if (actor == null || trigger == null) throw new NotSupportedException("Unknown bonus-draw effect owner.");
            int effectIndex = trigger.GetEffectStates().ToList().IndexOf(effect);
            if (effectIndex < 0) throw new NotSupportedException("Detached bonus-draw effect definition.");
            return BonusDrawState.UnitKey(trace.UnitId(actor), trace.TriggerStateId(actor, trigger), effectIndex);
        }
        internal static BonusDrawState Capture(CardManager cards, FullBattleTrace trace)
        {
            var counters = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (CardState card in trace.KnownCards)
                foreach (var entry in card.GetEffectStates().Select((effect, index) => new { effect, index }))
                    if (entry.effect.GetCardEffect() is CardEffectDrawAdditionalNextTurn && Count(entry.effect) != 0)
                        counters[BonusDrawState.CardKey(trace.CardId(card), entry.index)] = Count(entry.effect);
            foreach (CharacterState unit in trace.KnownUnits)
                foreach (var trigger in trace.TriggerStates(unit))
                    foreach (var entry in trigger.Key.GetEffectStates().Select((effect, index) => new { effect, index }))
                        if (entry.effect.GetCardEffect() is CardEffectDrawAdditionalNextTurn && Count(entry.effect) != 0)
                            counters[BonusDrawState.UnitKey(trace.UnitId(unit), trigger.Value, entry.index)] = Count(entry.effect);
            var listeners = new List<BonusDrawListener>();
            var signal = cards.bonusDrawCountCardSignal;
            if (AccessTools.Field(signal.GetType(), "OnceListener").GetValue(signal) != null)
                throw new NotSupportedException("Unknown one-shot bonus-draw listener.");
            var callbacks = (Delegate?)AccessTools.Field(signal.GetType(), "Listener").GetValue(signal);
            foreach (Delegate callback in callbacks?.GetInvocationList() ?? Array.Empty<Delegate>())
            {
                object closure = callback.Target ?? throw new NotSupportedException("Unknown static bonus-draw listener.");
                FieldInfo[] fields = closure.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                CardEffectState? effect = fields.Where(field => field.FieldType == typeof(CardEffectState)).Select(field => (CardEffectState?)field.GetValue(closure)).FirstOrDefault();
                CardEffectParams? parameters = fields.Where(field => field.FieldType == typeof(CardEffectParams)).Select(field => (CardEffectParams?)field.GetValue(closure)).FirstOrDefault();
                if (!(effect?.GetCardEffect() is CardEffectDrawAdditionalNextTurn)) throw new NotSupportedException("Unmodeled bonus-draw listener.");
                string key = Key(trace, effect, parameters); counters[key] = Count(effect);
                listeners.Add(new BonusDrawListener(key, Upgrade(effect.GetSourceCardEffectData())!));
            }
            return new BonusDrawState(counters.Select(pair => new BonusDrawCounter(pair.Key, pair.Value)).ToArray(), listeners);
        }
        internal sealed class Record
        {
            public string Key { get; set; } = "";
            public CardActionEffect Effect { get; set; } = null!;
            public CombatContext Before { get; set; } = null!;
            public CombatContext? After { get; set; }
            public int Amount { get; set; }
            public bool Sampled { get; set; }
            public bool Completed { get; set; }
        }
        internal static readonly List<Record> Records = new List<Record>();
        private static Record? current;
        [HarmonyPatch(typeof(CardEffectDrawAdditionalNextTurn), nameof(CardEffectDrawAdditionalNextTurn.ApplyEffect))]
        private static class ApplyPatch
        {
            private static void Postfix(CardEffectState cardEffectState, CardEffectParams cardEffectParams, ref IEnumerator __result)
            { if (FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode) __result = Observe(__result, cardEffectState, cardEffectParams); }
        }
        private static IEnumerator Observe(IEnumerator native, CardEffectState effect, CardEffectParams parameters)
        {
            FullBattleTrace trace = FullBattleTrace.Active!; Record? parent = current;
            var record = new Record { Key = Key(trace, effect, parameters), Effect = UnitTriggerActionProbe.Capture(effect)!, Before = trace.CaptureContext() };
            Records.Add(record); current = record;
            try { while (native.MoveNext()) yield return native.Current; record.Completed = true; }
            finally { (native as IDisposable)?.Dispose(); current = parent; record.After = trace.CaptureContext(); }
        }
        [HarmonyPatch(typeof(CardEffectState), nameof(CardEffectState.GetIntInRange))]
        private static class SamplePatch
        {
            private static void Postfix(CardEffectState __instance, int __result)
            { if (current != null && !current.Sampled && __instance.GetCardEffect() is CardEffectDrawAdditionalNextTurn) { current.Amount = __result; current.Sampled = true; } }
        }
    }
}
