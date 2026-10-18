using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalOpeningMorselsScenario
    {
        internal const string Name = "original-opening-morsels";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        private static bool observing, control;
        private static ApplicationRecord? current;
        internal static readonly List<TestRecord> Tests = new List<TestRecord>();
        internal static readonly List<ApplicationRecord> Applications = new List<ApplicationRecord>();
        internal static readonly List<FoodRecord> Foods = new List<FoodRecord>();
        internal sealed class FoodRecord
        {
            public int RoomIndex { get; set; }
            public CombatTeam Team { get; set; }
            public int[] FoodIds { get; set; } = Array.Empty<int>();
            public int FoodStatBefore { get; set; }
            public int FoodStatAfter { get; set; }
            public int ExpectedEaten { get; set; }
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? Predicted { get; set; }
            public TrainCombatState? Actual { get; set; }
            public bool Completed { get; set; }
            public string? Error { get; set; }
            public string? Difference { get; set; }
            public string? UnsupportedReason { get; set; }
        }
        internal sealed class TestRecord
        {
            public int RoomIndex { get; set; }
            public int RoomCount { get; set; }
            public bool Control { get; set; }
            public bool Expected { get; set; }
            public bool Returned { get; set; }
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? Actual { get; set; }
            public string? Error { get; set; }
            public string? Difference { get; set; }
        }
        internal sealed class ApplicationRecord
        {
            public int EffectIndex { get; set; }
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? Actual { get; set; }
            public TrainCombatState? Predicted { get; set; }
            public bool Completed { get; set; }
            public int Notifications { get; set; }
            public string? Error { get; set; }
            public string? Difference { get; set; }
            public string? UnsupportedReason { get; set; }
        }
        private static bool Active => Acquired && FullBattleTrace.Active != null && !observing && !AllGameManagers.Instance!.GetSaveManager().PreviewMode;
        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData data = save.GetAllGameData();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Expected fresh pre-battle Morsel acquisition.");
            CardData card = save.GetDeckState().Select(item => data.FindCardData(item.GetCardDataID()))
                .First(item => item.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true);
            CharacterData steward = card.GetSpawnCharacterData();
            Set(steward, "health", 100); Set(steward, "attackDamage", 50); Set(steward, "size", 1);
            Set(steward, "triggers", new List<CharacterTriggerData>()); Set(steward, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            Set(steward, "unitAbility", null!); Set(card, "cost", 0); Set(card, "traits", new List<CardTraitData>());
            save.GetDeckState().Clear(); save.AddCardToDeck(card, applyExistingRelicModifiers: false, showAnimation: false, notifyDeckCount: false);
            CollectableRelicData owner = data.GetAllCollectableRelicData().Single(item => item.GetID() == RelicOpeningSpawnModel.DataId);
            save.AddRelic(owner); Acquired = true;
            CombatRelicState captured = RelicProbe.Capture(managers).Single(item => item.DataId == RelicOpeningSpawnModel.DataId);
            string? error = RelicModel.Validate(new[] { captured }); if (error != null) throw new InvalidOperationException(error);
            using (var archive = NativeFixtureCapture.Capture(new { Schema = 1, Relic = captured }))
            using (var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "opening-morsel-definition.mt2f"))) archive.Write(stream);
            log.LogInfo("ORIGINAL-OPENING-MORSELS-ACQUIRED unchanged owner and original Miner; native floors 2,1,0, no source card.");
        }
        internal static void PrepareDeployment(AllGameManagers managers)
        {
            if (Prepared || !Acquired || managers.GetCombatManager()!.GetTurnCount() != 0) throw new InvalidOperationException("Expected Morsel deployment.");
            Prepared = true;
            RelicState owner = managers.GetSaveManager().GetCollectedRelics().Single(item => item.GetRelicDataID() == RelicOpeningSpawnModel.DataId);
            CollectableRelicData definition = managers.GetSaveManager().GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == RelicOpeningSpawnModel.DataId);
            foreach (int floor in new[] { -1, managers.GetRoomManager()!.GetNumRooms() })
            {
                var isolated = new RelicEffectSpawnUnitStartOfCombat();
                isolated.Initialize(owner, definition, definition.GetEffects()[0]);
                Set(isolated, "roomIndex", floor); control = true;
                try { if (isolated.TestEffectStartOfBattle(new RelicEffectParams(), managers.GetCoreManagers())) throw new InvalidOperationException("Out-of-range native spawn Test returned true."); }
                finally { control = false; }
            }
        }
        [HarmonyPatch(typeof(RelicEffectSpawnUnitStartOfCombat), nameof(RelicEffectSpawnUnitStartOfCombat.TestEffectStartOfBattle))]
        private static class TestPatch
        {
            private static void Prefix(RelicEffectSpawnUnitStartOfCombat __instance, out TestRecord? __state)
            {
                __state = null; if (!Active) return; observing = true;
                var row = new TestRecord { RoomIndex = (int)AccessTools.Field(__instance.GetType(), "roomIndex").GetValue(__instance),
                    RoomCount = AllGameManagers.Instance!.GetRoomManager()!.GetNumRooms(), Control = control }; Tests.Add(row); __state = row;
                try { row.Before = FullBattleTrace.Active!.CaptureTrain(); row.Expected = RelicOpeningSpawnModel.Test(row.RoomIndex, row.RoomCount); }
                catch (Exception error) { row.Error = error.ToString(); } finally { observing = false; }
            }
            private static void Postfix(bool __result, TestRecord? __state)
            {
                if (__state == null) return; observing = true;
                try { __state.Returned = __result; __state.Actual = FullBattleTrace.Active!.CaptureTrain();
                    if (__result != __state.Expected || !JToken.DeepEquals(JToken.FromObject(__state.Before), JToken.FromObject(__state.Actual))) __state.Difference = "Opening spawn Test result/state differs."; }
                catch (Exception error) { __state.Error = error.ToString(); } finally { observing = false; }
            }
        }
        [HarmonyPatch(typeof(RelicEffectSpawnUnitStartOfCombat), nameof(RelicEffectSpawnUnitStartOfCombat.ApplyEffectStartOfBattle))]
        private static class ApplyPatch
        { private static void Postfix(RelicEffectSpawnUnitStartOfCombat __instance, ref IEnumerator __result) { if (Active) __result = Observe(__result, __instance); } }
        private static IEnumerator Observe(IEnumerator native, RelicEffectSpawnUnitStartOfCombat effect)
        {
            var row = new ApplicationRecord { EffectIndex = effect.SourceRelicState.GetEffects().IndexOf(effect) }; Applications.Add(row); observing = true;
            try { row.Before = FullBattleTrace.Active!.CaptureTrain(); TrainCombatResult result = RelicOpeningSpawnModel.Apply(row.Before, RelicOpeningSpawnModel.DataId, row.EffectIndex);
                row.Predicted = result.State; row.UnsupportedReason = result.UnsupportedReason; current = row; }
            catch (Exception error) { row.Error = error.ToString(); } finally { observing = false; }
            try { while (native.MoveNext()) yield return native.Current; row.Completed = true; }
            finally
            {
                (native as IDisposable)?.Dispose(); observing = true;
                try { row.Actual = FullBattleTrace.Active!.CaptureTrain(); if (row.Predicted != null && !JToken.DeepEquals(JToken.FromObject(row.Predicted), JToken.FromObject(row.Actual))) row.Difference = "Opening spawn full train differs."; }
                catch (Exception error) { row.Error = error.ToString(); } finally { current = null; observing = false; }
            }
        }
        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.NotifyRelicTriggered))]
        private static class NotifyPatch
        { private static void Prefix(RelicState triggeredRelic) { if (current != null && triggeredRelic.GetRelicDataID() == RelicOpeningSpawnModel.DataId) current.Notifications++; } }
        [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.DoFeederRules))]
        private static class FoodPatch
        {
            private static void Postfix(RoomState room, Team.Type teamType, ref IEnumerator __result)
            { if (Active) __result = ObserveFood(__result, room, teamType); }
        }
        private static IEnumerator ObserveFood(IEnumerator native, RoomState room, Team.Type team)
        {
            var row = new FoodRecord { RoomIndex = room.GetRoomIndex(), Team = team == Team.Type.Monsters ? CombatTeam.Player : CombatTeam.Enemy };
            Foods.Add(row); observing = true;
            try
            {
                row.Before = FullBattleTrace.Active!.CaptureTrain();
                row.FoodStatBefore = AllGameManagers.Instance!.GetSaveManager().GetRunStat(RunStat.StatType.FoodUnitsEaten);
                var living = new List<CharacterState>(); room.AddCharactersToList(living, team);
                row.FoodIds = living.Where(unit => unit.IsAlive && CombatFeederRules.GetIsFoodUnit(unit)).Select(FullBattleTrace.Active.UnitId).ToArray();
                RoomCombatResult result = RoomCombatModel.ApplyFeeding(row.Before.Rooms.Single(item => item.RoomIndex == row.RoomIndex), row.Team);
                row.UnsupportedReason = result.UnsupportedReason;
                row.ExpectedEaten = result.Events.Count(item => item.Kind == "Eaten");
                if (result.Supported) row.Predicted = CardSpellModel.WithContext(new TrainCombatState(row.Before.Rooms.Select(item =>
                    item.RoomIndex == row.RoomIndex ? result.State! : item).ToArray(), row.Before.Movement, row.Before.EnemySlotsPerRoom,
                    result.State!.Context), result.State.Context!);
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
                    row.FoodStatAfter = AllGameManagers.Instance!.GetSaveManager().GetRunStat(RunStat.StatType.FoodUnitsEaten);
                    if (row.Predicted != null && !JToken.DeepEquals(JToken.FromObject(row.Predicted), JToken.FromObject(row.Actual)))
                        row.Difference = "Native feeding full train differs.";
                    if (row.FoodStatAfter - row.FoodStatBefore != (row.Team == CombatTeam.Player ? row.ExpectedEaten : 0))
                        row.Difference = "Native FoodUnitsEaten statistic differs.";
                }
                catch (Exception error) { row.Error = error.ToString(); }
                finally { observing = false; }
            }
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
