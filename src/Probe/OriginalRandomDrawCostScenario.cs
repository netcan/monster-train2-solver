using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalRandomDrawCostScenario
    {
        internal const string Name = "original-random-draw-cost";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static int[] UnitCards { get; private set; } = Array.Empty<int>();
        internal static int[] Carriers { get; private set; } = Array.Empty<int>();
        internal static int Champion { get; private set; }
        internal static readonly List<Record> Records = new List<Record>();
        private static int step;
        private static bool actionMark;
        private static bool control;
        internal static object Evidence => new { OwnerId = RelicRandomDrawCostModel.DataId, UnitCards, Carriers, Champion,
            Roles = new[] { "base-zero", "base-one", "base-three", "base-high", "negative-delta", "positive-delta", "temporary-full", "permanent-full", "X-cost" } };
        internal sealed class Record
        {
            public int CardId { get; set; }
            public bool Control { get; set; }
            public int BaseCost { get; set; }
            public int CostBefore { get; set; }
            public int CostAfter { get; set; }
            public bool Result { get; set; }
            public bool ExpectedResult { get; set; }
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? Predicted { get; set; }
            public TrainCombatState? Actual { get; set; }
            public bool Completed { get; set; }
            public string? Difference { get; set; }
            public string? UnsupportedReason { get; set; }
            public string? Error { get; set; }
        }
        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData data = save.GetAllGameData();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Random draw cost requires pre-battle acquisition.");
            CollectableRelicData owner = data.GetAllCollectableRelicData().Single(item => item.GetID() == RelicRandomDrawCostModel.DataId);
            CardData steward = save.GetDeckState().Select(card => data.FindCardData(card.GetCardDataID()))
                .First(card => card?.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true)!;
            CardData champion = data.FindCardData(save.GetDeckState().First(card => card.IsChampionCard()).GetCardDataID())
                ?? throw new InvalidOperationException("Missing original champion control definition.");
            foreach (CardData card in new[] { steward, champion })
            {
                CharacterData unit = card.GetSpawnCharacterData()!;
                Set(unit, "health", 80); Set(unit, "attackDamage", 30); Set(unit, "size", 1);
                Set(unit, "triggers", new List<CharacterTriggerData>()); Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
                Set(unit, "unitAbility", null!); Set(card, "traits", new List<CardTraitData>());
                Set(card, "cost", card == champion ? 99 : 0);
            }
            save.GetDeckState().Clear();
            foreach (CardData card in new[] { steward, steward, champion }) save.AddCardToDeck(card, applyExistingRelicModifiers: false, showAnimation: false, notifyDeckCount: false);
            save.AddRelic(owner);
            string? error = RelicModel.Validate(new[] { RelicProbe.Capture(managers).Single(item => item.DataId == RelicRandomDrawCostModel.DataId) });
            if (error != null) throw new InvalidOperationException(error);
            Acquired = true; log.LogInfo("ORIGINAL-RANDOM-DRAW-COST-ACQUIRED unchanged biased draw cost and +2 draw.");
        }
        internal static void PrepareDeployment(AllGameManagers managers)
        {
            if (Prepared || managers.GetCombatManager()!.GetTurnCount() != 0) throw new InvalidOperationException("Expected deployment preparation.");
            CardManager cards = managers.GetCardManager()!;
            UnitCards = cards.GetHand().Where(card => !card.IsChampionCard()).Select(FullBattleTrace.Active!.CardId).ToArray();
            CardState champion = cards.GetHand().Single(card => card.IsChampionCard()); Champion = FullBattleTrace.Active!.CardId(champion);
            if (UnitCards.Length != 2) throw new InvalidOperationException("Expected two deployment Stewards.");
            foreach (CardState unit in cards.GetHand().Where(card => !card.IsChampionCard())) Discount(unit, -99, false);
            AllGameData data = managers.GetSaveManager().GetAllGameData();
            CardData template = data.GetAllCardData().First(card => card.GetCardType() == CardType.Spell);
            var carriers = new List<int>(); int[] costs = { 0, 1, 3, 99, 3, 1, 3, 3, 0 };
            for (int i = 0; i < costs.Length; i++)
            {
                CardData card = UnityEngine.Object.Instantiate(template); card.name = "PojuRandomDrawCost" + i;
                Set(card, "id", "bc057003-0000-4000-8000-00000000000" + i); Set(card, "cost", costs[i]);
                Set(card, "costType", i == 8 ? CardData.CostType.ConsumeRemainingEnergy : CardData.CostType.Default);
                Set(card, "targetless", true); Set(card, "targetsRoom", true); Set(card, "isUnitAbility", false); Set(card, "isRoomAbility", false);
                Set(card, "traits", new List<CardTraitData>()); Set(card, "effects", new List<CardEffectData>());
                Set(card, "effectTriggers", new List<CharacterTriggerData>()); Set(card, "triggers", new List<CardTriggerEffectData>());
                Set(card, "startingUpgrades", new List<CardUpgradeData>());
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)).Add(card);
                CardState created = cards.AddNewCard(card, CardPile.DeckPile, false, false, animate: false)!;
                if (i == 4) Discount(created, -2, true);
                if (i == 5) Discount(created, 2, true);
                if (i == 6) Discount(created, -99, false);
                if (i == 7) Discount(created, -99, true);
                carriers.Add(FullBattleTrace.Active!.CardId(created));
            }
            Carriers = carriers.ToArray(); Prepared = true; FullBattleTrace.Active!.InvalidateCardRules();
            var effect = managers.GetSaveManager().GetCollectedRelics().Single(item => item.GetRelicDataID() == RelicRandomDrawCostModel.DataId)
                .GetEffects().OfType<RelicEffectRandomCardCostOnDraw>().Single();
            control = true;
            try
            {
                foreach (CardState? card in new CardState?[] { null, champion })
                    effect.ApplyEffectOnCardDrawn(new CardDrawnRelicEffectParams(new RelicEffectParams { relicManager = managers.GetRelicManager() })
                        { cardState = card!, cardStatistics = managers.GetCardStatistics() }, managers.GetCoreManagers());
            }
            finally { control = false; }
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
        }
        private static void Discount(CardState card, int delta, bool permanent)
        {
            var upgrade = new CardUpgradeState(); upgrade.Setup(); upgrade.SetCostReduction(-delta);
            (permanent ? card.GetCardStateModifiers() : card.GetTemporaryCardStateModifiers()).AddUpgrade(upgrade);
        }
        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (step >= 2) return null;
            PlayCardAction? action = BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == UnitCards[step] && play.RoomIndex == 0);
            if (action == null) throw new InvalidOperationException("Missing controlled random draw-cost deployment " + step);
            step++; actionMark = true; return action;
        }
        internal static bool IsScenarioAction(PlayCardAction action)
        { if (!actionMark) return false; actionMark = false; return true; }
        [HarmonyPatch(typeof(RelicEffectRandomCardCostOnDraw), nameof(RelicEffectRandomCardCostOnDraw.ApplyEffectOnCardDrawn))]
        private static class ApplyPatch
        {
            private static void Prefix(RelicEffectRandomCardCostOnDraw __instance, CardDrawnRelicEffectParams relicEffectParams, out Record? __state)
            {
                __state = null;
                if (!Prepared || FullBattleTrace.Active == null || __instance.SourceRelicState?.GetRelicDataID() != RelicRandomDrawCostModel.DataId) return;
                var row = new Record { Control = control }; Records.Add(row); __state = row;
                try
                {
                    var managers = AllGameManagers.Instance!; CardState? card = relicEffectParams.cardState;
                    row.CardId = card == null ? 0 : FullBattleTrace.Active.CardId(card); row.BaseCost = card?.GetCostWithoutAnyModifications() ?? 0;
                    row.CostBefore = card?.GetCost(managers.GetCardStatistics(), managers.GetMonsterManager(), managers.GetRelicManager()) ?? 0;
                    row.Before = FullBattleTrace.Active.CaptureTrain();
                    var result = RelicRandomDrawCostModel.Apply(row.Before.Context!, row.CardId, out bool passed); row.ExpectedResult = passed;
                    row.UnsupportedReason = result.UnsupportedReason;
                    if (result.Supported) row.Predicted = CardSpellModel.WithContext(row.Before, result.Context);
                }
                catch (Exception error) { row.Error = error.ToString(); }
            }
            private static void Postfix(bool __result, CardDrawnRelicEffectParams relicEffectParams, Record? __state)
            {
                if (__state == null) return;
                try
                {
                    var managers = AllGameManagers.Instance!; __state.Result = __result;
                    __state.CostAfter = relicEffectParams.cardState?.GetCost(managers.GetCardStatistics(), managers.GetMonsterManager(), managers.GetRelicManager()) ?? 0;
                    __state.Actual = FullBattleTrace.Active!.CaptureTrain(); __state.Completed = true;
                    if (__result != __state.ExpectedResult) __state.Difference = "Draw-cost callback result differs.";
                    if (__state.Predicted != null && !JToken.DeepEquals(JToken.FromObject(__state.Predicted), JToken.FromObject(__state.Actual)))
                        __state.Difference = "Draw-cost complete state/RNG differs.";
                }
                catch (Exception error) { __state.Error = error.ToString(); }
            }
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field)!.SetValue(target, value);
    }
}
