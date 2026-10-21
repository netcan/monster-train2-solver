using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class HoldoverCalibration
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_HOLDOVER_CALIBRATION") == "1";
        internal static bool Prepared { get; private set; }
        internal static bool ControlsUnchanged { get; private set; }
        private static bool control;
        private static readonly HashSet<CardState> observed = new HashSet<CardState>();
        internal sealed class Row
        {
            public string Kind { get; set; } = "";
            public bool Control { get; set; }
            public bool WasPlayed { get; set; }
            public bool Recast { get; set; }
            public bool TriggersOnUnplayed { get; set; }
            public int CardId { get; set; }
            public CardInstanceState? Before { get; set; }
            public CardInstanceState? After { get; set; }
            public CombatContext? BeforeContext { get; set; }
            public CombatContext? AfterContext { get; set; }
            public BattlePlayRules? Definitions { get; set; }
        }
        internal static readonly List<Row> Rows = new List<Row>();
        private static CardInstanceState Capture(CardState card) => CardModifierProbe.Capture(new[] { card },
            value => control ? -77 : FullBattleTrace.Active!.CardId(value)).Single();
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (!Enabled || Prepared) return;
            var cards = managers.GetCardManager()!;
            CardState owner = cards.GetAllCards(new List<CardState>()).FirstOrDefault(card => card.HasTrait<CardTraitRetain>())
                ?? throw new InvalidOperationException("Holdover calibration requires an actual acquired native Holdover owner.");
            var trace = FullBattleTrace.Active!;
            var initial = trace.CaptureDecision(); int frame = UnityEngine.Time.frameCount;
            JObject rng = Rng();
            var all = managers.GetSaveManager().GetAllGameData();
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.HasTrait<CardTraitRetain>())) observed.Add(card);
            Prepared = true; control = true;
            try
            {
                foreach (string id in new[] { owner.GetCardDataID(), OriginalUnplayableCardsScenario.ScrapId,
                    OriginalUnpurgableCardsScenario.BlightId, OriginalUnpurgableCardsScenario.CurseId })
                {
                    var card = new CardState(); card.Setup(all.FindCardData(id)!, managers.GetSaveManager());
                    var definition = new CardTraitData(); definition.Setup(CardRetainModel.Trait);
                    if (!card.HasTrait<CardTraitRetain>()) card.AddTrait(definition);
                    // Detached authored control: the original Curse supplies the
                    // unplayed trigger, while this added marker exercises the AND condition.
                    if (id == OriginalUnpurgableCardsScenario.CurseId && !card.HasTrait<CardTraitUnplayable>())
                    { var unplayable = new CardTraitData(); unplayable.Setup("CardTraitUnplayable"); card.AddTrait(unplayable); }
                    var trait = card.GetTraitStates().OfType<CardTraitRetain>().Single();
                    observed.Add(card);
                    foreach (bool pending in new[] { false, true })
                    {
                        foreach (bool played in new[] { false, true })
                        foreach (bool recast in new[] { false, true })
                        {
                            AccessTools.Field(typeof(CardTraitRetain), "retained").SetValue(trait, pending);
                            Drain(trait.OnCardDiscarded(new CardManager.DiscardCardParams {
                                discardCard = card, wasPlayed = played, isRecastCopy = recast }, managers.GetCoreManagers()));
                        }
                        AccessTools.Field(typeof(CardTraitRetain), "retained").SetValue(trait, pending);
                        trait.OnPreOpeningHand(card, managers.GetCoreManagers());
                        AccessTools.Field(typeof(CardTraitRetain), "retained").SetValue(trait, pending);
                        trait.OnTraitStateReset();
                    }
                }
            }
            finally { control = false; }
            ControlsUnchanged = frame == UnityEngine.Time.frameCount && JToken.DeepEquals(rng, Rng()) &&
                JToken.DeepEquals(JToken.FromObject(initial), JToken.FromObject(trace.CaptureDecision()));
            if (!ControlsUnchanged) throw new InvalidOperationException("Detached Holdover callbacks changed live decision, RNG or frame.");
            log.LogInfo("HOLDOVER-CONTROLS-PASS rows=" + Rows.Count + " native acquired owner=" + owner.GetCardDataID());
        }
        private static JObject Rng()
        {
            var result = new JObject();
            foreach (RngId id in Enum.GetValues(typeof(RngId)))
                result[id.ToString()] = JToken.FromObject(RngCalibration.Words(RandomManager.GetState(id)));
            result["Unity"] = JToken.FromObject(UnityEngine.Random.state);
            return result;
        }
        private static void Drain(IEnumerator iterator)
        { while (iterator.MoveNext()) if (iterator.Current is IEnumerator nested) Drain(nested); }
        private static Row? Begin(string kind, CardState card)
        {
            if (!Enabled || !Prepared || !observed.Contains(card) || AllGameManagers.Instance!.GetSaveManager().PreviewMode) return null;
            var row = new Row { Kind = kind, Control = control, Before = Capture(card) };
            row.CardId = row.Before.InstanceId; Rows.Add(row); return row;
        }
        [HarmonyPatch(typeof(CardTraitRetain), nameof(CardTraitRetain.OnCardDiscarded))]
        private static class DiscardPatch
        {
            private static void Postfix(CardManager.DiscardCardParams discardCardParams, ref IEnumerator __result)
            { if (Enabled && Prepared) __result = Wrap(__result, discardCardParams); }
            private static IEnumerator Wrap(IEnumerator native, CardManager.DiscardCardParams parameters)
            {
                var row = Begin("Discard", parameters.discardCard);
                if (row != null) { row.WasPlayed = parameters.wasPlayed; row.Recast = parameters.isRecastCopy;
                    row.TriggersOnUnplayed = parameters.discardCard.TriggersOnUnplayed(); }
                while (native.MoveNext()) yield return native.Current;
                if (row != null) row.After = Capture(parameters.discardCard);
            }
        }
        [HarmonyPatch(typeof(CardTraitRetain), nameof(CardTraitRetain.OnPreDrawingHand))]
        private static class DrawPatch
        {
            private static void Prefix(CardState thisCard, out Row? __state)
            {
                __state = Begin("PreDraw", thisCard);
                if (__state == null) return;
                __state.BeforeContext = FullBattleTrace.Active!.CaptureContext();
                __state.Definitions = FullBattleTrace.Active.CaptureDecision().PlayRules;
            }
            private static void Postfix(CardState thisCard, Row? __state)
            { if (__state != null) { __state.After = Capture(thisCard); __state.AfterContext = FullBattleTrace.Active!.CaptureContext(); } }
        }
        [HarmonyPatch(typeof(CardTraitRetain), nameof(CardTraitRetain.OnPreOpeningHand))]
        private static class OpeningPatch
        {
            private static void Prefix(CardState thisCard, out Row? __state) => __state = Begin("OpeningReset", thisCard);
            private static void Postfix(CardState thisCard, Row? __state) { if (__state != null) __state.After = Capture(thisCard); }
        }
        [HarmonyPatch(typeof(CardTraitRetain), nameof(CardTraitRetain.OnTraitStateReset))]
        private static class ResetPatch
        {
            private static void Prefix(CardTraitRetain __instance, out Row? __state) =>
                __state = Begin("TraitReset", (CardState)AccessTools.Field(typeof(CardTraitState), "associatedCard").GetValue(__instance));
            private static void Postfix(CardTraitRetain __instance, Row? __state)
            { if (__state != null) __state.After = Capture((CardState)AccessTools.Field(typeof(CardTraitState), "associatedCard").GetValue(__instance)); }
        }
    }
}
