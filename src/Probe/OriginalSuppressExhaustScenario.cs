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
    internal static class OriginalSuppressExhaustScenario
    {
        internal const string Name = "original-suppress-exhaust";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static int[] Units { get; private set; } = Array.Empty<int>();
        internal static int[] Carriers { get; private set; } = Array.Empty<int>();
        internal static readonly List<Record> Records = new List<Record>();
        private static bool control;
        private static bool actionMark;
        private static int step;
        internal static object Evidence => new { OwnerId = RelicSuppressExhaustModel.DataId, Units, Carriers,
            Roles = new[] { "consume-a", "consume-b", "consume-c", "consume-d", "plain", "persistent-consume", "infusion-consume", "monster-consume" } };
        internal sealed class Record
        {
            public string Phase { get; set; } = "";
            public bool Control { get; set; }
            public int CardId { get; set; }
            public bool WasPlayed { get; set; }
            public string? SuppressionBefore { get; set; }
            public string? SuppressionAfter { get; set; }
            public string? ExpectedSuppression { get; set; }
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
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle) throw new InvalidOperationException("Expected pre-battle suppression acquisition.");
            CardData steward = save.GetDeckState().Select(card => data.FindCardData(card.GetCardDataID()))
                .First(card => card?.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true)!;
            CharacterData unit = steward.GetSpawnCharacterData()!;
            Set(unit, "health", 80); Set(unit, "attackDamage", 30); Set(unit, "size", 1);
            Set(unit, "triggers", new List<CharacterTriggerData>()); Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            Set(unit, "unitAbility", null!); Set(steward, "cost", 0); Set(steward, "traits", new List<CardTraitData>());
            save.GetDeckState().Clear();
            for (int i = 0; i < 2; i++) save.AddCardToDeck(steward, applyExistingRelicModifiers: false, showAnimation: false, notifyDeckCount: false);
            save.AddRelic(data.GetAllCollectableRelicData().Single(item => item.GetID() == RelicSuppressExhaustModel.DataId));
            string? error = RelicModel.Validate(new[] { RelicProbe.Capture(managers).Single(owner => owner.DataId == RelicSuppressExhaustModel.DataId) });
            if (error != null) throw new InvalidOperationException(error);
            Acquired = true; log.LogInfo("ORIGINAL-SUPPRESS-EXHAUST-ACQUIRED unchanged original 50% Spell owner.");
        }
        internal static void PrepareDeployment(AllGameManagers managers)
        {
            if (Prepared || managers.GetCombatManager()!.GetTurnCount() != 0) throw new InvalidOperationException("Expected deployment preparation.");
            CardManager cards = managers.GetCardManager()!; Units = cards.GetHand().Select(FullBattleTrace.Active!.CardId).ToArray();
            if (Units.Length != 2) throw new InvalidOperationException("Expected two authored Stewards.");
            AllGameData data = managers.GetSaveManager().GetAllGameData();
            CardData spell = data.GetAllCardData().First(card => card.GetCardType() == CardType.Spell);
            CardData steward = data.FindCardData(cards.GetHand()[0].GetCardDataID())!; var created = new List<CardState>();
            for (int i = 0; i < 8; i++)
            {
                CardData card = UnityEngine.Object.Instantiate(i == 7 ? steward : spell); card.name = "PojuSuppressExhaust" + i;
                Set(card, "id", "bc057004-0000-4000-8000-00000000000" + i); Set(card, "cost", 0);
                Set(card, "costType", CardData.CostType.Default); Set(card, "targetless", true); Set(card, "targetsRoom", true);
                Set(card, "isUnitAbility", false); Set(card, "isRoomAbility", false);
                var traits = new List<CardTraitData>();
                if (i != 4) { var consume = new CardTraitData(); consume.Setup(RelicSuppressExhaustModel.Trait); traits.Add(consume); }
                if (i == 5 || i == 6) { var retained = new CardTraitData(); retained.Setup(i == 5 ? "CardTraitPersistent" : "CardTraitInfusion"); traits.Add(retained); }
                Set(card, "traits", traits);
                if (i != 7) Set(card, "effects", new List<CardEffectData>());
                Set(card, "triggers", new List<CardTriggerEffectData>()); Set(card, "effectTriggers", new List<CharacterTriggerData>());
                Set(card, "startingUpgrades", new List<CardUpgradeData>());
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)).Add(card);
                CardState state = cards.AddNewCard(card, i < 6 ? CardPile.HandPile : CardPile.DiscardPile, false, false, animate: false)
                    ?? throw new InvalidOperationException("Failed to create suppression control " + i);
                // AddNewCard supports hand/deck/discard; author off-hand controls
                // in the exhausted pile before any recorded battle action.
                if (i >= 6) { cards.GetDiscardPile().Remove(state); cards.GetExhaustedPile().Add(state); }
                created.Add(state);
            }
            Carriers = created.Select(FullBattleTrace.Active!.CardId).ToArray(); Prepared = true; FullBattleTrace.Active!.InvalidateCardRules();
            var effect = managers.GetSaveManager().GetCollectedRelics().Single(owner => owner.GetRelicDataID() == RelicSuppressExhaustModel.DataId)
                .GetEffects().OfType<RelicEffectSuppressExhaust>().Single();
            CardDiscardedRelicEffectParams Params(CardState card, bool played, Type? prior = null) => new CardDiscardedRelicEffectParams(new RelicEffectParams { relicManager = managers.GetRelicManager() })
                { discardCardParams = new CardManager.DiscardCardParams { discardCard = card, wasPlayed = played, outSuppressTraitOnDiscard = prior! } };
            void Hit()
            { for (int i = 0; i < 32; i++) if (effect.TestOnPlayCardPreDiscard(Params(created[0], true))) return; throw new InvalidOperationException("No eligible control hit in 32 draws."); }
            control = true;
            try
            {
                effect.TestOnPlayCardPreDiscard(Params(created[0], false, typeof(CardTraitPersistent)));
                Hit(); var p = Params(created[0], true);
                effect.TestEffectOnCardDiscarded(p, managers.GetCoreManagers()); effect.TestEffectOnCardDiscarded(p, managers.GetCoreManagers());
                foreach (int i in new[] { 7, 4, 5, 6 }) effect.TestOnPlayCardPreDiscard(Params(created[i], true));
                Hit(); p = Params(created[0], true); effect.TestEffectOnCardDiscarded(p, managers.GetCoreManagers());
                IEnumerator apply = effect.ApplyEffectOnCardDiscarded(p, managers.GetCoreManagers());
                try { while (apply.MoveNext()) { } } finally { (apply as IDisposable)?.Dispose(); }
                effect.TestEffectOnCardDiscarded(p, managers.GetCoreManagers());
            }
            finally { control = false; }
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
        }
        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (step >= 8 || step >= 2 && source.Spawn.Turn == 0) return null;
            int id = step < 2 ? Units[step] : Carriers[step - 2];
            PlayCardAction? action = BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == id && play.RoomIndex == 0);
            if (action == null) throw new InvalidOperationException("Missing controlled suppression action " + step);
            step++; actionMark = true; return action;
        }
        internal static bool IsScenarioAction(PlayCardAction action) { if (!actionMark) return false; actionMark = false; return true; }
        private static bool Active(RelicEffectSuppressExhaust effect) => Prepared && FullBattleTrace.Active != null && effect.SourceRelicState?.GetRelicDataID() == RelicSuppressExhaustModel.DataId;
        private static Record Begin(string phase, CardDiscardedRelicEffectParams p)
        {
            var row = new Record { Phase = phase, Control = control, CardId = FullBattleTrace.Active!.CardId(p.discardCardParams.discardCard),
                WasPlayed = p.discardCardParams.wasPlayed, SuppressionBefore = p.discardCardParams.outSuppressTraitOnDiscard?.FullName };
            Records.Add(row);
            try
            {
                row.Before = FullBattleTrace.Active.CaptureTrain(); RelicFirstCardCostResult result;
                string? suppression = row.SuppressionBefore; bool passed = false;
                if (phase == "Pre") result = RelicSuppressExhaustModel.PreDiscard(row.Before.Context!, row.CardId, row.WasPlayed, out passed, out suppression, suppression);
                else if (phase == "Apply") result = RelicSuppressExhaustModel.Apply(row.Before.Context!, out suppression, suppression);
                else { passed = RelicSuppressExhaustModel.Test(row.Before.Context!); result = new RelicFirstCardCostResult(row.Before.Context!); }
                row.ExpectedResult = passed; row.ExpectedSuppression = suppression; row.UnsupportedReason = result.UnsupportedReason;
                if (result.Supported) row.Predicted = CardSpellModel.WithContext(row.Before, result.Context);
            }
            catch (Exception error) { row.Error = error.ToString(); }
            return row;
        }
        private static void Finish(Record row, CardDiscardedRelicEffectParams p)
        {
            try
            {
                row.Actual = FullBattleTrace.Active!.CaptureTrain(); row.SuppressionAfter = p.discardCardParams.outSuppressTraitOnDiscard?.FullName;
                if (row.Phase != "Apply" && row.Result != row.ExpectedResult || row.SuppressionAfter != row.ExpectedSuppression) row.Difference = "Suppression result/output differs.";
                if (row.Predicted != null && !JToken.DeepEquals(JToken.FromObject(row.Predicted), JToken.FromObject(row.Actual))) row.Difference = "Suppression complete state/RNG differs.";
            }
            catch (Exception error) { row.Error = error.ToString(); }
        }
        [HarmonyPatch(typeof(RelicEffectSuppressExhaust), nameof(RelicEffectSuppressExhaust.TestOnPlayCardPreDiscard))]
        private static class PrePatch
        {
            private static void Prefix(RelicEffectSuppressExhaust __instance, CardDiscardedRelicEffectParams relicEffectParams, out Record? __state) => __state = Active(__instance) ? Begin("Pre", relicEffectParams) : null;
            private static void Postfix(bool __result, CardDiscardedRelicEffectParams relicEffectParams, Record? __state)
            { if (__state == null) return; __state.Result = __result; __state.Completed = true; Finish(__state, relicEffectParams); }
        }
        [HarmonyPatch(typeof(RelicEffectSuppressExhaust), nameof(RelicEffectSuppressExhaust.TestEffectOnCardDiscarded))]
        private static class TestPatch
        {
            private static void Prefix(RelicEffectSuppressExhaust __instance, CardDiscardedRelicEffectParams relicEffectParams, out Record? __state) => __state = Active(__instance) ? Begin("Test", relicEffectParams) : null;
            private static void Postfix(bool __result, CardDiscardedRelicEffectParams relicEffectParams, Record? __state)
            { if (__state == null) return; __state.Result = __result; __state.Completed = true; Finish(__state, relicEffectParams); }
        }
        [HarmonyPatch(typeof(RelicEffectSuppressExhaust), nameof(RelicEffectSuppressExhaust.ApplyEffectOnCardDiscarded))]
        private static class ApplyPatch
        {
            private static void Postfix(RelicEffectSuppressExhaust __instance, CardDiscardedRelicEffectParams relicEffectParams, ref IEnumerator __result)
            { if (Active(__instance)) __result = Observe(__result, relicEffectParams); }
        }
        private static IEnumerator Observe(IEnumerator native, CardDiscardedRelicEffectParams p)
        { Record row = Begin("Apply", p); try { while (native.MoveNext()) yield return native.Current; row.Completed = true; }
            finally { (native as IDisposable)?.Dispose(); Finish(row, p); } }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field)!.SetValue(target, value);
    }
}
