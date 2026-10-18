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
    internal static class OriginalGrosMichelScenario
    {
        internal const string Name = "original-gros-michel";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static bool CaptureReady { get; private set; }
        internal static int Seed { get; private set; }
        internal static readonly List<Record> Records = new List<Record>();
        private static bool observing;
        private static Record? active;
        internal sealed class Record
        {
            public string Kind { get; set; } = "";
            public string Effect { get; set; } = "";
            public bool? Returned { get; set; }
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? Predicted { get; set; }
            public TrainCombatState? Actual { get; set; }
            public List<int> Rolls { get; set; } = new List<int>();
            public List<string> Notifications { get; set; } = new List<string>();
            public bool Completed { get; set; }
            public string? Error { get; set; }
            public string? Difference { get; set; }
            public string? UnsupportedReason { get; set; }
        }
        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData data = save.GetAllGameData();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle || save.GetBalatroGrosMichelCombatRoundCount() != 0)
                throw new InvalidOperationException("Expected fresh pre-battle Gros Michel acquisition and saved count zero.");
            CardData[] deck = save.GetDeckState().Select(card => data.FindCardData(card.GetCardDataID())).ToArray();
            CardData steward = deck.First(card => card.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true);
            CardData champion = deck.First(card => card.GetSpawnCharacterData()?.IsChampion() == true);
            CardData spell = deck.First(card => card.GetCardType() == CardType.Spell);
            CharacterData unit = steward.GetSpawnCharacterData();
            Set(unit, "health", 100); Set(unit, "attackDamage", 30); Set(unit, "size", 1);
            Set(unit, "triggers", new List<CharacterTriggerData>()); Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            Set(unit, "unitAbility", null!); Set(steward, "cost", 0);
            save.GetDeckState().Clear();
            foreach (CardData card in new[] { steward, steward, steward, champion, spell })
                save.AddCardToDeck(card, applyExistingRelicModifiers: false, showAnimation: false, notifyDeckCount: false);
            CollectableRelicData owner = data.GetAllCollectableRelicData().Single(item => item.GetID() == RelicEndCombatModel.GrosMichelId);
            CheckOriginal(owner); save.AddRelic(owner); Acquired = true;
            log.LogInfo("ORIGINAL-GROS-MICHEL-ACQUIRED unchanged +10 non-Champion Monster attack, two waiting rounds, then 10% destruction.");
        }
        private static void CheckOriginal(CollectableRelicData owner)
        {
            RelicEffectData[] effects = owner.GetEffects().ToArray();
            if (owner.name != RelicEndCombatModel.GrosMichelKey || owner.DisallowInDeploymentPhase || effects.Length != 3 ||
                effects[0].GetEffectClassName() != RelicModel.ModifyCardAttack || effects[0].GetParamInt() != 10 ||
                effects[0].GetParamInt2() != 0 || effects[0].GetParamFloat() != 0 || effects[0].GetParamCardType() != CardType.Monster ||
                !effects[0].GetParamBool3() || effects[0].GetParamBool() || effects[0].GetParamBool2() || effects[0].GetParamBool4() ||
                effects[0].GetParamString().Length != 0 || effects[0].GetParamCharacterSubtype()?.IsNone != true || effects[0].GetParamCardFilter() != null ||
                effects[1].GetEffectClassName() != RelicEndCombatModel.GrosMichelDestroy || effects[1].GetParamInt() != 10 || effects[1].GetParamInt2() != 2 ||
                effects[2].GetEffectClassName() != RelicModel.BalatroBananaShake || effects[2].GetParamInt() != 0 || effects[2].GetParamInt2() != 2 ||
                effects.Any(effect => effect.GetEffectConditions().Count != 0))
                throw new InvalidOperationException("Original Gros Michel definition changed.");
        }
        internal static void PrepareDeployment(AllGameManagers managers)
        {
            if (Prepared || !Acquired || managers.GetCombatManager()!.GetTurnCount() != 0)
                throw new InvalidOperationException("Expected Gros Michel deployment.");
            CardManager cards = managers.GetCardManager()!;
            CardState[] owned = cards.GetAllCards(new List<CardState>()).ToArray();
            CardState ordinary = owned.First(card => card.GetCardType() == CardType.Monster && !card.IsChampionCard());
            CardState champion = owned.First(card => card.IsChampionCard()); CardState spell = owned.First(card => card.GetCardType() == CardType.Spell);
            CaptureReady = true;
            try
            {
                foreach (CardState card in new[] { ordinary, champion, spell })
                {
                    managers.GetRelicManager().ApplyCardStateModifiers(card, resetTempCardModifiers: false);
                    managers.GetRelicManager().ApplyCardStateModifiers(card);
                }
            }
            finally { CaptureReady = false; }
            foreach (CardState card in owned) if (!cards.GetHand().Contains(card)) cards.DrawSpecificCard(card);
            RandomState random = (RandomState)AccessTools.Field(typeof(RandomManager), "_randomState").GetValue(null);
            HadesRNG battle = random.FindState(RngId.Battle).rng;
            for (int seed = 1; seed < 100000; seed++)
            {
                battle.Init(seed);
                // The isolated natural turn sequence advances Battle RNG once between these callbacks.
                int[] rolls = Enumerable.Range(0, 7).Select(_ => RandomManager.Range(0, 100, RngId.Battle)).ToArray();
                if (rolls[0] < 10 && rolls[2] < 10 && rolls[4] >= 10 && rolls[6] < 10)
                { battle.Init(seed); Seed = seed; break; }
            }
            if (Seed == 0) throw new InvalidOperationException("Missing waiting/miss/hit native Battle RNG seed.");
            Prepared = true; FullBattleTrace.Active!.InvalidateCardRules(); Set(managers.GetCombatManager(), "combatStateChanged", true);
        }
        private static bool Own(IRelicEffect effect) => Enabled &&
            ((RelicState)AccessTools.Field(typeof(RelicEffectBase), "_srcRelicState").GetValue(effect)).GetRelicDataID() == RelicEndCombatModel.GrosMichelId;
        private static Record? Begin(string kind, IRelicEffect effect)
        {
            if (!Prepared || observing || active != null || AllGameManagers.Instance!.GetSaveManager().PreviewMode || !Own(effect)) return null;
            observing = true; var row = new Record { Kind = kind, Effect = effect.GetType().Name }; Records.Add(row);
            try
            {
                row.Before = FullBattleTrace.Active!.CaptureTrain(); row.Predicted = row.Before;
                if (kind == "Apply")
                {
                    CombatContext context = RelicEndCombatModel.Apply(row.Before.Context!, out string? error);
                    row.UnsupportedReason = error; row.Predicted = CardSpellModel.WithContext(row.Before, context);
                }
                active = row;
            }
            catch (Exception error) { row.Error = error.ToString(); }
            finally { observing = false; }
            return row;
        }
        private static void Finish(Record? row, bool? returned = null)
        {
            if (row == null) return; observing = true;
            try
            {
                row.Returned = returned; row.Actual = FullBattleTrace.Active!.CaptureTrain(); row.Completed = true;
                if (row.Kind == "Test" && returned != true || row.Predicted != null && !JToken.DeepEquals(JToken.FromObject(row.Predicted), JToken.FromObject(row.Actual)))
                    row.Difference = "Native Gros Michel result or full state differs.";
            }
            catch (Exception error) { row.Error = error.ToString(); }
            finally { active = null; observing = false; }
        }
        [HarmonyPatch(typeof(GrosMichelRelicEffectChanceToDestroySelf), nameof(GrosMichelRelicEffectChanceToDestroySelf.TestEffectEndOfCombat))]
        private static class TestPatch
        {
            private static void Prefix(GrosMichelRelicEffectChanceToDestroySelf __instance, out Record? __state) => __state = Begin("Test", __instance);
            private static void Postfix(bool __result, Record? __state) => Finish(__state, __result);
        }
        [HarmonyPatch(typeof(GrosMichelRelicEffectChanceToDestroySelf), nameof(GrosMichelRelicEffectChanceToDestroySelf.ApplyEffectEndOfCombat))]
        private static class ApplyPatch
        { private static void Postfix(GrosMichelRelicEffectChanceToDestroySelf __instance, ref IEnumerator __result) => __result = Observe(__result, "Apply", __instance); }
        [HarmonyPatch(typeof(RelicEffectBalatroBananaShake), nameof(RelicEffectBalatroBananaShake.ApplyEffectEndOfCombat))]
        private static class AttemptPatch
        { private static void Postfix(RelicEffectBalatroBananaShake __instance, ref IEnumerator __result) { if (Own(__instance)) __result = Observe(__result, "Attempt", __instance); } }
        private static IEnumerator Observe(IEnumerator native, string kind, IRelicEffect effect)
        { Record? row = Begin(kind, effect); try { while (native.MoveNext()) yield return native.Current; }
            finally { (native as IDisposable)?.Dispose(); Finish(row); } }
        [HarmonyPatch(typeof(RandomManager), nameof(RandomManager.Range), new[] { typeof(int), typeof(int), typeof(RngId) })]
        private static class RangePatch
        { private static void Postfix(int min, int max, RngId rngId, int __result) { if (active != null && !observing && min == 0 && max == 100 && rngId == RngId.Battle) active.Rolls.Add(__result); } }
        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.NotifyRelicTriggered))]
        private static class TriggeredPatch
        { private static void Prefix(RelicState triggeredRelic) { if (active != null && triggeredRelic.GetRelicDataID() == RelicEndCombatModel.GrosMichelId) active.Notifications.Add("Triggered"); } }
        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.NotifyRelicTriggerAttempted))]
        private static class AttemptedPatch
        { private static void Prefix(RelicState triggeredRelic) { if (active != null && triggeredRelic.GetRelicDataID() == RelicEndCombatModel.GrosMichelId) active.Notifications.Add("Attempted"); } }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
