#if DEVELOPMENT_PROBE
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace MonsterTrain2Poju.Probe
{
    internal static class TurnEnergyMechanismBatch
    {
        private static bool active;
        private static int? turn;
        private static Dictionary<int, (int Count, int Size)>? floors;
        private static List<object> requests = new List<object>();
        private static int notifications;
        private static void Set(object target, string field, object value) => SpawnStatusScalingMechanismBatch.Set(target, field, value);
        internal static void Capture(FullBattleTrace trace, AllGameManagers managers)
        {
            if (!TurnEnergyScenario.Enabled) return;
            var save = managers.GetSaveManager(); var combat = managers.GetCombatManager();
            var queue = (ICollection)AccessTools.Property(typeof(CombatManager), "TriggerQueue").GetValue(combat);
            if (save.PreviewMode || combat.IsRunningTriggerQueue || queue.Count != 0 || typeof(CardState).Assembly.ManifestModule.ModuleVersionId.ToString() != "bf93f060-beda-46b1-b808-95ebf1e9a99d")
                throw new InvalidOperationException("Turn energy batch requires settled pinned native state.");
            var originals = SpawnStatusScalingMechanismBatch.Owners(save.GetAllGameData())
                .SelectMany(owner => owner.GetEffects().Select((data, index) => (Owner: owner, Data: data, Index: index)))
                .Where(source => RelicTurnEnergyModel.IsEffect(source.Data.GetEffectClassName())).ToArray();
            if (originals.Length != 6) throw new InvalidOperationException("Original dynamic energy denominator changed.");
            var definitions = originals.Select(source => (Id: source.Owner.GetID(), source.Owner, source.Data, Type: source.Data.GetEffectClassName())).ToList();
            foreach (int initial in new[] { -3, 0, 1, 5, int.MinValue, int.MaxValue })
                foreach (int reduction in new[] { -1, 0, 1, int.MaxValue })
                    foreach (bool deployment in new[] { false, true })
                    {
                        var data = new RelicEffectData(); Set(data, "paramInt", initial); Set(data, "paramInt2", reduction); Set(data, "paramBool", deployment);
                        Set(data, "effectConditions", new List<RelicEffectCondition>());
                        definitions.Add(("control:" + definitions.Count, originals[0].Owner, data, RelicTurnEnergyModel.Reducing));
                    }
            foreach (int bonus in new[] { -3, 0, 1, int.MinValue })
                foreach (int threshold in new[] { 0, 3, 20 })
                    foreach (int extra in new[] { -2, 0, 2 })
                    {
                        var data = new RelicEffectData(); Set(data, "paramMinInt", bonus); Set(data, "paramMaxInt", threshold); Set(data, "paramInt", extra);
                        Set(data, "effectConditions", new List<RelicEffectCondition>());
                        definitions.Add(("control:" + definitions.Count, originals[0].Owner, data, RelicTurnEnergyModel.Occupied));
                    }
            IRelicEffect Effect(RelicData owner, RelicEffectData data, string type)
            { IRelicEffect effect = type == RelicTurnEnergyModel.Reducing ? (IRelicEffect)new RelicEffectModifyEnergyReducingEveryTurn() : new RelicEffectModifyEnergyWhenFloorsOccupied(); effect.Initialize(null!, owner, data); return effect; }
            object Describe() => originals.Select(source => new { Id = source.Owner.GetID(), Key = source.Owner.name, source.Index,
                Rule = RelicResourceProbe.Definition(Effect(source.Owner, source.Data, source.Data.GetEffectClassName()), source.Index, managers) }).ToArray();
            var before = JToken.FromObject(trace.CaptureDecision()); var original = JToken.FromObject(Describe()); var rng = RngStates(); int frame = Time.frameCount;
            var rooms = Enumerable.Range(0, managers.GetRoomManager().GetNumRooms()).Select(index => managers.GetRoomManager().GetRoom(index)).Where(room => !room.GetIsPyreRoom()).ToArray();
            if (rooms.Length != 3) throw new InvalidOperationException("Turn energy controls require three ordinary rooms.");
            var parameters = new RelicEffectParams { relicManager = managers.GetRelicManager() };
            var samples = new List<object>(); var rules = new List<object>(); var timer = Stopwatch.StartNew();
            try
            {
                foreach (var definition in definitions)
                {
                    var effect = Effect(definition.Owner, definition.Data, definition.Type);
                    rules.Add(new { definition.Id, Rule = RelicResourceProbe.Definition(effect, 0, managers) });
                    IEnumerable<(int Turn, int Mask, int[] Sizes)> inputs = definition.Type == RelicTurnEnergyModel.Reducing ?
                        new[] { -1, 0, 1, 2, 4, 5, 6, int.MaxValue, int.MinValue }.Select(value => (value, 7, new[] { 1, 1, 1 })) :
                        Enumerable.Range(0, 8).SelectMany(mask => new[] { new[] { 0, 0, 0 }, new[] { 1, 1, 1 }, new[] { 6, 6, 7 },
                            new[] { 7, 7, 6 }, new[] { 7, 7, 7 }, new[] { 0, 9, 20 }, new[] { 10, 10, 10 } }.Select(sizes => (1, mask, sizes)));
                    foreach (var input in inputs)
                    {
                        turn = input.Turn; floors = rooms.Select((room, index) => (Id: room.GetInstanceID(), Count: (input.Mask >> index) & 1, Size: input.Sizes[index]))
                            .ToDictionary(value => value.Id, value => (value.Count, value.Size));
                        active = true; requests = new List<object>(); notifications = 0;
                        int cachedBefore = definition.Type == RelicTurnEnergyModel.Reducing ? (int)AccessTools.Field(effect.GetType(), "_lastCheckedTurnIndex").GetValue(effect) : 0;
                        int queryBefore = ((IModifyEnergyPerTurnRelicEffect)effect).GetEnergyModification(parameters);
                        bool condition = effect.AreConditionsTrue(managers.GetCardStatistics());
                        bool test = condition && ((ITurnPhaseTimingRelicEffect)effect).TestEffectTurnPhaseTiming(parameters, managers.GetCoreManagers());
                        string? error = null;
                        try
                        {
                            if (test)
                            {
                                var callback = ((ITurnPhaseTimingRelicEffect)effect).ApplyEffectTurnPhaseTiming(parameters, managers.GetCoreManagers());
                                if (callback.MoveNext()) throw new InvalidOperationException("Dynamic energy unexpectedly yielded.");
                            }
                        }
                        catch (OverflowException) { error = nameof(OverflowException); }
                        int queryAfter = ((IModifyEnergyPerTurnRelicEffect)effect).GetEnergyModification(parameters);
                        int cachedAfter = definition.Type == RelicTurnEnergyModel.Reducing ? (int)AccessTools.Field(effect.GetType(), "_lastCheckedTurnIndex").GetValue(effect) : 0;
                        active = false;
                        samples.Add(new { Definition = definition.Id, Turn = input.Turn, input.Mask, input.Sizes, CachedBefore = cachedBefore, CachedAfter = cachedAfter,
                            QueryBefore = queryBefore, QueryAfter = queryAfter, Condition = condition, Test = test, Requests = requests.ToArray(), Notifications = notifications, Error = error });
                    }
                }
            }
            finally { active = false; turn = null; floors = null; }
            timer.Stop(); var after = JToken.FromObject(trace.CaptureDecision()); var originalAfter = JToken.FromObject(Describe()); var rngAfter = RngStates();
            if (!JToken.DeepEquals(before, after) || !JToken.DeepEquals(original, originalAfter) || !JToken.DeepEquals(rng, rngAfter) || frame != Time.frameCount || queue.Count != 0)
                throw new InvalidOperationException("Turn energy component batch changed its source state.");
            using (var document = NativeFixtureCapture.Capture(new { Schema = 1, Scope = "DefinitionDrivenTurnEnergy", GameVersion = Application.version,
                GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId, Before = before, After = after, Original = original, OriginalAfter = originalAfter,
                RngBefore = rng, RngAfter = rngAfter, FrameBefore = frame, FrameAfter = Time.frameCount, QueueBefore = 0, QueueAfter = queue.Count,
                Seconds = timer.Elapsed.TotalSeconds, Definitions = rules, Samples = samples }))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "turn-energy-batch.mt2f"))) document.Write(output);
        }
        [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.GetTurnCount))]
        private static class TurnControl { private static bool Prefix(ref int __result) { if (!active || !turn.HasValue) return true; __result = turn.Value; return false; } }
        [HarmonyPatch(typeof(RoomState), nameof(RoomState.GetNumCharacters))]
        private static class CountControl
        { private static bool Prefix(RoomState __instance, Team.Type team, ref int __result) { if (!active || team != Team.Type.Monsters || floors == null || !floors.TryGetValue(__instance.GetInstanceID(), out var input)) return true; __result = input.Count; return false; } }
        [HarmonyPatch(typeof(RoomState), nameof(RoomState.GetCurrentCapacityCount))]
        private static class SizeControl
        { private static bool Prefix(RoomState __instance, Team.Type team, ref int __result) { if (!active || team != Team.Type.Monsters || floors == null || !floors.TryGetValue(__instance.GetInstanceID(), out var input)) return true; __result = input.Size; return false; } }
        [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.AddEnergy))]
        private static class AddRequests
        { private static bool Prefix(int addEnergy, bool suppressSignal) { if (!active) return true; requests.Add(new { Kind = "Add", Amount = addEnergy, Suppress = suppressSignal }); return false; } }
        [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.RemoveEnergy))]
        private static class RemoveRequests
        { private static bool Prefix(int removeEnergy) { if (!active) return true; requests.Add(new { Kind = "Remove", Amount = removeEnergy, Suppress = false }); return false; } }
        [HarmonyPatch]
        private static class NotifyRequests
        {
            private static MethodBase TargetMethod() => typeof(RelicEffectBase).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic).Single(method => method.Name == "NotifyRelicTriggered" && method.GetParameters().Length == 5);
            private static bool Prefix() { if (!active) return true; notifications++; return false; }
        }
        private static JObject RngStates() => JObject.FromObject(Enum.GetValues(typeof(RngId)).Cast<RngId>().Where(id => id != RngId.NonDeterministic && id != RngId.Chatter)
            .ToDictionary(id => id.ToString(), id => RngCalibration.Words(RandomManager.GetState(id))));
    }
}
#endif
