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
    internal static class OriginalOpeningDeckSpawnScenario
    {
        internal const string Name = "original-opening-deck-spawn";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        private static bool observing, resetControl;
        private static ApplicationRecord? current;
        internal static readonly List<TestRecord> Tests = new();
        internal static readonly List<ApplicationRecord> Applications = new();
        private static bool Active => Acquired && FullBattleTrace.Active != null && !observing && !AllGameManagers.Instance!.GetSaveManager().PreviewMode;
        internal sealed class TestRecord
        {
            public int EffectIndex { get; set; }
            public bool Isolated { get; set; }
            public int[] CacheBefore { get; set; } = Array.Empty<int>();
            public int[] CacheAfter { get; set; } = Array.Empty<int>();
            public int[] ExpectedCache { get; set; } = Array.Empty<int>();
            public bool Returned { get; set; }
            public bool Expected { get; set; }
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? Predicted { get; set; }
            public TrainCombatState? Actual { get; set; }
            public string? Error { get; set; }
            public string? Difference { get; set; }
            public string? UnsupportedReason { get; set; }
        }
        internal sealed class ApplicationRecord
        {
            public int EffectIndex { get; set; }
            public TrainCombatState Before { get; set; } = null!;
            public BattlePlayRules Definitions { get; set; } = null!;
            public TrainCombatState? Predicted { get; set; }
            public TrainCombatState? Actual { get; set; }
            public bool Completed { get; set; }
            public int Notifications { get; set; }
            public int NonDrawableAfter { get; set; }
            public string? Error { get; set; }
            public string? Difference { get; set; }
            public string? UnsupportedReason { get; set; }
        }
        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData data = save.GetAllGameData();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle) throw new InvalidOperationException("Expected fresh deck-spawn acquisition.");
            CardData steward = save.GetDeckState().Select(item => data.FindCardData(item.GetCardDataID()))
                .First(item => item.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true);
            CardData spell = save.GetDeckState().Select(item => data.FindCardData(item.GetCardDataID())).First(item => item.GetCardType() == CardType.Spell);
            CharacterData unit = steward.GetSpawnCharacterData();
            Set(unit, "health", 100); Set(unit, "attackDamage", 50); Set(unit, "size", 1);
            Set(unit, "triggers", new List<CharacterTriggerData>()); Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            Set(unit, "unitAbility", null!); Set(steward, "cost", 4); Set(steward, "traits", new List<CardTraitData>());
            save.GetDeckState().Clear();
            for (int i = 0; i < 5; i++) save.AddCardToDeck(steward, applyExistingRelicModifiers: false, showAnimation: false, notifyDeckCount: false);
            save.AddCardToDeck(spell, applyExistingRelicModifiers: false, showAnimation: false, notifyDeckCount: false);
            save.AddRelic(data.GetAllCollectableRelicData().Single(item => item.GetID() == RelicOpeningDeckSpawnModel.DataId)); Acquired = true;
            string? error = RelicModel.Validate(RelicProbe.Capture(managers)); if (error != null) throw new InvalidOperationException(error);
            log.LogInfo("ORIGINAL-OPENING-DECK-SPAWN-ACQUIRED unchanged original owner; five authored cost-four Stewards and one non-Monster control.");
        }
        internal static void PrepareDeployment(AllGameManagers managers)
        {
            if (!Acquired || Prepared || managers.GetCombatManager()!.GetTurnCount() != 0) throw new InvalidOperationException("Expected deployment.");
            Prepared = true;
        }
        private static List<CardState> Cache(RelicEffectSpawnUnitFromDeck effect) => (List<CardState>)AccessTools.Field(effect.GetType(), "toProcessCards").GetValue(effect);
        private static int[] CacheIds(RelicEffectSpawnUnitFromDeck effect) => Cache(effect).Select(FullBattleTrace.Active!.CardId).ToArray();
        [HarmonyPatch(typeof(RelicEffectSpawnUnitFromDeck), nameof(RelicEffectSpawnUnitFromDeck.TestEffectStartOfBattle))]
        private static class TestPatch
        {
            private static void Prefix(RelicEffectSpawnUnitFromDeck __instance, out TestRecord? __state)
            {
                __state = null; if (!Active) return; observing = true;
                var data = (RelicEffectData)AccessTools.Field(typeof(RelicEffectBase), "_srcRelicEffectData").GetValue(__instance);
                var row = new TestRecord { EffectIndex = data.GetParamInt() + 1, Isolated = !__instance.SourceRelicState.GetEffects().Contains(__instance) };
                Tests.Add(row); __state = row;
                try
                {
                    row.CacheBefore = CacheIds(__instance); row.Before = FullBattleTrace.Active!.CaptureTrain();
                    TrainCombatResult result = RelicOpeningDeckSpawnModel.Test(row.Before, RelicOpeningDeckSpawnModel.DataId, row.EffectIndex, out bool passed);
                    row.Expected = passed; row.UnsupportedReason = result.UnsupportedReason;
                    if (result.Supported)
                    {
                        row.ExpectedCache = result.State!.Context!.Relics!.Single(item => item.DataId == RelicOpeningDeckSpawnModel.DataId)
                            .ResourceEffects!.Single(item => item.EffectIndex == row.EffectIndex).DeckSpawn!.CachedCardIds.ToArray();
                        row.Predicted = row.Isolated ? row.Before : result.State;
                    }
                }
                catch (Exception error) { row.Error = error.ToString(); }
                finally { observing = false; }
            }
            private static void Postfix(RelicEffectSpawnUnitFromDeck __instance, bool __result, TestRecord? __state)
            {
                if (__state == null) return; observing = true;
                try
                {
                    __state.Returned = __result; __state.CacheAfter = CacheIds(__instance); __state.Actual = FullBattleTrace.Active!.CaptureTrain();
                    if (__result != __state.Expected || !__state.CacheAfter.SequenceEqual(__state.ExpectedCache) ||
                        __state.Predicted != null && !JToken.DeepEquals(JToken.FromObject(__state.Predicted), JToken.FromObject(__state.Actual)))
                        __state.Difference = "Native opening deck-spawn Test/cache/full train differs.";
                }
                catch (Exception error) { __state.Error = error.ToString(); }
                finally { observing = false; }
            }
        }
        [HarmonyPatch(typeof(RelicEffectSpawnUnitFromDeck), nameof(RelicEffectSpawnUnitFromDeck.ApplyEffectStartOfBattle))]
        private static class ApplyPatch
        { private static void Postfix(RelicEffectSpawnUnitFromDeck __instance, ref IEnumerator __result) { if (Active) __result = Observe(__result, __instance); } }
        private static IEnumerator Observe(IEnumerator native, RelicEffectSpawnUnitFromDeck effect)
        {
            var row = new ApplicationRecord { EffectIndex = effect.SourceRelicState.GetEffects().IndexOf(effect) }; Applications.Add(row); observing = true;
            try
            {
                row.Definitions = FullBattleTrace.Active!.CaptureDecision().PlayRules!;
                row.Before = FullBattleTrace.Active.CaptureTrain();
                TrainCombatResult result = RelicOpeningDeckSpawnModel.Apply(row.Before, row.Definitions, RelicOpeningDeckSpawnModel.DataId, row.EffectIndex);
                row.Predicted = result.State; row.UnsupportedReason = result.UnsupportedReason; current = row;
            }
            catch (Exception error) { row.Error = error.ToString(); }
            finally { observing = false; }
            try { while (native.MoveNext()) yield return native.Current; row.Completed = true; }
            finally
            {
                (native as IDisposable)?.Dispose(); observing = true;
                try
                {
                    row.Actual = FullBattleTrace.Active!.CaptureTrain();
                    row.NonDrawableAfter = ((List<CardState>)AccessTools.Field(typeof(CardManager), "nonDrawableCards").GetValue(AllGameManagers.Instance!.GetCardManager())).Count;
                    if (row.Predicted != null && !JToken.DeepEquals(JToken.FromObject(row.Predicted), JToken.FromObject(row.Actual))) row.Difference = "Native opening deck-spawn full train differs.";
                }
                catch (Exception error) { row.Error = error.ToString(); }
                finally { current = null; observing = false; }
            }
        }
        [HarmonyPatch(typeof(CardManager), nameof(CardManager.DrawHand))]
        private static class DrawPatch
        { private static void Postfix(ref IEnumerator __result) { if (Active) __result = AfterDraw(__result); } }
        private static IEnumerator AfterDraw(IEnumerator native)
        {
            try { while (native.MoveNext()) yield return native.Current; }
            finally { (native as IDisposable)?.Dispose(); }
            if (!Active || resetControl || AllGameManagers.Instance!.GetCombatManager()!.GetTurnCount() < 1) yield break;
            AllGameManagers managers = AllGameManagers.Instance;
            if (managers.GetCardManager()!.GetDrawPile().Any(card => card.IsSpawnerCard())) yield break;
            RelicState owner = managers.GetSaveManager().GetCollectedRelics().Single(item => item.GetRelicDataID() == RelicOpeningDeckSpawnModel.DataId);
            CollectableRelicData data = managers.GetSaveManager().GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == RelicOpeningDeckSpawnModel.DataId);
            var isolated = new RelicEffectSpawnUnitFromDeck(); isolated.Initialize(owner, data, data.GetEffects()[1]);
            Cache(isolated).AddRange(Cache((RelicEffectSpawnUnitFromDeck)owner.GetEffects()[1]));
            if (isolated.TestEffectStartOfBattle(new RelicEffectParams(), managers.GetCoreManagers())) throw new InvalidOperationException("Empty draw-pile unit Test returned true.");
            resetControl = true;
        }
        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.NotifyRelicTriggered))]
        private static class NotifyPatch
        { private static void Prefix(RelicState triggeredRelic) { if (current != null && triggeredRelic.GetRelicDataID() == RelicOpeningDeckSpawnModel.DataId) current.Notifications++; } }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
