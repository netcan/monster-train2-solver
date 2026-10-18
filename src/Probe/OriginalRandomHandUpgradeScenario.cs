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
    internal static class OriginalRandomHandUpgradeScenario
    {
        internal const string Name = "original-random-hand-upgrade";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static bool CombatPrepared { get; private set; }
        internal static int[] UnitCards { get; private set; } = Array.Empty<int>();
        internal static int[] Carriers { get; private set; } = Array.Empty<int>();
        internal static readonly List<Record> Records = new List<Record>();
        private static int step;
        private static bool actionMark;
        internal static object Evidence => new { OwnerId = RelicRandomHandUpgradeModel.DataId, UnitCards, Carriers,
            Roles = new[] { "eligible-a", "eligible-b", "zero-cost", "X-cost", "unplayable" } };
        internal sealed class Record
        {
            public string Phase { get; set; } = "";
            public int CardId { get; set; }
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
                throw new InvalidOperationException("Random hand upgrade requires pre-battle acquisition.");
            CollectableRelicData owner = data.GetAllCollectableRelicData().Single(item => item.GetID() == RelicRandomHandUpgradeModel.DataId);
            CardData steward = save.GetDeckState().Select(card => data.FindCardData(card.GetCardDataID()))
                .First(card => card?.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true)!;
            CharacterData unit = steward.GetSpawnCharacterData()!;
            Set(unit, "health", 80); Set(unit, "attackDamage", 30); Set(unit, "size", 1);
            Set(unit, "triggers", new List<CharacterTriggerData>()); Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            Set(steward, "cost", 0); save.GetDeckState().Clear();
            for (int i = 0; i < 5; i++) save.AddCardToDeck(steward, applyExistingRelicModifiers: false, showAnimation: false, notifyDeckCount: false);
            save.AddRelic(owner);
            string? error = RelicModel.Validate(new[] { RelicProbe.Capture(managers).Single(item => item.DataId == RelicRandomHandUpgradeModel.DataId) });
            if (error != null) throw new InvalidOperationException(error);
            Acquired = true; log.LogInfo("ORIGINAL-RANDOM-HAND-UPGRADE-ACQUIRED unchanged owner, filters and FreeFromBlessing.");
        }
        internal static void Prepare(AllGameManagers managers)
        {
            CardManager cards = managers.GetCardManager()!;
            if (!Prepared)
            {
                UnitCards = cards.GetHand().Where(card => card.GetCardType() == CardType.Monster).Select(FullBattleTrace.Active!.CardId).ToArray();
                if (UnitCards.Length != 5) throw new InvalidOperationException("Expected all five controlled Stewards.");
                Prepared = true;
            }
            if (CombatPrepared) return;
            if (managers.GetCombatManager()!.GetTurnCount() != 0) throw new InvalidOperationException("Hand controls must be authored before the first recorded turn.");
            AllGameData data = managers.GetSaveManager().GetAllGameData();
            CardData template = data.GetAllCardData().First(card => card.GetCardType() == CardType.Spell);
            var carriers = new List<int>();
            for (int i = 0; i < 5; i++)
            {
                CardData card = UnityEngine.Object.Instantiate(template); card.name = "PojuRandomHandUpgrade" + i;
                Set(card, "id", "bc057001-0000-4000-8000-00000000000" + i); Set(card, "cost", i is 2 or 3 ? 0 : 99);
                Set(card, "costType", i == 3 ? CardData.CostType.ConsumeRemainingEnergy : CardData.CostType.Default);
                Set(card, "targetless", true); Set(card, "targetsRoom", true); Set(card, "isUnitAbility", false); Set(card, "isRoomAbility", false);
                var traits = new List<CardTraitData>();
                if (i >= 2) { var persistent = new CardTraitData(); persistent.Setup("CardTraitPersistent"); traits.Add(persistent); }
                if (i == 4) { var unplayable = new CardTraitData(); unplayable.Setup("CardTraitUnplayable"); traits.Add(unplayable); }
                Set(card, "traits", traits); Set(card, "effects", new List<CardEffectData>());
                Set(card, "effectTriggers", new List<CharacterTriggerData>()); Set(card, "triggers", new List<CardTriggerEffectData>());
                Set(card, "startingUpgrades", new List<CardUpgradeData>());
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)).Add(card);
                CardState created = cards.AddNewCard(card, CardPile.HandPile, false, false, animate: false)!;
                carriers.Add(FullBattleTrace.Active!.CardId(created));
            }
            Carriers = carriers.ToArray(); CombatPrepared = true; FullBattleTrace.Active!.InvalidateCardRules();
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
        }
        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (step >= 6 || source.Spawn.Turn == 0 && step >= 2) return null;
            if (source.Spawn.Turn > 0 && !CombatPrepared) throw new InvalidOperationException("Missing active hand controls.");
            PlayCardAction? action;
            if (step < 5) action = BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == UnitCards[step] && play.RoomIndex == (step < 2 ? 0 : 1));
            else action = BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => Carriers.Take(2).Contains(play.CardInstanceId) && play.RoomIndex == 0);
            if (action != null) { step++; actionMark = true; } return action;
        }
        internal static bool IsScenarioAction(PlayCardAction action)
        { if (!actionMark) return false; actionMark = false; return true; }
        private static bool Active => Acquired && FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode;
        private static Record Begin(string phase, CardState card)
        {
            var row = new Record { Phase = phase }; Records.Add(row);
            try
            {
                row.CardId = FullBattleTrace.Active!.CardId(card); row.Before = FullBattleTrace.Active.CaptureTrain();
                CombatContext context = row.Before.Context!; RelicCheaperSpellUpgradeResult result;
                if (phase == "TestPlay") { result = RelicRandomHandUpgradeModel.Test(context, row.CardId, out bool passed); row.ExpectedResult = passed; }
                else if (phase == "ApplyPlay") result = RelicRandomHandUpgradeModel.Apply(context);
                else if (phase == "ApplyDiscard") result = RelicRandomHandUpgradeModel.Discarded(context, row.CardId);
                else { row.ExpectedResult = context.Relics!.Single(owner => owner.DataId == RelicRandomHandUpgradeModel.DataId)
                    .CardModifiers!.Single().RandomHandUpgrade!.UpgradedCardIds.Contains(row.CardId); result = new RelicCheaperSpellUpgradeResult(context); }
                row.UnsupportedReason = result.UnsupportedReason;
                if (result.Supported) row.Predicted = CardSpellModel.WithContext(row.Before, result.Context);
            }
            catch (Exception error) { row.Error = error.ToString(); }
            return row;
        }
        private static void Finish(Record row)
        {
            try { row.Actual = FullBattleTrace.Active!.CaptureTrain();
                if (row.Predicted != null && !JToken.DeepEquals(JToken.FromObject(row.Predicted), JToken.FromObject(row.Actual))) row.Difference = "Random hand upgrade state/RNG differs.";
                if (row.Phase.StartsWith("Test", StringComparison.Ordinal) && row.Result != row.ExpectedResult) row.Difference = "Random hand upgrade Test result differs."; }
            catch (Exception error) { row.Error = error.ToString(); }
        }
        [HarmonyPatch(typeof(RelicEffectAddTempUpgradeToRandomCardInHand), nameof(RelicEffectAddTempUpgradeToRandomCardInHand.TestEffectOnCardPlayed))]
        private static class TestPlayPatch
        {
            private static void Prefix(CardPlayedRelicEffectParams relicEffectParams, out Record? __state) => __state = Active ? Begin("TestPlay", relicEffectParams.cardState) : null;
            private static void Postfix(bool __result, Record? __state) { if (__state == null) return; __state.Result = __result; __state.Completed = true; Finish(__state); }
        }
        [HarmonyPatch(typeof(RelicEffectAddTempUpgradeToRandomCardInHand), nameof(RelicEffectAddTempUpgradeToRandomCardInHand.TestEffectOnCardDiscarded))]
        private static class TestDiscardPatch
        {
            private static void Prefix(CardDiscardedRelicEffectParams relicEffectParams, out Record? __state) => __state = Active ? Begin("TestDiscard", relicEffectParams.discardCardParams.discardCard) : null;
            private static void Postfix(bool __result, Record? __state) { if (__state == null) return; __state.Result = __result; __state.Completed = true; Finish(__state); }
        }
        [HarmonyPatch(typeof(RelicEffectAddTempUpgradeToRandomCardInHand), nameof(RelicEffectAddTempUpgradeToRandomCardInHand.ApplyEffectOnCardPlayed))]
        private static class ApplyPlayPatch
        {
            private static void Postfix(CardPlayedRelicEffectParams relicEffectParams, ref IEnumerator __result)
            { if (Active) __result = Observe(__result, "ApplyPlay", relicEffectParams.cardState); }
        }
        [HarmonyPatch(typeof(RelicEffectAddTempUpgradeToRandomCardInHand), nameof(RelicEffectAddTempUpgradeToRandomCardInHand.ApplyEffectOnCardDiscarded))]
        private static class ApplyDiscardPatch
        {
            private static void Postfix(CardDiscardedRelicEffectParams relicEffectParams, ref IEnumerator __result)
            { if (Active) __result = Observe(__result, "ApplyDiscard", relicEffectParams.discardCardParams.discardCard); }
        }
        private static IEnumerator Observe(IEnumerator native, string phase, CardState card)
        {
            Record row = Begin(phase, card);
            try { while (native.MoveNext()) yield return native.Current; row.Completed = true; }
            finally { (native as IDisposable)?.Dispose(); Finish(row); }
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field)!.SetValue(target, value);
    }
}
