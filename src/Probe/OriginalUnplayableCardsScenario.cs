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
    internal static class OriginalUnplayableCardsScenario
    {
        internal const string Name = "original-unplayable-cards";
        internal const string ScrapId = "d4f0ff05-1fe1-483c-adda-928a8b05808c";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static int[] NegativeIds { get; private set; } = Array.Empty<int>();
        internal static readonly List<object> Queries = new List<object>();
        internal static bool QueriesUnchanged { get; private set; }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "attackDamage", 60); Set(unit, "health", 200); Set(unit, "size", 1);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState[] created = { Add(steward.GetID()), Add(steward.GetID()), Add("bc4cd099-93df-4771-ae72-4984fc50ab9f") };
            var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); created[2].AddTemporaryTrait(freeze, managers.GetCoreManagers());
            CardState[] negative = { Add(ScrapId), Add("ed50a054-2de3-47b8-8419-204f72e75fbd"), Add("f94572f9-d280-4990-a112-2c94f8fd7ede") };
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            NegativeIds = negative.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            RoomManager rooms = managers.GetRoomManager()!; RoomUI ui = rooms.GetRoomUI(); int selected = rooms.GetSelectedRoom();
            BattleTurnState before = FullBattleTrace.Active!.CaptureDecision(); int frame = UnityEngine.Time.frameCount; JObject rng = RngStates();
            try
            {
                foreach (CardState card in negative)
                {
                    CardTraitState trait = card.GetTraitStates().Single(value => value is CardTraitUnplayable);
                    for (int floor = 0; floor < rooms.GetNumRooms(); floor++)
                    {
                        Set(ui, "selectedRoom", floor);
                        bool canPlay = cards.CanPlayHandCard(card, floor, null!, null!, null!, out var error);
                        bool fromHand = trait.GetIsPlayableFromHand(cards, rooms, floor, out var handError);
                        bool fromPlay = trait.GetIsPlayableFromPlay(cards, rooms, floor, out var playError);
                        Queries.Add(new { Before = before, Action = new PlayCardAction(FullBattleTrace.Active.CardId(card), floor),
                            DataId = card.GetCardDataID(), SourceTraits = CardSourceTraitProbe.Capture(card), CanPlay = canPlay,
                            Error = error.ToString(), FromHand = fromHand, HandError = handError.ToString(), FromPlay = fromPlay,
                            PlayError = playError.ToString(), CanDiscard = card.CanBeDiscarded() });
                    }
                }
            }
            finally { Set(ui, "selectedRoom", selected); }
            QueriesUnchanged = frame == UnityEngine.Time.frameCount && JToken.DeepEquals(rng, RngStates()) &&
                JToken.DeepEquals(JToken.FromObject(before), JToken.FromObject(FullBattleTrace.Active.CaptureDecision()));
            if (!QueriesUnchanged) throw new InvalidOperationException("Unplayable queries changed decision/RNG/frame.");
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-UNPLAYABLE-CARDS-PREPARED three unchanged definitions, twelve restored legality queries, Persistent retention, ordinary discard/redraw and Refine smelt.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids, int scrapId)
        {
            int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || index == 2 && source.Spawn.Turn < 1) return null;
            if (index == 2) return new PlayCardAction(ids[index], 0, recursionSelections: [new CardRecursionSelection(0, [scrapId])]);
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == index);
        }
        [HarmonyPatch(typeof(CardManager), nameof(CardManager.SelectCardFromHand))]
        private static class AutoSelectPatch
        {
            [HarmonyPrefix]
            private static bool Prefix(CardManager __instance, HandSelectionUI.Params parameters, ref IEnumerator __result)
            {
                if (!Enabled || !Prepared) return true;
                __result = Select(__instance, parameters); return false;
            }
        }
        private static IEnumerator Select(CardManager cards, HandSelectionUI.Params parameters)
        {
            CardState selected = cards.GetHand().Single(card => FullBattleTrace.Active!.CardId(card) == NegativeIds[0]);
            if (parameters.filterCallback(selected)) throw new InvalidOperationException("Native smelt rejected original Persistent Scrap Metal.");
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
