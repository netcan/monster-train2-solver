using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal sealed class CardCycleProbe
    {
        private readonly UnitPlayModelProbe projection;
        private readonly ManualLogSource log;
        private readonly List<Record> records = new List<Record>();
        private static CardCycleProbe? active;
        internal IReadOnlyList<Record> Records => records;
        internal int Mismatches => records.Count(record => record.Difference != null);
        internal int Unsupported => records.Count(record => !record.Predicted.Supported);

        internal CardCycleProbe(ManualLogSource log, UnitPlayModelProbe projection)
        { this.log = log; this.projection = projection; active = this; }

        internal CardCycleState Capture(CardManager cards, string kind)
        {
            AllGameManagers managers = AllGameManagers.Instance!;
            CombatProjection state = projection.Capture(managers, managers.GetSaveManager(),
                managers.GetCombatManager()!, cards);
            var interactions = new List<string>();
            bool drawing = kind != "Discard";
            if (managers.GetSaveManager().GetCollectedRelics().Count > 0) interactions.Add("Relics");
            if (managers.GetSaveManager().GetMutators().Count > 0) interactions.Add("Mutators");
            foreach (string field in new[] { "nonDrawableCards", "nextDrawnTempCardUpgrades" })
                if (drawing && ((ICollection)AccessTools.Field(typeof(CardManager), field).GetValue(cards)).Count > 0)
                    interactions.Add(field);
            var standby = (Dictionary<CardState, RemoveFromStandByCondition>)AccessTools.Field(
                typeof(CardManager), "pileStandBy").GetValue(cards);
            if (kind != "SpellDraw" && standby.Values.Any(condition => condition != null && condition.GetReturnLocation() != CardPile.KeepInStandBy))
                interactions.Add("Cards returning from standby");
            string[] callbacks = kind == "Draw"
                ? new[] { "OnPreDrawingHand", "OnDrawingHand", "OnCardDrawn", "OnDeckShuffled", "IgnoreDraw" }
                : kind == "SpellDraw" ? new[] { "OnCardDrawn", "OnDeckShuffled", "IgnoreDraw" }
                : new[] { "OnCardDiscarded", "GetIsDiscardable" };
            IEnumerable<CardState> inspectedCards = drawing
                ? cards.GetHand().Concat(cards.GetDrawPile()).Concat(cards.GetDiscardPile()) : cards.GetHand();
            foreach (CardState card in inspectedCards)
            {
                foreach (CardTraitState trait in card.GetTraitStates())
                {
                    // SelfPurge acts only when wasPlayed=true; DiscardHand sets it false.
                    if (!(kind == "Discard" && trait is CardTraitSelfPurge) &&
                        callbacks.Any(callback => trait.GetType().GetMethod(callback,
                        BindingFlags.Instance | BindingFlags.Public)?.DeclaringType != typeof(CardTraitState)))
                        interactions.Add(trait.GetType().Name + " " + kind + " callback");
                    if (drawing && trait is CardTraitMagneticState)
                        interactions.Add("Magnetic draw priority");
                    if (kind == "Discard" && (trait is CardTraitEphemeral || trait is CardTraitInfusion ||
                        trait is CardTraitPersistent)) interactions.Add(trait.GetType().Name + " discard routing");
                }
                if (kind == "Discard" && card.GetTriggers().Count > 0) interactions.Add("Card discard triggers");
            }
            RoomManager rooms = managers.GetRoomManager()!;
            for (int index = 0; index < rooms.GetNumRooms(); index++)
            {
                RoomState room = rooms.GetRoom(index);
                var units = new List<CharacterState>();
                room.AddCharactersToList(units, Team.Type.Heroes | Team.Type.Monsters);
                if (room.Attachments.Count > 0 || units.Any(unit => unit.GetRoomStateModifiers().Count > 0))
                    interactions.Add("Room card-manager modifiers");
            }
            if (cards.GetDrawingDeckCardsDisabledThisTurn()) interactions.Add("Deck draw disabled");
            uint[] words = RngCalibration.Words(RandomManager.GetState(RngId.CardDraw));
            return new CardCycleState(state.Hand, state.Draw, state.Discard,
                new UnityRng(words[0], words[1], words[2], words[3]), state.DrawModifier,
                interactions.Distinct().OrderBy(value => value, StringComparer.Ordinal).ToArray(), BonusDrawProbe.Capture(cards, FullBattleTrace.Active!));
        }

        private IEnumerator Wrap(IEnumerator native, CardManager cards, string kind, int handSize)
        {
            FullBattleTrace? trace = FullBattleTrace.Active;
            Record? record = null;
            try
            {
                AllGameManagers? managers = AllGameManagers.Instance;
                if (trace != null && managers != null && !managers.GetSaveManager().PreviewMode &&
                    managers.GetSaveManager().GetGameSequence() == SaveData.GameSequence.InBattle)
                {
                    CardCycleState before = Capture(cards, kind);
                    record = new Record
                    {
                        Index = records.Count, Turn = managers.GetCombatManager()!.GetTurnCount(), Kind = kind,
                        HandSize = handSize, MaxHandSize = cards.GetMaxHandSize(), Before = before,
                        Predicted = kind == "Draw" ? CardCycleModel.DrawHand(before, handSize, cards.GetMaxHandSize())
                            : CardCycleModel.DiscardHand(before)
                    };
                    records.Add(record);
                }
            }
            catch (Exception error) { trace?.CaptureFailure(error); }
            while (native.MoveNext()) yield return native.Current;
            if (record == null) yield break;
            try
            {
                record.Actual = Capture(cards, kind);
                if (record.Predicted.Supported)
                {
                    JToken expected = Comparable(record.Predicted.State!);
                    JToken actual = Comparable(record.Actual);
                    record.Difference = JToken.DeepEquals(expected, actual) ? null : "Card pile, modifier or RNG differs";
                    log.LogInfo("CARD-CYCLE-" + (record.Difference == null ? "MATCH" : "MISMATCH") +
                        " index=" + record.Index + " kind=" + kind + " turn=" + record.Turn);
                }
                else log.LogInfo("CARD-CYCLE-UNSUPPORTED " + record.Predicted.UnsupportedReason);
            }
            catch (Exception error) { trace?.CaptureFailure(error); }
        }

        private static JToken Comparable(CardCycleState state) => JToken.FromObject(new
        { state.Hand, state.Draw, state.Discard, state.Rng, state.DrawModifier, state.BonusDraw });

        private Record? BeginSpellDraw(CardManager cards, int count, CardState? playedCard, CardType cardType)
        {
            AllGameManagers? managers = AllGameManagers.Instance;
            if (playedCard == null || FullBattleTrace.Active == null || managers == null || managers.GetSaveManager().PreviewMode ||
                managers.GetSaveManager().GetGameSequence() != SaveData.GameSequence.InBattle) return null;
            CardCycleState before = Capture(cards, "SpellDraw");
            int id = FullBattleTrace.Active.CardId(playedCard);
            if (cardType != CardType.Invalid) before = new CardCycleState(before.Hand, before.Draw, before.Discard,
                before.Rng, before.DrawModifier, before.ExternalInteractions.Concat(new[] { "Unmodeled typed draw" }).ToArray(), before.BonusDraw);
            var record = new Record { Index = records.Count, Turn = managers.GetCombatManager()!.GetTurnCount(), Kind = "SpellDraw",
                HandSize = count, MaxHandSize = cards.GetMaxHandSize(), PlayedCardId = id, Before = before,
                Predicted = CardCycleModel.DrawCards(before, count, cards.GetMaxHandSize(), id) };
            records.Add(record); return record;
        }

        private void CompleteSpellDraw(CardManager cards, Record? record)
        {
            if (record == null) return;
            record.Actual = Capture(cards, "SpellDraw");
            if (record.Predicted.Supported)
            {
                record.Difference = JToken.DeepEquals(Comparable(record.Predicted.State!), Comparable(record.Actual)) ? null :
                    "Spell draw card piles, modifier or RNG differs";
                log.LogInfo("CARD-CYCLE-" + (record.Difference == null ? "MATCH" : "MISMATCH") + " index=" + record.Index + " kind=SpellDraw");
            }
            else log.LogInfo("CARD-CYCLE-UNSUPPORTED " + record.Predicted.UnsupportedReason);
        }

        internal sealed class Record
        {
            public int Index { get; set; }
            public int Turn { get; set; }
            public string Kind { get; set; } = "";
            public int HandSize { get; set; }
            public int MaxHandSize { get; set; }
            public int PlayedCardId { get; set; }
            public CardCycleState Before { get; set; } = null!;
            public CardCycleResult Predicted { get; set; } = null!;
            public CardCycleState? Actual { get; set; }
            public string? Difference { get; set; }
        }

        [HarmonyPatch(typeof(CardManager), nameof(CardManager.DrawHand))]
        private static class DrawPatch
        {
            private static void Postfix(CardManager __instance, int handSize, ref IEnumerator __result)
            { if (active != null) __result = active.Wrap(__result, __instance, "Draw", handSize); }
        }

        [HarmonyPatch(typeof(CardManager), nameof(CardManager.DrawCards))]
        private static class SpellDrawPatch
        {
            private static void Prefix(CardManager __instance, int cardCount, CardState playedCard, CardType cardType, out Record? __state)
            {
                __state = null;
                try { __state = active?.BeginSpellDraw(__instance, cardCount, playedCard, cardType); }
                catch (Exception error) { FullBattleTrace.Active?.CaptureFailure(error); }
            }
            private static void Postfix(CardManager __instance, Record? __state)
            {
                try { active?.CompleteSpellDraw(__instance, __state); }
                catch (Exception error) { FullBattleTrace.Active?.CaptureFailure(error); }
            }
        }

        [HarmonyPatch(typeof(CardManager), nameof(CardManager.DiscardHand))]
        private static class DiscardPatch
        {
            private static void Postfix(CardManager __instance, ref IEnumerator __result)
            { if (active != null) __result = active.Wrap(__result, __instance, "Discard", 0); }
        }
    }
}
