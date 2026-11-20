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
    internal static class RelentlessDrawMechanismBatch
    {
        private static bool active;
        private static Dictionary<int, bool>? floors;
        private static CharacterState? actor;
        private static int queries, signals;
        internal static readonly List<object> Tests = new List<object>();
        internal static void Capture(FullBattleTrace trace, AllGameManagers managers)
        {
            if (!OccupiedFloorDrawScenario.RelentlessVariant) return;
            var save = managers.GetSaveManager(); var combat = managers.GetCombatManager();
            var queue = (ICollection)AccessTools.Property(typeof(CombatManager), "TriggerQueue").GetValue(combat);
            if (save.PreviewMode || combat.IsRunningTriggerQueue || queue.Count != 0 || typeof(CardState).Assembly.ManifestModule.ModuleVersionId.ToString() != "bf93f060-beda-46b1-b808-95ebf1e9a99d")
                throw new InvalidOperationException("Relentless draw batch requires settled pinned state.");
            var sources = SpawnStatusScalingMechanismBatch.Owners(save.GetAllGameData()).SelectMany(owner => owner.GetEffects().Select((data, index) => (Owner: owner, Data: data, Index: index)))
                .Where(source => source.Data.GetEffectClassName() == RelicRelentlessDrawModel.Effect).ToArray();
            if (sources.Length != 2) throw new InvalidOperationException("Original Relentless draw denominator changed.");
            var definitions = sources.Select(source => (Id: source.Owner.GetID(), source.Owner, source.Data)).ToList();
            foreach (int amount in new[] { -3, 0, 1, 3, 16777217, 16777219, int.MinValue, int.MaxValue })
            {
                var data = new RelicEffectData(); SpawnStatusScalingMechanismBatch.Set(data, "paramInt", amount);
                SpawnStatusScalingMechanismBatch.Set(data, "effectConditions", new List<RelicEffectCondition>());
                definitions.Add(("control:" + definitions.Count, sources[0].Owner, data));
            }
            var rooms = Enumerable.Range(0, managers.GetRoomManager().GetNumRooms()).Select(index => managers.GetRoomManager().GetRoom(index)).ToArray();
            if (rooms.Length != 4 || !rooms[3].GetIsPyreRoom()) throw new InvalidOperationException("Relentless draw matrix requires pinned room roster.");
            var characters = new List<CharacterState>(); foreach (var room in rooms) room.AddCharactersToList(characters, Team.Type.Heroes | Team.Type.Monsters);
            actor = characters.First();
            object Describe() => sources.Select(source => new { Id = source.Owner.GetID(), Key = source.Owner.name, source.Index,
                Amount = source.Data.GetParamInt(), Types = source.Owner.GetEffects().Select(data => data.GetEffectClassName()).ToArray() }).ToArray();
            var before = JToken.FromObject(trace.CaptureDecision()); var original = JToken.FromObject(Describe()); var rng = RngStates(); int frame = Time.frameCount;
            var rules = new List<object>(); var samples = new List<object>(); var timer = Stopwatch.StartNew();
            try
            {
                foreach (var definition in definitions)
                {
                    var effect = new RelicEffectAdditionalDrawDuringRelentless(); effect.Initialize(null!, definition.Owner, definition.Data);
                    rules.Add(new { definition.Id, Rule = RelicResourceProbe.Definition(effect, 0, managers) });
                    var mutable = new MutableRules(); mutable.AddMutation(effect);
                    int? Draw(out string? error)
                    { try { error = null; return mutable.GetRule(MutableRules.Condition.ChangeStartingHandSize, 5, 1, 10); } catch (OverflowException) { error = "OverflowException"; return null; } }
                    foreach (bool stale in new[] { false, true })
                    foreach (int mask in Enumerable.Range(0, 16))
                    foreach (string callback in new[] { "StartOfBattle", "BeforeDraw" })
                    {
                        float oldValue = stale ? definition.Data.GetParamInt() : 0;
                        AccessTools.Property(effect.GetType(), "Value").SetValue(effect, oldValue);
                        int? queryBefore = Draw(out string? errorBefore);
                        floors = rooms.Select((room, index) => (Id: room.GetInstanceID(), Has: (mask & (1 << index)) != 0)).ToDictionary(value => value.Id, value => value.Has);
                        queries = 0; signals = 0; active = true;
                        bool returned = callback == "StartOfBattle" ? effect.TestEffectStartOfBattle(new RelicEffectParams(), managers.GetCoreManagers()) : effect.TestEffectTurnPhaseTiming(new RelicEffectParams(), managers.GetCoreManagers());
                        active = false; int? queryAfter = Draw(out string? errorAfter);
                        samples.Add(new { definition.Id, Stale = oldValue, Mask = mask, Callback = callback, Returned = returned, Value = effect.Value,
                            Queries = queries, Signals = signals, QueryBefore = queryBefore, QueryAfter = queryAfter, ErrorBefore = errorBefore, ErrorAfter = errorAfter });
                    }
                }
            }
            finally { active = false; actor = null; floors = null; }
            timer.Stop(); var after = JToken.FromObject(trace.CaptureDecision()); var originalAfter = JToken.FromObject(Describe()); var rngAfter = RngStates();
            if (!JToken.DeepEquals(before, after) || !JToken.DeepEquals(original, originalAfter) || !JToken.DeepEquals(rng, rngAfter) || frame != Time.frameCount || queue.Count != 0)
                throw new InvalidOperationException("Relentless draw batch changed source state.");
            using (var document = NativeFixtureCapture.Capture(new { Schema = 1, Scope = "DefinitionDrivenRelentlessDraw", GameVersion = Application.version,
                GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId, Before = before, After = after, Original = original, OriginalAfter = originalAfter,
                RngBefore = rng, RngAfter = rngAfter, FrameBefore = frame, FrameAfter = Time.frameCount, QueueBefore = 0, QueueAfter = queue.Count,
                Seconds = timer.Elapsed.TotalSeconds, Definitions = rules, Samples = samples }))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "relentless-draw-batch.mt2f"))) document.Write(output);
        }
        [HarmonyPatch(typeof(RoomState), nameof(RoomState.GetCharactersWithStatus))]
        private static class FloorControl
        {
            private static bool Prefix(RoomState __instance, Team.Type onTeam, string statusId, ref List<CharacterState> charactersWithStatus)
            {
                if (!active || floors == null || !floors.TryGetValue(__instance.GetInstanceID(), out bool present)) return true;
                if (onTeam != (Team.Type.Heroes | Team.Type.Monsters) || statusId != "relentless") throw new InvalidOperationException("Relentless draw status query changed.");
                queries++; charactersWithStatus = present ? new List<CharacterState> { actor! } : new List<CharacterState>(); return false;
            }
        }
        [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.DispatchCardDrawNextTurnSignal))]
        private static class Signals { private static bool Prefix() { if (!active) return true; signals++; return false; } }
        [HarmonyPatch]
        private static class ActualTests
        {
            private static IEnumerable<MethodBase> TargetMethods()
            { foreach (string name in new[] { "TestEffectStartOfBattle", "TestEffectTurnPhaseTiming" }) yield return AccessTools.Method(typeof(RelicEffectAdditionalDrawDuringRelentless), name); }
            private static void Prefix(RelicEffectAdditionalDrawDuringRelentless __instance, out float __state) => __state = __instance.Value;
            private static void Postfix(RelicEffectAdditionalDrawDuringRelentless __instance, float __state, bool __result, MethodBase __originalMethod)
            {
                if (!active && OccupiedFloorDrawScenario.RelentlessVariant && OccupiedFloorDrawScenario.Prepared && !AllGameManagers.Instance!.GetSaveManager().PreviewMode)
                    Tests.Add(new { Kind = __originalMethod.Name, Turn = AllGameManagers.Instance.GetCombatManager().GetTurnCount(), Before = __state, After = __instance.Value, Returned = __result });
            }
        }
        private static JObject RngStates() => JObject.FromObject(Enum.GetValues(typeof(RngId)).Cast<RngId>().Where(id => id != RngId.NonDeterministic && id != RngId.Chatter)
            .ToDictionary(id => id.ToString(), id => RngCalibration.Words(RandomManager.GetState(id))));
    }
}
#endif
