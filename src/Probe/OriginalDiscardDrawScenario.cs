using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalDiscardDrawScenario
    {
        internal const string Name = "original-discard-draw";
        internal const string CardId = "a6a701f6-cb73-4880-b9b3-ed1abb3b23d4";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static readonly List<object> Counts = new List<object>();
        internal sealed class DiscardSample
        {
            public int CardId { get; set; }
            public bool WasPlayed { get; set; }
            public CombatContext Before { get; set; } = null!;
            public CombatContext? After { get; set; }
            public bool Completed { get; set; }
        }
        internal static readonly List<DiscardSample> Discards = new List<DiscardSample>();
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            Set(all.GetBalanceData(), "maxHandSize", 30);
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "attackDamage", 60); Set(unit, "health", 500); Set(unit, "size", 1);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            List<CardData> data = (List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all);
            CardData Authored(string suffix, string effectType, int amount, int cost = 0)
            {
                CardData card = UnityEngine.Object.Instantiate(all.GetAllCardData().First(item => item.GetCardType() == CardType.Spell));
                card.name = "PojuDiscardDraw" + suffix; Set(card, "id", "ac110034-0000-4000-8000-00000000000" + suffix);
                Set(card, "cost", cost); Set(card, "costType", CardData.CostType.Default); Set(card, "targetless", true); Set(card, "targetsRoom", true);
                Set(card, "traits", new List<CardTraitData>()); Set(card, "startingUpgrades", new List<CardUpgradeData>());
                Set(card, "triggers", new List<CardTriggerEffectData>()); Set(card, "effectTriggers", new List<CharacterTriggerData>());
                var effect = new CardEffectData(effectType, null!, Team.Type.None); effect.Cheat_SetTargetMode(TargetMode.Room); Set(effect, "paramInt", amount);
                if (effectType == "CardEffectNULL") Set(effect, "paramBool2", true);
                if (effectType == "CardEffectDrawType") Set(effect, "targetCardType", CardType.Monster);
                Set(card, "effects", new List<CardEffectData> { effect }); data.Add(card); return card;
            }
            CardData battery = Authored("1", "CardEffectGainEnergy", 7), draw = Authored("2", "CardEffectDraw", 1),
                typed = Authored("3", "CardEffectDrawType", 1), blank = Authored("4", "CardEffectNULL", 0, 4), spend = Authored("5", "CardEffectNULL", 0, 3);
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState[] created = { Add(steward.GetID()), Add(steward.GetID()), Add(CardId), Add(CardId), Add(draw.GetID()), Add(typed.GetID()),
                Add(spend.GetID()), Add(CardId), Add(draw.GetID()), Add(battery.GetID()), Add(CardId), Add(draw.GetID()) };
            foreach (CardState card in created.Skip(3)) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            for (int i = 0; i < 6; i++) cards.AddNewCard(steward, CardPile.DeckPile, false, false, animate: false);
            for (int i = 0; i < 40; i++) cards.AddNewCard(blank, CardPile.DeckPile, false, false, animate: false);
            CardState[] ordered = cards.GetDrawPile().OrderBy(card => card.GetCardType() == CardType.Monster ? 0 : 1).ToArray();
            cards.GetDrawPile().Clear(); cards.GetDrawPile().AddRange(ordered);
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-DISCARD-DRAW-PREPARED original Rail Spike X three/zero/ten, natural discard, minus-one cost/plus-one X recipients, blocked ordinary/typed draws and next-turn reset; authored max hand thirty.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            int index = Enumerable.Range(0, ids.Length).Where(i => i != 2 && source.Spawn.Train.Context!.Statistics!.Value(ids[i], "TimesPlayed", "ThisBattle") == 0).DefaultIfEmpty(-1).First();
            if (index < 0 || index >= 3 && source.Spawn.Turn < (index < 6 ? 1 : index < 9 ? 2 : 3)) return null;
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == (index == 1 ? 1 : 0) && play.TargetUnitId == 0);
        }
        [HarmonyPatch(typeof(CardTraitScalingAddCards), "GetAdditionalCards")]
        private static class CountPatch
        {
            private static void Postfix(CardTraitScalingAddCards __instance, bool forPreviewText, int __result)
            {
                if (Enabled && Prepared && !forPreviewText && !AllGameManagers.Instance!.GetSaveManager().PreviewMode)
                    Counts.Add(new { CardId = FullBattleTrace.Active!.CardId(__instance.GetCard()), Actual = __result, Context = FullBattleTrace.Active.CaptureContext() });
            }
        }
        [HarmonyPatch(typeof(CardTraitScalingAddCards), nameof(CardTraitScalingAddCards.OnCardDiscarded))]
        private static class DiscardPatch
        {
            private static void Postfix(CardManager.DiscardCardParams discardCardParams, ref IEnumerator __result)
            {
                if (Enabled && Prepared && !AllGameManagers.Instance!.GetSaveManager().PreviewMode)
                    __result = Observe(__result, discardCardParams.discardCard, discardCardParams.wasPlayed);
            }
        }
        private static IEnumerator Observe(IEnumerator native, CardState card, bool played)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            var row = new DiscardSample { CardId = trace.CardId(card), WasPlayed = played, Before = trace.CaptureContext() }; Discards.Add(row);
            try { while (native.MoveNext()) yield return native.Current; row.Completed = true; }
            finally { (native as IDisposable)?.Dispose(); row.After = trace.CaptureContext(); }
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
