using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalPileDrawScenario
    {
        internal const string Name = "original-pile-draw";
        internal const string CardId = "eb7d86ac-26c0-4099-b101-26e0a787806c";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static bool QueriesUnchanged { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static readonly List<object> Queries = new List<object>();
        internal static readonly List<object> Adjustments = new List<object>();
        private sealed class AdjustmentSample
        {
            public int CardId { get; set; }
            public int InputAmount { get; set; }
            public int ActualAmount { get; set; }
            public CombatContext Before { get; set; } = null!;
            public CombatContext? After { get; set; }
        }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "attackDamage", 60); Set(unit, "health", 500); Set(unit, "size", 1);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            while (save.GetVisibleDeckCount() < 30) save.AddCardToDeck(steward, applyExistingRelicModifiers: false, showAnimation: false, notifyDeckCount: false);
            if (save.GetVisibleDeckCount() != 30) throw new InvalidOperationException("Pile draw fixture requires thirty visible permanent cards.");
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            CardData blank = UnityEngine.Object.Instantiate(all.GetAllCardData().First(card => card.GetCardType() == CardType.Spell));
            blank.name = "PojuPileDrawBlank"; Set(blank, "id", "ac110025-0000-4000-8000-000000000001"); Set(blank, "cost", 0);
            Set(blank, "costType", CardData.CostType.Default); Set(blank, "targetless", true); Set(blank, "targetsRoom", true);
            Set(blank, "traits", new List<CardTraitData>()); Set(blank, "startingUpgrades", new List<CardUpgradeData>());
            Set(blank, "triggers", new List<CardTriggerEffectData>()); Set(blank, "effectTriggers", new List<CharacterTriggerData>());
            var empty = new CardEffectData("CardEffectNULL", null!, Team.Type.None); empty.Cheat_SetTargetMode(TargetMode.Room); Set(empty, "paramBool2", true);
            Set(blank, "effects", new List<CardEffectData> { empty });
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(blank);
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState[] created = { Add(steward.GetID()), Add(steward.GetID()), Add(CardId) };
            var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); created[2].AddTemporaryTrait(freeze, managers.GetCoreManagers());
            for (int index = 0; index < 8; index++) cards.AddNewCard(blank, CardPile.DeckPile, false, false, animate: false);
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            FullBattleTrace trace = FullBattleTrace.Active!; trace.CaptureDecision();
            List<CardState> deck = save.GetDeckState(); CardState[] original = deck.ToArray();
            JToken before = JToken.FromObject(trace.CaptureDecision()); JObject rng = RngStates(); int frame = UnityEngine.Time.frameCount;
            try
            {
                foreach (int count in new[] { 0, 14, 15, 29, 30 })
                {
                    deck.Clear(); deck.AddRange(original.Take(count));
                    CombatContext context = trace.CaptureContext();
                    int actual = created[2].GetTraitStates().OfType<CardTraitScalingCardDrawCountPerPile>().Single().GetPileCountModifier(cards, save);
                    Queries.Add(new { CardId = trace.CardId(created[2]), VisibleDeckCount = save.GetVisibleDeckCount(), ActualBonus = actual, Context = context,
                        StateUnchanged = frame == UnityEngine.Time.frameCount && JToken.DeepEquals(JToken.FromObject(context), JToken.FromObject(trace.CaptureContext())) });
                }
            }
            finally { deck.Clear(); deck.AddRange(original); }
            QueriesUnchanged = frame == UnityEngine.Time.frameCount && JToken.DeepEquals(rng, RngStates()) && JToken.DeepEquals(before, JToken.FromObject(trace.CaptureDecision()));
            if (!QueriesUnchanged) throw new InvalidOperationException("Pile draw queries failed to restore permanent deck, decision, RNG or frame.");
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-PILE-DRAW-PREPARED original Mulch, visible save-deck thresholds zero/fourteen/fifteen/twenty-nine/thirty, distinct runtime draw pile, Consume and next-turn eight-card draw/reset.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || index == 2 && source.Spawn.Turn < 1) return null;
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == (index == 1 ? 1 : 0));
        }
        [HarmonyPatch(typeof(CardManager), nameof(CardManager.AdjustDrawCountModifier))]
        private static class AdjustPatch
        {
            private static void Prefix(int amount, CardState fromPlayedCard, out AdjustmentSample? __state)
            { __state = Enabled && Prepared && !AllGameManagers.Instance!.GetSaveManager().PreviewMode && fromPlayedCard?.GetCardDataID() == CardId ?
                new AdjustmentSample { CardId = FullBattleTrace.Active!.CardId(fromPlayedCard), InputAmount = amount, Before = FullBattleTrace.Active.CaptureContext() } : null; }
            private static void Postfix(int __result, AdjustmentSample? __state)
            {
                if (__state != null) { __state.ActualAmount = __result; __state.After = FullBattleTrace.Active!.CaptureContext(); Adjustments.Add(__state); }
            }
        }
        private static JObject RngStates()
        {
            var result = new JObject();
            foreach (RngId id in Enum.GetValues(typeof(RngId))) if (id != RngId.NonDeterministic && id != RngId.Chatter)
                result[id.ToString()] = JToken.FromObject(RngCalibration.Words(RandomManager.GetState(id)));
            return result;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
