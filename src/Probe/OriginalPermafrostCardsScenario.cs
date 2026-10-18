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
    internal static class OriginalPermafrostCardsScenario
    {
        internal const string Name = "original-permafrost-cards";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static int JunkId { get; private set; }
        internal static BattleTurnState? Initial { get; private set; }
        internal static bool QueriesUnchanged { get; private set; }
        internal sealed class DrawRecord
        {
            public int CardId { get; set; }
            public bool Control { get; set; }
            public CardInstanceState Before { get; set; } = null!;
            public CardInstanceState? After { get; set; }
        }
        internal static readonly List<DrawRecord> Draws = new List<DrawRecord>();
        private static bool control;
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "attackDamage", 60); Set(unit, "health", 200); Set(unit, "size", 1);
            CharacterData guard = ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(all))
                .Single(character => character.name == "ShieldGuardT1_BasicNonAttack"); Set(guard, "health", 400);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState[] created = { Add(steward.GetID()), Add(steward.GetID()), Add("3ca50b9d-9c52-49c8-884f-f4409a4489c9"),
                Add("561f7a74-93a3-438f-9bfd-fbc84d979bdd"), Add("d4b4e903-0d62-4a3b-bb05-002a358fcba1") };
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            JunkId = FullBattleTrace.Active!.CardId(Add("ed50a054-2de3-47b8-8419-204f72e75fbd"));
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Initial = FullBattleTrace.Active.CaptureDecision();
            int frame = UnityEngine.Time.frameCount; JObject rng = RngStates();
            control = true;
            try
            {
                foreach (CardState card in created.Skip(2))
                for (int repeat = 0; repeat < 2; repeat++)
                    card.GetTraitStates().OfType<CardTraitPermafrost>().Single().OnCardDrawn(card, managers.GetCoreManagers());
            }
            finally { control = false; }
            QueriesUnchanged = frame == UnityEngine.Time.frameCount && JToken.DeepEquals(rng, RngStates()) &&
                JToken.DeepEquals(JToken.FromObject(Initial), JToken.FromObject(FullBattleTrace.Active.CaptureDecision()));
            if (!QueriesUnchanged) throw new InvalidOperationException("Repeated Permafrost draw callbacks changed an already frozen decision/RNG/frame.");
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-PERMAFROST-CARDS-PREPARED original Preserve/Tome/Ice Tornado, automatic Freeze, idempotent callbacks, retention, chosen Junk freeze, Consume and natural redraw/re-freeze.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids, int junkId)
        {
            CombatContext context = source.Spawn.Train.Context!;
            int index = Array.FindIndex(ids, id => context.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0)
            {
                if (context.Statistics!.Value(ids[4], "TimesPlayed", "ThisBattle") >= 2 || source.Spawn.Turn < 2) return null;
                index = 4;
            }
            if (index >= 2 && source.Spawn.Turn < 1) return null;
            if (index == 2) return new PlayCardAction(ids[index], 0, recursionSelections: [new CardRecursionSelection(0, [junkId])]);
            int room = index < 2 ? index : source.Spawn.Train.Rooms.Where(value => value.RoomIndex < 3 &&
                value.Units.Any(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0)).Select(value => value.RoomIndex).DefaultIfEmpty(-1).Min();
            return room < 0 ? null : BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == room);
        }
        [HarmonyPatch(typeof(CardTraitPermafrost), nameof(CardTraitPermafrost.OnCardDrawn))]
        private static class DrawPatch
        {
            private static void Prefix(CardState thisCard, out DrawRecord? __state)
            {
                __state = null;
                if (!Enabled || !Prepared || AllGameManagers.Instance!.GetSaveManager().PreviewMode) return;
                int id = FullBattleTrace.Active!.CardId(thisCard); if (!CardIds.Skip(2).Contains(id)) return;
                __state = new DrawRecord { CardId = id, Control = control, Before = FullBattleTrace.Active.CaptureContext().FindCard(id)! }; Draws.Add(__state);
            }
            private static void Postfix(DrawRecord? __state)
            { if (__state != null) __state.After = FullBattleTrace.Active!.CaptureContext().FindCard(__state.CardId); }
        }
        [HarmonyPatch(typeof(CardManager), nameof(CardManager.SelectCardFromHand))]
        private static class SelectPatch
        {
            private static bool Prefix(CardManager __instance, HandSelectionUI.Params parameters, ref IEnumerator __result)
            {
                if (!Enabled || !Prepared) return true;
                __result = Select(__instance, parameters); return false;
            }
        }
        private static IEnumerator Select(CardManager cards, HandSelectionUI.Params parameters)
        {
            CardState selected = cards.GetHand().Single(card => FullBattleTrace.Active!.CardId(card) == JunkId);
            if (parameters.filterCallback(selected)) throw new InvalidOperationException("Native Preserve rejected unfrozen original Junk.");
            yield return parameters.cardChosenCallback(selected, cards);
        }
        private static JObject RngStates()
        {
            var states = new JObject();
            foreach (RngId id in Enum.GetValues(typeof(RngId))) if (id != RngId.NonDeterministic && id != RngId.Chatter)
                states[id.ToString()] = JToken.FromObject(RngCalibration.Words(RandomManager.GetState(id)));
            return states;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
