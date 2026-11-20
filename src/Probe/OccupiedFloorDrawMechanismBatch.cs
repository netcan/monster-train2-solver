#if DEVELOPMENT_PROBE
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class OccupiedFloorDrawMechanismBatch
    {
        internal static bool Active { get; private set; }
        private static CombatManager.Phase phase;
        private static Team.Type team;
        private static CharacterState? actor;
        private static Dictionary<int, bool>? floors;
        private static int signals, queries;
        internal static void Capture(FullBattleTrace trace, AllGameManagers managers)
        {
            if (!OccupiedFloorDrawScenario.Enabled) return;
            var combat = managers.GetCombatManager(); var save = managers.GetSaveManager();
            var queue = (ICollection)AccessTools.Property(typeof(CombatManager), "TriggerQueue").GetValue(combat);
            if (save.PreviewMode || combat.IsRunningTriggerQueue || queue.Count != 0 || typeof(CardState).Assembly.ManifestModule.ModuleVersionId.ToString() != "bf93f060-beda-46b1-b808-95ebf1e9a99d")
                throw new InvalidOperationException("Occupied draw batch requires settled pinned native state.");
            var sources = SpawnStatusScalingMechanismBatch.Owners(save.GetAllGameData())
                .SelectMany(owner => owner.GetEffects().Select((data, index) => (Owner: owner, Data: data, Index: index)))
                .Where(source => source.Data.GetEffectClassName() == RelicOccupiedFloorDrawModel.Effect).ToArray();
            if (sources.Length != 3) throw new InvalidOperationException("Original occupied draw denominator changed.");
            var rooms = Enumerable.Range(0, managers.GetRoomManager().GetNumRooms()).Select(index => managers.GetRoomManager().GetRoom(index)).ToArray();
            var ordinary = rooms.Where(room => !room.GetIsPyreRoom()).ToArray();
            if (ordinary.Length != 3) throw new InvalidOperationException("Occupied draw matrix requires three ordinary floors.");
            var actors = new List<CharacterState>(); foreach (var room in rooms) room.AddCharactersToList(actors, Team.Type.Heroes | Team.Type.Monsters);
            actor = actors.First();
            object Describe() => sources.Select(source => new { Id = source.Owner.GetID(), Key = source.Owner.name, source.Index,
                Effects = source.Owner.GetEffects().Select(data => new { Type = data.GetEffectClassName(), Int = data.GetParamInt(), Int2 = data.GetParamInt2(), Min = data.GetParamMinInt() }).ToArray() }).ToArray();
            var before = JToken.FromObject(trace.CaptureDecision()); var original = JToken.FromObject(Describe());
            var rng = RngStates(); int frame = Time.frameCount;
            var definitions = new List<object>(); var samples = new List<object>(); var timer = Stopwatch.StartNew();
            try
            {
                foreach (var source in sources)
                {
                    var effect = new RelicEffectAdditionalDrawForEnemyOccupiedFloors(); effect.Initialize(null!, source.Owner, source.Data);
                    definitions.Add(new { Id = source.Owner.GetID(), Rule = RelicResourceProbe.Definition(effect, 0, managers) });
                    foreach (int stale in new[] { 0, 3 })
                    foreach (int mask in Enumerable.Range(0, 8))
                    foreach (string tick in new[] { "MonsterTurn", "PreCombat", "Combat", "HeroTurn", "EndMonsterTurn", "MonsterTurnQueueClear" })
                    foreach (Team.Type actorTeam in new[] { Team.Type.Monsters, Team.Type.Heroes })
                    foreach (string callback in new[] { "EndTurn", "AfterDraw", "Birth", "Killed" })
                    {
                        AccessTools.Property(effect.GetType(), "Value").SetValue(effect, (float)stale);
                        phase = (CombatManager.Phase)Enum.Parse(typeof(CombatManager.Phase), tick); team = actorTeam;
                        floors = ordinary.Select((room, index) => (Id: room.GetInstanceID(), Present: (mask & (1 << index)) != 0)).ToDictionary(value => value.Id, value => value.Present);
                        signals = 0; queries = 0; Active = true;
                        bool? test = callback == "EndTurn" ? effect.TestEffectEndOfTurn(new EndOfTurnRelicEffectParams(new RelicEffectParams()), managers.GetCoreManagers()) :
                            callback == "AfterDraw" ? effect.TestEffectTurnPhaseTiming(new RelicEffectParams(), managers.GetCoreManagers()) : (bool?)null;
                        if (callback == "Birth") Drain(effect.OnCharacterAdded(actor, null!, managers.GetCoreManagers()));
                        if (callback == "Killed") Drain(effect.OnCharacterKilled(actor, managers.GetCoreManagers()));
                        Active = false;
                        samples.Add(new { Id = source.Owner.GetID(), Stale = stale, Mask = mask, Phase = tick, Team = actorTeam.ToString(), Callback = callback,
                            Returned = test, Value = effect.Value, Signals = signals, Queries = queries });
                    }
                }
            }
            finally { Active = false; actor = null; floors = null; }
            timer.Stop(); var after = JToken.FromObject(trace.CaptureDecision()); var originalAfter = JToken.FromObject(Describe()); var rngAfter = RngStates();
            if (!JToken.DeepEquals(before, after) || !JToken.DeepEquals(original, originalAfter) || !JToken.DeepEquals(rng, rngAfter) || frame != Time.frameCount || queue.Count != 0)
                throw new InvalidOperationException("Occupied draw batch changed its source state.");
            using (var document = NativeFixtureCapture.Capture(new { Schema = 1, Scope = "DefinitionDrivenOccupiedFloorDraw", GameVersion = Application.version,
                GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId, Before = before, After = after, Original = original, OriginalAfter = originalAfter,
                RngBefore = rng, RngAfter = rngAfter, FrameBefore = frame, FrameAfter = Time.frameCount, QueueBefore = 0, QueueAfter = queue.Count,
                Seconds = timer.Elapsed.TotalSeconds, Definitions = definitions, Samples = samples }))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "occupied-floor-draw-batch.mt2f"))) document.Write(output);
        }
        private static void Drain(IEnumerator callback) { while (callback.MoveNext()) if (callback.Current is IEnumerator child) Drain(child); }
        [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.GetCombatPhase))]
        private static class PhaseControl { private static bool Prefix(ref CombatManager.Phase __result) { if (!Active) return true; __result = phase; return false; } }
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.GetTeamType))]
        private static class TeamControl { private static bool Prefix(CharacterState __instance, ref Team.Type __result) { if (!Active || !ReferenceEquals(__instance, actor)) return true; __result = team; return false; } }
        [HarmonyPatch(typeof(RoomState), nameof(RoomState.HasLivingCharacters))]
        private static class FloorControl
        {
            private static bool Prefix(RoomState __instance, Team.Type team, bool includeOuterTrainBoss, bool excludeCharactersThatWillDespawn, ref bool __result)
            {
                if (!Active || floors == null || !floors.TryGetValue(__instance.GetInstanceID(), out bool living)) return true;
                if (team != Team.Type.Heroes || !includeOuterTrainBoss || !excludeCharactersThatWillDespawn) throw new InvalidOperationException("Occupied draw native living query changed.");
                queries++; __result = living; return false;
            }
        }
        [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.DispatchCardDrawNextTurnSignal))]
        private static class SignalControl { private static bool Prefix() { if (!Active) return true; signals++; return false; } }
        private static JObject RngStates() => JObject.FromObject(Enum.GetValues(typeof(RngId)).Cast<RngId>().Where(id => id != RngId.NonDeterministic && id != RngId.Chatter)
            .ToDictionary(id => id.ToString(), id => RngCalibration.Words(RandomManager.GetState(id))));
    }
}
#endif
