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
    internal static class OriginalStartTurnPropagationScenario
    {
        internal const string Name = "original-start-turn-propagation";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static int[] UnitCards { get; private set; } = Array.Empty<int>();
        internal static readonly List<TestRecord> Tests = new();
        internal static readonly List<ApplicationRecord> Applications = new();
        private static int step;
        private static bool actionMark;
        private static bool emptyControl;
        internal static object Evidence => new { OwnerId = RelicStartTurnPropagationModel.DataId, UnitCards,
            Statuses = new[] { "armor", "regen", "spikes", "spell weakness", "immobile" } };
        internal sealed class TestRecord
        {
            public bool EmptyControl { get; set; }
            public bool Result { get; set; }
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? Actual { get; set; }
            public string? Difference { get; set; }
            public string? Error { get; set; }
        }
        internal sealed class ApplicationRecord
        {
            public int Turn { get; set; }
            public BattlePlayRules Rules { get; set; } = null!;
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? Predicted { get; set; }
            public TrainCombatState? Actual { get; set; }
            public bool Completed { get; set; }
            public string? UnsupportedReason { get; set; }
            public string? Difference { get; set; }
            public string? Error { get; set; }
        }
        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData data = save.GetAllGameData();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Start-turn propagation requires pre-battle acquisition.");
            CollectableRelicData owner = data.GetAllCollectableRelicData().Single(item => item.GetID() == RelicStartTurnPropagationModel.DataId);
            CardData steward = save.GetDeckState().Select(card => data.FindCardData(card.GetCardDataID()))
                .First(card => card?.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true)!;
            CharacterData unit = steward.GetSpawnCharacterData()!;
            Set(unit, "health", 80); Set(unit, "attackDamage", 30); Set(unit, "size", 1); Set(unit, "triggers", new List<CharacterTriggerData>());
            Set(unit, "startingStatusEffects", new[] { new StatusEffectStackData { statusId = "armor", count = 4 },
                new StatusEffectStackData { statusId = "regen", count = 2 }, new StatusEffectStackData { statusId = "spikes", count = 2 },
                new StatusEffectStackData { statusId = "spell weakness", count = 2 }, new StatusEffectStackData { statusId = "immobile", count = 1 } });
            Set(steward, "cost", 0); save.GetDeckState().Clear();
            for (int i = 0; i < 2; i++) save.AddCardToDeck(steward, applyExistingRelicModifiers: false, showAnimation: false, notifyDeckCount: false);
            save.AddRelic(owner);
            string? error = RelicModel.Validate(new[] { RelicProbe.Capture(managers).Single(item => item.DataId == RelicStartTurnPropagationModel.DataId) });
            if (error != null) throw new InvalidOperationException(error);
            Acquired = true; log.LogInfo("ORIGINAL-START-TURN-PROPAGATION-ACQUIRED unchanged all-room +1 propagation.");
        }
        internal static void PrepareDeployment(AllGameManagers managers)
        {
            if (Prepared || managers.GetCombatManager()!.GetTurnCount() != 0) throw new InvalidOperationException("Expected deployment preparation.");
            UnitCards = managers.GetCardManager()!.GetHand().Select(FullBattleTrace.Active!.CardId).ToArray();
            if (UnitCards.Length != 2) throw new InvalidOperationException("Missing controlled propagation units.");
            Prepared = true;
            // Execute the unchanged native Test once with empty combat floors; it must still return true.
            var owner = managers.GetSaveManager().GetCollectedRelics().Single(item => item.GetRelicDataID() == RelicStartTurnPropagationModel.DataId);
            var effect = owner.GetEffects().OfType<RelicEffectAddEffectStartOfTurn>().Single();
            emptyControl = true;
            try { effect.TestEffectTurnPhaseTiming(new RelicEffectParams { relicManager = managers.GetRelicManager() }, managers.GetCoreManagers()); }
            finally { emptyControl = false; }
            FullBattleTrace.Active!.InvalidateCardRules();
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
        }
        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (step >= 2) return null;
            PlayCardAction? action = BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == UnitCards[step] && play.RoomIndex == step);
            if (action != null) { step++; actionMark = true; } return action;
        }
        internal static bool IsScenarioAction(PlayCardAction action)
        { if (!actionMark) return false; actionMark = false; return true; }
        private static bool Active(RelicEffectAddEffectStartOfTurn effect) => Prepared && FullBattleTrace.Active != null &&
            effect.SourceRelicState?.GetRelicDataID() == RelicStartTurnPropagationModel.DataId && !AllGameManagers.Instance!.GetSaveManager().PreviewMode;
        [HarmonyPatch(typeof(RelicEffectAddEffectStartOfTurn), nameof(RelicEffectAddEffectStartOfTurn.TestEffectTurnPhaseTiming))]
        private static class TestPatch
        {
            private static void Prefix(RelicEffectAddEffectStartOfTurn __instance, out TestRecord? __state)
            {
                __state = null; if (!Active(__instance)) return;
                var row = new TestRecord { EmptyControl = emptyControl }; __state = row; Tests.Add(row);
                try { row.Before = FullBattleTrace.Active!.CaptureTrain(); } catch (Exception error) { row.Error = error.ToString(); }
            }
            private static void Postfix(bool __result, TestRecord? __state)
            {
                if (__state == null) return;
                try { __state.Result = __result; __state.Actual = FullBattleTrace.Active!.CaptureTrain();
                    if (!__result || !JToken.DeepEquals(JToken.FromObject(__state.Before), JToken.FromObject(__state.Actual))) __state.Difference = "Propagation Test changed state/RNG or rejected its non-strict action."; }
                catch (Exception error) { __state.Error = error.ToString(); }
            }
        }
        [HarmonyPatch(typeof(RelicEffectAddEffectStartOfTurn), nameof(RelicEffectAddEffectStartOfTurn.ApplyEffectTurnPhaseTiming))]
        private static class ApplyPatch
        {
            private static void Postfix(RelicEffectAddEffectStartOfTurn __instance, ref IEnumerator __result)
            { if (Active(__instance)) __result = Observe(__result); }
        }
        private static IEnumerator Observe(IEnumerator native)
        {
            var row = new ApplicationRecord(); Applications.Add(row);
            try { FullBattleTrace trace = FullBattleTrace.Active!; row.Before = trace.CaptureTrain(); row.Rules = trace.CapturePlayRules(trace.CaptureCanonicalSpawn());
                row.Turn = AllGameManagers.Instance!.GetCombatManager()!.GetTurnCount();
                var resource = row.Before.Context!.Relics!.Single(owner => owner.DataId == RelicStartTurnPropagationModel.DataId).ResourceEffects!.Single();
                var predicted = RelicStartTurnPropagationModel.Apply(row.Before, row.Rules.Rooms, resource);
                row.Predicted = predicted.State; row.UnsupportedReason = predicted.UnsupportedReason; }
            catch (Exception error) { row.Error = error.ToString(); }
            try { while (native.MoveNext()) yield return native.Current; row.Completed = true; }
            finally { (native as IDisposable)?.Dispose();
                try { row.Actual = FullBattleTrace.Active!.CaptureTrain();
                    if (row.Predicted != null && !JToken.DeepEquals(JToken.FromObject(row.Predicted), JToken.FromObject(row.Actual))) row.Difference = "Start-turn propagation complete state/RNG differs."; }
                catch (Exception error) { row.Error = error.ToString(); } }
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field)!.SetValue(target, value);
    }
}
