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
    internal static class OriginalRandomFreezeScenario
    {
        internal const string Name = "original-random-freeze";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static int[] UnitCards { get; private set; } = Array.Empty<int>();
        internal static int[] Carriers { get; private set; } = Array.Empty<int>();
        internal static readonly List<TestRecord> Tests = new List<TestRecord>();
        internal static readonly List<ApplicationRecord> Applications = new List<ApplicationRecord>();
        private static int step;
        private static bool actionMark;
        internal static object Evidence => new { OwnerId = RelicRandomFreezeModel.DataId, UnitCards, Carriers,
            Roles = new[] { "eligible-a", "eligible-b", "already-frozen", "Blight", "Junk" } };
        internal sealed class TestRecord
        {
            public int[] Candidates { get; set; } = Array.Empty<int>();
            public bool Result { get; set; }
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? Actual { get; set; }
            public string? Difference { get; set; }
            public string? Error { get; set; }
        }
        internal sealed class ApplicationRecord
        {
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
                throw new InvalidOperationException("Random-freeze owner requires one pre-battle acquisition.");
            CollectableRelicData owner = data.GetAllCollectableRelicData().Single(item => item.GetID() == RelicRandomFreezeModel.DataId);
            RelicEffectData effect = owner.GetEffects().Single();
            if (owner.name != RelicRandomFreezeModel.AssetKey || !owner.DisallowInDeploymentPhase ||
                effect.GetEffectClassName() != RelicRandomFreezeModel.Effect || effect.GetParamInt() != 1 || effect.GetEffectConditions().Count != 0)
                throw new InvalidOperationException("Original FreezeRandomEndOfTurn definition changed.");
            CardData steward = save.GetDeckState().Select(card => data.FindCardData(card.GetCardDataID()))
                .First(card => card?.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true)!;
            CharacterData unit = steward.GetSpawnCharacterData()!;
            Set(unit, "health", 80); Set(unit, "attackDamage", 30); Set(unit, "size", 1);
            Set(unit, "triggers", new List<CharacterTriggerData>()); Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            Set(steward, "cost", 0); save.GetDeckState().Clear();
            for (int i = 0; i < 2; i++) save.AddCardToDeck(steward, applyExistingRelicModifiers: false, showAnimation: false, notifyDeckCount: false);
            save.AddRelic(owner);
            string? error = RelicModel.Validate(new[] { RelicProbe.Capture(managers).Single(item => item.DataId == RelicRandomFreezeModel.DataId) });
            if (error != null) throw new InvalidOperationException(error);
            Acquired = true; log.LogInfo("ORIGINAL-RANDOM-FREEZE-ACQUIRED unchanged one-card end-turn freeze and native hand filter.");
        }
        internal static void PrepareDeployment(AllGameManagers managers)
        {
            if (Prepared || managers.GetCombatManager()!.GetTurnCount() != 0) throw new InvalidOperationException("Expected deployment setup.");
            CardManager cards = managers.GetCardManager()!; AllGameData data = managers.GetSaveManager().GetAllGameData();
            UnitCards = cards.GetHand().Where(card => card.GetCardType() == CardType.Monster).Select(FullBattleTrace.Active!.CardId).ToArray();
            if (UnitCards.Length != 2) throw new InvalidOperationException("Missing controlled Stewards.");
            CardData template = data.GetAllCardData().First(card => card.GetCardType() == CardType.Spell);
            var carriers = new List<int>();
            for (int i = 0; i < 5; i++)
            {
                CardData card = UnityEngine.Object.Instantiate(template); card.name = "PojuRandomFreeze" + i;
                Set(card, "id", "f1ee2e01-0000-4000-8000-00000000000" + i); Set(card, "cost", 99); Set(card, "costType", CardData.CostType.Default);
                Set(card, "cardType", i == 3 ? CardType.Blight : i == 4 ? CardType.Junk : CardType.Spell);
                Set(card, "targetless", true); Set(card, "targetsRoom", true); Set(card, "isUnitAbility", false); Set(card, "isRoomAbility", false);
                var persistent = new CardTraitData(); persistent.Setup("CardTraitPersistent");
                Set(card, "traits", new List<CardTraitData> { persistent }); Set(card, "effects", new List<CardEffectData>());
                Set(card, "effectTriggers", new List<CharacterTriggerData>()); Set(card, "triggers", new List<CardTriggerEffectData>());
                Set(card, "startingUpgrades", new List<CardUpgradeData>());
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)).Add(card);
                CardState created = cards.AddNewCard(card, CardPile.HandPile, false, false, animate: false)!;
                if (i == 2) { var frozen = new CardTraitData(); frozen.Setup("CardTraitFreeze"); cards.AddTemporaryTraitToCard(created, frozen); }
                carriers.Add(FullBattleTrace.Active!.CardId(created));
            }
            Carriers = carriers.ToArray(); FullBattleTrace.Active!.InvalidateCardRules(); Prepared = true;
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
        }
        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (step >= 2) return null;
            PlayCardAction? action = BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == UnitCards[step] && play.RoomIndex == 0);
            if (action != null) { step++; actionMark = true; } return action;
        }
        internal static bool IsScenarioAction(PlayCardAction action)
        { if (!actionMark) return false; actionMark = false; return true; }
        private static bool Active => Acquired && FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode;
        [HarmonyPatch(typeof(RelicEffectFreezeRandomCards), nameof(RelicEffectFreezeRandomCards.TestEffect))]
        private static class TestPatch
        {
            private static void Prefix(out TestRecord? __state)
            {
                __state = null; if (!Active) return; var row = new TestRecord(); __state = row; Tests.Add(row);
                try { var hand = AllGameManagers.Instance!.GetCardManager()!.GetHand(shouldCopy: true); CardEffectFreezeRandomCard.FilterCards(hand);
                    row.Candidates = hand.Select(FullBattleTrace.Active!.CardId).ToArray(); row.Before = FullBattleTrace.Active.CaptureTrain(); }
                catch (Exception error) { row.Error = error.ToString(); }
            }
            private static void Postfix(bool __result, TestRecord? __state)
            {
                if (__state == null) return;
                try { __state.Result = __result; __state.Actual = FullBattleTrace.Active!.CaptureTrain();
                    int[]? predicted = RelicRandomFreezeModel.Candidates(__state.Before.Context!, out string? error);
                    if (error != null || predicted == null || !predicted.SequenceEqual(__state.Candidates) || __result != (predicted.Length > 0) ||
                        !JToken.DeepEquals(JToken.FromObject(__state.Before), JToken.FromObject(__state.Actual)))
                        __state.Difference = "Random-freeze Test/filter/state/RNG differs: " + error; }
                catch (Exception error) { __state.Error = error.ToString(); }
            }
        }
        [HarmonyPatch(typeof(RelicEffectFreezeRandomCards), nameof(RelicEffectFreezeRandomCards.ApplyEffect))]
        private static class ApplyPatch
        {
            private static void Postfix(ref IEnumerator __result) { if (Active) __result = Observe(__result); }
        }
        private static IEnumerator Observe(IEnumerator native)
        {
            var row = new ApplicationRecord(); Applications.Add(row);
            try { row.Before = FullBattleTrace.Active!.CaptureTrain(); var predicted = RelicRandomFreezeModel.ApplySelected(row.Before);
                row.Predicted = predicted.State; row.UnsupportedReason = predicted.UnsupportedReason; }
            catch (Exception error) { row.Error = error.ToString(); }
            try { while (native.MoveNext()) yield return native.Current; row.Completed = true; }
            finally { (native as IDisposable)?.Dispose();
                try { row.Actual = FullBattleTrace.Active!.CaptureTrain();
                    if (row.Predicted != null && !JToken.DeepEquals(JToken.FromObject(row.Predicted), JToken.FromObject(row.Actual)))
                        row.Difference = "Random-freeze shuffle, trait or complete state differs."; }
                catch (Exception error) { row.Error = error.ToString(); }
            }
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field)!.SetValue(target, value);
    }
}
