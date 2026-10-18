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
    internal static class OriginalPileRecursionScenario
    {
        internal const string Name = "original-pile-recursion";
        internal const string CardId = "1b7663f2-c34d-4b06-a959-f2b4e2f5b567";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static bool QueriesUnchanged { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static readonly List<object> Queries = new List<object>();
        internal sealed class RecursionSample
        {
            public int CardId { get; set; }
            public CombatContext Before { get; set; } = null!;
            public CombatContext? After { get; set; }
            public List<int> NativeBonuses { get; } = new List<int>();
            public bool Completed { get; set; }
        }
        internal static readonly List<RecursionSample> Samples = new List<RecursionSample>();
        private static RecursionSample? current;
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "attackDamage", 60); Set(unit, "health", 500); Set(unit, "size", 1);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            CardData blank = UnityEngine.Object.Instantiate(all.GetAllCardData().First(card => card.GetCardType() == CardType.Spell));
            blank.name = "PojuPileRecursionBlank"; Set(blank, "id", "ac110026-0000-4000-8000-000000000001"); Set(blank, "cost", 0);
            Set(blank, "costType", CardData.CostType.Default); Set(blank, "targetless", true); Set(blank, "targetsRoom", true);
            Set(blank, "traits", new List<CardTraitData>()); Set(blank, "startingUpgrades", new List<CardUpgradeData>());
            Set(blank, "triggers", new List<CardTriggerEffectData>()); Set(blank, "effectTriggers", new List<CharacterTriggerData>());
            var empty = new CardEffectData("CardEffectNULL", null!, Team.Type.None); empty.Cheat_SetTargetMode(TargetMode.Room); Set(empty, "paramBool2", true);
            Set(blank, "effects", new List<CardEffectData> { empty });
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(blank);
            for (int index = 0; index < 5; index++)
            {
                CardState candidate = cards.AddNewCard(index < 3 ? blank : steward, CardPile.HandPile, false, false, animate: false);
                cards.MoveToStandByPile(candidate, wasPlayed: false, wasExhausted: true, condition: new RemoveFromStandByCondition(() => CardPile.KeepInStandBy));
            }
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState[] created = { Add(steward.GetID()), Add(steward.GetID()), Add(CardId), Add(CardId) };
            for (int index = 0; index < 5; index++) cards.AddNewCard(blank, CardPile.DeckPile, false, false, animate: false);
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            FullBattleTrace trace = FullBattleTrace.Active!; trace.CaptureDecision();
            List<CardState> exhausted = cards.GetExhaustedPile(), draw = cards.GetDrawPile();
            var standby = (Dictionary<CardState, RemoveFromStandByCondition>)AccessTools.Field(typeof(CardManager), "pileStandBy").GetValue(cards);
            KeyValuePair<CardState, RemoveFromStandByCondition>[] originalStandby = standby.ToArray();
            CardState[] originalExhausted = exhausted.ToArray(), originalDraw = draw.ToArray(), pool = originalExhausted.Concat(originalDraw).ToArray();
            JToken before = JToken.FromObject(trace.CaptureDecision()); JObject rng = RngStates(); int frame = UnityEngine.Time.frameCount;
            try
            {
                foreach (int count in new[] { 0, 4, 5, 10 })
                {
                    exhausted.Clear(); draw.Clear(); exhausted.AddRange(pool.Take(count)); draw.AddRange(pool.Skip(count));
                    standby.Clear(); foreach (var pair in originalStandby) if (exhausted.Contains(pair.Key)) standby.Add(pair.Key, pair.Value);
                    CombatContext context = trace.CaptureContext();
                    int actual = created[2].GetTraitStates().OfType<CardTraitScalingCardDrawCountPerPile>().Single().GetPileCountModifier(cards, save);
                    Queries.Add(new { CardId = trace.CardId(created[2]), ExhaustedCount = count, ActualBonus = actual, Context = context,
                        StateUnchanged = frame == UnityEngine.Time.frameCount && JToken.DeepEquals(JToken.FromObject(context), JToken.FromObject(trace.CaptureContext())) });
                }
            }
            finally
            {
                exhausted.Clear(); exhausted.AddRange(originalExhausted); draw.Clear(); draw.AddRange(originalDraw);
                standby.Clear(); foreach (var pair in originalStandby) standby.Add(pair.Key, pair.Value);
            }
            QueriesUnchanged = frame == UnityEngine.Time.frameCount && JToken.DeepEquals(rng, RngStates()) && JToken.DeepEquals(before, JToken.FromObject(trace.CaptureDecision()));
            if (!QueriesUnchanged) throw new InvalidOperationException("Pile recursion queries failed to restore source piles, decision, RNG or frame.");
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-PILE-RECURSION-PREPARED original Unearth Treasure, five total exhausted/three eligible spells, positive then below-threshold random recursion, Consume/source-filter exclusion and zero/four/five/ten count controls.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0) return null;
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == (index == 1 ? 1 : 0));
        }
        [HarmonyPatch(typeof(CardEffectRecursion), nameof(CardEffectRecursion.ApplyEffect))]
        private static class ApplyPatch
        {
            private static void Postfix(CardEffectParams cardEffectParams, ref IEnumerator __result)
            {
                if (Enabled && Prepared && !AllGameManagers.Instance!.GetSaveManager().PreviewMode && cardEffectParams.playedCard?.GetCardDataID() == CardId)
                    __result = Observe(__result, cardEffectParams.playedCard);
            }
        }
        private static IEnumerator Observe(IEnumerator native, CardState card)
        {
            FullBattleTrace trace = FullBattleTrace.Active!; RecursionSample? parent = current;
            var row = new RecursionSample { CardId = trace.CardId(card), Before = trace.CaptureContext() };
            Samples.Add(row); current = row;
            try { while (native.MoveNext()) yield return native.Current; row.Completed = true; }
            finally { (native as IDisposable)?.Dispose(); current = parent; row.After = trace.CaptureContext(); }
        }
        [HarmonyPatch(typeof(CardTraitScalingModifierPerPile), nameof(CardTraitScalingModifierPerPile.GetPileCountModifier))]
        private static class BonusPatch
        {
            private static void Postfix(CardTraitScalingModifierPerPile __instance, int __result)
            { if (current != null && __instance.GetCard()?.GetCardDataID() == CardId) current.NativeBonuses.Add(__result); }
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
